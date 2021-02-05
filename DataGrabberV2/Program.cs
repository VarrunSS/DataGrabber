using DataGrabberV2.BusinessLogic;
using DataGrabberV2.LogWriter;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace DataGrabberV2
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
