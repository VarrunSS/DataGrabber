using DataGrabber.BusinessLogic.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber.BusinessLogic.Factory
{
    public class ConcreteCreatorSelenium : Creator
    {

        public override IScraper Factory()
        {
            return SeleniumScraper.Instance;
        }

    }
}
