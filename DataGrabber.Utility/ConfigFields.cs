using DataGrabber.LogWriter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber.Utility
{
    public static class ConfigFields
    {
        static ConfigFields()
        {
            Initialize();
        }


        public static double MaxBrowserWaitTimeInMinutes { get; set; }

        public static double WaitTimeToLoadNewTabInSeconds { get; set; }

        public static double WaitTimeToLoadPageInSeconds { get; set; }

        public static double WaitTimeToLoadResultsAfterClickInSeconds { get; set; }
        
        public static int InputSplitSize { get; set; }

        public static int MaxDegreeOfParallelism { get; set; }



        public static string ApplicationBasePath { get; set; }

        public static string LogPath { get; set; }

        public static string InputFolder { get; set; }

        public static string GoogleCaptchaFolder { get; set; }        

        public static string InputFile { get; set; }

        public static string Output { get; set; }

        public static string SelectorGadget { get; set; }

        public static string CanvasFingerprintDefender { get; set; }

        public static string BlockImage { get; set; }

        public static string ChromeDriver { get; set; }

        public static string IpAddressApi { get; set; }

        public static int RotateIpAddressAfter_Seconds { get; set; }
        


        public static string EmailTemplatePath { get; set; }

        public static string MailDisplayName { get; set; }

        public static string MailSubject { get; set; }

        public static string MailFromAddress { get; set; }

        public static string MailToAddress { get; set; }

        public static string MailCCAddress { get; set; }

        public static string MailBCCAddress { get; set; }



        public static string Environment { get; private set; }

        public static string EmailHostServer { get; private set; }

        public static int PortNumber { get; private set; }

        public static string HostUserName { get; private set; }

        public static string HostPassword { get; private set; }

        public static bool EnableSsl { get; private set; }





        private static void Initialize()
        {
            try
            {
                MaxBrowserWaitTimeInMinutes = Convert.ToDouble(ConfigurationManager.AppSettings["MaxBrowserWaitTimeInMinutes"]);
                WaitTimeToLoadNewTabInSeconds = Convert.ToDouble(ConfigurationManager.AppSettings["WaitTimeToLoadNewTabInSeconds"]);
                WaitTimeToLoadPageInSeconds = Convert.ToDouble(ConfigurationManager.AppSettings["WaitTimeToLoadPageInSeconds"]);
                WaitTimeToLoadResultsAfterClickInSeconds = Convert.ToDouble(ConfigurationManager.AppSettings["WaitTimeToLoadResultsAfterClickInSeconds"]);
                InputSplitSize = Convert.ToInt32(ConfigurationManager.AppSettings["InputSplitSize"]);
                MaxDegreeOfParallelism = Convert.ToInt32(ConfigurationManager.AppSettings["MaxDegreeOfParallelism"]);

                ApplicationBasePath = ConfigurationManager.AppSettings["ApplicationBasePath"].ToString();
                LogPath = Path.Combine(ApplicationBasePath, ConfigurationManager.AppSettings["LogPath"].ToString());
                InputFolder = Path.Combine(ApplicationBasePath, ConfigurationManager.AppSettings["InputFolder"].ToString());
                GoogleCaptchaFolder = Path.Combine(ApplicationBasePath, ConfigurationManager.AppSettings["GoogleCaptchaFolder"].ToString());
                InputFile = Path.Combine(ApplicationBasePath, ConfigurationManager.AppSettings["InputFile"].ToString());
                Output = Path.Combine(ApplicationBasePath, ConfigurationManager.AppSettings["Output"].ToString());
                SelectorGadget = Path.Combine(ApplicationBasePath, ConfigurationManager.AppSettings["SelectorGadget"].ToString());
                CanvasFingerprintDefender = Path.Combine(ApplicationBasePath, ConfigurationManager.AppSettings["CanvasFingerprintDefender"].ToString());
                BlockImage = Path.Combine(ApplicationBasePath, ConfigurationManager.AppSettings["BlockImage"].ToString());
                                
                ChromeDriver = Path.Combine(ApplicationBasePath, ConfigurationManager.AppSettings["ChromeDriver"].ToString());

                IpAddressApi = ConfigurationManagerWrapper.GetValueFromAppSettings("URL_IpAddressApi");
                RotateIpAddressAfter_Seconds = Convert.ToInt32(ConfigurationManagerWrapper.GetValueFromAppSettings("RotateIpAddressAfter_Seconds"));

                Environment = ConfigurationManagerWrapper.GetValueFromAppSettings("Environment").ToUpper().Trim();
                EmailTemplatePath = Path.Combine(ApplicationBasePath, ConfigurationManager.AppSettings["EmailTemplatePath"].ToString());
                MailDisplayName = ConfigurationManagerWrapper.GetValueFromAppSettings("Mail_DisplayName");
                MailSubject = ConfigurationManagerWrapper.GetValueFromAppSettings("Mail_Subject");
                MailFromAddress = ConfigurationManagerWrapper.GetValueFromAppSettings("Mail_FromAddress");
                MailToAddress = ConfigurationManagerWrapper.GetValueFromAppSettings("Mail_ToAddress");
                MailCCAddress = ConfigurationManagerWrapper.GetValueFromAppSettings("Mail_CCAddress");
                MailBCCAddress = ConfigurationManagerWrapper.GetValueFromAppSettings("Mail_BCCAddress");

                EmailHostServer = ConfigurationManagerWrapper.GetValueFromAppSettings("EmailHostServer");
                PortNumber = Convert.ToInt32(ConfigurationManagerWrapper.GetValueFromAppSettings("PortNumber"));
                HostUserName = ConfigurationManagerWrapper.GetValueFromAppSettings("HostUserName");
                HostPassword = ConfigurationManagerWrapper.GetValueFromAppSettings("HostPassword");
                EnableSsl = bool.Parse(ConfigurationManagerWrapper.GetValueFromAppSettings("EnableSsl"));

            }
            catch (Exception ex)
            {
                Logger.Write("Exception occurred in Initialize() - ConfigFields. Message: " + ex.Message);
            }
            finally
            {

            }
        }



    }
}
