using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace WindowTabs.CSharp.Services
{
    internal static class JsoncHelper
    {
        public static string RemoveJsoncComments(string json)
        {
            var withoutSingleLine = Regex.Replace(json, @"//.*?(?=\r?\n|$)", string.Empty);
            return Regex.Replace(withoutSingleLine, @"/\*[\s\S]*?\*/", string.Empty);
        }

        public static JObject ParseObject(string json)
        {
            return JObject.Parse(RemoveJsoncComments(json));
        }

        public static JArray ParseArray(string json)
        {
            return JArray.Parse(RemoveJsoncComments(json));
        }
    }
}
