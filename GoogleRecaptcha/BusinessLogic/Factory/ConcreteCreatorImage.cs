using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleRecaptcha.BusinessLogic.Contract;

namespace GoogleRecaptcha.BusinessLogic.Factory
{
    public class ConcreteCreatorImage : GoogleRecaptchaCreator
    {

        public override ICaptchaSolver Factory()
        {
            return new ImageCaptchaSolver();
        }

    }
}
