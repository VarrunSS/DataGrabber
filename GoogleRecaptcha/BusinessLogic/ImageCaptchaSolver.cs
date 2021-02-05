using GoogleRecaptcha.BusinessLogic.Contract;
using GoogleRecaptcha.Models;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleRecaptcha.BusinessLogic
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
