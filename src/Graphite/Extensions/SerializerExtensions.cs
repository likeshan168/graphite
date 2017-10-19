﻿using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Graphite.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Graphite.Extensions
{
    public static class SerializerExtensions
    {
        public class IpAddressConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(IPAddress);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((IPAddress)value).ToString());
            }

            public override object ReadJson(JsonReader reader, Type objectType, 
                object existingValue, JsonSerializer serializer)
            {
                return IPAddress.Parse(JToken.Load(reader).Value<string>());
            }
        }

        public static JsonSerializerSettings SerializeIpAddresses(this JsonSerializerSettings settings)
        {
            settings.Converters.Add(new IpAddressConverter());
            return settings;
        }

        public class GraphiteIsoDateTimeConverter : IsoDateTimeConverter
        {
            private bool _adjustToLocal;

            /// <summary>
            /// Converts to local time after deserialization.
            /// </summary>
            public GraphiteIsoDateTimeConverter AdjustToLocalAfterDeserializing()
            {
                _adjustToLocal = true;
                return this;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var result = base.ReadJson(reader, objectType, existingValue, serializer);
                if (_adjustToLocal && result is DateTime)
                    return ((DateTime) result).ToLocalTime();
                return result;
            }
        }

        public static JsonSerializerSettings ConfigureIsoDateTimeConverter(this JsonSerializerSettings settings, 
            Action<GraphiteIsoDateTimeConverter> configure = null)
        {
            settings.RemoveConverters<IsoDateTimeConverter>();
            configure?.Invoke(settings.GetOrAddConverter<GraphiteIsoDateTimeConverter>());
            return settings;
        }

        public static T GetOrAddConverter<T>(this JsonSerializerSettings settings)
            where T : JsonConverter, new()
        {
            var converter = settings.Converters.OfType<T>().FirstOrDefault();
            if (converter != null) return converter;
            converter = new T();
            settings.Converters.Add(converter);
            return converter;
        }

        public static void AddConverter<T>(this JsonSerializerSettings settings)
            where T : JsonConverter, new()
        {
            settings.Converters.Add(new T());
        }

        public static JsonSerializerSettings RemoveConverters<T>(this JsonSerializerSettings settings) 
            where T : JsonConverter
        {
            settings.Converters.OfType<T>().ToList()
                .ForEach(x => settings.Converters.Remove(x));
            return settings;
        }

        public class GraphiteMicrosoftJsonDateTimeConverter : JsonConverter
        {
            private bool _adjustToLocal;
            private bool _adjustToUtc;

            /// <summary>
            /// Converts to local time after deserialization.
            /// </summary>
            public GraphiteMicrosoftJsonDateTimeConverter AdjustToLocalAfterDeserializing()
            {
                _adjustToLocal = true;
                return this;
            }

            /// <summary>
            /// Converts to utc time before deserialization.
            /// </summary>
            public GraphiteMicrosoftJsonDateTimeConverter AdjustToUtcBeforeSerializing()
            {
                _adjustToUtc = true;
                return this;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var datetime = value as DateTime?;
                if (datetime != null && _adjustToUtc)
                    datetime = datetime.Value.ToUniversalTime();
                writer.WriteValue(datetime ?? value);
            }

            public override object ReadJson(JsonReader reader, Type objectType,
                object existingValue, JsonSerializer serializer)
            {
                var datetime = reader.Value as DateTime?;
                if (datetime != null && _adjustToLocal)
                    return datetime.Value.ToLocalTime();
                return datetime;
            }
        }

        public static JsonSerializerSettings WriteMicrosoftJsonDateTime(this JsonSerializerSettings settings,
            Action<GraphiteMicrosoftJsonDateTimeConverter> configure = null)
        {
            settings.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
            settings.RemoveConverters<GraphiteMicrosoftJsonDateTimeConverter>();
            configure?.Invoke(settings.GetOrAddConverter<GraphiteMicrosoftJsonDateTimeConverter>());
            return settings;
        }

        public static JsonSerializerSettings WriteEnumNames(this JsonSerializerSettings settings)
        {
            settings.Converters.Add(new StringEnumConverter());
            return settings;
        }

        public static JsonSerializerSettings WriteNonNumericFloatsAsDefault(this JsonSerializerSettings settings)
        {
            settings.FloatFormatHandling = FloatFormatHandling.DefaultValue;
            return settings;
        }

        public static JsonSerializerSettings UseCamelCaseNaming(this JsonSerializerSettings settings)
        {
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            return settings;
        }

        public static JsonSerializerSettings IgnoreCircularReferences(this JsonSerializerSettings settings)
        {
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            return settings;
        }

        public static JsonSerializerSettings IgnoreNullValues(this JsonSerializerSettings settings)
        {
            settings.NullValueHandling = NullValueHandling.Ignore;
            return settings;
        }

        public static JsonSerializerSettings FailOnUnmatchedElements(this JsonSerializerSettings settings)
        {
            settings.MissingMemberHandling = MissingMemberHandling.Error;
            return settings;
        }

        public static JsonSerializerSettings PrettyPrintInDebugMode(this JsonSerializerSettings settings)
        {
            if (Assembly.GetCallingAssembly().IsInDebugMode())
                settings.Formatting = Formatting.Indented;
            return settings;
        }
    }
}
