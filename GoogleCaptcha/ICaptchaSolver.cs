using GoogleCaptcha.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleCaptcha
{
   public interface ICaptchaSolver
    {

        CaptchaResponse Result { get; set; }

        void StartProcess();
    }
}
