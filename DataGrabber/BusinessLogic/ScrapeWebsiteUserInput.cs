using DataGrabber.LogWriter;
using DataGrabber.Models;
using DataGrabber.Utility;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber.BusinessLogic
{
    public class ScrapeWebsiteUserInput
    {
        private ScrapeWebsiteUserInput() { }

        public static ScrapeWebsiteUserInput Instance { get; } = new ScrapeWebsiteUserInput();

        public void Main()
        {
            try
            {
                Console.Write("Enter URL to be scraped: ");
                string url = Console.ReadLine();


                List<WebScrapeUserInput> userInput = new List<WebScrapeUserInput>() {
                    new WebScrapeUserInput(){}
                };
                string json = JsonConvert.SerializeObject(userInput);

                // start processing request
                ProcessRequest(url);

                Console.ReadLine();

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in Main -- ScrapeProductsListInput -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
        }

        private void ProcessRequest(string URL)
        {
            try
            {
                // init chrome browser
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("no-sandbox");
                options.AddArgument("--start-maximized");
                options.AddExtension(ConfigFields.SelectorGadget);
                options.AddExtension(ConfigFields.CanvasFingerprintDefender);
                

                ChromeDriver driver = new ChromeDriver(options);

                // go to target URL
                driver.Navigate().GoToUrl(URL);


            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ProcessRequest -- ScrapeProductsListInput -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
        }

    }
}


//try
//{
//List<WebScrapeInput> inputs = new List<WebScrapeInput>();

//}
//catch (Exception ex)
//{

//}
//finally
//{

//}