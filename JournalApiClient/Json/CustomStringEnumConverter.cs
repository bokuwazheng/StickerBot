using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Linq;

namespace JournalApiClient.Json
{
    /// <summary>
    /// Replaces GraphQL.Client.Serializer.Newtonsoft.ConstantCaseEnumConverter serialization behavior with default behavior. 
    /// </summary>
    public class CustomStringEnumConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type type = value.GetType();

            if (!type.IsEnum)
                throw new InvalidOperationException("Only type Enum is supported");

            foreach (int enumValue in type.GetEnumValues().Cast<int>())
            {
                if (enumValue == (int)value)
                {
                    writer.WriteValue(enumValue);

                    return;
                }
            }

            throw new ArgumentException("Enum not found");
        }
    }
}
