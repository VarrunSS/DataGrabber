using DataGrabber.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber.BusinessLogic.Contract
{
    public interface IScraper
    {

        List<WebScrapeOutput> ProcessRequest(WebScrapeInput input);

        void ProcessDetailRequest(WebScrapeInput input, ref List<WebScrapeOutput> output);



        void SetDefaultConfiguration(WebScrapeUserInput data, ref WebScrapeInput input);

    }
}
