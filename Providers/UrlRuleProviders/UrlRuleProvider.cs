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
using System.Collections.Generic;
using System.Text;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Framework.Providers;
using DotNetNuke.Instrumentation;


namespace Satrabel.HttpModules.Provider
{
    /// <summary>
    /// Description résumée de UrlProvider
    /// </summary>
    public abstract class UrlRuleProvider
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(UrlRuleProvider));
        protected UrlRuleProvider(){}
        
        public string Name {
            get {
                return this.GetType().Name;            
            }
        }
        public string HelpUrl { get; set; }

        public bool HostProvider { get; set; } // for host menu

        public abstract List<UrlRule> GetRules(int PortalId);

        [Obsolete("CacheKeys is deprecated, please override GetCacheKeys instead.")]
        public string[] CacheKeys { get; set; }

        public virtual string[] GetCacheKeys(int PortalId) {
            return new string[0];
        }

        public UrlRuleSetting[] Settings { get; set; }

        public bool GetPortalSettingAsBoolean(int portalID, string key)
        {
            bool DefaultValue = false;
            UrlRuleSetting set = Settings.SingleOrDefault(s => s.Name == key);
            if (set != null) 
            {
                DefaultValue = bool.Parse(set.DefaultValue);
            }
            return PortalController.GetPortalSettingAsBoolean( Name + "_"+ key, portalID, DefaultValue);
        }

        
        private readonly ProviderConfiguration _providerConfiguration = ProviderConfiguration.GetProviderConfiguration("urlRule");

        public bool GetProviderSettingAsBoolean(string ProviderName, string key, bool DefaultValue)
        {
            bool Value = DefaultValue;
            var objProvider = (DotNetNuke.Framework.Providers.Provider)_providerConfiguration.Providers[ProviderName];
            if (!String.IsNullOrEmpty(objProvider.Attributes[key]))
            {
                try
                {
                    Value = bool.Parse(objProvider.Attributes[key]);
                }
                catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
            return Value;
        }

       
        protected static string CleanupUrl(string text)
        {
            const string replaceWith = "-";

            const string accentFrom = "ÀÁÂÃÄÅàáâãäåảạăắằẳẵặấầẩẫậÒÓÔÕÖØòóôõöøỏõọồốổỗộơớờởợÈÉÊËèéêëẻẽẹếềểễệÌÍÎÏìíîïỉĩịÙÚÛÜùúûüủũụưứừửữựÿýỳỷỹỵÑñÇçĞğİıŞş₤€ßđ";
            const string accentTo = "AAAAAAaaaaaaaaaaaaaaaaaaaOOOOOOoooooooooooooooooooEEEEeeeeeeeeeeeeIIIIiiiiiiiUUUUuuuuuuuuuuuuuyyyyyyNnCcGgIiSsLEsd";

            text = text.ToLower().Trim();

            StringBuilder result = new StringBuilder(text.Length);
            int i = 0; int last = text.ToCharArray().GetUpperBound(0);
            foreach (char c in text)
            {

                //use string for manipulation
                var ch = c.ToString();
                if (ch == " ")
                {
                    ch = replaceWith;
                }
                else if (@".[]|:;`%\\""^№".Contains(ch))
                    ch = "";
                else if (@" &$+,/=?@~#<>(){}¿¡«»!'‘’–*…“”".Contains(ch))
                    ch = replaceWith;
                else
                {
                    for (int ii = 0; ii < accentFrom.Length; ii++)
                    {
                        if (ch == accentFrom[ii].ToString())
                        {
                            ch = accentTo[ii].ToString();
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
            string retval = result.ToString();
            if (!string.IsNullOrEmpty(replaceWith))
            {
                while (retval.Contains(replaceWith + replaceWith))
                {
                    retval = retval.Replace(replaceWith + replaceWith, replaceWith);
                }
                foreach (char c in replaceWith)
                {
                    retval = retval.Trim(c);
                }
            }

            return retval;
        }
    }

    public enum UrlRuleSettingType
    {
        Boolen = 1
    }

    public class UrlRuleSetting {

        string _Name;
        string _DefaultValue;
        UrlRuleSettingType _ValueType;

        public UrlRuleSetting (string Name, bool DefaultValue)
        {
            _Name = Name;
            _DefaultValue = DefaultValue.ToString();
            _ValueType = UrlRuleSettingType.Boolen;        
        }

        public string Name { get { return _Name; } }
        public string DefaultValue { get { return _DefaultValue; } }
        public UrlRuleSettingType ValueType { get { return _ValueType; } }        
    }

}