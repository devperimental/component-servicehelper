﻿using Microsoft.Extensions.Configuration;

namespace PlatformX.Storage.NTesting
{
    public class TestHelper
    {
        public static string Default_IpAddress { get; internal set; }
        public static string Default_CorrelationId { get; internal set; }
        public static string Default_ServiceKeyValue { get; internal set; }
        public static string Default_ServiceSecretValue { get; internal set; }

        public static IConfigurationRoot GetIConfigurationRoot(string outputPath)
        {
            return new ConfigurationBuilder()
                .SetBasePath(outputPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public static T GetConfiguration<T>(string outputPath, string sectionName) where T : new()
        {
            var configuration = new T();

            var configRoot = GetIConfigurationRoot(outputPath);

            configRoot
                .GetSection(sectionName)
                .Bind(configuration);

            return configuration;
        }
    }
}
