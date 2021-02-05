using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleRecaptchaV2.BusinessLogic.Contract;

namespace GoogleRecaptchaV2.BusinessLogic.Factory
{
    public class ConcreteCreatorImage : GoogleRecaptchaCreator
    {

        public override ICaptchaSolver Factory()
        {
            return new ImageCaptchaSolver();
        }

    }
}
