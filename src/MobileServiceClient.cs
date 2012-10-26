//    Copyright 2012 Ken Egozi
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Phone.Controls;
using System.Threading;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows;
using MobileService.Sdk.WP7;

namespace MobileServices.Sdk {
            
  	public sealed partial class MobileServiceClient {
		internal static readonly JsonSerializer Serializer;

        //session info
		readonly string applicationKey;
		readonly string serviceUrl;

        //Login info
		public string CurrentAuthToken { get; private set; }
		public MobileServiceUser CurrentUser { get; private set; }
        WebAuthenticationBrokerStruct Broker;
        Action<MobileServiceUser, Exception> successContinueWith;

        // *** Constructor ***
        public MobileServiceClient(string serviceUrl, string applicationKey)
        {
            this.serviceUrl = serviceUrl;
            this.applicationKey = applicationKey;
        }

		// *** Static Constructor (for the JSON Serializer) ***
		static MobileServiceClient() {
			Serializer = new JsonSerializer();
			Serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
			Serializer.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
			Serializer.DateFormatHandling = DateFormatHandling.IsoDateFormat;//.DateTimeZoneHandling = DateTimeZoneHandling.Utc
		}

	
        /// <summary>
        /// Log a user into a Mobile Services application given an access
        /// token.
        /// </summary>
        /// <param name="authenticationToken">
        /// OAuth access token that authenticates the user.
        /// </param>
        /// <returns>
        /// Task that will complete when the user has finished authentication.
        /// </returns>
        public void LoginInBackground(string authenticationToken,  Action<MobileServiceUser, Exception> continueWith)
        {
            // Proper Async Tasks Programming cannot integrate with Windows Phone (stupid) Async Mechanisim which use Events... (ex: UploadStringCompleted)
            //var asyncTask = new Task<MobileServiceUser>(() => this.StartLoginAsync(authenticationToken));
            //asyncTask.Start();
            //return asyncTask;

            if (authenticationToken == null)
            {
                throw new ArgumentNullException("authenticationToken");
            }
            else if (string.IsNullOrEmpty(authenticationToken))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Error! Empty Argument: {0}",
                        "authenticationToken"));
            }

            
            var client = new WebClient();
			//URL
            var url = serviceUrl + LoginAsyncUriFragment + "?mode=" + LoginAsyncAuthenticationTokenKey;
            
            //Request Details
            client.Headers[HttpRequestHeader.ContentType] = RequestJsonContentType;
            var payload = new JObject();
            payload[LoginAsyncAuthenticationTokenKey] = authenticationToken;

            //Do with Response
            client.UploadStringCompleted += (x, args) => {
				if (args.Error != null) {
					var ex = args.Error;
					if (args.Error.InnerException != null)
						ex = args.Error.InnerException;
					continueWith(null, ex);
					return;
				}
				var result = JObject.Parse(args.Result);
				CurrentAuthToken = result["authenticationToken"].Value<string>();
				CurrentUser = new MobileServiceUser(result["user"]["userId"].Value<string>());
				continueWith(CurrentUser, null);
			};
            
            //Go!
			client.UploadStringAsync(new Uri(url), payload.ToString());
		}
        


        /// <summary>
        /// Log a user into a Mobile Services application given a provider name and optional token object.
        /// </summary>
        /// <param name="provider" type="MobileServiceAuthenticationProvider">
        /// Authentication provider to use.
        /// </param>
        /// <param name="token" type="JObject">
        /// Optional, provider specific object with existing OAuth token to log in with.
        /// </param>
        /// <returns>
        /// Task that will complete when the user has finished authentication.
        /// </returns>
        public void LoginWithBrowser(MobileServiceAuthenticationProvider provider, WebAuthenticationBrokerStruct Broker, Action<MobileServiceUser, Exception> continueWith)
        {
            // Proper Async Tasks Programming cannot integrate with Windows Phone (stupid) Async Mechanisim which use Events... (ex: UploadStringCompleted)
             //var asyncTask =  new Task<MobileServiceUser>(() => this.StartLoginAsync(provider, authorizationBrowser));
             //asyncTask.Start();
             //return asyncTask;
            this.Broker = Broker;
            successContinueWith += continueWith;

            if (this.LoginInProgress)
            {
                throw new InvalidOperationException("Error, Login is still in progress..");
            }
            if (!Enum.IsDefined(typeof(MobileServiceAuthenticationProvider), provider))
            {
                throw new ArgumentOutOfRangeException("provider");
            }

            string providerName = provider.ToString().ToLower();
            this.LoginInProgress = true;
            
            try
            {
                //Launch the OAuth flow.

                Broker.dispacher.BeginInvoke(()=>{Broker.loadingGrid.Visibility = Visibility.Visible;}); 
                Broker.authorizationBrowser.Navigating += this.OnAuthorizationBrowserNavigating;
                Broker.authorizationBrowser.Navigated += this.OnAuthorizationBrowserNavigated;
                Broker.authorizationBrowser.Navigate(new Uri(this.serviceUrl + LoginAsyncUriFragment + "/" + providerName));

              
            }
            catch (Exception ex)
            {
                //on Error
                CompleteOAuthFlow(false, ex.Message);
            }
        }

        /// <summary>
        /// Handles the navigating event of the OAuth web browser control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnAuthorizationBrowserNavigated(object sender, NavigationEventArgs e)
        {
            Broker.authorizationBrowser.Navigated -= this.OnAuthorizationBrowserNavigated;
            Broker.dispacher.BeginInvoke(()=>{
                Broker.loadingGrid.Visibility = Visibility.Collapsed;
                Broker.authorizationBrowser.Visibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// Handles the navigating event of the OAuth web browser control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnAuthorizationBrowserNavigating(object sender, NavigatingEventArgs e)
        {
            Uri uri = e.Uri;

            if (uri != null && uri.AbsoluteUri.StartsWith(this.serviceUrl+ LoginAsyncDoneUriFragment))
            {
                Dictionary<string, string> fragments = this.ProcessFragments(uri.Fragment);

                string tokenString;
                bool success = fragments.TryGetValue("token", out tokenString);
                var tokenJSON = JObject.Parse(tokenString);
                
                if(success && tokenJSON !=null)
                {
                    e.Cancel = true;
                    
                    //Save user info
                    this.CurrentAuthToken = tokenJSON[LoginAsyncAuthenticationTokenKey].Value<string>();
                    this.CurrentUser = new MobileServiceUser(tokenJSON["user"]["userId"].Value<string>());
                    //Done with succses
                    CompleteOAuthFlow(success);
                }
                else           
                    CompleteOAuthFlow(false);

            }

            //TODO: check if MobileServices return Error ==> StartsWith(this.serviceUrl+ LoginAsyncUriFragment) && Contains("error");
        }

        /// <summary>
        /// Complete the OAuth flow.
        /// </summary>
        /// <param name="success">Whether the operation was successful.</param>
        private void CompleteOAuthFlow(bool success, string errorMsg = null)
        {
            this.LoginInProgress = false;
            Broker.authorizationBrowser.Navigated -= this.OnAuthorizationBrowserNavigated;
            Broker.authorizationBrowser.Navigating -= this.OnAuthorizationBrowserNavigating;
           
            //Hide Broker UI
            Broker.dispacher.BeginInvoke(()=>{
                Broker.authorizationBrowser.NavigateToString(String.Empty);
                Broker.authorizationBrowser.Visibility = Visibility.Collapsed;
                Broker.loadingGrid.Visibility = Visibility.Collapsed;
            });

            // Invoke ContinueWith Method
            if (successContinueWith != null)
                successContinueWith(this.CurrentUser, success? null : new InvalidOperationException(errorMsg));
            
        }

        /// <summary>
        /// Process the URI fragment string.
        /// </summary>
        /// <param name="fragment">The URI fragment.</param>
        /// <returns>The key-value pairs.</returns>
        private Dictionary<string, string> ProcessFragments(string fragment)
        {
            Dictionary<string, string> processedFragments = new Dictionary<string, string>();

            if (fragment[0] == '#')
            {
                fragment = fragment.Substring(1);
            }

            string[] fragmentParams = fragment.Split('&');

            foreach (string fragmentParam in fragmentParams)
            {
                string[] keyValue = fragmentParam.Split('=');

                if (keyValue.Length == 2)
                {
                    processedFragments.Add(keyValue[0], HttpUtility.UrlDecode(keyValue[1]));
                }
            }

            return processedFragments;
        }

	    public void Logout() {
			CurrentUser = null;
			CurrentAuthToken = null;
		}



        //### Still  Tables??###


		public void Get(string relativeUrl, Action<string, Exception> continuation) {
			Execute("GET", relativeUrl, string.Empty, continuation);
		}

		public void Post(string relativeUrl, object payload, Action<string, Exception> continuation) {
			Execute("POST", relativeUrl, payload, continuation);
		}

		public void Delete(string relativeUrl, Action<Exception> continuation) {
			Execute("DELETE", relativeUrl, string.Empty, (s, err) => continuation(err));
		}

		public void Patch(string relativeUrl, object payload, Action<string, Exception> continuation) {
			Execute("PATCH", relativeUrl, payload, continuation);
		}

		void Execute(string method, string relativeUrl, object payload, Action<string, Exception> continuation) {
			var endpointUrl = serviceUrl + relativeUrl;
			var client = new WebClient();
			client.UploadStringCompleted += (x, args) =>
				OperationCompleted(args, continuation);
			client.DownloadStringCompleted += (x, args) =>
				OperationCompleted(args, continuation);
			SetMobileServiceHeaders(client);
			if (method == "GET") {
				client.DownloadStringAsync(new Uri(endpointUrl));
				return;
			}

			var payloadString = payload as string;
			if (payloadString == null && payload != null) {
				var buffer = new StringBuilder();
				using (var writer = new StringWriter(buffer))
					Serializer.Serialize(writer, payload);
				payloadString = buffer.ToString();
			}
			client.UploadStringAsync(new Uri(endpointUrl), method, payloadString);
		}

		void OperationCompleted(AsyncCompletedEventArgs args, Action<string, Exception> continuation) {
			if (args.Error != null) {
				var ex = args.Error;
				var webException = ex as WebException;
				if (webException != null) {
					var response = webException.Response as HttpWebResponse;
					if (response != null) {
						var code = response.StatusCode;
						var msg = response.StatusDescription;
						try {
							using (var reader = new StreamReader(response.GetResponseStream())) {
								msg += "\r\n" + reader.ReadToEnd();
							}
						}
						catch (Exception) {
							msg += "\r\nResponse body could not be extracted";
						}
						ex = new Exception(string.Format("Http error [{0}] - {1}", (int)code, msg), ex);
					}
				}
				continuation(null, ex);
				return;
			}
			string result = null;
			var uploadStringCompletedEventArgs = args as UploadStringCompletedEventArgs;
			if (uploadStringCompletedEventArgs != null)
				result = uploadStringCompletedEventArgs.Result;
			var downloadStringCompletedEventArgs = args as DownloadStringCompletedEventArgs;
			if (downloadStringCompletedEventArgs != null)
				result = downloadStringCompletedEventArgs.Result;
			if (result == null) {
				throw new InvalidOperationException("args should be either UploadStringCompletedEventArgs or DownloadStringCompletedEventArgs");
			}
			continuation(result, null);
		}

		void SetMobileServiceHeaders(WebClient client) {
			if (CurrentAuthToken != null) {
				client.Headers["X-ZUMO-AUTH"] = CurrentAuthToken;
			}
			if (applicationKey != null) {
				client.Headers["X-ZUMO-APPLICATION"] = applicationKey;
			}
		}

		public MobileServiceTable GetTable(string tableName) {
			return new MobileServiceTable(this, tableName);
		}

		public MobileServiceTable<TItem> GetTable<TItem>(string tableName) {
			return new MobileServiceTable<TItem>(this, tableName);
		}

		public MobileServiceTable<TItem> GetTable<TItem>() {
			var tableName = typeof(TItem).Name;
			return GetTable<TItem>(tableName);
		}
	}

	public class MobileServiceQuery {
		private int top;
		private int skip;
		private string orderby;
		private string filter;
		private string select;

		public MobileServiceQuery Top(int top) {
			this.top = top;
			return this;
		}

		public MobileServiceQuery Skip(int skip) {
			this.skip = skip;
			return this;
		}

		public MobileServiceQuery OrderBy(string orderby) {
			this.orderby = orderby;
			return this;
		}

		public MobileServiceQuery Filter(string filter) {
			this.filter = filter;
			return this;
		}

		public MobileServiceQuery Select(string select) {
			this.select = select;
			return this;
		}

		public override string ToString() {
			var query = new List<string>();
			if (top != 0) {
				query.Add("$top=" + top);
			}
			if (skip != 0) {
				query.Add("$skip=" + skip);
			}
			if (!string.IsNullOrEmpty(filter)) {
				query.Add("$filter=" + filter);
			}
			if (!string.IsNullOrEmpty(select)) {
				query.Add("$select=" + select);
			}
			if (!string.IsNullOrEmpty(orderby)) {
				query.Add("$orderby=" + orderby);
			}

			return string.Join("&", query);
		}
	}
}




/*

	public void Login(string liveAuthenticationToken, Action<string, Exception> continuation) {
			var client = new WebClient();
			var url = serviceUrl + "login?mode=authenticationToken";
			client.UploadStringCompleted += (x, args) => {
				if (args.Error != null) {
					var ex = args.Error;
					if (args.Error.InnerException != null)
						ex = args.Error.InnerException;
					continuation(null, ex);
					return;
				}
				var result = JObject.Parse(args.Result);
				CurrentAuthToken = result["authenticationToken"].Value<string>();
				CurrentUserId = result["user"]["userId"].Value<string>();
				continuation(CurrentUserId, null);
			};
			client.Headers[HttpRequestHeader.ContentType] = "application/json";
			var payload = new JObject();
			payload["authenticationToken"] = liveAuthenticationToken;
			client.UploadStringAsync(new Uri(url), payload.ToString());
		}

 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 

  if (/*ErrorHttp/)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Authentication Failed! ({0})", result.ResponseErrorDetail));
                }
                else if (/*UserCancel/)
                {
                    throw new InvalidOperationException("Authentication Canceled!");
                }

                int i = result.ResponseData.IndexOf("#token=");
                if (i > 0)
                {
                    response = JValue.Parse(Uri.UnescapeDataString(result.ResponseData.Substring(i + 7)));
                }
                else
                {
                        i = result.ResponseData.IndexOf("#error=");
                        if (i > 0)
                        {
                            throw new InvalidOperationException(string.Format(
                                CultureInfo.InvariantCulture,
                                Resources.MobileServiceClient_Login_Error_Response,
                                Uri.UnescapeDataString(result.ResponseData.Substring(i + 7))));
                        }
                        else
                        {
                            throw new InvalidOperationException(Resources.MobileServiceClient_Login_Invalid_Response_Format);
                        }
                    
                }

                // Get the Mobile Services auth token and user data
                this.CurrentAuthToken = response[LoginAsyncAuthenticationTokenKey].Value<string>();
                this.CurrentUser = new MobileServiceUser(response["user"]["userId"].Value<string>();
 
  
*/