using DataGrabber.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrabber.Models
{
    public static class ModelHelper
    {

        public static bool SetYesOrNo(this string text)
        {
            return (!string.IsNullOrEmpty(text) && text.ToLower() == "yes");
        }


        public static WebScrapeOutput Clone(this WebScrapeOutput output)
        {
            var serialized = JsonConvert.SerializeObject(output);
            return JsonConvert.DeserializeObject<WebScrapeOutput>(serialized);
        }


        public static string ForClass(this XPathType xPath, string name, string additionalXPath = "")
        {
            string result = string.Empty;
            name = name.Replace(".", " ").Trim();
            result = (xPath == XPathType.Relative) ? "." : result;
            result += $"//*[contains(concat(' ', normalize-space(@class), ' '), ' {name} ')]";
            result += string.IsNullOrEmpty(additionalXPath) ? string.Empty : $"/{additionalXPath.TrimStart('/')}";
            return result;
        }

        public static string ForID(this XPathType xPath, string name, string additionalXPath = "")
        {
            string result = string.Empty;
            name = name.Replace("#", " ").Trim();
            result = (xPath == XPathType.Relative) ? "." : result;
            result += $"//*[@id='{name}']";
            result += string.IsNullOrEmpty(additionalXPath) ? string.Empty : $"/{additionalXPath.TrimStart('/')}";
            return result;
        }

        public static string ForTag(this XPathType xPath, string name, string additionalXPath = "")
        {
            string result = string.Empty;
            result = (xPath == XPathType.Relative) ? "." : result;
            result += $"//{name}";
            result += string.IsNullOrEmpty(additionalXPath) ? string.Empty : $"/{additionalXPath.TrimStart('/')}";
            return result;
        }

        public static string ForXPath(this XPathType xPath, string name, string additionalXPath = "")
        {
            string result = string.Empty;
            name = name.TrimStart('.');
            // case insensitive
            if (name.Contains("contains(text()"))
            {
                name = name.Replace("//*[contains(text(),", "//text()[contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'),");
                name = name.Replace("//following-sibling::*", "/parent::*//following-sibling::*");
            }

            if (name.StartsWith("/"))
            {
                result = (xPath == XPathType.Relative) ? "." : result;
            }
            result += $"{name}";
            result += string.IsNullOrEmpty(additionalXPath) ? string.Empty : $"/{additionalXPath.TrimStart('/')}";
            return result;
        }



    }
}
