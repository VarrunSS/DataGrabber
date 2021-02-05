using DataGrabber.LogWriter;
using DataGrabber.Models;
using DataGrabber.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber.Utility
{
    public class ScrapeRestService
    {

        private ScrapeRestService() { }

        public static ScrapeRestService Instance { get; } = new ScrapeRestService();



        public ApiResponse GetIpAddress()
        {
            var result = new ApiResponse();
            string Url = string.Empty;

            try
            {
                Url = ConfigFields.IpAddressApi;

                ServicePointManager.Expect100Continue = true;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                var client = new RestClient(Url) { };
                var request = new RestRequest(Method.GET);

                IRestResponse response = client.Execute(request);
                result.StatusCode = response.StatusCode;
                result.Message = response.Content;
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ScrapeRestService -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
            return result;
        }

        public bool CheckIfIpIsWorking(string ipAddress, int port)
        {
            bool result = false;

            try
            {

                //ServicePointManager.Expect100Continue = false;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                WebProxy proxy = new WebProxy(ipAddress, port);
                proxy.BypassProxyOnLocal = true;

                var client = new RestClient("http://ip-api.com/json");
                client.Proxy = proxy;

                var request = new RestRequest(Method.GET);
                request.AddHeader("cache-control", "no-cache");
                request.Timeout = 10000;
                IRestResponse response = client.Execute(request);

                Logger.Write($"INFO: Content: {response.Content} success in ScrapeRestService -> DataGrabber.");

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string myip = (string)JObject.Parse(response.Content)["query"];

                    if (myip == ipAddress)
                    {
                        result = true;
                        Logger.Write($"SUCCESS: ip {ipAddress} port {port} success in ScrapeRestService -> DataGrabber.");
                        Console.WriteLine($"SUCCESS: ip {ipAddress} port {port} success in ScrapeRestService -> DataGrabber.");
                    }
                    else
                    {
                        Logger.Write($"FAIL: ip {ipAddress} port {port} failed in ScrapeRestService -> DataGrabber.");
                        Console.WriteLine($"FAIL: ip {ipAddress} port {port} success in ScrapeRestService -> DataGrabber.");
                    }
                }
                else
                {
                    Logger.Write($"INFO: Code: {response.StatusCode} Content: {response.Content} success in ScrapeRestService -> DataGrabber.");
                }


            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ScrapeRestService -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
            return result;
        }



        public ApiResponse GetWebResponse(WebsiteInformation websiteInfo)
        {
            var result = new ApiResponse();

            try
            {

                result = RestHelper.Instance.ServiceFactory(websiteInfo.URL, websiteInfo.HttpVerb, websiteInfo.RequestBody);

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ScrapeRestService -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
            return result;
        }





        //public bool CheckIfIpIsWorking(string host, int port)
        //{
        //    var is_success = false;
        //    try
        //    {
        //        var connsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //        connsock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 200);
        //        System.Threading.Thread.Sleep(500);
        //        var hip = IPAddress.Parse(host);
        //        var ipep = new IPEndPoint(hip, port);
        //        connsock.Connect(ipep);
        //        if (connsock.Connected)
        //        {
        //            is_success = true;
        //        }
        //        connsock.Close();
        //    }
        //    catch (Exception)
        //    {
        //        is_success = false;
        //    }
        //    return is_success;
        //}

    }

}





//private readonly string Url = "https://www.g2.com/g2-meta-data?d=www.g2.com";
//private readonly string Cookie = "__cfduid=d01e1dbe48b6afa63ac25c4f7dba22d2c1570731662;cf_clearance=114f93770535d9803308c2e89d2bd056089d0e4f-1570731666-0-150;events_distinct_id=ee3d5f78-f1b2-4620-91ad-1cc4a4a62a97;_g2_session_id=2b8ded6157fb023f75a22fa3aaf1a7ef;_ga=GA1.2.1896345232.1570731672;_gid=GA1.2.968539086.1570731672;eventsIdentified=ee3d5f78-f1b2-4620-91ad-1cc4a4a62a97;driftt_aid=54a3328f-7abb-4088-9896-9be97af8a078;driftt_aid=54a3328f-7abb-4088-9896-9be97af8a078;DFTT_END_USER_PREV_BOOTSTRAPPED=true;_delighted_fst=1570731977062:{};fs_uid=rs.fullstory.com`J476`4616273593663488:5970380678004736/1602226002;driftt_sid=aef0b0f5-7468-4343-8d29-f3c0d60688d6;__hstc=171774463.fccd7dc3c1249997480ae457e7a69c25.1570795300301.1570795300301.1570795300301.1;hubspotutk=fccd7dc3c1249997480ae457e7a69c25;__hssrc=1;__d_hsutk=true;__hssc=171774463.2.1570795300302;reese84={TOKEN_COMES_HERE};ahoy_visit=be43665e-6426-468b-ac6e-b5544e38bd65;ahoy_visitor=cc67905d-d2ab-42ac-a591-a11a1b5a797e;mp_6b2f1bd84e9deef411802c5b0b2536df_mixpanel={\"distinct_id\": \"16db6e742e44f0-0ea0b5634615f1-5e4f281b-1fa400-16db6e742e554e\",\"$device_id\": \"16db6e742e44f0-0ea0b5634615f1-5e4f281b-1fa400-16db6e742e554e\",\"$initial_referrer\": \"https://www.g2.com/products/scrapestorm/reviews\",\"$initial_referring_domain\": \"www.g2.com\",\"$search_engine\": \"google\"};_gat=1";


//public ApiResponse GetNewCookieToken(string newCookieToken)
//{
//    var result = new ApiResponse();

//    try
//    {
//        ServicePointManager.Expect100Continue = true;
//        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

//        var client = new RestClient(Url) { };
//        var request = new RestRequest(Method.POST);
//        request.AddHeader("cookie", Cookie.Replace("{TOKEN_COMES_HERE}", newCookieToken));
//        request.AddParameter("undefined", string.Format("\"{0}\"", newCookieToken), ParameterType.RequestBody);

//        IRestResponse response = client.Execute(request);
//        result.StatusCode = response.StatusCode;
//        result.Message = response.Content;
//    }
//    catch (Exception ex)
//    {
//        Logger.Write("Exception in GetNewCookieToken -- ScrapeRestService -> DataGrabber. Message: " + ex.Message);
//    }
//    finally
//    {

//    }
//    return result;
//}

//public ApiResponse RefreshNewCookieToken(string oldCookieToken)
//{
//    var result = new ApiResponse();

//    try
//    {
//        string json = GetRefreshCookieTokenJson(oldCookieToken);

//        ServicePointManager.Expect100Continue = true;
//        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

//        var client = new RestClient(Url) { };
//        var request = new RestRequest(Method.POST);

//        request.AddHeader("cookie", Cookie.Replace("{TOKEN_COMES_HERE}", oldCookieToken));
//        request.AddHeader("content-type", "application/json");
//        request.AddHeader("accept", "application/json; charset=utf-8");
//        request.AddParameter("application/json", json, ParameterType.RequestBody);


//        IRestResponse response = client.Execute(request);
//        result.StatusCode = response.StatusCode;
//        result.Message = response.Content;
//    }
//    catch (Exception ex)
//    {
//        Logger.Write("Exception in RefreshNewCookieToken -- ScrapeRestService -> DataGrabber. Message: " + ex.Message);
//    }
//    finally
//    {

//    }
//    return result;
//}

//private string GetRefreshCookieTokenJson(string oldCookieToken)
//{
//    string json = string.Empty;
//    json = File.ReadAllText(ConfigFields.InputFolder + "Scrape-G2-CompSpecs.json");
//    json = json.Replace("{TOKEN_COMES_HERE}", oldCookieToken);
//    return json;
//}

