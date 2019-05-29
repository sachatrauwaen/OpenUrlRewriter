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
using DotNetNuke.Common.Utilities;
using Satrabel.HttpModules.Config;
using System.Web.Caching;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Cache;
using Satrabel.HttpModules.Provider;
using System.Collections.Generic;

namespace Satrabel.OpenUrlRewriter.Components
{
    public class CacheController
    {
        
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(CacheController));

        private int _portalId;
        private IEnumerable<UrlRule> _rules;

        public CacheController(int PortalId) {
            _portalId = PortalId;
            _rules = GetUrlRuleConfig().Rules;
        }

        public const string UrlRuleConfigCacheKey = "UrlRuleConfig{0}";

        public UrlRuleConfiguration GetUrlRuleConfig()
        {
            
            string cacheKey = String.Format(UrlRuleConfigCacheKey, _portalId);                        
            var config =  CBO.GetCachedObject<UrlRuleConfiguration>(
                            new CacheItemArgs(cacheKey, DataCache.TabCacheTimeOut, DataCache.TabCachePriority, _portalId), 
                            GetUrlRuleConfigCallBack);

            if (config.Rules.Count == 0)
            {
                Logger.Error("Rules.Count = 0 -> ClearCache " + cacheKey);
                DataCache.ClearCache(cacheKey);
            }
            return config;
        }

        public void CheckCache()
        {

            string cacheKey = String.Format(UrlRuleConfigCacheKey, _portalId);
            var config = CBO.GetCachedObject<UrlRuleConfiguration>(
                            new CacheItemArgs(cacheKey, DataCache.TabCacheTimeOut, DataCache.TabCachePriority, _portalId),
                            GetUrlRuleConfigCallBack);

            if (config.Rules.Count == 0)
            {
                Logger.Error("CheckCache Rules.Count = 0 -> ClearCache " + cacheKey);
                DataCache.ClearCache(cacheKey);
            }
            
        }

        private object GetUrlRuleConfigCallBack(CacheItemArgs cacheItemArgs)
	    {
            int PortalId = (int)cacheItemArgs.ParamList[0];
            UrlRuleConfiguration config = UrlRuleConfiguration.GenerateConfig(PortalId);
            string[] keys = UrlRuleConfiguration.GetCacheKeys(PortalId);
            List<string> keyLst = new List<string>();
            foreach (string key in keys) 
            {
                if (DataCache.GetCache(key) != null)
                {
                    keyLst.Add(CachingProvider.GetCacheKey(key));
                }
            }
            keys = keyLst.ToArray();

            int CacheTimeout = 20 * Convert.ToInt32(DotNetNuke.Entities.Host.Host.PerformanceSetting);
            cacheItemArgs.CacheTimeOut = CacheTimeout;
            cacheItemArgs.CacheDependency = new DNNCacheDependency(null, keys);
            
            #if DEBUG
            cacheItemArgs.CacheCallback = new CacheItemRemovedCallback(this.RemovedCallBack);
            #endif

            return config;
	    }

        private void RemovedCallBack(string k, object v, CacheItemRemovedReason r)
        {            
            Logger.Info(k + " : " + r.ToString() + "/" + Environment.StackTrace);
        }

        #region Rewriter Rules

        public IEnumerable<UrlRule> GetRules()
        {
            return _rules;
        }
        /*
        private IEnumerable<UrlRule> GetRules(int portalId)
        {
            if (portalId < 0) // host menu
                return new List<UrlRule>();
            else
                return UrlRuleConfiguration.GenerateConfig(portalId).Rules;
        }
        */
        /*
        private  IEnumerable<UrlRule> GetRules(int portalId, string CultureCode)
        {
            if (CultureCode == "") CultureCode = null;            
            return GetRules(portalId).Where(r => string.Equals(r.CultureCode,CultureCode, StringComparison.OrdinalIgnoreCase));
        }
        */
        private  UrlRule GetFirstRule(IEnumerable<UrlRule> rules, string CultureCode)
        {
            UrlRule rule = null;
            if (CultureCode == "") CultureCode = null;
            if (CultureCode == null)
            {
                rule = rules.FirstOrDefault(r => string.IsNullOrEmpty(r.CultureCode));
            }
            else
            {
                rule = rules.FirstOrDefault(r => string.Equals(r.CultureCode, CultureCode, StringComparison.OrdinalIgnoreCase));
                if (rule == null)
                {
                    rule = rules.FirstOrDefault(r => string.IsNullOrEmpty(r.CultureCode));
                }
            }
            return rule;
        }


