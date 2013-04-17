using System;
using System.Net;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MobileServices.Sdk
{
    public class MobileServiceTable<TItem> : MobileServiceTable
    {
        public MobileServiceTable(MobileServiceClient client, string tableName)
            : base(client, tableName)
        {
        }

        public void GetAll(Action<TItem[], Exception> continuation)
        {
            Get(null, continuation);
        }

        public void Get(MobileServiceQuery query, Action<TItem[], Exception> continuation)
        {
            Get(query, (arr, ex) =>
            {
                if (ex != null)
                {
                    continuation(null, ex);
                    return;
                }

                continuation(arr.ToObject<TItem[]>(MobileServiceClient.Serializer), null);
            });
        }

        public void Insert(TItem item, Action<TItem, Exception> continuation)
        {
            var jobject = JObject.FromObject(item, MobileServiceClient.Serializer);
            Insert(jobject, (ans, err) =>
            {
                if (err != null)
                {
                    continuation(default(TItem), err);
                    return;
                }

                var results = JsonConvert.DeserializeObject<TItem>(ans);

                continuation(results, null);
            });
        }

    }

    public class MobileServiceTable
    {
        private readonly MobileServiceClient client;
        private readonly string tableName;

        public MobileServiceTable(MobileServiceClient client, string tableName)
        {
            this.client = client;
            this.tableName = tableName;
        }

        void JArrayHandler(string ans, Exception err, Action<JArray, Exception> continuation)
        {
            if (err != null)
            {
                continuation(null, err);
                return;
            }

            var results = JArray.Parse(ans);

            continuation(results, null);
        }

        public void GetAll(Action<JArray, Exception> continuation)
        {
            Get(null, continuation);
        }

        public void Get(MobileServiceQuery query, Action<JArray, Exception> continuation)
        {
            var tableUrl = "tables/" + tableName;
            if (query != null)
            {
                var queryString = query.ToString();
                if (queryString.Length > 0)
                {
                    tableUrl += "?" + queryString;
                }
            }
            client.Get(tableUrl, (res, exception) => JArrayHandler(res, exception, continuation));
        }

        public void Update(JObject updates, Action<Exception> continuation)
        {
            JToken idToken;

            if (updates.TryGetValue("id", out idToken) == false)
            {
                throw new Exception("missing [id] field");
            }

            var id = idToken.Value<object>().ToString();
            var tableUrl = "tables/" + tableName + "/" + id;

            client.Patch(tableUrl, updates, (s, exception) => continuation(exception));
        }

        public void Delete(object id, Action<Exception> continuation)
        {
            var tableUrl = "tables/" + tableName + "/" + id;
            client.Delete(tableUrl, continuation);
        }

        public void Insert(JObject item, Action<string, Exception> continuation)
        {
            var tableUrl = "tables/" + tableName;

            item.Remove("id");

            var nullProperties = item.Properties().Where(p => p.Value.Type == JTokenType.Null).ToArray();
            foreach (var nullProperty in nullProperties)
            {
                item.Remove(nullProperty.Name);
            }

            client.Post(tableUrl, item, continuation);
        }

    }

}
