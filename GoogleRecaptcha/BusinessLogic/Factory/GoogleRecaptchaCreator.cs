using GoogleRecaptcha.BusinessLogic.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleRecaptcha.BusinessLogic.Factory
{
    public abstract class GoogleRecaptchaCreator
    {

        public abstract ICaptchaSolver Factory();


        public static ICaptchaSolver GetInstance(string captchaType)
        {
            GoogleRecaptchaCreator result;

            switch (captchaType.ToLower())
            {
                case "audio":
                    result = new ConcreteCreatorAudio();
                    break;
                    
                case "image":
                    result = new ConcreteCreatorImage();
                    break;

                default:
                    result = new ConcreteCreatorAudio();
                    break;
            }

            return result.Factory();

        }

    }
}
