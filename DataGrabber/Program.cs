using DataGrabber.BusinessLogic;
using DataGrabber.LogWriter;
using DataGrabber.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber
{
    class Program
    {
        static void Main(string[] args)
        {
            //ScrapeWebsiteUserInput.Instance.Main();
            Logger.Write($"Process Started -> DataGrabber. Time: {DateTime.Now.ToString()}");
            
            ScrapeData.Instance.Main();

            Logger.Write($"Process Ended -> DataGrabber. Time: {DateTime.Now.ToString()}");
        }
    }
}
