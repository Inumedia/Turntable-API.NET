using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TTAPI.Recv;
using TTAPI.Send;

namespace TTAPI
{
    public static class TTWebInterface
    {
        static HttpClient client = new HttpClient();

        /// <summary>
        /// Calls <see cref="TTAPI.TTWebInterface.Request"/> and interprets the returned data as <see cref="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of command that we will serialize to.</typeparam>
        /// <param name="call">The API call that we will attempt to execute.</param>
        /// <param name="method">The calling method.  Either POST or GET.</param>
        /// <returns>The returned data from the call in the form of <see cref="T"/></returns>
        /// <exception cref="System.Exception">Thrown if the returned data from the request contains an error or was not successful.</exception>
        public static async Task<T> RequestAsync<T>(APICall call, string method = "POST") where T : Command
        {
            string data = await RequestAsync(call, method);
            data = Command.Preprocess(typeof(T), data);
            T deserialized = JsonSerializer.Deserialize<T>(data);
            bool successful = deserialized.err == null;
            if (successful)
                return deserialized;
            else throw new Exception(deserialized.err ?? String.Format("There was an error deserializing the returned data. ({0})", data));
        }

        /// <summary>
        /// Calls <see cref="TTAPI.TTWebInterface.Request"/> and interprets the returned data as <see cref="T"/>.  Throws an exception if there was an error returned.
        /// </summary>
        /// <typeparam name="T">The type of command that we will serialize to.</typeparam>
        /// <param name="call">The API call that we will attempt to execute.</param>
        /// <param name="results">The output of the request.</param>
        /// <param name="method">The calling method.  Either POST or GET.</param>
        /// <returns>Whether or not the request was successful.</returns>
        public static bool Request<T>(APICall call, out T results, string method = "POST") where T : Command
        {
            string data = RequestAsync(call, method).GetAwaiter().GetResult();
            data = Command.Preprocess(typeof(T), data);
            try
            {
                T deserialized = JsonSerializer.Deserialize<T>(data);
                bool successful = deserialized.err == null;
                results = deserialized;
                return successful;
            }
            catch (Exception ex)
            {
                results = null;
                return false;
            }
        }

        /// <summary>
        /// Calls Turntable.fm's RPC equivalent of the API call.
        /// </summary>
        /// <param name="call">The API call that we will attempt to execute.</param>
        /// <param name="method">The calling method.  Either POST or GET.</param>
        /// <returns>The returned data from the call.  Usually in the form of an array, [successful, {objectData}]</returns>
        public static async Task<string> RequestAsync(APICall call, string method = "POST")
        {
            string calling = string.Format("http://turntable.fm/api/{0}", call.api);
            if (call.CustomInterfaceAddress != null)
                calling = call.CustomInterfaceAddress;

            var variables = call
                    .GetType()
                    .GetProperties()
                    .Select(property => new Tuple<string, object>(property.Name, property.GetValue(call)))
                    .Where(property => property.Item2 != null)
                    .ToDictionary(property => property.Item1, property => property.Item2.ToString());

            variables.Add("client", "web");

            FormUrlEncodedContent content = new FormUrlEncodedContent(variables);
            HttpResponseMessage response = null;
            if (method != "GET")
            {
                response = await client.PostAsync(calling, content);
            }
            else
            {
                calling = string.Format("{0}?{1}", calling, content.ToString());
                response = await client.GetAsync(calling);
            }

            response.EnsureSuccessStatusCode();
            var responseData = await response.Content.ReadAsStringAsync();
            return responseData;
        }
    }
}
