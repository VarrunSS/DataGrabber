using System;
using System.Collections.Generic;
using System.Text;

namespace DataGrabberV2.Models.RestLibrary.ConfigTool
{


    public class SiteConfiguration
    {
        public string websiteConfigurationName { get; set; }
        public string scrapingMechanism { get; set; }
        public bool shouldRotateProxy { get; set; }
        public bool requireInputValues { get; set; }
        public bool shouldDisableJavaScript { get; set; }
        public string waitingTimeAfterPageLoad { get; set; }
    }

    public class WebsiteDetail
    {
        public string websiteNamePrefix { get; set; }
        public string webscrapeType { get; set; }
        public string websiteURL { get; set; }
        public List<string> websiteURLs { get; set; }
        public object dbWebsiteURLs { get; set; }
        public bool doesHaveSearchButton { get; set; }
        public string searchButtonPathType { get; set; }
        public string searchButton { get; set; }
        public bool doesHaveResetButton { get; set; }
        public string resetButtonPathType { get; set; }
        public string resetButton { get; set; }
        public List<InputField> inputFields { get; set; }
        public List<object> dbInputFields { get; set; }
    }

    public class Field
    {
        public string fieldName { get; set; }
        public string fieldPathType { get; set; }
        public string fieldPath { get; set; }
        public bool shouldCheckElementInBody { get; set; }
        public string removeText { get; set; }
        public string attributeName { get; set; }
    }
    public class InputField
    {
        public string fieldName { get; set; }
        public string fieldPathType { get; set; }
        public string fieldPath { get; set; }
        public string targetType { get; set; }
        public bool hasPartnerElement { get; set; }
        public string waitingTimeAfterElementChange { get; set; }
    }
    public class ProductDetail
    {
        public string overallContainerPathType { get; set; }
        public string overallContainer { get; set; }
        public string productContainerPathType { get; set; }
        public string productContainer { get; set; }
        public List<Field> fields { get; set; }
        public List<object> dbFields { get; set; }
    }

    public class PaginationDetail
    {
        public string pagingType { get; set; }
        public string paginationContainerPathType { get; set; }
        public string paginationContainer { get; set; }
        public bool doesHaveNextButton { get; set; }
        public string nextButtonPathType { get; set; }
        public string nextButton { get; set; }
        public string activeClassForCurrentPage { get; set; }
        public string disabledClassForLastPage { get; set; }
        public bool shouldLimitPaging { get; set; }
        public string pagingLimit { get; set; }
    }

    public class MailingInformation
    {
        public bool shouldSendMail { get; set; }
        public string mailToAddress { get; set; }
        public string mailCCAddress { get; set; }
        public string mailBCCAddress { get; set; }
    }

    public class FullConfigurationDetail
    {
        public bool isSuccess { get; set; }
        public string message { get; set; }
        public string loginUser { get; set; }
        public SiteConfiguration siteConfiguration { get; set; }
        public WebsiteDetail websiteDetail { get; set; }
        public ProductDetail productDetail { get; set; }
        public PaginationDetail paginationDetail { get; set; }
        public MailingInformation mailingInformation { get; set; }
        public string configGUID { get; set; }
        public string configName { get; set; }
        public string configType { get; set; }
        public string url { get; set; }
        public string createdBy { get; set; }
        public string createdOn { get; set; }
        public string updatedBy { get; set; }
        public string updatedOn { get; set; }
        public string status { get; set; }
        public bool shouldShowDownload { get; set; }
        public string scrapeID { get; set; }
    }


}
