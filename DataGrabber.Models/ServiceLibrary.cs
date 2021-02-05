using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber.Models
{
    class ServiceLibrary
    {
    }

    public class ApiResponse
    {


        public HttpStatusCode StatusCode { get; set; }

        public string Message { get; set; }
    }


    public class CookieResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("renewInSec")]
        public int RenewInSec { get; set; }

        [JsonProperty("cookieDomain")]
        public string CookieDomain { get; set; }

    }


    public class IpResponse
    {
        [JsonProperty("ip")]
        public string IpAddress { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

    }

}