        public UrlRule GetModuleRule(string CultureCode, int TabId, string Url)
        {
            UrlRule rule = null;
            IEnumerable<UrlRule> rules;
            if (TabId == Null.NullInteger)
            {
                rules = _rules.Where(r => r.RuleType == UrlRuleType.Module && r.IsMatchUrl(Url) && r.RemoveTab == true);
            }
            else
            {
                rules = _rules.Where(r => r.RuleType == UrlRuleType.Module && r.IsMatchUrl(Url));
            }

            if (TabId != Null.NullInteger)
            {
                //with tabid
                var tabRules = rules.Where(r => r.TabId == TabId);
                rule = GetFirstRule(tabRules, CultureCode);
            }
            else 
            {
                rule = GetFirstRule(rules, CultureCode);
            }
            //without tabid
            if (rule == null)
            {
                var tabRules = rules.Where(r => r.TabId == 0); // added 7/2/2014
                rule = GetFirstRule(tabRules, CultureCode);
            }
            if (rule == null)
            {
                // redirection rule from Rewrite rule RedirectDestination
                rule = GetRedirectModuleRule(CultureCode, TabId, Url);
            }
            return rule;
        }

        public UrlRule GetModuleRuleByParameters(string CultureCode, int TabId, string Parameters)
        {
            UrlRule rule = null;
            IEnumerable<UrlRule> rules;
            if (TabId == Null.NullInteger)
            {
                rules = _rules.Where(r => r.RuleType == UrlRuleType.Module && r.Action == UrlRuleAction.Rewrite && r.IsMatch(Parameters) && r.RemoveTab == true);
            }
            else
            {
                rules = _rules.Where(r => r.RuleType == UrlRuleType.Module && r.Action == UrlRuleAction.Rewrite && r.IsMatch(Parameters));
            }

            if (TabId != Null.NullInteger)
            {
                //with tabid
                var tabRules = rules.Where(r => r.TabId == TabId);
                rule = GetFirstRule(tabRules, CultureCode);
            }
            //without tabid
            if (rule == null)
            {
                rule = GetFirstRule(rules, CultureCode);
            }            
            return rule;
        }

        public UrlRule GetCustomModuleRule(string CultureCode, int TabId, string Url)
        {
            UrlRule rule = null;
            IEnumerable<UrlRule> rules;
            if (TabId == Null.NullInteger)
            {
                rules = _rules.Where(r => r.RuleType == UrlRuleType.Custom && r.Action == UrlRuleAction.Rewrite && r.IsMatchUrl(Url) && r.RemoveTab == true);
            }
            else
            {
                rules = _rules.Where(r => r.RuleType == UrlRuleType.Custom && r.Action == UrlRuleAction.Rewrite && r.IsMatchUrl(Url));
            }

            if (TabId != Null.NullInteger)
            {
                //with tabid
                var tabRules = rules.Where(r => r.TabId == TabId);
                rule = GetFirstRule(tabRules, CultureCode);
            }
            else
            {
                rule = GetFirstRule(rules, CultureCode);
            }
            //without tabid
            if (rule == null)
            {
                var tabRules = rules.Where(r => r.TabId <= 0);
                rule = GetFirstRule(tabRules, CultureCode);
            }
            /*
            if (rule == null)
            {
                // redirection rule from Rewrite rule RedirectDestination
                rule = GetRedirectCustomModuleRule(CultureCode, TabId, Url);
            }
             */
            return rule;
        }

        public UrlRule GetCustomModuleRuleByParameters(string CultureCode, int TabId, string Parameters)
        {
            UrlRule rule = null;
            IEnumerable<UrlRule> rules;
            if (TabId == Null.NullInteger)
            {
                rules = _rules.Where(r => r.RuleType == UrlRuleType.Custom && r.Action == UrlRuleAction.Rewrite && r.IsMatch(Parameters) && r.RemoveTab == true);
            }
            else
            {
                rules = _rules.Where(r => r.RuleType == UrlRuleType.Custom && r.Action == UrlRuleAction.Rewrite && r.IsMatch(Parameters));
            }

            rules = rules.Where(r => !r.Url.Contains("[pagename]")); // pagename present, it not possible to know the right url

            if (TabId != Null.NullInteger)
            {
                //with tabid
                var tabRules = rules.Where(r => r.TabId == TabId);
                rule = GetFirstRule(tabRules, CultureCode);
            }
            else
            {
                rule = GetFirstRule(rules, CultureCode);
            }
            //without tabid
            if (rule == null)
            {
                var tabRules = rules.Where(r => r.TabId <= 0);
                rule = GetFirstRule(tabRules, CultureCode);
            }            
            return rule;
        }


        public UrlRule GetModuleRule(string CultureCode, string Url)
        {
            return GetModuleRule(CultureCode, Null.NullInteger, Url);
        }

