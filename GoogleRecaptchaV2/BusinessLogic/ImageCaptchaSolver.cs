using GoogleRecaptchaV2.BusinessLogic.Contract;
using GoogleRecaptchaV2.Models;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleRecaptchaV2.BusinessLogic
{
    public partial class ImageCaptchaSolver : CaptchaSolver, ICaptchaSolver
    {
        public override CaptchaResponse Result { get; set; }

        private ChromeDriver Driver { get; set; }


    }

    public partial class ImageCaptchaSolver : CaptchaSolver, ICaptchaSolver
    {

        public override void StartProcess()
        {
        }

        public override void Initialize(string GoogleCaptchaBasePath, ChromeDriver driver)
        {
            throw new NotImplementedException();
        }
    }
}
