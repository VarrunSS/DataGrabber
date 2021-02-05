using DataGrabber.LogWriter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber.Models
{

    public class WebScrapeUserInputLibrary
    {
    }

    public class WebScrapeUserInput
    {
        public WebScrapeUserInput()
        {
            WebsiteConfigurationName = string.Empty;
            WebsiteInfo = new List<WebsiteInfo>();
            WaitingTimeAfterPageLoad = 0;
            WaitingTimeAfterPageClick = 0;
            DefaultWebsiteConfiguration = new WebsiteInfo();
            SearchButton = new Element();
            ResetSearchButton = new Element();
            ParentContainer = new Element();
            Container = new Element();
            ProductDetails = new List<TargetElement>() { new TargetElement() { } };
            pagingType = string.Empty;
            Pagination = new Element();
            hasNextButton = string.Empty;
            NextPaginationButton = new Element();
            LoadMoreButton = new Element();
            ActivePageClass = string.Empty;
            DisabledPageClass = string.Empty;
            shouldLimitPaging = string.Empty;
            PagingLimit = 0;
            OutputSheetNumber = 1;
        }


        [JsonProperty("Website Configuration Name")]
        public string WebsiteConfigurationName { get; set; }

        [JsonProperty("Website Info")]
        public List<WebsiteInfo> WebsiteInfo { get; set; }



        [JsonProperty("Does Website Require Input Values? (Yes/ No)")]
        public string doesWebsiteRequireInputValues { get; set; }

        [JsonProperty("Waiting Time After Page Load (in seconds)")]
        public int WaitingTimeAfterPageLoad { get; set; }

        [JsonProperty("Waiting Time After Page Click (in seconds)")]
        public int WaitingTimeAfterPageClick { get; set; }


        [JsonProperty("Should Set Browser Cookie? (Yes/ No)")]
        public string shouldSetBrowserCookie { get; set; }

        public bool ShouldSetBrowserCookie => shouldSetBrowserCookie.SetYesOrNo();

        [JsonProperty("Captcha")]
        public Element Captcha { get; set; }

        [JsonProperty("Default Website Configuration")]
        public WebsiteInfo DefaultWebsiteConfiguration { get; set; }


        [JsonProperty("Does it have 'Search' button to get results? (Yes/ No)")]
        public string hasSearchButton { get; set; }

        public bool HasSearchButton => hasSearchButton.SetYesOrNo();


        [JsonProperty("Search Button")]
        public Element SearchButton { get; set; }

        [JsonProperty("Does it have 'Reset Search' button to get results? (Yes/ No)")]
        public string hasResetSearchButton { get; set; }

        public bool HasResetSearchButton => hasResetSearchButton.SetYesOrNo();

        [JsonProperty("Reset Search Button")]
        public Element ResetSearchButton { get; set; }



        [JsonProperty("Does results open in new tab? (Yes/ No)")]
        public string doesResultsOpenInNewTab { get; set; }

        public bool DoesResultsOpenInNewTab => doesResultsOpenInNewTab.SetYesOrNo();


        [JsonProperty("Products List")]
        public Element ParentContainer { get; set; }

        [JsonProperty("Product")]
        public Element Container { get; set; }

        [JsonProperty("Details to be scraped (Class, ID, TagName, XPath)")]
        public List<TargetElement> ProductDetails { get; set; }

        [JsonProperty("Details to be scraped are available inside an iFrame? (Yes/ No)")]
        public string fetchDataFromIFrame { get; set; }

        public bool FetchDataFromIFrame => fetchDataFromIFrame.SetYesOrNo();

        [JsonProperty("iFrame Element")]
        public Element IFrameElement { get; set; }



        [JsonProperty("Paging Type (NoPaging, LoadOnClick, LoadOnShowMore, LoadOnScroll)")]
        public string pagingType { get; set; }

        [JsonProperty("Pagination Container")]
        public Element Pagination { get; set; }

        [JsonProperty("Does it have 'Next' button in Pagination? (Yes/ No)")]
        public string hasNextButton { get; set; }

        [JsonProperty("Next Button in Pagination")]
        public Element NextPaginationButton { get; set; }

        [JsonProperty("Active Class for current page in Pagination")]
        public string ActivePageClass { get; set; }

        [JsonProperty("Disabled Class for last page in Pagination")]
        public string DisabledPageClass { get; set; }

        [JsonProperty("Load More Button")]
        public Element LoadMoreButton { get; set; }

        [JsonProperty("Should Limit Paging? (Yes/ No)")]
        public string shouldLimitPaging { get; set; }

        [JsonProperty("Paging Limit")]
        public int PagingLimit { get; set; }

        [JsonProperty("Specify the Sheet Number in which Output data should be written")]
        public int OutputSheetNumber { get; set; }



        [JsonProperty("Should Fetch Data from Details Page? (Yes/ No)")]
        public string shouldFetchDataFromDetailsPage { get; set; }

        [JsonProperty("Target Name for Input URLs")]
        public WebsiteInfo TargetNameForInputURL { get; set; }

        [JsonProperty("Detailed Information Page")]
        public WebScrapeUserInput DetailedInformationPage { get; set; }


        [JsonProperty("Should send mail attached with output file ? (Yes/ No)")]
        public string shouldSendMailOfOutputData { get; set; }

        [JsonProperty("Mail Information")]
        public MailInformation MailInfo { get; set; }



        [JsonProperty("Should Disable JavaScript in Browser? (Yes/ No)")]
        public string shouldDisableJavaScript { get; set; }

        public bool ShouldDisableJavaScript => shouldDisableJavaScript.SetYesOrNo();


        [JsonProperty("Should Rotate Proxy IP? (Yes/ No)")]
        public string shouldRotateProxyIP { get; set; }

        public bool ShouldRotateProxyIP => shouldRotateProxyIP.SetYesOrNo();


        [JsonProperty("Should Pick Proxy IP From List? (Yes/ No)")]
        public string shouldPickProxyIPFromList { get; set; }

        public bool ShouldPickProxyIPFromList => shouldPickProxyIPFromList.SetYesOrNo();


        [JsonProperty("Should Set Browser Width & Height? (Yes/ No)")]
        public string shouldSetBrowserWidthHeight { get; set; }

        public bool ShouldSetBrowserWidthHeight => shouldSetBrowserWidthHeight.SetYesOrNo();

        [JsonProperty("Browser Dimension")]
        public BrowserDimension BrowserScreenDimension { get; set; }

        [JsonProperty("Scraping Mechanism (Selenium, RestClient)")]
        public string ScrapingMechanism { get; set; }



        public bool ShouldFetchDataFromDetailsPage => shouldFetchDataFromDetailsPage.SetYesOrNo();

        public bool DoesWebsiteRequireInputValues => doesWebsiteRequireInputValues.SetYesOrNo();

        public PaginationType PagingType
        {
            get
            {
                switch (pagingType)
                {
                    case "NoPaging":
                        return PaginationType.NoPaging;

                    case "LoadOnClick":
                        return PaginationType.LoadOnClick;

                    case "LoadOnShowMore":
                        return PaginationType.LoadOnShowMore;

                    case "LoadOnScroll":
                        return PaginationType.LoadOnScroll;

                }

                return PaginationType.NoPaging;
            }
        }

        public bool HasNextButton => hasNextButton.SetYesOrNo();

        public bool ShouldLimitPaging => shouldLimitPaging.SetYesOrNo();

        public bool ShouldSendMailOfOutputData => shouldSendMailOfOutputData.SetYesOrNo();

    }

    public class WebsiteInfo
    {
        public WebsiteInfo()
        {
            WebsiteName = string.Empty;
            WebsiteURL = string.Empty;
            InputInfo = new List<InputTargetElement>();

            WebsiteNamePrefix = string.Empty;

        }

        [JsonProperty("Website Name")]
        public string WebsiteName { get; set; }

        [JsonProperty("Website URL")]
        public string WebsiteURL { get; set; }

        [JsonProperty("Website URLs")]
        public string[] WebsiteURLs { get; set; }

        [JsonProperty("Input Info")]
        public List<InputTargetElement> InputInfo { get; set; }

        [JsonProperty("Website Name Prefix")]
        public string WebsiteNamePrefix { get; set; }

        [JsonProperty("Target Name")]
        public string TargetName { get; set; }

        [JsonProperty("Input Values")]
        public string[] InputValues { get; set; }


        [JsonProperty("Webscrape Type (SingleURL, MultipleURLs, MultipleInputs)")]
        public string webScrapeType { get; set; }

        public ScrapeType WebScrapeType
        {
            get
            {
                ScrapeType result = ScrapeType.SingleURL;

                try
                {
                    switch (webScrapeType.ToLower())
                    {
                        case "singleurl": return ScrapeType.SingleURL;
                        case "multipleurls": return ScrapeType.MultipleURLs;
                        case "multipleinputs": return ScrapeType.MultipleInputs;
                        default: break;
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



        [JsonProperty("Website URLs (With Request Body)")]
        public List<UrlWithBody> WebsiteURLsWithBody { get; set; }

        [JsonProperty("Website Pattern Configuration")]
        public WebsitePattern WebsitePatternConfig { get; set; }

    }

    public class Element
    {
        public Element()
        {
            ElementType = string.Empty;
            ElementIdentifier = string.Empty;
        }

        [JsonProperty("Element Type")]
        public string ElementType { get; set; }

        [JsonProperty("Element Identifier")]
        public string ElementIdentifier { get; set; }


        public string GetXPath()
        {
            string result = string.Empty;

            switch (ElementType.ToLower())
            {
                case "class":
                    result = XPathType.Relative.ForClass(ElementIdentifier);
                    break;

                case "id":
                    result = XPathType.Relative.ForID(ElementIdentifier);
                    break;

                case "tagname":
                    result = XPathType.Relative.ForTag(ElementIdentifier);
                    break;

                case "xpath":
                    result = XPathType.Relative.ForXPath(ElementIdentifier);
                    break;

                default:
                    break;

            }

            return result;

        }

    }

    public class TargetElement : Element
    {
        public TargetElement() : base()
        {
            TargetName = string.Empty;
            TargetType = string.Empty;
            AttributeName = string.Empty;
            TargetValue = string.Empty;
            RemoveText = string.Empty;
            Separator = ",";
        }

        [JsonProperty("Target Name")]
        public string TargetName { get; set; }

        [JsonProperty("Target Type")]
        public string TargetType { get; set; }

        [JsonProperty("Attribute Name (src / href / data-)")]
        public string AttributeName { get; set; }

        [JsonProperty("Value")]
        public string TargetValue { get; set; }

        [JsonProperty("Remove Text")]
        public string RemoveText { get; set; }

        [JsonProperty("Separator")]
        public string Separator { get; set; }

        [JsonProperty("Only Check if element exists? (Yes/ No)")]
        public string onlyCheckIfElementExists { get; set; }

        public bool OnlyCheckIfElementExists => onlyCheckIfElementExists.SetYesOrNo();

        [JsonProperty("Waiting Time After Element Change (in seconds)")]
        public int WaitingTimeAfterElementChange { get; set; }


        [JsonProperty("Get Child Element? (Yes/ No)")]
        public string getChildElement { get; set; }

        public bool GetChildElement => getChildElement.SetYesOrNo();

        [JsonProperty("Child Node")]
        public TargetElement ChildNode { get; set; }


        [JsonProperty("Should Check Element In Body? (Yes/ No)")]
        public string shouldCheckElemInBody { get; set; }

        public bool ShouldCheckElemInBody => shouldCheckElemInBody.SetYesOrNo();

    }

    public class InputTargetElement : TargetElement
    {
        public InputTargetElement() : base()
        {
            //PartnerElement = new InputTargetElement();
            ChildTargetName = string.Empty;
            hasChild = string.Empty;
            IsComplete = false;
        }

        [JsonProperty("Partner Element")]
        public InputTargetElement PartnerElement { get; set; }


        //[JsonProperty("Should Replace Value? (Yes/ No)")]
        //public string shouldReplaceValue { get; set; }

        //public bool ShouldReplaceValue => shouldReplaceValue.SetYesOrNo();

        [JsonProperty("Child Target Name")]
        public string ChildTargetName { get; set; }

        [JsonProperty("Has Child?")]
        public string hasChild { get; set; }

        public bool HasChild => hasChild.SetYesOrNo();

        public bool IsComplete { get; set; }

    }

    public class TargetInput
    {
        public TargetInput()
        {
        }



    }

    public class BrowserDimension
    {
        [JsonProperty("Width")]
        public int Width { get; set; }

        [JsonProperty("Height")]
        public int Height { get; set; }

    }

    public class UrlWithBody
    {
        [JsonProperty("URL")]
        public string Url { get; set; }

        [JsonProperty("Body")]
        public string Body { get; set; }

    }


    public class WebsitePattern
    {
        [JsonProperty("Http Verb")]
        public string httpVerb { get; set; }

        [JsonProperty("Start Index")]
        public int StartIndex { get; set; }

        [JsonProperty("Current Page")]
        public int CurrentPage { get; set; }

        [JsonProperty("Items Per Page")]
        public int ItemsPerPage { get; set; }

        [JsonProperty("Pattern Settings")]
        public List<PatternSetting> PatternSettings { get; set; }

        public HttpVerbType HttpVerb
        {
            get
            {
                switch (httpVerb)
                {
                    case "GET": return HttpVerbType.GET;
                    case "POST": return HttpVerbType.POST;
                    default: return HttpVerbType.GET;
                }
            }
        }

    }

    public class PatternSetting
    {
        [JsonProperty("Url Pattern")]
        public string UrlPattern { get; set; }

        [JsonProperty("Total Items")]
        public int TotalItems { get; set; }

        [JsonProperty("Request Body")]
        public List<RequestBody> RequestBodyParams { get; set; }

    }

    public class RequestBody
    {
        [JsonProperty("Param Name")]
        public string ParamName { get; set; }

        [JsonProperty("Value")]
        public string Value { get; set; }

    }


    public enum ScrapeType
    {
        SingleURL,
        MultipleURLs,
        MultipleInputs
    }

    public enum HttpVerbType
    {
        GET,
        POST,
        PUT


    }



}
