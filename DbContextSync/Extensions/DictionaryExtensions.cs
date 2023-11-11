using System.Collections.Generic;

namespace DbContextSync.Extensions
{
    public static class DictionaryExtensions
    {
        public static void AddToKeyedList<T>(this Dictionary<string, List<T>> source, string key, T value)
        {
            if (source.TryGetValue(key, out List<T>? list))
            {
                list.Add(value);
            }
            else
            {
                source.Add(key, new List<T>() { value });
            }
        }

        public static void AddToKeyedList<T>(this Dictionary<string, List<T>> source, string key, List<T> values)
        {
            if (source.TryGetValue(key, out List<T>? list))
            {
                list.AddRange(values);
            }
            else
            {
                source.Add(key, values);
            }
        }
    }
}