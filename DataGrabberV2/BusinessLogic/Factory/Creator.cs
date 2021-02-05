using DataGrabberV2.BusinessLogic.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabberV2.BusinessLogic.Factory
{
    public abstract class Creator
    {

        public abstract IScraper Factory();


        public static IScraper GetInstance(string scrapingMechanism)
        {
            Creator result;

            switch (scrapingMechanism)
            {
                case "Selenium":
                    result = new ConcreteCreatorSelenium();
                    break;

                case "RestClient":
                    result = new ConcreteCreatorRestClient();
                    break;

                default:
                    result = new ConcreteCreatorSelenium();
                    break;
            }

            return result.Factory();
        }


        //public static IScraper SetFactory<T>(T obj) where T : new()
        //{
        //    switch (obj.GetType().Name)
        //    {
        //        case nameof(ConcreteCreatorSelenium):
        //            return new SeleniumScraper();

        //        case nameof(ConcreteCreatorRestClient):
        //            return new SeleniumScraper();

        //        default:
        //            return new SeleniumScraper();
        //    }
        //}

    }
}
