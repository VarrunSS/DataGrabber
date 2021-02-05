using DataGrabberV2.BusinessLogic.Contract;
using DataGrabberV2.LogWriter;
using DataGrabberV2.Models;
using DataGrabberV2.Utility;
using GoogleRecaptchaV2.BusinessLogic.Contract;
using GoogleRecaptchaV2.BusinessLogic.Factory;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace DataGrabberV2.BusinessLogic
{
    public sealed class SeleniumScraper : IScraper
    {

        private SeleniumScraper() { }

        public static SeleniumScraper Instance { get; } = new SeleniumScraper();

        private readonly double _MaxBrowserWaitTimeInMinutes_ = ConfigFields.MaxBrowserWaitTimeInMinutes;
        private readonly double _WaitTimeToLoadNewTabInSeconds_ = ConfigFields.WaitTimeToLoadNewTabInSeconds;
        private readonly double _WaitTimeToLoadPageInSeconds_ = ConfigFields.WaitTimeToLoadPageInSeconds;
        private readonly double _WaitTimeToLoadResultsAfterClickInSeconds_ = ConfigFields.WaitTimeToLoadResultsAfterClickInSeconds;
        private readonly int _MaxDegreeOfParallelism_ = ConfigFields.MaxDegreeOfParallelism;
        private readonly int _InputSplitSize_ = ConfigFields.InputSplitSize;

        public string CookieValue = string.Empty;

        public List<OpenQA.Selenium.Cookie> AllCookies = new List<OpenQA.Selenium.Cookie>();


        private void TestingGoogleCaptcha(ChromeDriver driver)
        {

            ICaptchaSolver solver = GoogleRecaptchaCreator.GetInstance("audio");
            solver.Initialize(ConfigFields.GoogleCaptchaFolder, driver);
            solver.StartProcess();

            if (solver.Result.IsCaptchaSolved)
            {
                Console.WriteLine(solver.Result.CaptchaText);
            }


        }


        public List<WebScrapeOutput> ProcessRequest(WebScrapeInput input)
        {
            List<WebScrapeOutput> result = new List<WebScrapeOutput>();

            try
            {
                // init chrome browser
                SetBrowserOptions(input, out ChromeDriver driver);

                // Set the default page load timeout
                //driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(10);

                // go to target URL
                driver.Navigate().GoToUrl(input.Website.URL);

                //TestingGoogleCaptcha(driver);

                SetCookie(driver, input, false);
                // close cookie confirmation box
                //CloseCookiePopup(driver, input);

                ProcessRequestFor(driver, input, ref result);

                // close chrome browser
                driver.Close();
                driver.Quit();
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ProcessRequest -- ScrapeProductsList -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }
            return result;
        }

        private void ProcessRequestFor(ChromeDriver driver, WebScrapeInput input, ref List<WebScrapeOutput> result)
        {
            try
            {
                switch (input.Website.WebScrapeType)
                {
                    case ScrapeType.SingleURL:
                        ProcessRequestForSingleURL(driver, input, ref result);
                        break;

                    case ScrapeType.MultipleURLs:
                        ProcessRequestForMultipleURLs(driver, input, ref result);
                        break;

                    case ScrapeType.MultipleInputs:
                        if (input.DoesWebsiteRequireInputValues)
                        {
                            ProcessRequestForMultipleInputs(driver, input, ref result);
                        }
                        break;
                }

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ProcessRequestForSingleURL -- ScrapeProductsListInput -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }
        }

        private void ProcessRequestForSingleURL(ChromeDriver driver, WebScrapeInput input, ref List<WebScrapeOutput> result)
        {
            string htmlData = string.Empty;
            HtmlDocument objResultSet = new HtmlDocument();
            try
            {
                bool isDataLoaded = true;

                try
                {
                    // inital wait after page load
                    driver.WaitForSomeSeconds(input.WaitingTimeAfterPageLoad);

                    // wait for content to be loaded through ajax 
                    WebDriverWait wait = driver.GetWebDriverWait();
                    By productContainer = By.XPath(input.Container.XPath);
                    wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(productContainer));
                }
                catch (Exception)
                {

                }

                // check if page depends on input values
                if (input.DoesWebsiteRequireInputValues)
                {
                    isDataLoaded = LoadPageBasedOnInput(driver, input);
                }

                if (isDataLoaded)
                {

                    Console.WriteLine($"URL: {driver.Url} Started");

                    int currentPage = 1;
                    bool hasMorePage = true;

                    do
                    {
                        // get full html
                        htmlData = driver.GetPageSource();

                        // load html in agility pack
                        objResultSet.LoadHtml(htmlData);

                        // get all details
                        input.Website.URL = driver.Url;


                        List<WebScrapeOutput> pageDetails = GetContent(driver, objResultSet, input, currentPage);

                        lock (result)
                        {
                            // add to existing results
                            result.AddRange(pageDetails.Clone());
                        }

                        // next page
                        hasMorePage = CheckIfMorePagesAreAvailable(driver, input, currentPage);

                        try
                        {
                            // inital wait after page load
                            driver.WaitForSomeSeconds(input.WaitingTimeAfterPageClick);

                        }
                        catch (Exception)
                        {
                        }

                        ++currentPage;
                    }
                    while (hasMorePage);


                    Console.WriteLine($"URL: {driver.Url} Ended");
                }

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ProcessRequestForSingleURL -- ScrapeProductsListInput -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }
        }

        private void ProcessRequestForMultipleURLs(ChromeDriver driver, WebScrapeInput input, ref List<WebScrapeOutput> result)
        {
            // loop through all URLs
            foreach (var pair in input.Website.URLs)
            {
                string uGuid = pair.Key.UniqueID;
                string mGuid = pair.Key.MappingID;
                string url = pair.Value.Url;
                string body = pair.Value.RequestBody;

                // save guid
                input.Website.UniqueID = uGuid;
                input.Website.MappingID = mGuid;

                // go to url in same tab
                driver.Navigate().GoToUrl(url);

                SetCookie(driver, input, true);

                ProcessRequestForSingleURL(driver, input, ref result);

                SetCookie(driver, input, true);
            }

            //foreach (string[,] url in input.Website.URLs)
            //{
            //    // go to url in same tab
            //    driver.Navigate().GoToUrl(url);

            //    ProcessRequestForSingleURL(driver, input, ref result);
            //}

            return;
        }

        private void ProcessRequestForMultipleInputs(ChromeDriver driver, WebScrapeInput input, ref List<WebScrapeOutput> result)
        {
            // loop through all input values
            foreach (var pair in input.Website.InputValues)
            {
                string uGuid = pair.Key.UniqueID;
                string mGuid = pair.Key.MappingID;
                string value = pair.Value;

                // save guid
                input.Website.UniqueID = uGuid;
                input.Website.MappingID = mGuid;

                var inputElem = input.Website.InputInfo.Where(v => v.TargetName == input.Website.TargetName).FirstOrDefault();
                if (inputElem != null)
                {
                    inputElem.Value = value;
                }

                // set first column as input value
                var col = input.ProductDetails.Where(v => v.TargetName == input.Website.TargetName).FirstOrDefault();
                if (col != null)
                {
                    col.Value = value;
                }

                ProcessRequestForSingleURL(driver, input, ref result);
            }

            return;
        }


        private List<WebScrapeOutput> GetContent(ChromeDriver driver, HtmlDocument htmlDoc, WebScrapeInput input, int pageNumber)
        {
            List<WebScrapeOutput> result = new List<WebScrapeOutput>();

            try
            {

                // check if data should be fetched from iframe
                if (input.FetchDataFromIFrame)
                {
                    // check if iframe elem is available
                    By iframeElemPath = By.XPath(input.IFrameElement.XPath);
                    if (!driver.IsElementPresent(iframeElemPath))
                    {
                        Logger.Write($"ERROR: Unable to find iframe element for {input.Website.Name} in GetContent-- ScrapeProductsList -> DataGrabberV2.");
                    }
                    else
                    {
                        // switch to iframe to get data
                        driver.SwitchTo().Frame(driver.FindElement(iframeElemPath));
                        htmlDoc.LoadHtml(driver.PageSource);
                    }

                }


                var bodyContainer = htmlDoc.DocumentNode.SelectSingleNode("//body");
                var parentContainers = htmlDoc.DocumentNode.SelectNodes(input.ParentContainer.XPath);

                if (parentContainers == null)
                {
                    // check for 3 times if parent is not found
                    int counter = 1, maxLoop = 3;
                    bool shouldStop = false;

                    while (counter <= maxLoop && !shouldStop)
                    {
                        Logger.Write($"Info: Retrying count: {counter}; Parent Container not found for WebsiteName: {input.Website.Name}; URL: {input.Website.URL} in ProcessRequest -- ScrapeProductsList -> DataGrabberV2. ");

                        driver.Navigate().Refresh();
                        driver.WaitForSomeSeconds(5 * counter);

                        parentContainers = htmlDoc.DocumentNode.SelectNodes(input.ParentContainer.XPath);
                        if (parentContainers != null)
                        {
                            shouldStop = true;
                        }

                        counter++;
                    }

                }

                if (parentContainers == null)
                {
                    Logger.Write($"ERROR: Parent Container not found for WebsiteName: {input.Website.Name}; URL: {input.Website.URL} in ProcessRequest -- ScrapeProductsList -> DataGrabberV2.");
                }
                else
                {

                    // loop through all parents
                    foreach (HtmlNode parentContainer in parentContainers)
                    {
                        var productContainers = parentContainer.SelectNodes(input.Container.XPath);

                        if (productContainers == null)
                        {
                            Logger.Write($"ERROR: Product Container not found for WebsiteName: {input.Website.Name}; URL: {input.Website.URL}; in ProcessRequest -- ScrapeProductsList -> DataGrabberV2.");
                        }
                        else
                        {
                            // loop through all products
                            foreach (HtmlNode container in productContainers)
                            {
                                // clear input product details
                                input.ClearProductDetails();

                                // get all product details -- which are not input attributes
                                foreach (ElementMapping mapping in input.ProductDetails.Where(v => !v.IsInputAttribute))
                                {
                                    mapping.Value = GetHTMLContent(driver, container, bodyContainer, mapping, input.Website.URL);
                                }

                                // set result if value is not empty
                                if (!string.IsNullOrEmpty(input.ProductDetails.Where(v => !v.IsInputAttribute).FirstOrDefault().Value))
                                {
                                    result.Add(input.Format(pageNumber, input.Website.UniqueID, input.Website.MappingID)); // format to output
                                }
                            }
                        }
                    }
                }

                if (input.FetchDataFromIFrame)
                {
                    // switch back to default content
                    driver.SwitchTo().DefaultContent();
                    htmlDoc.LoadHtml(driver.PageSource);
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in GetContent -- ScrapeProductsList -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }
            return result;
        }

        private string GetHTMLContent(ChromeDriver driver, HtmlNode container, HtmlNode bodyContainer, ElementMapping mapping, string baseURL)
        {
            string result = string.Empty;

            try
            {
                HtmlNode objNode = container.SelectNodes(mapping.XPath)?.FirstOrDefault();

                if (objNode == null)
                {
                    // check in entire body
                    if (mapping.ShouldCheckElemInBody)
                    {
                        objNode = bodyContainer.SelectNodes(mapping.XPath)?.FirstOrDefault();
                    }
                    // if attribute is present at parent node level
                    else if (!string.IsNullOrEmpty(mapping.AttributeName))
                    {
                        objNode = container;
                    }
                }

                if (objNode != null)
                {
                    // return Yes, if 'only check if element exists' is true. else empty
                    if (mapping.OnlyCheckIfElementExists)
                    {
                        return "Yes";
                    }

                    // remove comments
                    DataHelper.RemoveComments(objNode);

                    // get child node and combine the value
                    if (mapping.GetChildElement)
                    {
                        HtmlNodeCollection childNodes = objNode.SelectNodes(mapping.ChildNode.XPath);

                        if (childNodes == null)
                        {
                            Logger.Write($"ERROR: Child Node Path {mapping.ChildNode.XPath} not found in GetHTMLContent -- ScrapeProductsList -> DataGrabberV2. ");
                        }
                        else
                        {
                            foreach (HtmlNode child in childNodes)
                            {
                                string childResult = string.Empty;

                                if (string.IsNullOrEmpty(mapping.ChildNode.AttributeName))
                                {
                                    childResult += child.InnerText.Trim();
                                }
                                else
                                {
                                    childResult = child.GetAttributeValue(mapping.ChildNode.AttributeName, "");
                                    childResult = DataHelper.FormatTextFromHtmlContent(childResult);

                                    // get full page url
                                    if (child.Name == "a" && mapping.ChildNode.AttributeName.ToLower() == "href")
                                    {
                                        // The address of the page you crawled
                                        var uri = new Uri(baseURL);
                                        childResult = new Uri(uri, childResult).AbsoluteUri;
                                    }
                                    // for image -- get src
                                    else if (child.Name == "img" && mapping.ChildNode.AttributeName.ToLower() == "src" && !childResult.StartsWith("http"))
                                    {
                                        // The address of the page you crawled
                                        var uri = new Uri(baseURL);
                                        childResult = new Uri(uri, childResult).AbsoluteUri;
                                    }
                                }

                                result += childResult + mapping.ChildNode.Separator + " ";
                            }

                            result = result.Trim().TrimEnd(mapping.ChildNode.Separator.ToCharArray());
                            result = DataHelper.FormatTextFromHtmlContent(result);

                            // remove part of text if configured
                            if (!string.IsNullOrEmpty(mapping.ChildNode.RemoveText))
                            {
                                result = result.Replace(mapping.ChildNode.RemoveText, string.Empty).Trim();
                            }
                        }
                    }
                    else
                    {
                        // get inner text
                        if (string.IsNullOrEmpty(mapping.AttributeName))
                        {
                            // set text
                            result = objNode.InnerText.Trim();
                            result = DataHelper.FormatTextFromHtmlContent(result);

                            // remove part of text if configured
                            if (!string.IsNullOrEmpty(mapping.RemoveText))
                            {
                                result = result.Replace(mapping.RemoveText, string.Empty).Trim();
                            }
                        }
                        else
                        {

                            result = objNode.GetAttributeValue(mapping.AttributeName, "");
                            result = DataHelper.FormatTextFromHtmlContent(result);

                            // get full page url
                            if (objNode.Name == "a" && mapping.AttributeName.ToLower() == "href")
                            {
                                // The address of the page you crawled
                                var uri = new Uri(baseURL);
                                result = new Uri(uri, result).AbsoluteUri;
                            }
                            // for image -- get src
                            else if (objNode.Name == "img" && mapping.AttributeName.ToLower() == "src" && !result.StartsWith("http"))
                            {
                                // The address of the page you crawled
                                var uri = new Uri(baseURL);
                                result = new Uri(uri, result).AbsoluteUri;
                            }

                            // remove part of text if configured
                            if (!string.IsNullOrEmpty(mapping.RemoveText))
                            {
                                result = result.Replace(mapping.RemoveText, string.Empty).Trim();
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in GetHTMLContent -- ScrapeProductsList -> DataGrabberV2. Message: " + ex.Message);
                return result;
            }
            finally
            {

            }
            return result;
        }




        public void ProcessDetailRequest(WebScrapeInput input, ref List<WebScrapeOutput> result)
        {
            try
            {
                var detailInput = input.DetailedInformationPage;
                string TargetNameForInputURL = detailInput.TargetNameForInputURL.TargetName;

                // add unique id to each result if it does not have one
                AddUniqueIdentifier(ref result);

                // format input based on ScrapeType
                FormatDataBasedOnScrape(result, TargetNameForInputURL, ref detailInput);

                // get details
                List<WebScrapeOutput> output = new List<WebScrapeOutput>();

                Parallel.ForEach(detailInput.Websites,
                    new ParallelOptions { MaxDegreeOfParallelism = _MaxDegreeOfParallelism_ },
                    (urlData) =>
                    {
                        WebScrapeInput inp = detailInput.Clone();
                        inp.Website = urlData;

                        List<WebScrapeOutput> data = ProcessRequest(inp);

                        lock (output)
                            output.AddRange(data.Clone());
                    });

                List<WebScrapeOutput> addlOutput = new List<WebScrapeOutput>();

                // combine output with result
                foreach (var res in result)
                {
                    var details = output.Where(v => v.WebsiteURL.MappingID == res.UniqueID).ToList();

                    if (details != null)
                    {
                        int serialNum = 0;

                        foreach (var detail in details)
                        {
                            var products = detail.ProductDetails.Clone();

                            if (res.OutputSheetNumber == detail.OutputSheetNumber)
                            {
                                res.ProductDetails.AddRange(products);
                            }
                            else
                            {

                                // add serial number if matching products is > 1
                                if (details.Count > 1)
                                {
                                    ++serialNum;

                                    products.Add(new ElementMapping()
                                    {
                                        TargetName = "Position",
                                        Value = serialNum.ToString()
                                    });
                                }

                                addlOutput.Add(new WebScrapeOutput()
                                {
                                    WebsiteURL = detail.WebsiteURL,
                                    ProductDetails = products,
                                    OutputSheetNumber = detail.OutputSheetNumber,
                                    PageNumber = detail.PageNumber,
                                    CompletedOn = detail.CompletedOn
                                });
                            }
                        }

                    }
                }
                //}

                // combine both additional output with result
                result.AddRange(addlOutput);

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ProcessDetailRequest -- ScrapeProductsList -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }

            return;
        }

        private void AddUniqueIdentifier(ref List<WebScrapeOutput> result)
        {
            try
            {
                foreach (var data in result.ToList())
                {
                    data.UniqueID = DataHelper.GenerateID();
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in AddUniqueIdentifier -- ScrapeProductsListInput -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }
        }

        private void FormatDataBasedOnScrape(List<WebScrapeOutput> result, string TargetNameForInputURL, ref WebScrapeInput detailInput)
        {

            try
            {
                var defaultConfig = detailInput.Website = detailInput.Websites.FirstOrDefault().Clone();

                if (defaultConfig == null)
                    return;

                detailInput.Websites.Clear();

                switch (defaultConfig.WebScrapeType)
                {
                    case ScrapeType.SingleURL:
                        {
                            WebsiteInformation website = new WebsiteInformation()
                            {
                                Name = defaultConfig.WebsiteNamePrefix,
                                URL = defaultConfig.URL,
                                webScrapeType = defaultConfig.webScrapeType
                            };

                            if (!string.IsNullOrEmpty(website.Name))
                            {
                                detailInput.Websites.Add(website);
                            }

                            break;
                        }

                    case ScrapeType.MultipleURLs:
                        {
                            // format input
                            var sites = result.Select(v => new
                            {
                                MappingID = v.UniqueID,
                                URL = v.ProductDetails.Where(c => c.TargetName == TargetNameForInputURL).Select(c => c.Value).FirstOrDefault()
                            })?.ToList();

                            if (sites != null && sites.Count > 0)
                            {
                                string[] siteURLs = sites.Where(v => !string.IsNullOrEmpty(v.URL)).Select(v => v.URL).ToArray();

                                int splitSize = GetSplitSize(siteURLs);
                                var arrays = siteURLs.Split(splitSize);

                                foreach (var arr in arrays)
                                {
                                    var URLs = new List<KeyValuePair<Identifier, UrlPair>>();

                                    // add guid to url
                                    foreach (var url in arr.ToArray())
                                    {
                                        string guid = sites.Where(v => v.URL.ToString() == url).Select(v => v.MappingID).FirstOrDefault();
                                        URLs.Add(new KeyValuePair<Identifier, UrlPair>(
                                           new Identifier { UniqueID = string.Empty, MappingID = guid },
                                           new UrlPair { Url = url }
                                        ));
                                    }

                                    WebsiteInformation website = new WebsiteInformation()
                                    {
                                        Name = defaultConfig.Name,
                                        URL = defaultConfig.URL,
                                        webScrapeType = defaultConfig.webScrapeType,
                                        URLs = URLs
                                    };

                                    if (!string.IsNullOrEmpty(website.Name))
                                    {
                                        detailInput.Websites.Add(website);
                                    }
                                }

                            }

                            break;
                        }
                }






            }
            catch (Exception ex)
            {
                Logger.Write("Exception in FormatDataBasedOnScrape -- ScrapeProductsListInput -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }
        }

        private int GetSplitSize(string[] values)
        {
            return values.Length > _InputSplitSize_ ? _InputSplitSize_ : values.Length;
        }

        private bool CheckIfMorePagesAreAvailable(ChromeDriver driver, WebScrapeInput input, int pageNumber)
        {
            bool hasMorePage = true;
            HtmlDocument htmlDoc = new HtmlDocument();

            try
            {

                switch (input.PagingType)
                {
                    // check if paging type is set to NoPaging i.e all data are loaded upfront
                    case PaginationType.NoPaging:
                        {
                            hasMorePage = false;
                            break;
                        }

                    // if paging type is 'LoadOnClick', find next click button and click 
                    case PaginationType.LoadOnClick:
                        {

                            // check if page limit is set
                            if (input.ShouldLimitPaging && pageNumber >= input.PagingLimit)
                            {
                                return false;
                            }

                            // check if pagination container is available
                            if (!driver.IsElementPresent(By.XPath(input.Pagination.XPath)))
                            {
                                Logger.Write("ERROR: Unable to find pagination container in CheckIfMorePagesAreAvailable -- ScrapeProductsList -> DataGrabberV2.");
                                return false;
                            }
                            else
                            {
                                // load html in agility pack
                                htmlDoc.LoadHtml(driver.PageSource);

                                HtmlNode pagingContainer = htmlDoc.DocumentNode.SelectNodes(input.Pagination.XPath)?.LastOrDefault();
                                HtmlNode nextElem = null;

                                // if it has next button, then target next button
                                if (input.HasNextButton)
                                {
                                    nextElem = pagingContainer.SelectNodes(input.NextPaginationButton.XPath)?.FirstOrDefault();
                                }
                                else
                                {
                                    // if next button is not supplied, then find active button and click on next button
                                    nextElem = pagingContainer.SelectNodes(XPathType.Relative.ForClass(input.ActivePageClass) + "/following-sibling::*")?.FirstOrDefault();
                                }

                                if (nextElem == null)
                                {
                                    hasMorePage = false;
                                }
                                else
                                {
                                    if (nextElem.Attributes.Contains("class"))
                                    {
                                        string classes = nextElem.Attributes["class"].Value;
                                        bool isDisabled = nextElem.Attributes["disabled"] != null;

                                        if (classes.Contains(input.DisabledPageClass) || isDisabled)
                                        {
                                            hasMorePage = false;
                                        }
                                    }
                                }

                                if (hasMorePage)
                                {
                                    // check if next elem is available
                                    By nextElemPath = By.XPath(nextElem.XPath);
                                    if (!driver.IsElementPresent(nextElemPath))
                                    {
                                        return false;
                                    }

                                    // set next page elem   
                                    IWebElement nextPageElem = driver.FindElement(nextElemPath);


                                    SetCookie(driver, input, true);

                                    // find pagination elem and click on next page
                                    if (nextElem.Name == "a")
                                    {
                                        nextPageElem.ClickIfDisplayed(driver);
                                    }
                                    else
                                    {
                                        HtmlNode linkElem = nextElem.SelectNodes(".//a")?.FirstOrDefault();
                                        if (linkElem == null)
                                        {
                                            nextPageElem.ClickIfDisplayed(driver);
                                        }
                                        else
                                        {
                                            driver.FindElement(By.XPath(linkElem.XPath)).ClickIfDisplayed(driver);
                                        }
                                    }

                                    // wait for content to be loaded through ajax 
                                    WebDriverWait wait = driver.GetWebDriverWait();
                                    By productContainer = By.XPath(input.Container.XPath);
                                    wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(productContainer));

                                }
                            }

                            break;
                        }


                    // if paging type is 'LoadOnShowMore', empty content and load more data on page on clicking on 'load more' button
                    case PaginationType.LoadOnShowMore:
                        {
                            // check if page limit is set
                            if (input.ShouldLimitPaging && pageNumber >= input.PagingLimit)
                            {
                                return false;
                            }

                            By loadMorePath = By.XPath(input.LoadMoreButton.XPath);

                            // check if LoadMoreButton is available
                            if (!driver.IsElementPresent(loadMorePath))
                            {
                                Logger.Write("ERROR: Unable to find LoadMoreButton in CheckIfMorePagesAreAvailable -- ScrapeProductsList -> DataGrabberV2.");
                                return false;
                            }
                            else
                            {

                                // empty parent container
                                driver.EmptyParentContainer(input.ParentContainer.XPath, input.LoadMoreButton.XPath);

                                // set load more elem   
                                IWebElement loadMoreElem = driver.FindElement(loadMorePath);

                                if (loadMoreElem == null || loadMoreElem.Displayed == false)
                                {
                                    return false;
                                }

                                loadMoreElem.ClickIfDisplayed(driver);

                                // wait for content to be loaded through ajax 
                                WebDriverWait wait = driver.GetWebDriverWait();
                                By productContainer = By.XPath(input.Container.XPath);
                                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(productContainer));

                            }


                            break;
                        }

                    // if paging type is 'LoadOnScroll', scroll down and load more data on page
                    case PaginationType.LoadOnScroll:
                        {

                            break;
                        }

                    default:
                        hasMorePage = false;
                        break;
                }
            }
            catch (StaleElementReferenceException refEx)
            {
                // set next page elem                                    
                //nextPageElem = driver.FindElement(By.XPath(nextElem.XPath));

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in CheckIfMorePagesAreAvailable -- ScrapeProductsList -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }
            return hasMorePage;
        }



        private bool LoadPageBasedOnInput(ChromeDriver driver, WebScrapeInput data)
        {
            bool result = true;

            try
            {

                // close older tabs
                if (data.DoesResultsOpenInNewTab)
                {
                    if (driver.WindowHandles.Count > 1)
                    {
                        driver.Close();
                        driver.SwitchTo().Window(driver.WindowHandles.First());
                    }
                }


                WebDriverWait wait = driver.GetWebDriverWait();

                if (data.PagingType != PaginationType.NoPaging)
                {
                    // empty parent container
                    driver.EmptyParentContainer(data.ParentContainer.XPath, data.Pagination?.XPath ?? data.LoadMoreButton.XPath);
                }

                // set input values
                SetFieldsBasedOnInput(driver, wait, data);

                if (data.HasSearchButton)
                {
                    // check if search elem is available
                    By searchElemPath = By.XPath(data.SearchButton.XPath);
                    if (!driver.IsElementPresent(searchElemPath))
                    {
                        Logger.Write($"ERROR: Unable to find search button for {data.Website.Name} in LoadPageBasedOnInput-- ScrapeProductsList -> DataGrabberV2.");
                        result = false;
                    }
                    else
                    {
                        // click on search button
                        var searchButton = driver.FindElementByXPath(data.SearchButton.XPath);
                        searchButton.ClickIfDisplayed(driver);
                        driver.WaitForSomeSeconds(_WaitTimeToLoadResultsAfterClickInSeconds_);
                    }

                    if (data.DoesResultsOpenInNewTab)
                    {
                        driver.WaitForSomeSeconds(_WaitTimeToLoadNewTabInSeconds_);
                        driver.SwitchTo().Window(driver.WindowHandles.Last());


                        if (driver.WindowHandles.Count == 1)
                        {
                            result = false;
                            driver.SwitchTo().DefaultContent();

                            if (driver.IsAlertPresent())
                            {
                                IAlert alert = driver.SwitchTo().Alert();
                                alert.Accept();

                                driver.SwitchTo().DefaultContent();
                            }
                            //else
                            //{
                            //    By confirmationButton = By.XPath(data.DismissMessageElement.XPath);
                            //    if (driver.IsElementPresent(confirmationButton))
                            //    {
                            //        driver.FindElement(confirmationButton).ClickIfDisplayed(driver);
                            //    }
                            //}
                        }
                        else
                        {
                            driver.WaitForSomeSeconds(_WaitTimeToLoadPageInSeconds_);
                        }
                    }
                }

                // wait for content to be loaded through ajax 
                By productContainer = By.XPath(data.Container.XPath);
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(productContainer));

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in Main -- LoadPageBasedOnInput -> DataGrabberV2. Message: " + ex.Message);
                result = false;
            }
            finally
            {

            }
            return result;
        }

        private void SetBrowserOptions(WebScrapeInput input, out ChromeDriver driver)
        {
            try
            {
                ChromeDriverService service = ChromeDriverService.CreateDefaultService(ConfigFields.ChromeDriver);

                ChromeOptions options = new ChromeOptions();
                options.AddArguments("no-sandbox", "--start-maximized");

                //options.AddExtension(ConfigFields.CanvasFingerprintDefender);
                //options.AddExtension(ConfigFields.BlockImage);

                //options.AddArguments("disable-infobars", "disable-extensions");

                options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);

                if (input.ShouldDisableJavaScript)
                {
                    options.AddUserProfilePreference("profile.managed_default_content_settings.javascript", 2);
                }

                //options.SetExperimentalOption("useAutomationExtension", false);
                //options.setExperimentalOption("excludeSwitches", Collections.singletonList("enable-automation"));

                //options.AddAdditionalCapability("chrome.switches", ["--disable - javascript"]);
                //options.AddExtension(ConfigFields.SelectorGadget);
                //options.AddArgument("--proxy-server=75.151.213.85:8080");

                //int counter = 0, maxLoop = 5;
                //bool isIpWorking = false;
                
                driver = new ChromeDriver(service, options, TimeSpan.FromMinutes(_MaxBrowserWaitTimeInMinutes_));

                // check if browser dimension should be set
                if (input.ShouldSetBrowserWidthHeight)
                {
                    int width = input.BrowserScreenDimension.Width,
                        height = input.BrowserScreenDimension.Height;

                    driver.Manage().Window.Size = new Size(width, height);
                }


            }
            catch (Exception ex)
            {

                driver = new ChromeDriver();
                Logger.Write("Exception in SetBrowserOptions -- ScrapeProductsListInput -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }
        }




        private void SetCookie(ChromeDriver driver, WebScrapeInput data, bool justSetCookie = false)
        {
            try
            {
                if (!data.ShouldSetBrowserCookie)
                    return;

                // set token
                var newCookieTokens = AllCookies;

                // just set the cookie
                if (justSetCookie)
                {
                    driver.SetCookie(newCookieTokens);
                }


                int counter = 1, maxLoop = 5;
                bool isValidCookie = false;
                string domain = driver.Scripts().ExecuteScript("return document.domain").ToString();
                domain = domain.Replace("www.", string.Empty);

                // loop until valid cookie is obtained
                // or break after 5 attempts
                while (counter <= maxLoop && !isValidCookie)
                {
                    // check if captcha page is there
                    By captcha = By.XPath("//*[@class='g-recaptcha']");
                    if (driver.IsElementPresent(captcha))
                    {
                        if (counter > 1 && !isValidCookie)
                        {
                            newCookieTokens = SetAllBrowserCookie(driver, driver.Url, domain);
                        }

                        driver.SetCookie(newCookieTokens);
                        driver.Navigate().Refresh();
                        driver.WaitForSomeSeconds(2);
                    }
                    else
                    {
                        AllCookies = newCookieTokens;
                        isValidCookie = true;
                    }

                    counter++;
                }

                if (!isValidCookie)
                {
                    // failed even after 5 attempts

                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in SetCookie -- ScrapeProductsList -> DataGrabberV2. Message: " + ex.Message);

            }
            finally
            {

            }
        }

        private List<OpenQA.Selenium.Cookie> SetAllBrowserCookie(ChromeDriver driver, string baseURL, string domain)
        {

            var cookies = new List<OpenQA.Selenium.Cookie>();
            Process proc = new Process();

            try
            {
                Console.WriteLine(baseURL);
                Console.WriteLine(" Before process: " + DateTime.Now);
                Thread.Sleep(2000);

                proc = Process.Start(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                    baseURL);
                //baseURL + " --user-data-dir=\"dhoni\"");

                Thread.Sleep(TimeSpan.FromSeconds(60));

                //proc = Process.Start(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", baseURL); //  + " --new-window"
                //proc.WaitForExit(1000 * 60 * 5);    // Wait up to five minutes.
                //Console.WriteLine(" AFTER process: " + DateTime.Now);
                //if (proc != null)
                //{
                //    try
                //    {
                //        // Close the browser.
                //        if (!proc.WaitForExit(10000))
                //        {
                //            if (!proc.HasExited)
                //            {
                //                proc.Kill();
                //            }
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        Logger.Write("Exception in PROCESS KILL FAILED SetAllBrowserCookie -- ScrapeProductsListInput -> DataGrabberV2. Message: " + ex.Message);
                //    }
                //    finally
                //    {
                //        Thread.Sleep(5000);
                //    }
                //}

                var localAppDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string UserDataDirectoryPath = Path.Combine(localAppDirectoryPath, @"Google\Chrome\User Data\Default");

                //string randomUserDirectoryPath = Path.Combine(Environment.CurrentDirectory, "dhoni", "default");

                var cookieFilePath = Path.Combine(UserDataDirectoryPath, "Cookies") + ";pooling=false";
                var connString = string.Format(@"Data Source={0}", cookieFilePath);
                //string sVal = Chrome_GetLocalStorageKeyValue("Cookies", "", connString);


                //string Chrome_GetLocalStorageKeyValueRet = default(string);
                //const string GOOGLE_CHROME_LOCAL_STORAGE_PATH = @"\Google\Chrome\User Data\Default\Local Storage\";


                //string sValue = string.Empty;
                //string sHostFilePath;
                //string sDataSource;
                //string sUserPath;

                //sUserPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                //sHostFilePath = sUserPath + GOOGLE_CHROME_LOCAL_STORAGE_PATH + sHostFileName;


                // Check to see if Google Chrome Browser Installed
                //if (string.IsNullOrEmpty(sHostFilePath))
                //    Chrome_GetLocalStorageKeyValueRet = string.Empty;


                //sDataSource = "Data Source=" + sHostFilePath;


                // delete all cookies
                driver.Manage().Cookies.DeleteAllCookies();

                var oSQLiteConn = new SQLiteConnection(connString);
                using (oSQLiteConn)
                {
                    SQLiteCommand oSQLiteCmd = oSQLiteConn.CreateCommand();
                    using (oSQLiteCmd)
                    {
                        oSQLiteCmd.CommandText = string.Format("Select * FROM cookies where host_key like '%{0}%'", domain);
                        oSQLiteConn.Open();
                        SQLiteDataReader oSQLiteRead = oSQLiteCmd.ExecuteReader();
                        using (oSQLiteRead)
                        {
                            while (oSQLiteRead.Read())
                            {

                                // decrypt value
                                byte[] cookieValue = (byte[])oSQLiteRead["encrypted_value"];
                                cookieValue = ProtectedData.Unprotect(cookieValue, null, DataProtectionScope.CurrentUser);
                                string value = Encoding.ASCII.GetString(cookieValue);

                                // get expiry
                                DateTime expiry = DateTime.Now.AddYears(1); //DataHelper.UnixTimeStampToDateTime(Convert.ToDouble(oSQLiteRead["expires_utc"]));

                                var cookie = new OpenQA.Selenium.Cookie(
                                    Convert.ToString(oSQLiteRead["name"]),
                                    value,
                                    Convert.ToString(oSQLiteRead["host_key"]),
                                    Convert.ToString(oSQLiteRead["path"]),
                                    expiry
                                    );

                                // add cookie
                                driver.Manage().Cookies.AddCookie(cookie);

                                cookies.Add(cookie);
                            };
                        }


                    }
                    oSQLiteConn.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in SetAllBrowserCookie -- ScrapeProductsListInput -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {
                try
                {
                    if (proc != null)
                    {
                        proc.Kill();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write("Exception in finally block - SetAllBrowserCookie -- ScrapeProductsListInput -> DataGrabberV2. Message: " + ex.Message);
                }
            }
            return cookies;
        }

        private void SetFieldsBasedOnInput(ChromeDriver driver, WebDriverWait wait, WebScrapeInput data)
        {
            try
            {
                if (data.HasResetSearchButton)
                {
                    // check if reset search elem is available
                    By searchElemPath = By.XPath(data.SearchButton.XPath);
                    By resetSearchElemPath = By.XPath(data.ResetSearchButton.XPath);

                    if (!driver.IsElementPresent(resetSearchElemPath) && !driver.IsElementPresent(searchElemPath))
                    {
                        Logger.Write($"ERROR: Unable to find both reset & search button for {data.Website.Name} in SetFieldsBasedOnInput-- ScrapeProductsList -> DataGrabberV2.");
                        return;
                    }
                    else if (!driver.IsElementPresent(resetSearchElemPath))
                    {
                        Logger.Write($"ERROR: Unable to find reset search button for {data.Website.Name} in SetFieldsBasedOnInput-- ScrapeProductsList -> DataGrabberV2.");
                    }
                    else
                    {
                        // click on search button
                        var resetSearchButton = driver.FindElement(resetSearchElemPath);
                        resetSearchButton.ClickIfDisplayed(driver);
                    }
                }


                foreach (var inp in data.Website.InputInfo)
                {
                    By fieldPath = By.XPath(inp.XPath);

                    if (!driver.IsElementPresent(fieldPath))
                    {
                        Logger.Write($"ERROR: Input field missing in SetFieldsBasedOnInput -- LoadPageBasedOnInput -> DataGrabberV2." +
                            $"Details: Field - {inp.TargetName}");
                        return;
                    }

                    var elem = driver.FindElement(fieldPath);

                    if (elem.Enabled && !elem.Displayed)
                    {
                        break;
                    }
                    else
                    {
                        wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(fieldPath));
                    }


                    switch (inp.TargetType.ToLower())
                    {
                        case "select":
                            var select = new SelectElement(elem);
                            select.SelectByText(inp.Value);
                            driver.WaitForSomeSeconds(inp.WaitingTimeAfterElementChange);
                            break;

                        case "input":
                        case "textarea":

                            elem.MaxZIndex(driver);
                            elem.Clear();
                            elem.SendKeys(inp.Value);
                            driver.WaitForSomeSeconds(inp.WaitingTimeAfterElementChange);
                            break;

                        case "div":
                            elem.Click();
                            break;

                        case "custom-dropdown":
                        case "auto-complete":
                        case "auto-suggest":
                            if (elem.TagName == "div" || elem.TagName == "span")
                            {
                                elem.Click();
                            }
                            else
                            {
                                elem.SendKeys(inp.Value);
                            }
                            // wait for suggestion box to be visible
                            driver.WaitForSomeSeconds(inp.PartnerElement.WaitingTimeAfterElementChange);

                            By partnerPath = By.XPath(inp.PartnerElement.XPath);

                            // check if partner element is visible
                            if (!driver.IsElementPresent(partnerPath))
                            {
                                Logger.Write($"ERROR: Unable to find partner element in SetFieldsBasedOnInput -- LoadPageBasedOnInput -> DataGrabberV2." +
                                    $"Details: Field - {inp.TargetName}; Partner field: {inp.PartnerElement.TargetName}");
                                return;
                            }

                            var partnerElems = driver.FindElements(partnerPath);
                            foreach (IWebElement option in partnerElems)
                            {
                                if (option.Text.Trim().ToLower() == inp.Value.Trim().ToLower())
                                {
                                    // to avoid exception "stale element reference: element is not attached to the page document"
                                    //IWebElement optionElement = driver.FindElementByXPath(option.GetAbsoluteXPath(driver));

                                    option.ClickIfDisplayed(driver);
                                    break;
                                }
                            }

                            break;

                        default:

                            break;
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in SetFieldsBasedOnInput -- ScrapeProductsListInput -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }
        }




        public void SetDefaultConfiguration(WebScrapeUserInput data, ref WebScrapeInput input)
        {

            try
            {
                var defaultConfig = data.DefaultWebsiteConfiguration;

                if (defaultConfig == null)
                    return;

                switch (defaultConfig.WebScrapeType)
                {
                    case ScrapeType.SingleURL:
                        {
                            WebsiteInformation website = new WebsiteInformation()
                            {
                                Name = defaultConfig.WebsiteNamePrefix,
                                URL = defaultConfig.WebsiteURL,
                                webScrapeType = defaultConfig.webScrapeType
                            };

                            if (!string.IsNullOrEmpty(website.Name))
                            {
                                input.Websites.Add(website);
                            }

                            break;
                        }

                    case ScrapeType.MultipleURLs:
                        {
                            var siteURLs = defaultConfig.WebsiteURLs;

                            if (siteURLs != null && siteURLs.Length > 0)
                            {
                                int splitSize = GetSplitSize(siteURLs);
                                var arrays = siteURLs.Split(splitSize);

                                foreach (var arr in arrays)
                                {
                                    var URLs = new List<KeyValuePair<Identifier, UrlPair>>();

                                    // add guid to url
                                    foreach (var url in arr.ToArray())
                                    {
                                        URLs.Add(new KeyValuePair<Identifier, UrlPair>(
                                            new Identifier { UniqueID = string.Empty, MappingID = string.Empty },
                                            new UrlPair { Url = url }
                                        ));
                                    }

                                    WebsiteInformation website = new WebsiteInformation()
                                    {
                                        Name = defaultConfig.WebsiteNamePrefix,
                                        URL = defaultConfig.WebsiteURL,
                                        webScrapeType = defaultConfig.webScrapeType,
                                        URLs = URLs
                                    };

                                    if (!string.IsNullOrEmpty(website.Name))
                                    {
                                        input.Websites.Add(website);
                                    }
                                }
                            }

                            break;
                        }

                    case ScrapeType.MultipleInputs:
                        {
                            var values = defaultConfig.InputValues;

                            if (values != null && values.Length > 0)
                            {
                                int splitSize = GetSplitSize(values);
                                var arrays = values.Split(splitSize);

                                foreach (var arr in arrays)
                                {
                                    var InputValues = new List<KeyValuePair<Identifier, string>>();

                                    // add guid to url
                                    foreach (var value in arr.ToArray())
                                    {
                                        InputValues.Add(new KeyValuePair<Identifier, string>(
                                            new Identifier { UniqueID = string.Empty, MappingID = string.Empty }, value));
                                    }

                                    WebsiteInformation website = new WebsiteInformation()
                                    {
                                        Name = defaultConfig.WebsiteNamePrefix,
                                        URL = defaultConfig.WebsiteURL,
                                        webScrapeType = defaultConfig.webScrapeType,
                                        TargetName = defaultConfig.TargetName,
                                        InputValues = InputValues
                                    };

                                    // get all inputs
                                    foreach (InputTargetElement elem in defaultConfig.InputInfo)
                                    {
                                        ElementMapping mapping = new ElementMapping()
                                        {
                                            TargetName = elem.TargetName,
                                            TargetType = elem.TargetType,
                                            XPath = elem.GetXPath(),
                                            AttributeName = elem.AttributeName,
                                            Value = elem.TargetValue,
                                            WaitingTimeAfterElementChange = elem.WaitingTimeAfterElementChange
                                        };

                                        // get partner element
                                        if (elem.PartnerElement != null && !string.IsNullOrEmpty(elem.PartnerElement.TargetName))
                                        {
                                            ElementMapping partnerMapping = new ElementMapping()
                                            {
                                                TargetName = elem.PartnerElement.TargetName,
                                                TargetType = elem.PartnerElement.TargetType,
                                                XPath = elem.PartnerElement.GetXPath(),
                                                WaitingTimeAfterElementChange = elem.PartnerElement.WaitingTimeAfterElementChange

                                            };

                                            mapping.PartnerElement = partnerMapping;
                                        }

                                        if (!string.IsNullOrEmpty(mapping.TargetName))
                                        {
                                            website.InputInfo.Add(mapping);
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(website.Name))
                                    {
                                        input.Websites.Add(website);
                                    }
                                }

                            }


                            break;
                        }

                    default:
                        break;
                }

                if (input.Websites.Count == 0)
                {
                    if (defaultConfig.WebScrapeType == ScrapeType.MultipleURLs || defaultConfig.WebScrapeType == ScrapeType.MultipleInputs)
                    {
                        // set default data if there are no websites
                        WebsiteInformation website = new WebsiteInformation()
                        {
                            Name = defaultConfig.WebsiteNamePrefix,
                            URL = defaultConfig.WebsiteURL,
                            webScrapeType = defaultConfig.webScrapeType
                        };

                        if (!string.IsNullOrEmpty(website.Name))
                        {
                            input.Websites.Add(website);
                        }
                    }
                }


                // Set a column for input
                if (data.DefaultWebsiteConfiguration.WebScrapeType == ScrapeType.MultipleInputs)
                {
                    ElementMapping pdtMapping = new ElementMapping()
                    {
                        TargetName = data.DefaultWebsiteConfiguration.TargetName,
                        Value = "",
                        IsInputAttribute = true
                    };

                    if (!string.IsNullOrEmpty(pdtMapping.TargetName))
                    {
                        input.ProductDetails.Add(pdtMapping);
                    }
                }


            }
            catch (Exception ex)
            {
                Logger.Write("Exception in SetDefaultConfiguration -- ScrapeProductsListInput -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }

        }



    }
}
