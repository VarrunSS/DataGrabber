using DataGrabberV2.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabberV2.Utility
{
    public sealed class RestHelper
    {

        private RestHelper() { }

        public static RestHelper Instance { get; } = new RestHelper();

        public ApiResponse ServiceFactory(string URL, HttpVerbType methodType, string data = "")
        {
            ApiResponse response = new ApiResponse();
            RestRequest request = null;

            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                RestClient client = new RestClient(URL);

                // Assign rest request value based on type
                switch (methodType)
                {
                    case HttpVerbType.GET:
                        {
                            request = new RestRequest(Method.GET);
                        }
                        break;
                    case HttpVerbType.POST:
                        {
                            request = new RestRequest(Method.POST);
                            request.AddHeader("Content-Type", "application/json");
                            request.AddParameter("application/json", data, ParameterType.RequestBody);
                            break;
                        }
                    case HttpVerbType.PUT:
                        {
                            request = new RestRequest(Method.PUT);
                            request.AddHeader("Content-Type", "application/json");
                            request.AddParameter("application/json", data, ParameterType.RequestBody);
                        }
                        break;

                    default:
                        break;
                }

                IRestResponse restResponse = client.Execute(request);
                response.StatusCode = restResponse.StatusCode;
                response.Message = restResponse.Content;

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {

            }
            return response;
        }

    }
}
