using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.Elasticsearch;
using System.Globalization;

namespace Eshop.BuildingBlocks.Logging
{
    public static class LoggingExtension
    {
        public static void SetupLogging(this IServiceCollection services,
            string appName,
            string environment,
            string? elasticSearchConnectionString,
            bool allowLoggingIntoFile = false,
            Dictionary<string, LogEventLevel>? minimumLevelOverrides = null)
        {
            var logger = CreateLogger(appName, environment, elasticSearchConnectionString, allowLoggingIntoFile, minimumLevelOverrides);

            services.AddLogging(c =>
            {
                c.AddSerilog(logger, dispose: true);
            });
        }

        private static Logger CreateLogger(string appName, string environment, string? elasticSearchConnectionString, bool allowLoggingIntoFile, Dictionary<string, LogEventLevel>? minimumLevelOverrides)
        {
            var loggerConfiguration = CreateLoggerConfiguration(appName, environment, minimumLevelOverrides);

            if (allowLoggingIntoFile)
                loggerConfiguration.WriteTo.File(new CompactJsonFormatter(), $"log/{appName}_json_.log", rollingInterval: RollingInterval.Day);

            if (!string.IsNullOrWhiteSpace(elasticSearchConnectionString))
                loggerConfiguration.WriteTo.Elasticsearch(ConfigureElasticSink(elasticSearchConnectionString));

            var logger = loggerConfiguration.CreateLogger();
            return logger;
        }

        private static LoggerConfiguration CreateLoggerConfiguration(string appName, string environment, Dictionary<string, LogEventLevel>? minimumLevelOverrides)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.WithProperty("AppName", appName)
                .Enrich.WithProperty("Environment", environment)
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.WithProcessId()
                .Enrich.WithExceptionDetails()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);

            if (minimumLevelOverrides != null && minimumLevelOverrides.Any())
                foreach (var kv in minimumLevelOverrides)
                    loggerConfiguration.MinimumLevel.Override(kv.Key, kv.Value);

            return loggerConfiguration;
        }

        private static ElasticsearchSinkOptions ConfigureElasticSink(string elasticSearchConnectionString)
        {
            return new ElasticsearchSinkOptions(new Uri(elasticSearchConnectionString))
            {
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                MinimumLogEventLevel = LogEventLevel.Information,
                CustomFormatter = new EsPropertyTypeNamedFormatter()
            };
        }

        /// <summary>
        /// Creating a mapper to solve scoped variables of different types produces issues.
        /// Solution: https://github.com/serilog-contrib/serilog-sinks-elasticsearch/issues/184
        /// </summary>
        internal class EsPropertyTypeNamedFormatter : ElasticsearchJsonFormatter
        {
            // Use a property writer that can change the property names and values before writing to ES
            protected override void WritePropertiesValues(IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
            {
                string precedingDelimiter = "";
                foreach (KeyValuePair<string, LogEventPropertyValue> property in properties)
                {
                    string type;
                    object value;

                    try
                    {
                        if (property.Value is ScalarValue asScalar)
                        {
                            if (asScalar.Value is DateTime || asScalar.Value is DateTimeOffset)
                            {
                                type = "D_";
                                value = asScalar.Value;
                            }
                            else if (asScalar.Value is long || asScalar.Value is int || asScalar.Value is short || asScalar.Value is byte ||
                                     asScalar.Value is ulong || asScalar.Value is uint || asScalar.Value is ushort || asScalar.Value is sbyte)
                            {
                                type = "I_";
                                value = asScalar.Value;
                            }
                            else if (asScalar.Value is float || asScalar.Value is double || asScalar.Value is decimal)
                            {
                                if (IsNaN(asScalar.Value))
                                {
                                    type = "S_";
                                    value = "NaN";
                                }
                                else if (IsInfinity(asScalar.Value))
                                {
                                    type = "S_";
                                    value = "Infinity";
                                }
                                else
                                {
                                    type = "F_";
                                    value = asScalar.Value;
                                }
                            }
                            else if (asScalar.Value is bool)
                            {
                                type = "B_";
                                value = asScalar.Value;
                            }
                            else if (asScalar.Value is string || asScalar.Value is char)
                            {
                                type = "S_";
                                value = asScalar.Value;
                            }
                            else
                            {
                                type = "J_";
                                value = JsonConvert.SerializeObject(asScalar.Value);
                            }
                        }
                        else if (property.Value is SequenceValue sequenceValue)
                        {
                            type = "J_";
                            value = JsonConvert.SerializeObject(sequenceValue.Elements);
                        }
                        else if (property.Value is DictionaryValue dictionaryValue)
                        {
                            type = "J_";
                            value = JsonConvert.SerializeObject(dictionaryValue.Elements);
                        }
                        else
                        {
                            type = "_J";
                            value = JsonConvert.SerializeObject(property.Value);
                        }
                    }
                    catch (Exception e)
                    {
                        var errorDict = new Dictionary<string, object>
                        {
                            ["Message"] = "An exception occured in EsPropertyTypeNamedFormatter when serializing this value for ElasticSearch.",
                            ["Exception"] = e,
                        };

                        type = "_J";
                        value = JsonConvert.SerializeObject(errorDict);
                    }

                    string key = type + property.Key;
                    WriteJsonProperty(key, value, ref precedingDelimiter, output);
                }
            }
        }

        private static bool IsInfinity(object value)
        {
            if (value is float floatValue)
                return float.IsInfinity(floatValue);
            else if (value is double doubleValue)
                return double.IsInfinity(doubleValue);
            else
                return false;
        }

        private static bool IsNaN(object value)
        {
            if (value is float floatValue)
                return float.IsNaN(floatValue);
            else if (value is double doubleValue)
                return double.IsNaN(doubleValue);
            else
                return false;
        }
    }

}
