using GoogleRecaptchaV2.BusinessLogic.Contract;
using GoogleRecaptchaV2.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleRecaptchaV2.BusinessLogic.Factory
{
    public class ConcreteCreatorAudio : GoogleRecaptchaCreator
    {
        public override ICaptchaSolver Factory()
        {
            return new AudioCaptchaSolver();
        }

    }
}
