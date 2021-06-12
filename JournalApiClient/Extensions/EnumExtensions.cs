using System;
using System.ComponentModel;
using System.Linq;

namespace JournalApiClient.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Returns description if enum member has corresponding attribute. Otherwise returns name of enum member.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToDescription(this Enum value) => value
            .GetType()
            .GetField(value.ToString())
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .Cast<DescriptionAttribute>()
            .FirstOrDefault()?.Description ?? value.ToString();
    }
}
