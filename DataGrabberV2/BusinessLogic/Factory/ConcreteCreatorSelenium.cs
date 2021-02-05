using DataGrabberV2.BusinessLogic.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabberV2.BusinessLogic.Factory
{
    public class ConcreteCreatorSelenium : Creator
    {

        public override IScraper Factory()
        {
            return SeleniumScraper.Instance;
        }

    }
}
