using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataGrabberV2.Models.RestLibrary.ConfigTool
{
    public class BasicScrapeRequestDetail
    {
        public BasicScrapeRequestDetail()
        {
            ScrapeID = string.Empty;
            ConfigGUID = string.Empty;
            Status = string.Empty;
            ShouldShowDownload = false;
            DownloadPath = string.Empty;
            ErrorMessage = string.Empty;
            LoginUser = string.Empty;
            Action = string.Empty;
        }

        public string ScrapeID { get; set; }

        public string ConfigGUID { get; set; }

        public string Status { get; set; }

        public bool ShouldShowDownload { get; set; }

        public string DownloadPath { get; set; }

        public string ErrorMessage { get; set; }

        public string LoginUser { get; set; }

        public string Action { get; set; }

        public string SetDataForStart()
        {
            this.Action = "Start";
            return JsonConvert.SerializeObject(this);
        }

        public string SetDataForComplete(string outputPath)
        {
            this.Action = "Complete";
            this.DownloadPath = outputPath;
            return JsonConvert.SerializeObject(this);
        }

        public string SetDataForFail(string errorMessage)
        {
            this.Action = "Fail";
            this.ErrorMessage = errorMessage;
            return JsonConvert.SerializeObject(this);
        }

    }
}
