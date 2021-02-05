using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleRecaptcha.Models
{
    public class CaptchaResponse
    {
        public CaptchaResponse()
        {
            IsCaptchaSolved = false;
            CaptchaText = string.Empty;
        }

        public CaptchaResponse(bool IsCaptchaSolved, string CaptchaText)
        {
            this.IsCaptchaSolved = IsCaptchaSolved;
            this.CaptchaText = CaptchaText;
        }

        public bool IsCaptchaSolved { get; set; }

        public string CaptchaText { get; set; }
    }
}
