using GoogleRecaptchaV2.Models;
using OpenQA.Selenium.Chrome;

namespace GoogleRecaptchaV2.BusinessLogic.Contract
{
    public interface ICaptchaSolver
    {

        CaptchaResponse Result { get; set; }

        void StartProcess();

        void Initialize(string GoogleCaptchaBasePath, ChromeDriver driver);

    }
}
