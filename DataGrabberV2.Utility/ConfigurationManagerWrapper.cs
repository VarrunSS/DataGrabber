using DataGrabberV2.LogWriter;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabberV2.Utility
{
    public static class ConfigurationManagerWrapper
    {

        // Returns value if key is available in app settings
        public static string GetValueFromAppSettings(string key)
        {
            string result = string.Empty;

            try
            {
                var builder = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json");

                var configuration = builder.Build();

                if (!string.IsNullOrEmpty(configuration.GetSection(key).Value))
                {
                    // Key exists
                    result = configuration[key];
                }
                else
                {
                    // Key doesn't exist
                    Logger.Write($"ERROR in GetValueFromAppSettings in ConfigurationManagerWrapper; Message: Key ({key}) does not exist");
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in GetValueFromAppSettings in ConfigurationManagerWrapper; Message:" + ex.Message);
            }
            finally
            {

            }

            return result;
        }


    }
}
