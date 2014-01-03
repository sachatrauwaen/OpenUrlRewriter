using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text;


namespace Satrabel.HttpModules.Provider
{
    /// <summary>
    /// Description résumée de UrlProvider
    /// </summary>
    public abstract class UrlRuleProvider
    {
        protected UrlRuleProvider(){}

        public abstract List<UrlRule> GetRules( int PortalId );        

        protected static string CleanupUrl(string Url)
        {
            string replaceWith = "-";

            string AccentFrom = "ÀÁÂÃÄÅàáâãäåÒÓÔÕÖØòóôõöøÈÉÊËèéêëÌÍÎÏìíîïÙÚÛÜùúûüÿÑñÇç";
            string AccentTo = "AAAAAAaaaaaaOOOOOOooooooEEEEeeeeIIIIiiiiUUUUuuuuyNnCc";

            Url = Url.ToLower();

            StringBuilder result = new StringBuilder(Url.Length);
            string ch = ""; int i = 0; int last = Url.ToCharArray().GetUpperBound(0);
            foreach (char c in Url.ToCharArray())
            {

                //use string for manipulation
                ch = c.ToString();
                if (ch == " ")
                {
                    ch = replaceWith;
                }
                else if (@".[]|:;`%\\""".Contains(ch))
                    ch = "";
                else if (@" &$+,/=?@~#<>()¿¡«»!'".Contains(ch))
                    ch = replaceWith;
                else
                {
                    for (int ii = 0; ii < AccentFrom.Length; ii++)
                    {
                        if (ch == AccentFrom[ii].ToString())
                        {
                            ch = AccentTo[ii].ToString();
                        }
                    }
                }

                if (i == last)
                {
                    if (!(ch == "-" || ch == replaceWith))
                    {   //only append if not the same as the replacement character
                        result.Append(ch);
                    }
                }
                else
                    result.Append(ch);
                i++;//increment counter
            }
            result = result.Replace(replaceWith + replaceWith, replaceWith);
            result = result.Replace(replaceWith + replaceWith, replaceWith);
            return result.ToString();
        }

    }

}