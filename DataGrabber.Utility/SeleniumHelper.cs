using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Text;
using System.Threading.Tasks;
using DataGrabber.LogWriter;

namespace DataGrabber.Utility
{
    public static class SeleniumHelper
    {


        public static IJavaScriptExecutor Scripts(this IWebDriver driver)
        {
            return (IJavaScriptExecutor)driver;
        }

        public static void ClickIfDisplayed(this IWebElement elem, ChromeDriver driver)
        {
            try
            {
                if (elem != null && elem.Displayed)
                {
                    // set z-index to max
                    driver.Scripts().ExecuteScript(
                        @"
                        const element = arguments[0];
                        const elementRect = element.getBoundingClientRect();
                        const absoluteElementTop = elementRect.top + window.pageYOffset;
                        const middle = absoluteElementTop - (window.innerHeight / 2);
                        window.scrollTo(0, middle);

                        arguments[0].setAttribute('style', 'z-index:2147483638;position:absolute;');
                        ", elem);

                    elem.Click();
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static void MaxZIndex(this IWebElement elem, ChromeDriver driver)
        {
            try
            {
                if (elem != null && elem.Displayed)
                {
                    // set z-index to max
                    driver.Scripts().ExecuteScript(
                        @"
                        arguments[0].setAttribute('style', 'z-index:2147483638;position:absolute;');
                        ", elem);
                }
            }
            catch (Exception ex)
            {

            }

        }



        public static WebDriverWait GetWebDriverWait(this ChromeDriver driver)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromMinutes(ConfigFields.MaxBrowserWaitTimeInMinutes));
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(ElementNotVisibleException));
            return wait;
        }

        public static string GetPageSource(this ChromeDriver driver)
        {
            String pageSource = driver.Scripts().ExecuteScript("return document.getElementsByTagName('html')[0].outerHTML; ").ToString();
            return pageSource;
        }

        public static bool IsElementPresent(this ChromeDriver driver, By by)
        {
            try
            {
                driver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public static void SetCookie(this ChromeDriver driver, string cookieName, string cookieValue, string domain, string path = "/")
        {
            try
            {
                Cookie cookie = new Cookie(cookieName, cookieValue, domain, path, DateTime.Now.AddYears(1));

                while (driver.Manage().Cookies.GetCookieNamed(cookieName) != null)
                {
                    driver.Manage().Cookies.DeleteCookieNamed(cookieName);
                }

                driver.Manage().Cookies.AddCookie(cookie);
            }
            catch (Exception ex)
            {

            }
        }

        public static void SetCookie(this ChromeDriver driver, List<Cookie> cookies)
        {
            try
            {
                driver.Manage().Cookies.DeleteAllCookies();

                foreach (var cookie in cookies)
                {
                    driver.Manage().Cookies.AddCookie(cookie);
                }
            }
            catch (Exception ex)
            {

            }
        }


        public static void WaitForSomeSeconds(this ChromeDriver driver, double delay)
        {
            if (delay == 0) return;
            try
            {
                // Causes the WebDriver to wait for at least a fixed delay
                double interval = 1;
                delay = delay * 1000; // converting s to ms
                var now = DateTime.Now;
                var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(delay));
                wait.PollingInterval = TimeSpan.FromMilliseconds(interval);

                wait.Until(wd => (DateTime.Now - now) - TimeSpan.FromMilliseconds(delay) > TimeSpan.Zero);

            }
            catch (Exception ex)
            {
                Logger.Write("Exception occured in WaitForSomeSeconds -- SeleniumHelper-> DataHelper. Message: " + ex.Message);
            }
        }

        public static void EmptyParentContainer(this ChromeDriver driver, string parentXPath, string paginationXPath)
        {
            try
            {
                // empty parent container
                By parentContainerPath = By.XPath(parentXPath);
                By paginationPath = By.XPath(paginationXPath);

                // check if parent container exist
                if (!driver.IsElementPresent(parentContainerPath))
                {
                    Logger.Write("ERROR: Parent container not found -- EmptyParentContainer -> DataHelper.");
                    return;
                }
                //if (!driver.IsElementPresent(paginationPath))
                //{
                //    Logger.Write("ERROR: Pagination container not found -- EmptyParentContainer -> DataHelper.");
                //    return;
                //}


                IWebElement parent = driver.FindElement(parentContainerPath);

                if (parent.TagName == "table")
                {
                    driver.Scripts().ExecuteScript(
                        @"var Parent = arguments[0];
                        while(Parent.hasChildNodes()) {
                            Parent.removeChild(Parent.firstChild);
                        }", parent.FindElement(By.TagName("tbody")));
                }
                else
                {

                    if (!driver.IsElementPresent(paginationPath))
                    {
                        driver.Scripts().ExecuteScript("arguments[0].innerHTML='';", parent);
                    }
                    else
                    {
                        IWebElement page = driver.FindElement(paginationPath);

                        driver.Scripts().ExecuteScript(
                        @"
                        var Parent = arguments[0];
                        var Page = arguments[1];
                        while(Parent.hasChildNodes()) {
                            if(Parent.firstChild.contains(Page)) break;                            
                            Parent.removeChild(Parent.firstChild);
                        }", parent, page);
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception occured in -- EmptyParentContainer-> DataHelper. Message: " + ex.Message);
            }
            finally
            {

            }
        }


        public static bool IsAlertPresent(this ChromeDriver driver)
        {
            try
            {
                driver.SwitchTo().Alert();
                return true;
            }   // try 
            catch (NoAlertPresentException Ex)
            {
                return false;
            }   // catch 
        }

    }
}
