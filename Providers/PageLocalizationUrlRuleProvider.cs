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
using DotNetNuke.Entities.Tabs;
using System.Collections;

using System.Collections.Generic;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Framework.Providers;
using Apollo.DNN_Localization;
using Apollo.DNN.Modules.PageLocalization;

namespace Satrabel.HttpModules.Provider
{
    
    public class PageLocalizationUrlRuleProvider : UrlRuleProvider
    {

        private const string ProviderType = "urlRule";
        private const string ProviderName = "tabUrlRuleProvider";

        private readonly ProviderConfiguration _providerConfiguration = ProviderConfiguration.GetProviderConfiguration(ProviderType);
        private readonly bool useKeyWords;

        public PageLocalizationUrlRuleProvider()
        {
            var objProvider = (DotNetNuke.Framework.Providers.Provider)_providerConfiguration.Providers[ProviderName];
            if (!String.IsNullOrEmpty(objProvider.Attributes["useKeyWords"]))
            {
                useKeyWords = bool.Parse(objProvider.Attributes["useKeyWords"]);
            }
        }

        public override List<UrlRule> GetRules(int PortalId)
        {
            List<UrlRule> Rules = new List<UrlRule>();
            TabController tc = new TabController();

            var locTabLst = PageLocalizationController.List(PortalId);

            
            foreach (LocalizedTabInfo tab in locTabLst) {
                if (tab.PortalID > -1 && !tab.TabPath.StartsWith(@"//Admin//") && tab.TabPath != @"//Admin" && !tab.DisableLink && tab.TabType == TabType.Normal)
                {
                    var rule = new UrlRule
                    {
                        RuleType = UrlRuleType.Tab,
                        CultureCode = tab.Locale,
                        PortalId = tab.PortalID,
                        TabId = tab.TabID,
                        Parameters = "tabid=" + tab.TabID.ToString(),
                        Action = UrlRuleAction.Rewrite,
                        Url = CleanupUrl(GetTabUrl(tab))
                    };

                    TabInfo parentTab = tab;
                    while (parentTab.ParentId != Null.NullInteger)
                    {
                        parentTab = tc.GetTab(parentTab.ParentId);
                        rule.Url = CleanupUrl(GetTabUrl(parentTab)) + "/" + rule.Url;
                    }
                    Rules.Add(rule);
                }
            }
        
            return Rules;
        }

        protected string GetTabUrl(TabInfo tab) {
            if (useKeyWords && tab.KeyWords != "")
            {
                string[] keys = tab.KeyWords.Trim().Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
                if (keys.Length > 0 && keys[0].Trim() != ""){
                    return keys[0].Trim();
                }            
            }
            return tab.TabName;
        }
    }
}