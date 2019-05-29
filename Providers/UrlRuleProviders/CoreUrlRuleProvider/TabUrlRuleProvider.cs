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
using DotNetNuke.Entities.Tabs;
using System.Collections;

using System.Collections.Generic;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Framework.Providers;
using DotNetNuke.Services.Localization;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Cache;
using DotNetNuke.Common;
using DotNetNuke.Instrumentation;

namespace Satrabel.HttpModules.Provider
{

    public class TabUrlRuleProvider : UrlRuleProvider
    {
        
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(TabUrlRuleProvider));

        private const string ProviderName = "tabUrlRuleProvider";
        private readonly bool UseKeyWordsDefault;

        public TabUrlRuleProvider()
        {
            UseKeyWordsDefault = GetProviderSettingAsBoolean("tabUrlRuleProvider", "useKeyWords", false);
            Settings = new UrlRuleSetting[] { 
                new UrlRuleSetting("UseKeyWords", UseKeyWordsDefault), 
                new UrlRuleSetting("RemoveHomePage", false),
                //new UrlRuleSetting("CacheDependency", true) 
            };
            HelpUrl = "https://openurlrewriter.codeplex.com/wikipage?title=TabProvider";
            HostProvider = true;
        }

        public override List<UrlRule> GetRules(int PortalId)
        {
            bool useKeyWords = GetPortalSettingAsBoolean(PortalId, "UseKeyWords");
            bool RemoveHomePage = GetPortalSettingAsBoolean(PortalId, "RemoveHomePage");


            List<UrlRule> Rules = new List<UrlRule>();
            TabController tc = new TabController();
            Locale DefaultLocale = LocaleController.Instance.GetDefaultLocale(PortalId);
            Dictionary<string, Locale> dicLocales = LocaleController.Instance.GetLocales(PortalId);
            PortalInfo objPortal = new PortalController().GetPortal(PortalId, DefaultLocale.Code);
            int DefaultHomeTabId = -1;
            if (objPortal != null)
                DefaultHomeTabId = objPortal.HomeTabId;


            var tabs = tc.GetTabsByPortal(PortalId).Values;
            foreach (TabInfo tab in tabs)
            {
                if ( /*tab.PortalID > -1 && !tab.TabPath.StartsWith(@"//Admin//") && tab.TabPath != @"//Admin" &&*/  !tab.DisableLink /*&& tab.TabType == TabType.Normal*/ && !tab.IsDeleted)
                {

                    bool RemoveTab = RemoveHomePage && tab.TabID == DefaultHomeTabId;
                    bool MLNeutralHomeTab = LocaleController.Instance.GetLocales(PortalId).Count > 1 &&
                            tab.TabID == DefaultHomeTabId && string.IsNullOrEmpty(tab.CultureCode);

                    string cultureCode = tab.CultureCode;
                    string ruleCultureCode = (dicLocales.Count > 1 ? cultureCode : null);

                    var rule = new UrlRule
                    {
                        RuleType = UrlRuleType.Tab,
                        CultureCode = ruleCultureCode,
                        TabId = tab.TabID,
                        Parameters = "tabid=" + tab.TabID.ToString(),
                        Action = UrlRuleAction.Rewrite,
                        Url = CleanupUrl(GetTabUrl(tab, useKeyWords)),
                        RemoveTab = RemoveTab && !MLNeutralHomeTab
                    };

                    TabInfo parentTab = tab;
                    while (parentTab.ParentId != Null.NullInteger)
                    {
                        parentTab = tc.GetTab(parentTab.ParentId, PortalId, false);
                        rule.Url = CleanupUrl(GetTabUrl(parentTab, useKeyWords)) + "/" + rule.Url;
                    }
#if DNN71
                    var tabUrl = tab.TabUrls.SingleOrDefault(t =>  /*t.IsSystem
                                                                &&*/ t.HttpStatus == "200"
                                                                        && t.SeqNum == 0);
                    if (tabUrl != null && tabUrl.Url.Trim('/') != "")
                    {
                        rule.Url = tabUrl.Url.Trim('/');
                    }
#endif
                    bool ok2Continue = true;

                    if (tab.TabType != TabType.Normal)
                    {
                        string redirUrl = "";
                        switch (tab.TabType)
                        {
                            case TabType.Tab:
                                //Get the tab being linked to
                                TabInfo tempTab = new TabController().GetTab(Int32.Parse(tab.Url), tab.PortalID, false);
                                if (tempTab == null)
                                {
                                    Logger.Error(string.Format("Tab {0} of portal {1} redirects to a tab ({2}) that doesn't exist anymore", tab.TabPath, tab.PortalID, tab.Url));
                                    ok2Continue = false;
                                }
                                else
                                {
                                    redirUrl = tempTab.TabPath.Replace("//", "/").Substring(1);
                                }
                                break;
                            case TabType.File:
                                //var file = FileManager.Instance.GetFile(Int32.Parse(tab.Url.Substring(7)));
                                //tabUrl = file.RelativePath;
                                break;
                            case TabType.Url:
                                redirUrl = tab.Url;
                                break;
                        }
                        rule.Action = UrlRuleAction.Redirect;
                        rule.RedirectStatus = tab.PermanentRedirect ? 301 : 302;
                        rule.RedirectDestination = redirUrl;
                    }

                    if (ok2Continue)
                    {
                        Rules.Add(rule);
                        var ruleRedirect = new UrlRule
                        {
                            RuleType = UrlRuleType.Tab,
                            CultureCode = tab.CultureCode,
                            TabId = tab.TabID,
                            Parameters = "tabid=" + tab.TabID.ToString(),
                            Action = UrlRuleAction.Redirect,
                            Url = tab.TabPath.Replace("//", "/").TrimStart('/').ToLower(),
                            RedirectDestination = rule.Url,
                            RemoveTab = RemoveTab && !MLNeutralHomeTab
                        };
                        if (rule.Url != ruleRedirect.Url)
                        {
                            Rules.Add(ruleRedirect);
                        }

                        // if RemoveTab for multi-language and neutral home page
                        // add a culture specific rewrite
                        if (RemoveTab && MLNeutralHomeTab)
                        {
                            var ruleNeutral = new UrlRule
                            {
                                RuleType = UrlRuleType.Tab,
                                CultureCode = DefaultLocale.Code,
                                TabId = tab.TabID,
                                Parameters = rule.Parameters,
                                Action = UrlRuleAction.Rewrite,
                                Url = rule.Url,
                                RemoveTab = true
                            };
                            Rules.Add(ruleNeutral);
                            var ruleRedirectNeutral = new UrlRule
                            {
                                RuleType = UrlRuleType.Tab,
                                CultureCode = DefaultLocale.Code,
                                TabId = tab.TabID,
                                Parameters = ruleRedirect.Parameters,
                                Action = UrlRuleAction.Redirect,
                                Url = ruleRedirect.Url,
                                RedirectDestination = ruleNeutral.Url,
                                RemoveTab = true
                            };
                            if (ruleNeutral.Url != ruleRedirectNeutral.Url)
                            {
                                Rules.Add(ruleRedirectNeutral);
                            }

                        }
                    }
                }
            }
            return Rules;
        }

        protected string GetTabUrl(TabInfo tab, bool useKeyWords)
        {
            if (useKeyWords && tab.KeyWords != "")
            {
                string[] keys = tab.KeyWords.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (keys.Length > 0 && keys[0].Trim() != "")
                {
                    return keys[0].Trim();
                }
            }


            return tab.TabName;
        }

        public override string[] GetCacheKeys(int PortalId)
        {
            var CacheKeys = new List<string>();
            string cacheKey = string.Format(DataCache.TabCacheKey, PortalId);
            CacheKeys.Add(cacheKey);

#if DNN71
            cacheKey = string.Format(DataCache.TabUrlCacheKey, PortalId);
            CacheKeys.Add(cacheKey);
#endif
            return CacheKeys.ToArray();
        }


    }
}