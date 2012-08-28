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
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace MobileServices.Sdk {
	public class MobileServiceClient {
		static readonly JsonSerializer Serializer;

		string applicationKey;
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
			Read(table, (ans, err) => {
				if (err != null) {
					continuation(null, err);
					return;
				}

				var results = JArray.Parse(ans);

				continuation(results, null);
			});
		}

		public void Read<TItem>(string table, Action<TItem[], Exception> continuation) {
			Read(table, (ans, err) => {
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

		void Read(string table, Action<string, Exception> continuation) {
			var tableUrl = serviceUrl + "tables/" + table;
			var client = new WebClient();
			client.DownloadStringCompleted += (x, args) => {
				if (args.Error != null) {
					continuation(null, args.Error);
					return;
				}
				continuation(args.Result, null);
			};
			client.Headers["X-ZUMO-AUTH"] = CurrentAuthToken;
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
			client.Headers["X-ZUMO-AUTH"] = CurrentAuthToken;
			var jobject = item as JObject;

			if (jobject == null) {
				var body = new StringBuilder();
				using (var writer = new StringWriter(body)) {
					Serializer.Serialize(writer, item);
				}

				jobject = JObject.Parse(body.ToString());
			}
			jobject.Remove("id");
			client.UploadStringAsync(new Uri(tableUrl), jobject.ToString());
		}
	}
}
