using Newtonsoft.Json.Linq;
using MyLibrary.Common.Json.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Client.Utility
{
    internal class JsonUtility
    {
        public JsonRequest GetJsonRequest(string service, string method, Dictionary<string, object> parameters)
        {
            //create a jsonrequest object
            var jsonRequest = new JsonRequest
            {
                JsonRpc = "2.0",
                Id = Guid.NewGuid(),
                Method = string.Format("{0}.{1}", service, method),
                Params = parameters
            };

            return jsonRequest;
        }

        public T ResultToObject<T>(object Result)
        {
            if (Result is JObject)
            {
                return ((JObject)Result).ToObject<T>();
            }
            else if (Result is JArray)
            {
                return ((JArray)Result).ToObject<T>();
            }
            else
            {
                //attempt to make a direct cast on the result object
                return (T)Result;
            }
        } 
    }
}
