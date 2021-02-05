using DataGrabberV2.BusinessLogic.Contract;
using DataGrabberV2.LogWriter;
using DataGrabberV2.Models;
using DataGrabberV2.Utility;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabberV2.BusinessLogic
{
    public class RestClientScraper : IScraper
    {

        private RestClientScraper() { }

        public static RestClientScraper Instance { get; } = new RestClientScraper();


        private readonly int _MaxDegreeOfParallelism_ = ConfigFields.MaxDegreeOfParallelism;
        private readonly int _InputSplitSize_ = ConfigFields.InputSplitSize;


        public List<WebScrapeOutput> ProcessRequest(WebScrapeInput input)
        {
            var result = new List<WebScrapeOutput>();

            try
            {
                ProcessRequestFor(input, ref result);
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ProcessRequest -- RestClientScraper -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
            return result;
        }

        private void ProcessRequestFor(WebScrapeInput input, ref List<WebScrapeOutput> result)
        {
            try
            {
                switch (input.Website.WebScrapeType)
                {
                    case ScrapeType.SingleURL:
                        ProcessRequestForSingleURL(input, ref result);
                        break;

                    case ScrapeType.MultipleURLs:
                        ProcessRequestForMultipleURLs(input, ref result);
                        break;

                }

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ProcessRequestForSingleURL -- RestClientScraperInput -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
        }

        private void ProcessRequestForSingleURL(WebScrapeInput input, ref List<WebScrapeOutput> result)
        {
            string htmlData = string.Empty;
            HtmlDocument objResultSet = new HtmlDocument();

            try
            {
                // get full html
                //htmlData = driver.GetPageSource();
                Console.WriteLine($"URL: {input.Website.URL} Started");

                var resp = ScrapeRestService.Instance.GetWebResponse(input.Website);
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    htmlData = resp.Message;
                }

                // load html in agility pack
                objResultSet.LoadHtml(htmlData);

                int currentPage = 1;
                List<WebScrapeOutput> pageDetails = GetContent(objResultSet, input, currentPage);

                lock (result)
                {
                    // add to existing results
                    result.AddRange(pageDetails.Clone());
                }

                Console.WriteLine($"URL: {input.Website.URL} Ended");

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ProcessRequestForSingleURL -- RestClientScraperInput -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
        }

        private void ProcessRequestForMultipleURLs(WebScrapeInput input, ref List<WebScrapeOutput> result)
        {
            if (input.Website.URLs != null)
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
                    //driver.Navigate().GoToUrl(url);
                    input.Website.URL = url;
                    input.Website.RequestBody = body;

                    ProcessRequestForSingleURL(input, ref result);

                }
            }

            return;
        }

        private List<WebScrapeOutput> GetContent(HtmlDocument htmlDoc, WebScrapeInput input, int pageNumber)
        {
            List<WebScrapeOutput> result = new List<WebScrapeOutput>();

            try
            {

                var bodyContainer = htmlDoc.DocumentNode.SelectSingleNode("//html");
                var parentContainers = htmlDoc.DocumentNode.SelectNodes(input.ParentContainer.XPath);

                if (parentContainers == null)
                {
                    Logger.Write($"ERROR: Parent Container not found for WebsiteName: {input.Website.Name}; URL: {input.Website.URL} in ProcessRequest -- RestClientScraper -> DataGrabber.");
                }
                else
                {

                    // loop through all parents
                    foreach (HtmlNode parentContainer in parentContainers)
                    {
                        var productContainers = parentContainer.SelectNodes(input.Container.XPath);

                        if (productContainers == null)
                        {
                            Logger.Write($"ERROR: Product Container not found for WebsiteName: {input.Website.Name} in ProcessRequest -- RestClientScraper -> DataGrabber.");
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
                                    mapping.Value = GetHTMLContent(container, bodyContainer, mapping, input.Website.URL);
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

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in GetContent -- RestClientScraper -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
            return result;
        }

        private string GetHTMLContent(HtmlNode container, HtmlNode bodyContainer, ElementMapping mapping, string baseURL)
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
                            Logger.Write($"ERROR: Child Node Path {mapping.ChildNode.XPath} not found in GetHTMLContent -- RestClientScraper -> DataGrabber. ");
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
                Logger.Write("Exception in GetHTMLContent -- RestClientScraper -> DataGrabber. Message: " + ex.Message);
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
                Logger.Write("Exception in ProcessDetailRequest -- RestClientScraper -> DataGrabber. Message: " + ex.Message);
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
                Logger.Write("Exception in AddUniqueIdentifier -- RestClientScraperInput -> DataGrabber. Message: " + ex.Message);
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
                Logger.Write("Exception in FormatDataBasedOnScrape -- RestClientScraperInput -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
        }

        private int GetSplitSize<T>(T[] values)
        {
            return values.Length > _InputSplitSize_ ? _InputSplitSize_ : values.Length;
        }



        public void SetDefaultConfiguration(WebScrapeUserInput data, ref WebScrapeInput input)
        {

            // Set Website Pattern Configuration 
            SetWebsitePatternConfiguration(data, ref input);

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
                                webScrapeType = defaultConfig.webScrapeType,
                                HttpVerb = HttpVerbType.GET
                            };

                            if (!string.IsNullOrEmpty(website.Name))
                            {
                                input.Websites.Add(website);
                            }

                            // TODO: move to multiple URLs
                            if (defaultConfig.WebsiteURLsWithBody != null)
                            {
                                foreach (var urlsWithBody in defaultConfig.WebsiteURLsWithBody)
                                {
                                    website = new WebsiteInformation()
                                    {
                                        Name = defaultConfig.WebsiteNamePrefix,
                                        URL = urlsWithBody.Url,
                                        RequestBody = urlsWithBody.Body,
                                        webScrapeType = defaultConfig.webScrapeType,
                                        HttpVerb = HttpVerbType.POST
                                    };

                                    if (!string.IsNullOrEmpty(website.Name))
                                    {
                                        input.Websites.Add(website);
                                    }
                                }
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
                                        HttpVerb = HttpVerbType.GET,
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


                    default:
                        break;
                }

                if (input.Websites.Count == 0)
                {
                    if (defaultConfig.WebScrapeType == ScrapeType.MultipleURLs)
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


            }
            catch (Exception ex)
            {
                Logger.Write("Exception in SetDefaultConfiguration -- ScrapeProductsListInput -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }

        }


        public void SetWebsitePatternConfiguration(WebScrapeUserInput data, ref WebScrapeInput input)
        {

            try
            {
                var defaultConfig = data.DefaultWebsiteConfiguration;
                var patternConfig = defaultConfig.WebsitePatternConfig;

                if (patternConfig == null)
                    return;


                var urls = new List<UrlPair>();


                // loop through all settings
                foreach (var setting in patternConfig.PatternSettings)
                {


                    var urlPattern = setting.UrlPattern;
                    int
                        totalItems = setting.TotalItems,
                        startIndex = patternConfig.StartIndex,
                        currentPage = patternConfig.CurrentPage,
                        itemsPerPage = patternConfig.ItemsPerPage;

                    // get total loop count
                    var totalPage = totalItems / patternConfig.ItemsPerPage;

                    // limit paging if applicable
                    if (input.ShouldLimitPaging)
                    {
                        totalPage = totalPage > input.PagingLimit ? input.PagingLimit : totalPage;
                    }

                    for (int ind = 0; ind < totalPage; ind++)
                    {
                        var url = urlPattern.Clone().ToString();
                        var body = string.Empty;

                        if (patternConfig.HttpVerb == HttpVerbType.GET)
                        {
                            // replace key words in url
                            url = url.Replace("{StartIndex}", startIndex.ToString());
                            url = url.Replace("{ItemsPerPage}", itemsPerPage.ToString());
                            url = url.Replace("{CurrentPage}", currentPage.ToString());

                            // add custom page number to url
                            url += $"&utm_source_page={ind + 1}";
                        }
                        else // if(patternConfig.HttpVerb == HttpVerbType.POST)
                        {
                            // form body
                            body = "?";

                            if (setting.RequestBodyParams != null)
                            {
                                foreach (var param in setting.RequestBodyParams)
                                {
                                    body += body == "?" ? string.Empty : "&";
                                    body += $"{param.ParamName}='{param.Value}'";
                                }
                            }

                            // replace key words in url
                            body = body.Replace("{StartIndex}", startIndex.ToString());
                            body = body.Replace("{ItemsPerPage}", itemsPerPage.ToString());
                            body = body.Replace("{CurrentPage}", currentPage.ToString());

                        }

                        // add the url to list
                        urls.Add(new UrlPair { Url = url, RequestBody = body });

                        currentPage++;
                        startIndex = ((ind + 1) * itemsPerPage) + patternConfig.StartIndex;
                    }

                }


                var siteURLs = urls.ToArray();

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
                                new UrlPair { Url = url.Url, RequestBody = url.RequestBody }
                            ));
                        }

                        WebsiteInformation website = new WebsiteInformation()
                        {
                            Name = defaultConfig.WebsiteNamePrefix,
                            URL = defaultConfig.WebsiteURL,
                            webScrapeType = defaultConfig.webScrapeType,
                            HttpVerb = patternConfig.HttpVerb,
                            URLs = URLs
                        };

                        if (!string.IsNullOrEmpty(website.Name))
                        {
                            input.Websites.Add(website);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in SetWebsitePatternConfiguration -- ScrapeProductsListInput -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }

        }


    }
}
