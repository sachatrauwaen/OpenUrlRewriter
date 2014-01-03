using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;

using System.Collections;

using System.Collections.Generic;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Services.Localization;

namespace Satrabel.HttpModules.Provider
{
    public class LanguageUrlRuleProvider : UrlRuleProvider
    {
        public LanguageUrlRuleProvider(){}

        public override List<UrlRule> GetRules(int PortalId)
        {
            List<UrlRule> Rules = new List<UrlRule>();
            var Locales = LocaleController.Instance.GetLocales(PortalId).Values;
            foreach (Locale locale in Locales)
            {                
                string LocaleUrl;
                // more then 1 locale with same language part
                if (Locales.Count(l => l.Code.Substring(0, 2) == locale.Code.Substring(0, 2)) > 1)
                    LocaleUrl = locale.Code;
                else
                    LocaleUrl = locale.Code.Substring(0, 2);

                var rule = new UrlRule
                {
                    RuleType = UrlRuleType.Culture,
                    CultureCode = locale.Code,
                    Parameters = "language="+locale.Code,
                    Action = UrlRuleAction.Rewrite,
                    Url = LocaleUrl.ToLower()
                };
                Rules.Add(rule);
                var ruleRedirect = new UrlRule
                {
                    RuleType = UrlRuleType.Culture,
                    CultureCode = locale.Code,
                    Parameters = "language=" + locale.Code,
                    Action = UrlRuleAction.Redirect,
                    Url = locale.Code.ToLower(),
                    RedirectDestination = rule.Url
                };
                if (rule.Url != ruleRedirect.Url)
                {
                    Rules.Add(ruleRedirect);
                }
            }           
            return Rules;
        }
    }

}