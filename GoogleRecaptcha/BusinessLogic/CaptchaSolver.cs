using DataGrabber.LogWriter;
using DataGrabber.Utility;
using GoogleRecaptcha.BusinessLogic.Contract;
using GoogleRecaptcha.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleRecaptcha.BusinessLogic
{
    public abstract class CaptchaSolver : ICaptchaSolver
    {

        public abstract CaptchaResponse Result { get; set; }

        public abstract void Initialize(string GoogleCaptchaBasePath, ChromeDriver driver);

        public abstract void StartProcess();



        public virtual void CheckIfGoogleCaptchaIsPresent(ChromeDriver Driver)
        {
            // Check if google captcha is present or not
            By googleElemPath = By.XPath("//*[contains(@class, 'g-recaptcha ')]");
            if (!Driver.IsElementPresent(googleElemPath))
            {
                Logger.Write($"ERROR: Unable to find google captcha element -> GoogleReCaptcha.");
                Result = new CaptchaResponse(true, "Captcha Not Found");
                return;
            }
            else
            {
                Logger.Write($"INFO: google captcha element found -> GoogleReCaptcha.");
                Result = new CaptchaResponse(true, "Captcha Found");
                return;

            }
        }



    }
}
