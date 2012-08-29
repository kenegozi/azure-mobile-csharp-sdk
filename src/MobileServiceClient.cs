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
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace MobileServices.Sdk {
	public class MobileServiceClient {
		static readonly JsonSerializer Serializer;

		readonly string applicationKey;
		readonly string serviceUrl;

		public string CurrentAuthToken { get; private set; }
		public string CurrentUserId { get; private set; }

		public MobileServiceClient(string serviceUrl, string applicationKey) {
			this.serviceUrl = serviceUrl;
			this.applicationKey = applicationKey;
		}

		static MobileServiceClient() {
			Serializer = new JsonSerializer();
			Serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
			Serializer.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
			Serializer.DateFormatHandling = DateFormatHandling.IsoDateFormat;//.DateTimeZoneHandling = DateTimeZoneHandling.Utc
		}

		public void Logout() {
			CurrentUserId = null;
			CurrentAuthToken = null;
		}

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

		public void Read(string table, Action<JArray, Exception> continuation) {
			Read(table, null, continuation);
		}
		public void Read(string table, MobileServiceQuery query, Action<JArray, Exception> continuation) {
			Read(table, query, (ans, err) => {
				if (err != null) {
					continuation(null, err);
					return;
				}

				var results = JArray.Parse(ans);

				continuation(results, null);
			});
		}

		public void Read<TItem>(string table, Action<TItem[], Exception> continuation) {
			Read(table, null, continuation);
		}

		public void Read<TItem>(string table, MobileServiceQuery query, Action<TItem[], Exception> continuation) {
			Read(table, query, (ans, err) => {
				if (err != null) {
					continuation(null, err);
					return;
				}

				var results = JsonConvert.DeserializeObject<TItem[]>(ans);

				continuation(results, null);
			});
		}

		public void Insert<TItem>(string table, TItem item, Action<TItem, Exception> continuation) {
			Insert(table, item, (ans, err) => {
				if (err != null) {
					continuation(default(TItem), err);
					return;
				}

				var results = JsonConvert.DeserializeObject<TItem>(ans);

				continuation(results, null);
			});
		}

		public void Insert(string table, JObject item, Action<JObject, Exception> continuation) {
			Insert(table, item, (ans, err) => {
				if (err != null) {
					continuation(null, err);
					return;
				}

				continuation(JObject.Parse(ans), null);
			});
		}

		public void Delete(string table, object id, Action<Exception> continuation) {
			var tableUrl = serviceUrl + "tables/" + table + "/" + id;
			var client = new WebClient();
			client.UploadStringCompleted += (x, args) => {
				if (args.Error != null) {
					continuation(args.Error);
					return;
				}
				continuation(null);
			};

			SetMobileServiceHeaders(client);
			client.UploadStringAsync(new Uri(tableUrl), "DELETE", "");
		}

		void Read(string table, MobileServiceQuery query, Action<string, Exception> continuation) {
			var tableUrl = serviceUrl + "tables/" + table;
			if (query != null) {
				var queryString = query.ToString();
				if (queryString.Length > 0) {
					tableUrl += "?" + queryString;
				}
			}
			var client = new WebClient();
			client.DownloadStringCompleted += (x, args) => {
				if (args.Error != null) {
					continuation(null, args.Error);
					return;
				}
				continuation(args.Result, null);
			};
			SetMobileServiceHeaders(client);
			client.DownloadStringAsync(new Uri(tableUrl));
		}

		void Insert(string table, object item, Action<string, Exception> continuation) {
			var tableUrl = serviceUrl + "tables/" + table;
			var client = new WebClient();
			client.UploadStringCompleted += (x, args) => {
				if (args.Error != null) {
					continuation(null, args.Error);
					return;
				}
				continuation(args.Result, null);
			};
			SetMobileServiceHeaders(client);
			var jobject = item as JObject ?? JObject.FromObject(item, Serializer);

			jobject.Remove("id");

			var nullProperties = jobject.Properties().Where(p => p.Value.Type == JTokenType.Null).ToArray();
			foreach (var nullProperty in nullProperties) {
				jobject.Remove(nullProperty.Name);
			}

			client.UploadStringAsync(new Uri(tableUrl), jobject.ToString());
		}

		private void SetMobileServiceHeaders(WebClient client) {
			if (CurrentAuthToken != null) {
				client.Headers["X-ZUMO-AUTH"] = CurrentAuthToken;
			}
			if (applicationKey != null) {
				client.Headers["X-ZUMO-APPLICATION"] = applicationKey;
			}
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
