using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kojeom.Common
{
    /// <summary>
    /// JsonUtility가 최상위 배열([])을 직접 처리하지 못하는 문제를 래퍼로 우회.
    /// </summary>
    public static class JsonArrayUtility
    {
        [Serializable]
        private class Wrapper<T> { public T[] items; }

        public static T[] FromJson<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Array.Empty<T>();
            int i = 0;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            bool isArray = (i < json.Length && json[i] == '[');
            if (isArray)
            {
                string wrapped = "{\"items\":" + json + "}";
                var wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
                return wrapper?.items ?? Array.Empty<T>();
            }
            else
            {
                var single = JsonUtility.FromJson<T>(json);
                return single != null ? new[] { single } : Array.Empty<T>();
            }
        }

        public static string ToJson<T>(IList<T> list, bool prettyPrint = false)
        {
            var wrapper = new Wrapper<T> { items = list == null ? Array.Empty<T>() : new List<T>(list).ToArray() };
            string wrapped = JsonUtility.ToJson(wrapper, prettyPrint);
            int firstBracket = wrapped.IndexOf('[');
            int lastBracket = wrapped.LastIndexOf(']');
            if (firstBracket >= 0 && lastBracket >= firstBracket)
                return wrapped.Substring(firstBracket, lastBracket - firstBracket + 1);
            return wrapped;
        }
    }
}
