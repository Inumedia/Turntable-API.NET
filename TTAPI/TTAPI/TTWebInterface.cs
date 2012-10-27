using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using TTAPI.Recv;
using TTAPI.Send;

namespace TTAPI
{
    public static class TTWebInterface
    {
        static JavaScriptSerializer serializer = new JavaScriptSerializer();
        public static T Request<T>(APICall call, string method = "GET") where T : Command
        {
            string data = Request(call, method);
            bool successful = data.Substring(0, 5) == "[true";
            data = data.Remove(0, successful ? 7 : 8);
            data = data.Remove(data.Length - 1);
            T deserialized = serializer.Deserialize<T>(data);
            successful &= deserialized.err == null;
            if (successful)
                return deserialized;
            else throw new Exception(deserialized.err ?? String.Format("There was an error deserializing the returned data. ({0})", data));
            // I've always wanted to use ??.  And now I have!  :D
        }

        public static string Request(APICall call, string method = "POST")
        {
            string calling = string.Format("http://turntable.fm/api/{0}", call.api);
            if (call.CustomInterfaceAddress != null)
                calling = call.CustomInterfaceAddress;

            Type actualCall = call.GetType();
            FieldInfo[] infos = actualCall.GetFields();
            string[] dataEntries = new string[infos.Length+1];
            dataEntries[infos.Length] = "client=web";
            for (int i = 0; i < infos.Length; ++i)
            {
                FieldInfo field = infos[i];
                string fieldData = field.GetValue(call).ToString();
                string formatted = string.Format("{0}={1}", field.Name, fieldData);
                dataEntries[i] = formatted;
            }
            string serializedData = string.Join("&", dataEntries);
            
            WebRequest req;
            if (method != "GET")
            {   req = WebRequest.Create(calling);
                req.Method = method;
                req.ContentType = "application/x-www-form-urlencoded";

                using (StreamWriter input = new StreamWriter(req.GetRequestStream()))
                    input.Write(serializedData);
            }
            else
            {
                calling = string.Format("{0}?{1}", calling, serializedData);
                req = WebRequest.Create(calling);
            }

            //byte[] data = UTF8Encoding.UTF8.GetBytes(builder.ToString());
            //input.Write(data, 0, data.Length);

            WebResponse res = req.GetResponse();
            string outputData = null;
            using (StreamReader output = new StreamReader(res.GetResponseStream()))
                outputData = output.ReadToEnd();

            return outputData;
        }
    }
}
