using DataGrabber.LogWriter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber.Utility
{
    public static class ConfigurationManagerWrapper
    {

        // Returns value if key is available in app settings
        public static string GetValueFromAppSettings(string key)
        {
            string result = string.Empty;

            try
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains(key))
                {
                    // Key exists
                    result = ConfigurationManager.AppSettings[key];
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
