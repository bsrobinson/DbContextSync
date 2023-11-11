using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace DbContextSync.Extensions
{
    public static class EnumExtensions
    {
        public static string DisplayName(this Enum e)
        {
            return e.GetType().GetMember(e.ToString()).First().GetCustomAttribute<DisplayAttribute>()?.Name ?? "";
        }

        public static List<string> DisplayNameList<TEnum>() where TEnum : Enum
        {
            return ((TEnum[])Enum.GetValues(typeof(TEnum))).Select(v => v.DisplayName()).ToList();
        }

        public static List<TEnum> ToList<TEnum>() where TEnum : Enum
        {
            return ((TEnum[])Enum.GetValues(typeof(TEnum))).ToList();
        }
    }
}