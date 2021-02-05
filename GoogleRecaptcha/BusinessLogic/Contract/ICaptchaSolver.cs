using GoogleRecaptcha.Models;
using OpenQA.Selenium.Chrome;

namespace GoogleRecaptcha.BusinessLogic.Contract
{
    public interface ICaptchaSolver
    {

        CaptchaResponse Result { get; set; }

        void StartProcess();

        void Initialize(string GoogleCaptchaBasePath, ChromeDriver driver);

    }
}
