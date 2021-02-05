using GoogleRecaptcha.BusinessLogic.Contract;
using GoogleReCaptcha.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleRecaptcha.BusinessLogic.Factory
{
    public class ConcreteCreatorAudio : GoogleRecaptchaCreator
    {
        public override ICaptchaSolver Factory()
        {
            return new AudioCaptchaSolver();
        }

    }
}
