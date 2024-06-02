using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_bittorrent.src
{
    internal static class DictionaryExtensions
    {
        public static bool TryGetValue<T>(this Dictionary<string, object> dictionary, string key, out T value)
        {
            value = default;
            if (dictionary.TryGetValue(key, out var dict_value) && dict_value is T)
            {
                value = (T)dict_value;
                return true;
            }
            return false;
        }

        public static T GetValue<T>(this Dictionary<string, object> dictionary, string key)
        {
            return (T)dictionary[key];
        }

        public static Dictionary<string, object> GetDictionary(this Dictionary<string, object> dictionary, string key)
        {
            return (Dictionary<string, object>)dictionary[key];
        }
    }
}
