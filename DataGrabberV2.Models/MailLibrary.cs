using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabberV2.Models
{
    class MailLibrary
    {
    }

    public class MailInformation
    {

        [JsonProperty("Mail To Address")]
        public string MailToAddress { get; set; }

        [JsonProperty("Mail CC Address")]
        public string MailCCAddress { get; set; }

        [JsonProperty("Mail BCC Address")]
        public string MailBCCAddress { get; set; }

        public string AttachmentPath { get; set; }

        public string ConfigName { get; set; }

        public string StartedOn { get; set; }

        public string EndedOn { get; set; }

        public string RunTime { get; set; }


    }
}
