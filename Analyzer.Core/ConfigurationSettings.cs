using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Analyzer.Core
{
    public class ConfigurationSettings
    {
        private IConfigurationRoot _configuration;

        private ConfigurationSettings()
        {
            _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();
        }

        public static ConfigurationSettings Instance
            => new ConfigurationSettings();

        public string Get(string key)
            => _configuration[key];
    }
}
