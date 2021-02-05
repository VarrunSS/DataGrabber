using CsvHelper;
using DataGrabber.BusinessLogic.Contract;
using DataGrabber.BusinessLogic.Factory;
using DataGrabber.LogWriter;
using DataGrabber.Models;
using DataGrabber.Utility;
using GoogleRecaptcha.BusinessLogic.Contract;
using GoogleRecaptcha.BusinessLogic.Factory;
using GoogleReCaptcha.BusinessLogic;
using HtmlAgilityPack;
using Newtonsoft.Json;
using OfficeOpenXml;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace DataGrabber.BusinessLogic
{
    public class ScrapeData
    {
        private ScrapeData() { }

        public static ScrapeData Instance { get; } = new ScrapeData();


        private readonly int _MaxDegreeOfParallelism_ = ConfigFields.MaxDegreeOfParallelism;
        private readonly int _InputSplitSize_ = ConfigFields.InputSplitSize;


        public void Main()
        {
            try
            {
                List<WebScrapeInput> inputs = new List<WebScrapeInput>();

                // get data from db
                //inputs = SetDummyData();

                // read data from json file
                using (StreamReader r = new StreamReader(ConfigFields.InputFile))
                {
                    string json = r.ReadToEnd();
                    inputs = FormatData(json);
                }


                foreach (WebScrapeInput inp in inputs)
                {
                    List<WebScrapeOutput> result = new List<WebScrapeOutput>();

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    DateTime StartedOn = DateTime.Now;

                    if (inp.ShouldRotateProxyIP)
                    {
                        // start rotating ip
                        ProxyHelper.Instance.RotateIPAddress();
                        Thread.Sleep(20 * 1000);
                    }

                    IScraper scraper = Creator.GetInstance(inp.ScrapingMechanism);

                    Parallel.ForEach(inp.Websites,
                        new ParallelOptions { MaxDegreeOfParallelism = _MaxDegreeOfParallelism_ },
                        (urlData) =>
                        {
                            {
                                WebScrapeInput input = inp.Clone();
                                input.Website = urlData;

                                // process each request
                                List<WebScrapeOutput> output = scraper.ProcessRequest(input);

                                //result = DummyProcessRequest(input);

                                // if details page should be scraped
                                if (input.ShouldFetchDataFromDetailsPage)
                                {
                                    scraper.ProcessDetailRequest(input, ref output);
                                }

                                result.AddRange(output.Clone());

                            }
                        });

                    if (inp.ShouldRotateProxyIP)
                    {
                        ProxyHelper.Instance.StopTimer();
                    }

                    DateTime EndedOn = DateTime.Now;
                    stopwatch.Stop();

                    // save data in excel
                    DbResponse response = SaveDataExcelFile(result);

                    if (response.IsSuccess && inp.ShouldSendMailOfOutputData)
                    {
                        var mailInfo = inp.MailInfo.Clone();
                        mailInfo.AttachmentPath = response.Message;
                        mailInfo.ConfigName = inp.WebsiteConfigurationName;
                        mailInfo.StartedOn = StartedOn.ToString();
                        mailInfo.EndedOn = EndedOn.ToString();

                        TimeSpan ts = stopwatch.Elapsed;
                        mailInfo.RunTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

                        EmailUtility.Instance.SendEmail(mailInfo);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in Main -- ScrapeProductsList -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
            return;
        }

        private DbResponse SaveDataExcelFile(List<WebScrapeOutput> output)
        {
            DbResponse response = new DbResponse();

            try
            {
                int max = output.Select(v => v.ProductDetails).Max(v => v.Count());
                WebScrapeOutput headers = output.Where(v => v.ProductDetails.Count() == max).FirstOrDefault();
                List<WebScrapeOutput> content = new List<WebScrapeOutput>();
                DataTable tableData = new DataTable();

                string filePath = ConfigFields.Output;
                Directory.CreateDirectory(filePath);

                // save file in folder as per date
                string folderName = DateTime.Now.ToShortDateString().Replace("/", "-");
                filePath = Path.Combine(filePath, folderName);
                Directory.CreateDirectory(filePath);

                string fileName = $"{DateTime.Now.ToString("yyyyMMdd")}_Output_{headers.WebsiteURL.Name}_{DateTime.Now.Ticks.ToString()}.xlsx";
                string fullfilePath = Path.Combine(filePath, fileName);
                FileInfo eFile = new FileInfo(fullfilePath);

                ExcelPackage package = new ExcelPackage();

                // get number of sheets
                int[] sheetNumbers = output.Select(v => v.OutputSheetNumber).Distinct().ToArray();

                foreach (int nbr in sheetNumbers)
                {
                    tableData = new DataTable();
                    headers = output.FirstOrDefault(v => v.OutputSheetNumber == nbr);
                    content = output.Where(v => v.OutputSheetNumber == nbr).ToList();


                    // set headers
                    tableData.Columns.Add("CompletedOn");
                    tableData.Columns.Add("WebSiteURL");
                    foreach (ElementMapping header in headers.ProductDetails)
                    {
                        if (!tableData.Columns.Contains(header.TargetName))
                            tableData.Columns.Add(header.TargetName);
                    }
                    tableData.Columns.Add("PageNumber");

                    // set content
                    foreach (WebScrapeOutput data in content)
                    {
                        DataRow row = tableData.NewRow();
                        row["CompletedOn"] = data.CompletedOn;
                        row["WebSiteURL"] = data.WebsiteURL.URL;
                        foreach (ElementMapping body in data.ProductDetails)
                        {
                            if (tableData.Columns.Contains(body.TargetName))
                                row[body.TargetName] = body.Value;
                        }
                        row["PageNumber"] = data.PageNumber.ToString();

                        tableData.Rows.Add(row);
                    }

                    ExcelWorksheet ws = package.Workbook.Worksheets.Add($"ScrapeData_Sheet{nbr}");
                    ws.Cells["A1"].LoadFromDataTable(tableData, true);

                    using (ExcelRange rng = ws.Cells["A1:Z1"])
                    {
                        rng.Style.Font.Bold = true;
                    }

                    using (ExcelRange rng = ws.Cells["A1:Z" + (tableData.Rows.Count + 1)])
                    {
                        rng.AutoFitColumns();
                    }
                }

                package.SaveAs(eFile);
                response = new DbResponse(true, fullfilePath);
            }
            catch (Exception ex)
            {
                response = new DbResponse(false, ex.Message);
                Logger.Write("Exception in SaveDataFile -- ScrapeProductsList -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
            return response;
        }

        private List<WebScrapeInput> FormatData(string json)
        {
            List<WebScrapeInput> result = new List<WebScrapeInput>();

            try
            {
                List<WebScrapeUserInput> userInput = new List<WebScrapeUserInput>();

                userInput = JsonConvert.DeserializeObject<List<WebScrapeUserInput>>(json);

                // for every config, set values
                foreach (WebScrapeUserInput data in userInput)
                {
                    WebScrapeInput input = FormatUserInputData(data);

                    result.Add(input);
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in FormatData -- ScrapeProductsList -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }

            return result;

        }

        private WebScrapeInput FormatUserInputData(WebScrapeUserInput data)
        {
            WebScrapeInput input = new WebScrapeInput();

            try
            {

                // assign corresponding values
                input = new WebScrapeInput
                {
                    WebsiteConfigurationName = data.WebsiteConfigurationName,
                    DoesWebsiteRequireInputValues = data.DoesWebsiteRequireInputValues,
                    ShouldSetBrowserCookie = data.ShouldSetBrowserCookie,
                    ShouldDisableJavaScript = data.ShouldDisableJavaScript,
                    ShouldRotateProxyIP = data.ShouldRotateProxyIP,
                    ShouldPickProxyIPFromList = data.ShouldPickProxyIPFromList,
                    ScrapingMechanism = data.ScrapingMechanism,
                    ShouldSetBrowserWidthHeight = data.ShouldSetBrowserWidthHeight,
                    WaitingTimeAfterPageLoad = data.WaitingTimeAfterPageLoad,
                    WaitingTimeAfterPageClick = data.WaitingTimeAfterPageClick,
                    ParentContainer = new ElementMapping()
                    {
                        XPath = data.ParentContainer.GetXPath()
                    },
                    Container = new ElementMapping()
                    {
                        XPath = data.Container.GetXPath()
                    },
                    PagingType = data.PagingType,
                    ShouldLimitPaging = data.ShouldLimitPaging,
                    PagingLimit = data.PagingLimit,
                    OutputSheetNumber = data.OutputSheetNumber == 0 ? 1 : data.OutputSheetNumber,
                    ShouldFetchDataFromDetailsPage = data.ShouldFetchDataFromDetailsPage,
                    ShouldSendMailOfOutputData = data.ShouldSendMailOfOutputData
                };





                // set web site info for default configuration
                SetDefaultConfiguration(data, ref input);

                // loop through all elements and form xpath, if the site requires input values
                if (data.DoesWebsiteRequireInputValues)
                {
                    input.HasSearchButton = data.HasSearchButton;
                    input.HasResetSearchButton = data.HasResetSearchButton;

                    input.DoesResultsOpenInNewTab = data.DoesResultsOpenInNewTab;

                    // set search button
                    input.SearchButton = new ElementMapping()
                    {
                        XPath = data.SearchButton.GetXPath()
                    };

                    input.ResetSearchButton = new ElementMapping()
                    {
                        XPath = data.ResetSearchButton.GetXPath()
                    };

                }


                // loop through all elements and form xpath
                foreach (WebsiteInfo info in data.WebsiteInfo)
                {
                    WebsiteInformation website = new WebsiteInformation()
                    {
                        Name = info.WebsiteName,
                        URL = info.WebsiteURL,
                        webScrapeType = info.webScrapeType
                    };

                    if (data.DoesWebsiteRequireInputValues)
                    {
                        // get all inputs
                        foreach (InputTargetElement elem in info.InputInfo)
                        {
                            ElementMapping mapping = new ElementMapping()
                            {
                                TargetName = elem.TargetName,
                                TargetType = elem.TargetType,
                                XPath = elem.GetXPath(),
                                AttributeName = elem.AttributeName,
                                Value = elem.TargetValue
                            };

                            if (!string.IsNullOrEmpty(mapping.TargetName))
                            {
                                website.InputInfo.Add(mapping);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(website.Name))
                    {
                        input.Websites.Add(website);
                    }
                }


                if (data.FetchDataFromIFrame)
                {
                    input.FetchDataFromIFrame = data.FetchDataFromIFrame;

                    // set IFrame Element
                    input.IFrameElement = new ElementMapping()
                    {
                        XPath = data.IFrameElement.GetXPath()
                    };
                }



                // loop through all elements and form xpath
                foreach (TargetElement elem in data.ProductDetails)
                {
                    ElementMapping mapping = new ElementMapping()
                    {
                        TargetName = elem.TargetName,
                        XPath = elem.GetXPath(),
                        AttributeName = elem.AttributeName,
                        RemoveText = elem.RemoveText,
                        GetChildElement = elem.GetChildElement,
                        OnlyCheckIfElementExists = elem.OnlyCheckIfElementExists,
                        ShouldCheckElemInBody = elem.ShouldCheckElemInBody
                    };

                    // get child element if applicable
                    if (elem.GetChildElement)
                    {
                        if (elem.ChildNode != null)
                        {
                            mapping.ChildNode = new ElementMapping()
                            {
                                TargetName = elem.ChildNode.TargetName,
                                XPath = elem.ChildNode.GetXPath(),
                                AttributeName = elem.ChildNode.AttributeName,
                                RemoveText = elem.ChildNode.RemoveText,
                                Separator = elem.ChildNode.Separator
                            };
                        }
                    }

                    if (!string.IsNullOrEmpty(mapping.TargetName))
                    {
                        input.ProductDetails.Add(mapping);
                    }
                }

                // Paging elements based on type
                if (input.PagingType != PaginationType.NoPaging)
                {

                    switch (input.PagingType)
                    {
                        case PaginationType.LoadOnClick:

                            input.Pagination = new ElementMapping()
                            {
                                XPath = data.Pagination.GetXPath()
                            };
                            input.HasNextButton = data.HasNextButton;
                            input.NextPaginationButton = new ElementMapping()
                            {
                                XPath = data.NextPaginationButton.GetXPath()
                            };
                            input.ActivePageClass = data.ActivePageClass;
                            input.DisabledPageClass = data.DisabledPageClass;

                            break;

                        case PaginationType.LoadOnShowMore:

                            input.LoadMoreButton = new ElementMapping()
                            {
                                XPath = data.LoadMoreButton.GetXPath()
                            };

                            break;

                    }
                }

                // Check if detailed information page also needs to be scraped
                if (input.ShouldFetchDataFromDetailsPage)
                {


                    input.DetailedInformationPage = new WebScrapeInput();

                    // call recursive
                    input.DetailedInformationPage = FormatUserInputData(data.DetailedInformationPage);

                    input.DetailedInformationPage.TargetNameForInputURL = new WebsiteInformation()
                    {
                        TargetName = data.DetailedInformationPage.TargetNameForInputURL.TargetName
                    };

                }

                // get captcha details
                if (data.ShouldSetBrowserCookie)
                {
                    input.Captcha = new ElementMapping()
                    {
                        XPath = data.Captcha.GetXPath()
                    };

                }

                // check if mail needs to be sent
                if (input.ShouldSendMailOfOutputData)
                {
                    input.MailInfo = data.MailInfo;
                }

                // check and set width height
                if (data.ShouldSetBrowserWidthHeight)
                {
                    input.BrowserScreenDimension = data.BrowserScreenDimension;
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in FormatUserInputData -- ScrapeProductsListInput -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }

            return input;
        }

        private void SetDefaultConfiguration(WebScrapeUserInput data, ref WebScrapeInput input)
        {

            try
            {
                IScraper scraper = Creator.GetInstance(input.ScrapingMechanism);
                scraper.SetDefaultConfiguration(data, ref input);

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in SetDefaultConfiguration -- ScrapeProductsListInput -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }

        }

        private int GetSplitSize(string[] values)
        {
            return values.Length > _InputSplitSize_ ? _InputSplitSize_ : values.Length;
        }

        private bool CheckIfValidIPAddress(string host, int port)
        {
            bool result = false;

            try
            {
                WebProxy myproxy = new WebProxy(host, port)
                {
                    BypassProxyOnLocal = false
                };
                var request = (HttpWebRequest)WebRequest.Create("http://www.google.com");
                request.Proxy = myproxy;
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            }
            catch (Exception ex)
            {

            }
            finally
            {

            }
            return result;
        }






        // NOT USED
        private void SaveDataCSVFile(List<WebScrapeOutput> output)
        {
            try
            {
                string filePath = ConfigFields.Output;
                string fileName = "Output.csv";
                string fullfilePath = Path.Combine(filePath, fileName);

                //using (var memory = new MemoryStream())
                //{
                using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
                {
                    using (var csvWriter = new CsvWriter(writer))
                    {

                        csvWriter.Configuration.Delimiter = ";";

                        List<ElementMapping> headers = output.FirstOrDefault().ProductDetails;

                        // set headers
                        foreach (ElementMapping header in headers)
                        {
                            csvWriter.WriteField(header.TargetName);
                        }
                        csvWriter.WriteField("PageNumber");
                        csvWriter.NextRecord();

                        // set body content
                        foreach (WebScrapeOutput data in output)
                        {
                            foreach (ElementMapping body in data.ProductDetails)
                            {
                                csvWriter.WriteField(body.Value);
                            }
                            csvWriter.WriteField(data.PageNumber);
                            csvWriter.NextRecord();
                        }

                        writer.Flush();

                    }
                }
                //}


            }
            catch (Exception ex)
            {
                Logger.Write("Exception in SaveDataFile -- ScrapeProductsList -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
            return;
        }

        // NOT USED
        public bool ElementHasClass(IWebElement element, String active)
        {
            return element.GetAttribute("class").Contains(active);
        }

        // NOT USED
        private List<WebScrapeOutput> DummyProcessRequest(WebScrapeInput input)
        {
            List<WebScrapeOutput> result = new List<WebScrapeOutput>();

            foreach (var pair in input.Website.InputValues)
            {
                string value = pair.Value;

                Logger.WriteTest($"Input (line 111)-> {value}");

                lock (result)
                {
                    result.Add(new WebScrapeOutput()
                    {
                        WebsiteURL = new WebsiteInformation() { URL = input.Website.URL },
                        PageNumber = 1,
                        ProductDetails = new List<ElementMapping>()
                    {
                        new ElementMapping() { TargetName = "Test", Value = value }
                    }
                    });
                }
            }
            return result;
        }


        // NOT USED
        private void SetCookie(ChromeDriver driver, bool isDetailsPage = false, bool justSetCookie = false)
        {
            try
            {
                // set token
                string newCookieToken = "";// isDetailsPage ? CookieValue_Details : CookieValue_List;


                // just set the cookie
                if (justSetCookie)
                {
                    driver.SetCookie("reese84", newCookieToken, ".www.g2.com");
                }

                int counter = 1, maxLoop = 5;
                bool isValidCookie = false;

                // loop until valid cookie is obtained
                // or break after 5 attempts
                while (counter <= maxLoop && !isValidCookie)
                {
                    // check if captcha page is there
                    By captcha = By.XPath("//*[@class='g-recaptcha']");
                    if (driver.IsElementPresent(captcha))
                    {
                        driver.SetCookie("reese84", newCookieToken, ".www.g2.com");
                        driver.Navigate().Refresh();
                    }
                    else
                    {
                        //if (isDetailsPage)
                        //    CookieValue_Details = newCookieToken;
                        //else
                        //    CookieValue_List = newCookieToken;

                        //CookieValue = newCookieToken;
                        isValidCookie = true;

                        //if (counter > 2)
                        //    Logger.Write($"Success: New Cookie - { CookieValue }");
                    }

                    if (counter > 1 && !isValidCookie)
                    {
                        newCookieToken = GetNewCookieToken(newCookieToken);
                    }

                    counter++;
                }

                if (!isValidCookie)
                {
                    // failed even after 5 attempts

                }

                //By captcha = By.XPath("//*[@class='g-recaptcha']");
                //if (driver.IsElementPresent(captcha))
                //{
                //    while (driver.Manage().Cookies.GetCookieNamed("reese84") != null)
                //    {
                //        driver.Manage().Cookies.DeleteCookieNamed("reese84");
                //    }

                //    driver.Manage().Cookies.AddCookie(cookie);

                //    driver.Navigate().Refresh();
                //}

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in SetCookie -- ScrapeProductsList -> DataGrabber. Message: " + ex.Message);

            }
            finally
            {

            }
        }

        // NOT USED
        private string GetNewCookieToken(string newCookieToken)
        {
            string cookieToken = string.Empty;

            try
            {
                ApiResponse resp = null; // ScrapeRestService.Instance.GetNewCookieToken(newCookieToken);

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    CookieResponse result = JsonConvert.DeserializeObject<CookieResponse>(resp.Message);

                    // expired
                    if (result.RenewInSec == 0)
                    {
                        cookieToken = RefreshNewCookieToken(result.Token);
                    }
                    else
                    {
                        // set token
                        cookieToken = result.Token;
                    }
                }
                else
                {
                    Logger.Write($"ERROR: {resp.StatusCode.ToString()} in GetNewCookieToken -- ScrapeProductsList -> DataGrabber. Message: {resp.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in GetNewCookieToken -- ScrapeProductsList -> DataGrabber. Message: " + ex.Message);

            }
            finally
            {

            }
            return cookieToken;
        }

        // NOT USED
        private string RefreshNewCookieToken(string oldCookieToken)
        {
            string cookieToken = string.Empty;
            try
            {
                ApiResponse resp = null; // ScrapeRestService.Instance.RefreshNewCookieToken(oldCookieToken);

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    CookieResponse result = JsonConvert.DeserializeObject<CookieResponse>(resp.Message);

                    // set token
                    cookieToken = result.Token;
                }
                else
                {
                    Logger.Write($"ERROR: {resp.StatusCode.ToString()} in RefreshNewCookieToken -- ScrapeProductsList -> DataGrabber. Message: {resp.Message}");
                }

            }
            catch (Exception ex)
            {
                Logger.Write("Exception in RefreshNewCookieToken -- ScrapeProductsList -> DataGrabber. Message: " + ex.Message);
            }
            finally
            {

            }
            return cookieToken;
        }




    }
}



// wait for the grid to appear
//By parentContainer = By.XPath(data.ParentContainer.XPath);
//wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.PresenceOfAllElementsLocatedBy(parentContainer));

// get text before performing ajax call
//IReadOnlyCollection<IWebElement> beforeAjaxElem = driver.FindElement(parentContainer).FindElements(By.XPath(data.Container.XPath));
//string beforeAjaxText = beforeAjaxElem.FirstOrDefault().Text;


// wait for the loader to disapper
//By loader = By.XPath(data.Loader.XPath);
//wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(loader));
//wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.InvisibilityOfElementLocated(loader));

// get text after performing ajax call
//ReadOnlyCollection<IWebElement> afterAjaxElem = driver.FindElement(parentContainer).FindElements(By.XPath(data.Container.XPath));
//wait.Until(SeleniumExtras.WaitHelpers.SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(afterAjaxElem));
//string afterAjaxText = afterAjaxElem.FirstOrDefault().Text;

// check if both are same 
//if (beforeAjaxText == afterAjaxText)
//{
//    Logger.Write($"ERROR: Data not refreshed on clicking search button for {data.Website.Name} in LoadPageBasedOnInput-- ScrapeProductsList -> DataGrabber.");
//}

//wait.Until(d => (bool)(d as IJavaScriptExecutor).ExecuteScript("return jQuery.active == 0"));

//WaitForAjax(driver, "locator.paloaltonetworks.com");

//WaitForAjax(driver);





//div[contains(concat(' ', normalize-space(@class), ' '), ' Test ')]
// https://stackoverflow.com/questions/1604471/how-can-i-find-an-element-by-css-class-with-xpath


//foreach (HtmlNode nextElem in pagingContainer.SelectNodes(".//*[@class='active']/following-sibling::*").LastOrDefault())
//{
//    if (nextElem.Attributes.Contains("class"))
//    {
//        string classes = nextElem.Attributes["class"].Value;
//        if (classes.Contains("disabled"))
//        {
//            hasMorePage = false;
//        }
//        else
//        {
//            hasMorePage = true;
//            nextPageElem = paginationContainer.FindElement(By.XPath(nextElem.XPath));
//        }
//    }
//    else
//    {
//        hasMorePage = true;
//        nextPageElem = driver.FindElement(By.XPath(nextElem.XPath));
//    }

//    break;
//    // hasMorePage = (nextElem.SelectSingleNode("*[@class='disabled']") == null);
//}
//break;

//foreach (HtmlNode activeElem in pagingContainer.SelectNodes(".//*[@class='active']/following-sibling::*[@class='disabled']"))
//{
//    hasMorePage = false;
//}





// Wait until a page is fully loaded via JavaScript
//WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
//wait.Until((x) =>
//{
//    return ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete");
//});

//IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
//js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");



//// load images before reading URL -- for lazy loading
//if (objNode == null && mapping.AttributeName == "src")
//{
//    var allImages = container.SelectNodes(".//img");
//    if (allImages != null)
//    {
//        foreach (var images in allImages)
//        {
//            IWebElement image = driver.FindElement(By.XPath(images.XPath));

//            if (image != null)
//            {
//                object IsImgLoaded = ((IJavaScriptExecutor)driver).ExecuteScript(
//                   "return arguments[0].complete && " +
//                   "typeof arguments[0].naturalWidth != \"undefined\" && " +
//                   "arguments[0].naturalWidth > 0", image);

//                bool loaded = false;
//                if (IsImgLoaded is bool)
//                {
//                    loaded = (bool)IsImgLoaded;
//                }
//            }
//        }

//        objNode = container.SelectNodes(mapping.XPath)?.FirstOrDefault();
//    }
//}



//private List<WebScrapeInput> SetDummyData()
//{
//    WebScrapeInput cromaMobiles = new WebScrapeInput()
//    {
//        Website = new WebsiteInformation()
//        {
//            Name = "Croma_Mobiles",
//            URL = "https://www.croma.com/phones-wearables/mobile-phones/c/10"
//        },
//        ParentContainer = new ElementMapping()
//        {
//            TargetName = "ParentContainer",
//            XPath = XPathType.Absolute.ForClass("product-list-view")
//        },
//        Container = new ElementMapping()
//        {
//            TargetName = "Container",
//            XPath = XPathType.Relative.ForClass("product__list--item")
//        },
//        ProductDetails = new List<ElementMapping>()
//            {
//                new ElementMapping() {
//                    TargetName = "Title", XPath = XPathType.Relative.ForClass("product__list--name")
//                },
//                new ElementMapping() {
//                    TargetName = "MRP", XPath = XPathType.Relative.ForClass("pdpPriceMrp")
//                },
//                new ElementMapping() {
//                    TargetName = "OfferPrice", XPath = XPathType.Relative.ForClass("pdpPrice")
//                },
//                new ElementMapping() {
//                    TargetName = "PercentOff", XPath = XPathType.Relative.ForClass("listingDiscnt")
//                },
//                new ElementMapping() {
//                    TargetName = "Features", XPath = XPathType.Relative.ForClass("listmian-features", "ul")
//                }
//            },
//        PagingType = PaginationType.LoadOnClick,
//        Pagination = new ElementMapping()
//        {
//            TargetName = "Pagination",
//            XPath = "//*[@id='V01CPMM01030104']/ul"
//        },
//        ActivePageClass = "active",
//        DisabledPageClass = "disabled",
//        ShouldLimitPaging = true,
//        PagingLimit = 1
//    };

//    WebScrapeInput locator = new WebScrapeInput()
//    {
//        Website = new WebsiteInformation()
//        {
//            Name = "Locator",
//            URL = "https://locator.paloaltonetworks.com/"
//        },
//        ParentContainer = new ElementMapping()
//        {
//            TargetName = "ParentContainer",
//            XPath = "//*[@id='resultsCarousel']"
//        },
//        Container = new ElementMapping()
//        {
//            TargetName = "Container",
//            XPath = ".//li"
//        },
//        ProductDetails = new List<ElementMapping>()
//            {
//                new ElementMapping() {
//                    TargetName = "Title", XPath = ".//div[3]/h3"
//                },
//                new ElementMapping() {
//                    TargetName = "AddressType", XPath = ".//div[3]/div[1]/p[1]"
//                },
//                new ElementMapping() {
//                    TargetName = "Address", XPath = ".//div[3]/div[1]/address"
//                },
//                new ElementMapping() {
//                    TargetName = "Partner Level", XPath = ".//div[3]/div[2]/p[1]/span"
//                },
//                new ElementMapping() {
//                    TargetName = "Partner Type", XPath = ".//div[3]/div[2]/p[2]/span"
//                }
//            },
//        PagingType = PaginationType.NoPaging
//    };

//    WebScrapeInput flipkartLaptops = new WebScrapeInput()
//    {
//        Website = new WebsiteInformation()
//        {
//            Name = "Flipkart_Laptops",
//            URL = "https://www.flipkart.com/laptops/pr?sid=6bo,b5g&marketplace=FLIPKART"
//        },
//        ParentContainer = new ElementMapping()
//        {
//            TargetName = "ParentContainer",
//            XPath = XPathType.Relative.ForClass("_1HmYoV _35HD7C")
//        },
//        Container = new ElementMapping()
//        {
//            TargetName = "Container",
//            XPath = XPathType.Relative.ForClass("bhgxx2")
//        },
//        ProductDetails = new List<ElementMapping>()
//            {
//                new ElementMapping() {
//                    TargetName = "Title", XPath = XPathType.Relative.ForClass("_3wU53n")
//                },
//                new ElementMapping() {
//                    TargetName = "MRP", XPath = XPathType.Relative.ForClass("_3auQ3N _2GcJzG")
//                },
//                new ElementMapping() {
//                    TargetName = "OfferPrice", XPath = XPathType.Relative.ForClass("_1vC4OE _2rQ-NK")
//                },
//                new ElementMapping() {
//                    TargetName = "PercentOff", XPath = XPathType.Relative.ForClass("VGWI6T")
//                },
//                new ElementMapping() {
//                    TargetName = "Features", XPath = XPathType.Relative.ForClass("vFw0gD")
//                },
//                new ElementMapping() {
//                    TargetName = "DetailsURL", XPath = XPathType.Relative.ForClass("_31qSD5"), AttributeName = "href"
//                },
//                new ElementMapping() {
//                    TargetName = "ProductImageURL", XPath = XPathType.Relative.ForClass("_1Nyybr _30XEf0"), AttributeName = "src"
//                },
//                new ElementMapping() {
//                    TargetName = "ProductID", XPath = ".//*[@data-id]", AttributeName = "data-id"
//                }

//            },
//        PagingType = PaginationType.LoadOnClick,
//        Pagination = new ElementMapping()
//        {
//            TargetName = "Pagination",
//            XPath = XPathType.Relative.ForClass("_1ypTlJ")
//        },
//        ActivePageClass = "fyt9Eu",
//        DisabledPageClass = "disabled",
//        ShouldLimitPaging = true,
//        PagingLimit = 1
//    };

//    WebScrapeInput flipkartMobiles = new WebScrapeInput()
//    {
//        Website = new WebsiteInformation()
//        {
//            Name = "Flipkart_Mobiles",
//            URL = "https://www.flipkart.com/mobiles/mi~brand/pr?sid=tyy%2C4io&otracker=nmenu_sub_Electronics_0_Mi&page=3"
//        },
//        ParentContainer = new ElementMapping()
//        {
//            TargetName = "ParentContainer",
//            XPath = XPathType.Relative.ForClass("_1HmYoV _35HD7C")
//        },
//        Container = new ElementMapping()
//        {
//            TargetName = "Container",
//            XPath = XPathType.Relative.ForClass("bhgxx2")
//        },
//        ProductDetails = new List<ElementMapping>()
//            {
//                new ElementMapping() {
//                    TargetName = "Title", XPath = XPathType.Relative.ForClass("_3wU53n")
//                },
//                new ElementMapping() {
//                    TargetName = "MRP", XPath = XPathType.Relative.ForClass("_3auQ3N _2GcJzG")
//                },
//                new ElementMapping() {
//                    TargetName = "OfferPrice", XPath = XPathType.Relative.ForClass("_1vC4OE _2rQ-NK")
//                },
//                new ElementMapping() {
//                    TargetName = "PercentOff", XPath = XPathType.Relative.ForClass("VGWI6T")
//                },
//                new ElementMapping() {
//                    TargetName = "Features", XPath = XPathType.Relative.ForClass("vFw0gD")
//                }
//            },
//        PagingType = PaginationType.LoadOnClick,
//        Pagination = new ElementMapping()
//        {
//            TargetName = "Pagination",
//            XPath = XPathType.Relative.ForClass("_1ypTlJ")
//        },
//        ActivePageClass = "fyt9Eu",
//        DisabledPageClass = "disabled",
//        ShouldLimitPaging = true,
//        PagingLimit = 1
//    };

//    WebScrapeInput shiLaptops = new WebScrapeInput()
//    {
//        Website = new WebsiteInformation()
//        {
//            Name = "SHI_Laptops",
//            URL = "https://www.shi.com/shop/search/hardware/computers/notebooks-and-tablets?k=laptops&p=1,1000"
//        },
//        ParentContainer = new ElementMapping()
//        {
//            TargetName = "ParentContainer",
//            XPath = XPathType.Absolute.ForClass("srResults")
//        },
//        Container = new ElementMapping()
//        {
//            TargetName = "Container",
//            XPath = XPathType.Relative.ForClass("srProduct")
//        },
//        ProductDetails = new List<ElementMapping>()
//            {
//                new ElementMapping() {
//                    TargetName = "Title", XPath = XPathType.Relative.ForClass("srh_pr.pnm")
//                },
//                new ElementMapping() {
//                    TargetName = "MRP", XPath = XPathType.Relative.ForClass("srStockMSRP"), RemoveText = "MSRP:"
//                },
//                new ElementMapping() {
//                    TargetName = "OfferPrice", XPath = ".//price-range"
//                },
//                new ElementMapping() {
//                    TargetName = "Features", XPath = ".//ul"
//                },
//                new ElementMapping() {
//                    TargetName = "Manufacture Part Number", XPath = XPathType.Relative.ForClass("srMFR"), RemoveText = "Mfr Part #:"
//                },
//                new ElementMapping() {
//                    TargetName = "SHI Part Number", XPath = XPathType.Relative.ForClass("srPart"), RemoveText = "SHI Part #:"
//                }
//            },
//        PagingType = PaginationType.LoadOnClick,
//        Pagination = new ElementMapping()
//        {
//            TargetName = "Pagination",
//            XPath = XPathType.Absolute.ForClass("searchPages") //"//*[contains(@class, 'searchPages')]"
//        },
//        HasNextButton = true,
//        NextPaginationButton = new ElementMapping()
//        {
//            TargetName = "Next Page",
//            XPath = XPathType.Relative.ForClass("srh_bt.nxtp")
//        },
//        ActivePageClass = "active",
//        DisabledPageClass = "disabled",
//        ShouldLimitPaging = false,
//        PagingLimit = 1
//    };

//    WebScrapeInput amazonLaptops = new WebScrapeInput()
//    {
//        Website = new WebsiteInformation()
//        {
//            Name = "amazon_Laptops",
//            URL = "https://www.amazon.in/s?i=computers&rh=n%3A976392031%2Cn%3A976393031%2Cn%3A1375424031&page=2"
//        },
//        ParentContainer = new ElementMapping()
//        {
//            TargetName = "ParentContainer",
//            XPath = XPathType.Relative.ForClass("s-result-list")
//        },
//        Container = new ElementMapping()
//        {
//            TargetName = "Container",
//            XPath = XPathType.Relative.ForClass("s-result-item")
//        },
//        ProductDetails = new List<ElementMapping>()
//            {
//                new ElementMapping() {
//                    TargetName = "Title", XPath = XPathType.Relative.ForClass("a-size-medium a-color-base a-text-normal")
//                },
//                new ElementMapping() {
//                    TargetName = "MRP", XPath = ".//*[@data-a-strike='true']/span[1]"
//                },
//                new ElementMapping() {
//                    TargetName = "OfferPrice", XPath = ".//*[@data-a-color='price']/span[1]"
//                },
//                new ElementMapping() {
//                    TargetName = "DetailsURL", XPath = XPathType.Relative.ForClass("a-link-normal a-text-normal"), AttributeName = "href"
//                },
//                new ElementMapping() {
//                    TargetName = "ProductImageURL", XPath = XPathType.Relative.ForClass("s-image"), AttributeName = "src"
//                },
//                new ElementMapping() {
//                    TargetName = "ASIN", XPath = ".//*[@data-asin]", AttributeName = "data-asin"
//                }

//            },
//        PagingType = PaginationType.LoadOnClick,
//        Pagination = new ElementMapping()
//        {
//            TargetName = "Pagination",
//            XPath = XPathType.Relative.ForClass("a-pagination")
//        },
//        ActivePageClass = "a-selected",
//        DisabledPageClass = "a-disabled",
//        ShouldLimitPaging = false,
//        PagingLimit = 1
//    };

//    WebScrapeInput amazonDellLaptops = new WebScrapeInput()
//    {
//        Website = new WebsiteInformation()
//        {
//            Name = "amazon_Dell_Laptops",
//            URL = "https://www.amazon.in/s?bbn=1375424031&rh=n%3A976392031%2Cn%3A%21976393031%2Cn%3A1375424031%2Cp_89%3ADell&dc&fst=as%3Aoff&qid=1565184445&rnid=3837712031&ref=lp_1375424031_nr_p_89_0"
//        },
//        ParentContainer = new ElementMapping()
//        {
//            TargetName = "ParentContainer",
//            XPath = XPathType.Relative.ForClass("s-result-list")
//        },
//        Container = new ElementMapping()
//        {
//            TargetName = "Container",
//            XPath = XPathType.Relative.ForClass("s-result-item")
//        },
//        ProductDetails = new List<ElementMapping>()
//            {
//                new ElementMapping() {
//                    TargetName = "Title", XPath = XPathType.Relative.ForClass("a-size-medium a-color-base a-text-normal")
//                },
//                new ElementMapping() {
//                    TargetName = "MRP", XPath = ".//*[@data-a-strike='true']/span[1]"
//                },
//                new ElementMapping() {
//                    TargetName = "OfferPrice", XPath = ".//*[@data-a-color='price']/span[1]"
//                },
//                new ElementMapping() {
//                    TargetName = "DetailsURL", XPath = XPathType.Relative.ForClass("a-link-normal a-text-normal"), AttributeName = "href"
//                },
//                new ElementMapping() {
//                    TargetName = "ProductImageURL", XPath = XPathType.Relative.ForClass("s-image"), AttributeName = "src"
//                },
//                new ElementMapping() {
//                    TargetName = "ASIN", XPath = ".//*[@data-asin]", AttributeName = "data-asin"
//                }

//            },
//        PagingType = PaginationType.LoadOnClick,
//        Pagination = new ElementMapping()
//        {
//            TargetName = "Pagination",
//            XPath = XPathType.Relative.ForClass("a-pagination")
//        },
//        ActivePageClass = "a-selected",
//        DisabledPageClass = "a-disabled",
//        ShouldLimitPaging = false,
//        PagingLimit = 1
//    };

//    WebScrapeInput cdwDesktops = new WebScrapeInput()
//    {
//        Website = new WebsiteInformation()
//        {
//            Name = "CDW_Desktops",
//            URL = "https://www.cdw.com/search/computers/desktop-computers/?w=c2&maxrecords=72"
//        },
//        ParentContainer = new ElementMapping()
//        {
//            TargetName = "ParentContainer",
//            XPath = XPathType.Absolute.ForClass("search-results")
//        },
//        Container = new ElementMapping()
//        {
//            TargetName = "Container",
//            XPath = XPathType.Relative.ForClass("search-result")
//        },
//        ProductDetails = new List<ElementMapping>()
//            {
//                new ElementMapping() {
//                    TargetName = "Title", XPath = XPathType.Relative.ForClass("search-result-product-url")
//                },
//                new ElementMapping() {
//                    TargetName = "MRP", XPath = XPathType.Relative.ForClass("price-msrp single")
//                },
//                new ElementMapping() {
//                    TargetName = "OfferPrice", XPath =XPathType.Relative.ForClass("price-type-price")
//                },
//                new ElementMapping() {
//                    TargetName = "Features", XPath = XPathType.Relative.ForClass("product-specs")
//                },
//                new ElementMapping() {
//                    TargetName = "DetailsURL", XPath = XPathType.Relative.ForClass("search-result-product-url"), AttributeName = "href"
//                },
//                new ElementMapping() {
//                    TargetName = "ProductImageURL", XPath = XPathType.Relative.ForClass("search-result-product-image") + "/img", AttributeName = "src"
//                },
//                new ElementMapping() {
//                    TargetName = "Manufacture Part Number", XPath = XPathType.Relative.ForClass("mfg-code"), RemoveText = "MFG#:"
//                },
//                new ElementMapping() {
//                    TargetName = "CDW Part Number", XPath = XPathType.Relative.ForClass("cdw-code"), RemoveText = "CDW#:"
//                }
//            },
//        PagingType = PaginationType.LoadOnClick,
//        Pagination = new ElementMapping()
//        {
//            TargetName = "Pagination",
//            XPath = XPathType.Absolute.ForClass("search-pagination-list-container")
//        },
//        ActivePageClass = "search-pagination-active",
//        DisabledPageClass = "",
//        ShouldLimitPaging = true,
//        PagingLimit = 1
//    };

//    WebScrapeInput cdwThinClients = new WebScrapeInput()
//    {
//        Website = new WebsiteInformation()
//        {
//            Name = "CDW_ThinClients",
//            URL = "https://www.cdw.com/search/Computers/Thin-Clients/?w=C4&maxrecords=48"
//        },
//        ParentContainer = new ElementMapping()
//        {
//            TargetName = "ParentContainer",
//            XPath = XPathType.Absolute.ForClass("search-results")
//        },
//        Container = new ElementMapping()
//        {
//            TargetName = "Container",
//            XPath = XPathType.Relative.ForClass("search-result")
//        },
//        ProductDetails = new List<ElementMapping>()
//            {
//                new ElementMapping() {
//                    TargetName = "Title", XPath = XPathType.Relative.ForClass("search-result-product-url")
//                },
//                new ElementMapping() {
//                    TargetName = "MRP", XPath = XPathType.Relative.ForClass("price-msrp single")
//                },
//                new ElementMapping() {
//                    TargetName = "OfferPrice", XPath =XPathType.Relative.ForClass("price-type-price")
//                },
//                new ElementMapping() {
//                    TargetName = "Features", XPath = XPathType.Relative.ForClass("product-specs")
//                },
//                new ElementMapping() {
//                    TargetName = "DetailsURL", XPath = XPathType.Relative.ForClass("search-result-product-url"), AttributeName = "href"
//                },
//                new ElementMapping() {
//                    TargetName = "ProductImageURL", XPath = XPathType.Relative.ForClass("search-result-product-image") + "/img", AttributeName = "src"
//                },
//                new ElementMapping() {
//                    TargetName = "Manufacture Part Number", XPath = XPathType.Relative.ForClass("mfg-code"), RemoveText = "MFG#:"
//                },
//                new ElementMapping() {
//                    TargetName = "CDW Part Number", XPath = XPathType.Relative.ForClass("cdw-code"), RemoveText = "CDW#:"
//                }
//            },
//        PagingType = PaginationType.LoadOnClick,
//        Pagination = new ElementMapping()
//        {
//            TargetName = "Pagination",
//            XPath = XPathType.Absolute.ForClass("search-pagination-list-container")
//        },
//        ActivePageClass = "search-pagination-active",
//        DisabledPageClass = "",
//        ShouldLimitPaging = false,
//        PagingLimit = 2
//    };


//    WebScrapeInput cromaSmartTVs = new WebScrapeInput()
//    {
//        Website = new WebsiteInformation()
//        {
//            Name = "Croma_SmartTVs",
//            URL = "https://www.croma.com/campaign/c/3000?q=%3Arelevance%3AskuStockFlag%3Atrue%3Acategory%3A998&text=#"
//        },
//        ParentContainer = new ElementMapping()
//        {
//            TargetName = "ParentContainer",
//            XPath = XPathType.Absolute.ForClass("product-list-view")
//        },
//        Container = new ElementMapping()
//        {
//            TargetName = "Container",
//            XPath = XPathType.Relative.ForClass("product__list--item")
//        },
//        ProductDetails = new List<ElementMapping>()
//            {
//                new ElementMapping() {
//                    TargetName = "Title", XPath = XPathType.Relative.ForClass("product__list--name")
//                },
//                new ElementMapping() {
//                    TargetName = "MRP", XPath = XPathType.Relative.ForClass("pdpPriceMrp")
//                },
//                new ElementMapping() {
//                    TargetName = "OfferPrice", XPath = XPathType.Relative.ForClass("pdpPrice")
//                },
//                new ElementMapping() {
//                    TargetName = "PercentOff", XPath = XPathType.Relative.ForClass("listingDiscnt")
//                },
//                new ElementMapping() {
//                    TargetName = "Features", XPath = XPathType.Relative.ForClass("listmian-features", "ul")
//                }
//            },
//        PagingType = PaginationType.LoadOnClick,
//        Pagination = new ElementMapping()
//        {
//            TargetName = "Pagination",
//            XPath = XPathType.Relative.ForClass("pagination")
//        },
//        HasNextButton = true,
//        NextPaginationButton = new ElementMapping()
//        {
//            TargetName = "Next",
//            XPath = XPathType.Relative.ForClass("pagination-next")
//        },
//        ActivePageClass = "",
//        DisabledPageClass = "disabled",
//        ShouldLimitPaging = false,
//        PagingLimit = 1
//    };


//    WebScrapeInput amazonMobiles = new WebScrapeInput()
//    {
//        Website = new WebsiteInformation()
//        {
//            Name = "Amazon_Mobiles",
//            URL = "https://www.amazon.in/s?rh=n%3A976419031%2Cn%3A%21976420031%2Cn%3A1389401031&page=2&qid=1565255166&ref=lp_1389401031_pg_2"
//        },
//        ParentContainer = new ElementMapping()
//        {
//            XPath = XPathType.Relative.ForClass("s-search-results")
//        },
//        Container = new ElementMapping()
//        {
//            XPath = XPathType.Relative.ForClass(".s-include-content-margin")
//        },
//        ProductDetails = new List<ElementMapping>()
//        {
//            new ElementMapping()
//            {
//                TargetName = "Title", XPath = XPathType.Relative.ForClass(".a-color-base.a-text-normal")
//            },
//            new ElementMapping()
//            {
//                TargetName = "OfferPrice", XPath = XPathType.Relative.ForClass(".a-price-whole")
//            },
//            new ElementMapping()
//            {
//                TargetName = "MRP", XPath = XPathType.Relative.ForClass(".a-text-price", "span")
//            },
//            new ElementMapping()
//            {
//                TargetName = "ProductImageURL", XPath = XPathType.Relative.ForClass(".s-image"), AttributeName = "src"
//            },
//        },
//        PagingType = PaginationType.LoadOnClick,
//        Pagination = new ElementMapping()
//        {
//            XPath = XPathType.Relative.ForClass(".a-text-center")
//        },
//        HasNextButton = true,
//        NextPaginationButton = new ElementMapping()
//        {
//            XPath = XPathType.Relative.ForClass(".a-last", "a")
//        },
//        ShouldLimitPaging = true,
//        PagingLimit = 1
//    };

//    var result = new List<WebScrapeInput>
//    {
//        //cromaMobiles,
//        //locator,
//        //flipkartMobiles,
//        //shiLaptops,
//        //flipkartLaptops,
//        //amazonLaptops,
//        //cdwDesktops,
//        //amazonDellLaptops,
//        //cdwThinClients,
//        //cromaSmartTVs,
//        amazonMobiles
//    };

//    return result;
//}
