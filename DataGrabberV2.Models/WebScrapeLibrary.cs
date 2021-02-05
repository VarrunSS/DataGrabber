
using DataGrabberV2.LogWriter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabberV2.Models
{

    public class WebScrapeLibrary
    {

    }

    public class WebScrapeInput
    {
        public WebScrapeInput()
        {
            Websites = new List<WebsiteInformation>();
            ProductDetails = new List<ElementMapping>();
        }

        public string ScrapeID { get; set; }
        
        public string WebsiteConfigurationName { get; set; }

        public List<WebsiteInformation> Websites { get; set; }

        public WebsiteInformation Website { get; set; }



        public bool DoesWebsiteRequireInputValues { get; set; }

        public int WaitingTimeAfterPageLoad { get; set; }

        public int WaitingTimeAfterPageClick { get; set; }

        public bool ShouldSetBrowserCookie { get; set; }

        public ElementMapping Captcha { get; set; }

        public bool HasSearchButton { get; set; }

        public ElementMapping SearchButton { get; set; }

        public bool HasResetSearchButton { get; set; }

        public ElementMapping ResetSearchButton { get; set; }



        public bool DoesResultsOpenInNewTab { get; set; }

        //public ElementMapping DismissMessageElement { get; set; }


        public ElementMapping ParentContainer { get; set; }

        public ElementMapping Container { get; set; }

        public List<ElementMapping> ProductDetails { get; set; }

        public bool FetchDataFromIFrame { get; set; }

        public ElementMapping IFrameElement { get; set; }



        public PaginationType PagingType { get; set; }

        public bool HasNextButton { get; set; }

        public ElementMapping NextPaginationButton { get; set; }

        public ElementMapping Pagination { get; set; }

        public string ActivePageClass { get; set; }

        public string DisabledPageClass { get; set; }

        public ElementMapping LoadMoreButton { get; set; }

        public bool ShouldLimitPaging { get; set; }

        public int PagingLimit { get; set; }

        public int OutputSheetNumber { get; set; }


        public bool ShouldFetchDataFromDetailsPage { get; set; }

        public WebsiteInformation TargetNameForInputURL { get; set; }

        public WebScrapeInput DetailedInformationPage { get; set; }




        public bool ShouldSendMailOfOutputData { get; set; }

        public MailInformation MailInfo { get; set; }


        public bool ShouldDisableJavaScript { get; set; }

        public bool ShouldRotateProxyIP { get; set; }

        public bool ShouldPickProxyIPFromList { get; set; }

        public bool ShouldSetBrowserWidthHeight { get; set; }

        public BrowserDimension BrowserScreenDimension { get; set; }


        public string ScrapingMechanism { get; set; }


        public void ClearProductDetails()
        {
            foreach (ElementMapping mapping in ProductDetails)
            {
                if (!mapping.IsInputAttribute)
                    mapping.Value = "";
            }
        }

        public WebScrapeOutput Format(int Page, string UniqueID, string MappingID)
        {
            WebScrapeOutput output = new WebScrapeOutput();

            output = new WebScrapeOutput()
            {
                UniqueID = UniqueID,
                MappingID = MappingID,
                WebsiteURL = Website,
                ProductDetails = ProductDetails,
                PageNumber = Page,
                OutputSheetNumber = OutputSheetNumber,
                CompletedOn = DateTime.Now.ToString()
            };

            return output.Clone();
        }





    }

    public class ElementMapping
    {
        public ElementMapping()
        {
            TargetName = string.Empty;
            TargetType = string.Empty;
            XPath = string.Empty;
            Value = string.Empty;
            IsInputAttribute = false;
            AttributeName = string.Empty;
            RemoveText = string.Empty;
            Separator = ",";
            WaitingTimeAfterElementChange = 0;
        }

        public string TargetName { get; set; }

        public string TargetType { get; set; }

        public string XPath { get; set; }

        public string Value { get; set; }

        public bool IsInputAttribute { get; set; }

        public string AttributeName { get; set; }

        public string RemoveText { get; set; }

        public string Separator { get; set; }

        public bool OnlyCheckIfElementExists { get; set; }

        public ElementMapping PartnerElement { get; set; }

        public int WaitingTimeAfterElementChange { get; set; }


        public bool GetChildElement { get; set; }

        public ElementMapping ChildNode { get; set; }

        public bool ShouldCheckElemInBody { get; set; }

    }

    public class WebsiteInformation
    {
        public WebsiteInformation()
        {
            Name = string.Empty;
            URL = string.Empty;
            InputInfo = new List<ElementMapping>();
            WebsiteNamePrefix = string.Empty;
            HttpVerb = HttpVerbType.GET;
        }

        public string Name { get; set; }

        public string URL { get; set; }

        public string RequestBody { get; set; }

        public List<ElementMapping> InputInfo { get; set; }

        public string WebsiteNamePrefix { get; set; }

        public string TargetName { get; set; }

        public List<KeyValuePair<Identifier, string>> InputValues { get; set; }

        public List<KeyValuePair<Identifier, UrlPair>> URLs { get; set; }

        public string webScrapeType { get; set; }

        public ScrapeType WebScrapeType
        {
            get
            {
                ScrapeType result = ScrapeType.SingleURL;

                try
                {
                    if (!string.IsNullOrEmpty(webScrapeType))
                    {
                        switch (webScrapeType.ToLower())
                        {
                            case "singleurl": return ScrapeType.SingleURL;
                            case "multipleurls": return ScrapeType.MultipleURLs;
                            case "multipleinputs": return ScrapeType.MultipleInputs;
                            default: break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write("Exception in WebScrapeType Prop -- WebScrapeUserInput -> DataGrabber. Message: " + ex.Message);
                }
                finally
                {

                }

                return result;
            }
        }

        public string UniqueID { get; set; }

        public string MappingID { get; set; }

        public HttpVerbType HttpVerb { get; set; }

      
    }


    public struct Identifier
    {
        public string UniqueID;

        public string MappingID;
    }

    public struct UrlPair
    {
        public string Url;

        public string RequestBody;
    }

    public class WebScrapeOutput
    {
        public string UniqueID { get; set; }

        public string MappingID { get; set; }

        public WebsiteInformation WebsiteURL { get; set; }

        public List<ElementMapping> ProductDetails { get; set; }

        public int PageNumber { get; set; }

        public int OutputSheetNumber { get; set; }

        public string CompletedOn { get; set; }

    }

    public enum PaginationType
    {
        NoPaging,
        LoadOnClick,
        LoadOnShowMore,
        LoadOnScroll
    }

    public enum XPathType
    {
        Absolute,
        Relative
    }

    #region ModelHelper 



    #endregion

}