        // redirection rule from Rewrite rule RedirectDestination
        private UrlRule GetRedirectModuleRule(string CultureCode, int TabId, string Url)
        {
            Url = Url.ToLower();
            UrlRule rule = null;
            bool RemoveTab = TabId == Null.NullInteger;
            var rules = _rules.Where(r => r.RuleType == UrlRuleType.Module && r.IsMatchRedirectDestination(Url) && r.Action == UrlRuleAction.Rewrite /*&& r.RemoveTab == RemoveTab*/);
            //with tabid
            if (TabId != Null.NullInteger)
            {
                var tabRules = rules.Where(r => r.TabId == TabId);
                rule = GetFirstRule(tabRules, CultureCode);
            }
            //without tabid
            //if (rule == null) 22/3/2016
            else
            {
                rule = GetFirstRule(rules, CultureCode);
            }
            if (rule != null)
            {
                rule = new UrlRule()
                {
                    CultureCode = rule.CultureCode,
                    TabId = rule.TabId,
                    RuleType = rule.RuleType,
                    Action = UrlRuleAction.Redirect,
                    Url = rule.RedirectDestination,
                    RedirectDestination = rule.Url,
                    RemoveTab = rule.RemoveTab
                };
            }
            if (rule == null)
            {
                // try to find a rule with de begin of url match a rule RedirectDestination
                rules = _rules.Where(r => r.RuleType == UrlRuleType.Module && Url.StartsWith(r.RedirectDestination+"/")  && r.Action == UrlRuleAction.Rewrite /*&& r.RemoveTab == RemoveTab*/);
                //with tabid
                if (TabId != Null.NullInteger)
                {
                    var tabRules = rules.Where(r => r.TabId == TabId);
                    rule = GetFirstRule(tabRules, CultureCode);
                }
                //without tabid
                //if (rule == null) 22/3/2016
                else
                {
                    rule = GetFirstRule(rules, CultureCode);
                }
                if (rule != null)
                {
                    rule = new UrlRule()
                    {
                        CultureCode = rule.CultureCode,
                        TabId = rule.TabId,
                        RuleType = rule.RuleType,
                        Action = UrlRuleAction.Redirect,
                        Url = rule.RedirectDestination,
                        RedirectDestination = rule.Url,
                        RemoveTab = rule.RemoveTab
                    };
                }

            }
            return rule;
        }

        private UrlRule GetRedirectCustomModuleRule(string CultureCode, int TabId, string Url)
        {
            Url = Url.ToLower();
            UrlRule rule = null;
            bool RemoveTab = TabId == Null.NullInteger;
            var rules = _rules.Where(r => r.RuleType == UrlRuleType.Custom && r.IsMatchRedirectDestination(Url) && r.Action == UrlRuleAction.Rewrite /*&& r.RemoveTab == RemoveTab*/);
            //with tabid
            if (TabId != Null.NullInteger)
            {
                var tabRules = rules.Where(r => r.TabId == TabId);
                rule = GetFirstRule(tabRules, CultureCode);
            }
            //without tabid
            if (rule == null)
            {
                rule = GetFirstRule(rules, CultureCode);
            }
            if (rule != null)
            {
                rule = new UrlRule()
                {
                    CultureCode = rule.CultureCode,
                    TabId = rule.TabId,
                    RuleType = rule.RuleType,
                    Action = UrlRuleAction.Redirect,
                    Url = rule.RedirectDestination,
                    RedirectDestination = rule.Url,
                    RemoveTab = rule.RemoveTab
                };
            }
            return rule;
        }

        public UrlRule GetTabRule(string CultureCode, string Url)
        {
            // if force to lowercase look at all case bacause we redirect uppercase
            //if (UrlRewiterSettings.IsUrlToLowerCase())
            {
                Url = Url.ToLower();
            }

            var rules = _rules.Where(r => r.RuleType == UrlRuleType.Tab && r.Url == Url);
            var rule = GetFirstRule(rules, CultureCode);
            return rule;
        }

        public UrlRule GetRewriteTabRule(string CultureCode, int TabId)
        {
            var rules = _rules.Where(r => r.RuleType == UrlRuleType.Tab && r.Action == UrlRuleAction.Rewrite && r.TabId == TabId);
            var rule = GetFirstRule(rules, CultureCode);
            return rule;
        }

        /*
                private UrlRule GetTabModuleRule(int portalId, string CultureCode, string Url)
                {
                    var rule = GetRules(portalId, CultureCode).SingleOrDefault(r => r.RuleType == UrlRuleType.TabModule && r.Url == Url);
                    if (rule == null && !string.IsNullOrEmpty(CultureCode))
                    {
                        rule = GetRules(portalId, null).SingleOrDefault(r => r.RuleType == UrlRuleType.Tab && r.Url == Url);
                    }
                    return rule;
                }
        */
        public UrlRule GetLanguageRule(string Url)
        {
            var rule = _rules.FirstOrDefault(r => r.RuleType == UrlRuleType.Culture && r.Url == Url);
            return rule;
        }



        #endregion

    }
}
