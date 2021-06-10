using System;
using System.ComponentModel;
using System.Linq;

namespace JournalApiClient.Extensions
{
    public static class EnumExtensions
    {
        public static string ToDescriptionOrString(this Enum value) => value
            .GetType()
            .GetField(value.ToString())
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .Cast<DescriptionAttribute>()
            .FirstOrDefault()?.Description ?? value.ToString();
    }
}
