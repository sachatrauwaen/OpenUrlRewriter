#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2012
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion
#region Usings

using System;
using System.IO;
using System.Xml.Serialization;
using System.Xml.XPath;

using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Cache;
using DotNetNuke.Services.Log.EventLog;
using DotNetNuke.Entities.Tabs;
using System.Collections;
using System.Text;
using DotNetNuke.Entities.Users;
using DotNetNuke.Entities.Portals;
using Satrabel.HttpModules.Provider;
using DotNetNuke.ComponentModel;
using System.Collections.Generic;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Definitions;
using Satrabel.Services.Log.UrlRule;
using System.Linq;
using System.Web.Caching;
using Satrabel.OpenUrlRewriter.Components;
using System.Diagnostics;

#endregion

namespace Satrabel.HttpModules.Config
{
    [Serializable, XmlRoot("UrlRuleConfig")]
    public class UrlRuleConfiguration
    {
        //dnn7 private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(Globals));
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(UrlRuleConfiguration));
        


        private List<UrlRule> _rules;

        public List<UrlRule> Rules
        {
            get
            {
                return _rules;
            }
            set
            {
                _rules = value;
            }
        }


        public static UrlRuleConfiguration GetConfig(int portalId)
        {
            var cc = new CacheController(portalId);
            return cc.GetUrlRuleConfig();
        }

        public static UrlRuleConfiguration GenerateConfig(int portalId)
        {
            //string cacheKey = "UrlRuleConfig" + portalId;


            var config = new UrlRuleConfiguration();
            config.Rules = new List<UrlRule>();



            try
            {
                // 1 cache by portal
                //Logger.Trace("Get cache " + portalId );
                //config = (UrlRuleConfiguration)DataCache.GetCache(cacheKey);

                //if ((config == null))
                {
                    Stopwatch timer = new Stopwatch();
                    timer.Start();
                    Logger.Info("Rebuild cache start " + portalId + " at "+ DateTime.Now);

                    //config = new UrlRuleConfiguration { Rules = new List<UrlRule>() };

                    // generate admin page
                    GenerateAdminTab(portalId);
                    //if (false)
                    {

                        var builder = new UrlBuilder();
                        var Rules = builder.BuildUrlMap(portalId);
                        config.Rules.AddRange(Rules);



                        var storedRules = UrlRuleController.GetUrlRules(portalId);
                        foreach (UrlRuleInfo storedRule in storedRules)
                        {
                            if (storedRule.CultureCode == "") storedRule.CultureCode = null;
                            if (storedRule.RedirectDestination == "") storedRule.RedirectDestination = null;
                            if (storedRule.RedirectDestination != null) storedRule.RedirectDestination = storedRule.RedirectDestination.Trim();
                            if (storedRule.Url != null) storedRule.Url = storedRule.Url.Trim();
                        }

                        // add custom rules to cache
                        foreach (UrlRuleInfo storedRule in storedRules.Where(r => r.RuleType == (int)UrlRuleType.Custom /* && r.RuleAction == (int)UrlRuleAction.Redirect */)) // custom rule
                        {
                            UrlRule rule = new UrlRule()
                            {
                                RuleType = (UrlRuleType)storedRule.RuleType,
                                CultureCode = storedRule.CultureCode,
                                TabId = storedRule.TabId,
                                Parameters = storedRule.Parameters,
                                Action = (UrlRuleAction)storedRule.RuleAction,
                                RemoveTab = storedRule.RemoveTab,
                                RedirectDestination = storedRule.RedirectDestination,
                                RedirectStatus = storedRule.RedirectStatus,
                                Url = storedRule.Url
                            };
                            config.Rules.Add(rule);
                        }

                        // add rule for sitemap.xml rewrite to sitemap.aspx
                        /* replaced by a handler
                        UrlRule sitemapRule = new UrlRule()
                        {
                            RuleType = UrlRuleType.Custom,
                            Action = UrlRuleAction.Rewrite,
                            RedirectDestination = "~/Sitemap.aspx",
                            Url = ".*sitemap.xml"
                        };
                        config.Rules.Add(sitemapRule);
                        */
                        foreach (UrlRule rule in config.Rules)
                        {
                            if (rule.CultureCode == "") rule.CultureCode = null;
                            if (rule.RedirectDestination == "") rule.RedirectDestination = null;
                            rule.Url = rule.Url.ToLower();
                            // set RedirectDestination for automatic redirections corresponding to rewrite rule
                            if (rule.RuleType == UrlRuleType.Module && rule.Action == UrlRuleAction.Rewrite && string.IsNullOrEmpty(rule.RedirectDestination))
                            {
                                rule.RedirectDestination = rule.Parameters.Replace('=', '/').Replace('&', '/');
                            }
                            else if (rule.RuleType == UrlRuleType.Module && rule.Action == UrlRuleAction.Redirect && string.IsNullOrEmpty(rule.RedirectDestination))
                            {
                                var rewriteRule = config.Rules.FirstOrDefault(r => r.RuleType == UrlRuleType.Module && r.Parameters == rule.Parameters && r.Action == UrlRuleAction.Rewrite);
                                if (rewriteRule != null)
                                {
                                    rule.RedirectDestination = rewriteRule.Url;
                                }
                            }
                            if (rule.RedirectDestination != null)
                            {
                                rule.RedirectDestination = rule.RedirectDestination.ToLower();
                            }
                        }

                        var OldRules = new List<UrlRuleInfo>(storedRules);

                        var DistuctRules = config.Rules.Where(r => r.RuleType != UrlRuleType.Custom)
                                                        .GroupBy(r => new { r.RuleType, r.CultureCode, r.TabId, r.Url })
                                                        .Select(g => g.First());

                        foreach (UrlRule rule in DistuctRules)
                        {
                            var ruleInfoLst = storedRules.Where(r => r.RuleType == (int)rule.RuleType &&
                                                                                r.CultureCode == rule.CultureCode &&
                                                                                r.TabId == rule.TabId &&
                                                                                r.Url == rule.Url);

                            UrlRuleInfo ruleInfo = ruleInfoLst.FirstOrDefault();
                            if (ruleInfo == null)
                            {
                                ruleInfo = new UrlRuleInfo()
                                {
                                    PortalId = portalId,
                                    DateTime = DateTime.Now,
                                    RuleType = (int)rule.RuleType,
                                    CultureCode = rule.CultureCode,
                                    TabId = rule.TabId,
                                    Url = rule.Url,

                                    Parameters = rule.Parameters,
                                    RuleAction = (int)rule.Action,
                                    RemoveTab = rule.RemoveTab,
                                    RedirectDestination = rule.RedirectDestination,
                                    RedirectStatus = rule.RedirectStatus
                                };
                                ruleInfo.UrlRuleId = UrlRuleController.AddUrlRule(ruleInfo);
                                Logger.Info("AddUrlRule (UrlRuleId=" + ruleInfo.UrlRuleId + ")");
                                storedRules.Add(ruleInfo);
                            }
                            else
                            {
                                bool FirstOne = true; // for auto correction of double stored rules
                                foreach (var r in ruleInfoLst)
                                {
                                    OldRules.Remove(r);
                                    if (FirstOne)
                                    {
                                        if (ruleInfo.Parameters != rule.Parameters ||
                                            ruleInfo.RuleAction != (int)rule.Action ||
                                            ruleInfo.RemoveTab != rule.RemoveTab ||
                                            ruleInfo.RedirectDestination != rule.RedirectDestination ||
                                            ruleInfo.RedirectStatus != rule.RedirectStatus)
                                        {
                                            Logger.Info("UpdateUrlRule (UrlRuleId=" + ruleInfo.UrlRuleId +

                                                (ruleInfo.Parameters == rule.Parameters) + "/" +
                                                (ruleInfo.RuleAction == (int)rule.Action) + "/" +
                                                (ruleInfo.RemoveTab == rule.RemoveTab) + "/" +
                                                (ruleInfo.RedirectDestination == rule.RedirectDestination) + "/" +
                                                (ruleInfo.RedirectStatus == rule.RedirectStatus) + "-" +

                                                ruleInfo.Parameters + "/" + rule.Parameters + "/" +
                                                ruleInfo.RuleAction + "/" + (int)rule.Action + "/" +
                                                ruleInfo.RemoveTab + "/" + rule.RemoveTab + "/" +
                                                ruleInfo.RedirectDestination + "/" + rule.RedirectDestination + "/" +
                                                ruleInfo.RedirectStatus + "/" + rule.RedirectStatus +

                                                ")");

                                            ruleInfo.Parameters = rule.Parameters;
                                            ruleInfo.RuleAction = (int)rule.Action;
                                            ruleInfo.RemoveTab = rule.RemoveTab;
                                            ruleInfo.RedirectDestination = rule.RedirectDestination;
                                            ruleInfo.RedirectStatus = rule.RedirectStatus;
                                            UrlRuleController.UpdateUrlRule(ruleInfo);

                                        }
                                    }
                                    else
                                    {
                                        UrlRuleController.DeleteUrlRule(ruleInfo.UrlRuleId);
                                        Logger.Info("DeleteUrlRule (UrlRuleId=" + ruleInfo.UrlRuleId + ")");
                                    }

                                    FirstOne = false;
                                }
                            }
                        }

                        foreach (var storedRule in OldRules.Where(r => r.RuleType == (int)UrlRuleType.Tab ||
                                                                        r.RuleType == (int)UrlRuleType.Module))
                        {
                            if (storedRule.RuleAction == (int)UrlRuleAction.Rewrite)
                            {
                                var actualRule = Rules.FirstOrDefault(r => r.RuleType == (UrlRuleType)storedRule.RuleType &&
                                                                            r.CultureCode == storedRule.CultureCode &&
                                                                            r.TabId == storedRule.TabId &&
                                                                            r.Parameters == storedRule.Parameters);

                                if (actualRule != null)
                                {
                                    UrlRule rule = new UrlRule()
                                    {
                                        RuleType = (UrlRuleType)storedRule.RuleType,
                                        CultureCode = storedRule.CultureCode,
                                        TabId = storedRule.TabId,
                                        Parameters = storedRule.Parameters,
                                        Action = UrlRuleAction.Redirect,
                                        RemoveTab = storedRule.RemoveTab,
                                        RedirectDestination = actualRule.Url,
                                        RedirectStatus = storedRule.RedirectStatus,
                                        Url = storedRule.Url
                                    };
                                    config.Rules.Add(rule);
                                }
                            }
                        }
                    }
                    //Logger.MethodExit();
                    //DataCache.SetCache("UrlRuleConfig", config, TimeSpan.FromDays(1));
                    //int intCacheTimeout = 20 * Convert.ToInt32(DotNetNuke.Entities.Host.Host.PerformanceSetting);

                    timer.Stop();
                    double responseTime = timer.ElapsedMilliseconds / 1000.0;



                    Logger.Info("Rebuild cache end " + portalId + " (" + responseTime + ")" + " at " + DateTime.Now);
                    //DataCache.SetCache(cacheKey, config, TimeSpan.FromMinutes(intCacheTimeout));

                    //var onRemove = new CacheItemRemovedCallback(config.RemovedCallBack);

                    //DateTime absoluteExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(intCacheTimeout));

                    //DataCache.SetCache(cacheKey, config, null, absoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.AboveNormal, onRemove, false);


                }
            }
            catch (Exception ex)
            {
                //log it
                var objEventLog = new EventLogController();
                var objEventLogInfo = new LogInfo() { LogTypeKey = "GENERAL_EXCEPTION" };
                objEventLogInfo.AddProperty("UrlRewriter.UrlRuleConfig", "GetConfig Failed");
                objEventLogInfo.AddProperty("ExceptionMessage", ex.Message);
                objEventLogInfo.AddProperty("PortalId", portalId.ToString());
                //objEventLogInfo.LogTypeKey = EventLogController.EventLogType.HOST_ALERT.ToString();


                objEventLogInfo.AddProperty("Exception Type", ex.GetType().ToString());
                objEventLogInfo.AddProperty("Message", ex.Message);
                objEventLogInfo.AddProperty("Stack Trace", ex.StackTrace);
                if (ex.InnerException != null)
                {
                    objEventLogInfo.AddProperty("Inner Exception Message", ex.InnerException.Message);
                    objEventLogInfo.AddProperty("Inner Exception Stacktrace", ex.InnerException.StackTrace);
                }
                objEventLogInfo.BypassBuffering = true;

                objEventLog.AddLog(objEventLogInfo);
                Logger.Error(ex);
                //DataCache.SetCache(cacheKey, config, TimeSpan.FromMinutes(60));
            }


            return config;
        }

        [Obsolete("GetCacheKeys is deprecated, please use GetCacheKeys(int PortalId) instead.")]
        public static string[] GetCacheKeys()
        {
            string[] CacheKeys = new string[] { };
            try
            {
                string cacheKey = "OpenUrlRewriterCacheKeys";
                CacheKeys = (string[])DataCache.GetCache(cacheKey);
                if (CacheKeys == null)
                {
                    var builder = new UrlBuilder();
                    CacheKeys = builder.BuildCacheKeys();
                    DataCache.SetCache(cacheKey, CacheKeys);
                }
            }
            catch (Exception ex)
            {
                //log it
                var objEventLog = new EventLogController();

                var objEventLogInfo = new LogInfo() { LogTypeKey = "GENERAL_EXCEPTION" };
                objEventLogInfo.AddProperty("UrlRewriter.RewriterConfiguration", "GetCacheKeys Failed");
                objEventLogInfo.AddProperty("ExceptionMessage", ex.Message);

                objEventLogInfo.AddProperty("Exception Type", ex.GetType().ToString());
                objEventLogInfo.AddProperty("Message", ex.Message);
                objEventLogInfo.AddProperty("Stack Trace", ex.StackTrace);
                if (ex.InnerException != null)
                {
                    objEventLogInfo.AddProperty("Inner Exception Message", ex.InnerException.Message);
                    objEventLogInfo.AddProperty("Inner Exception Stacktrace", ex.InnerException.StackTrace);
                }
                objEventLogInfo.BypassBuffering = true;

                objEventLogInfo.LogTypeKey = EventLogController.EventLogType.HOST_ALERT.ToString();
                objEventLog.AddLog(objEventLogInfo);
                Logger.Error(ex);

            }
            return CacheKeys;
        }

        public static string[] GetCacheKeys(int PortalId)
        {
            string[] CacheKeys = new string[] { };
            try
            {

                var builder = new UrlBuilder();
                CacheKeys = builder.BuildCacheKeys(PortalId);


            }
            catch (Exception ex)
            {
                //log it
                var objEventLog = new EventLogController();
                var objEventLogInfo = new LogInfo();
                objEventLogInfo.AddProperty("UrlRewriter.RewriterConfiguration", "GetCacheKeys Failed");
                objEventLogInfo.AddProperty("ExceptionMessage", ex.Message);
                objEventLogInfo.LogTypeKey = EventLogController.EventLogType.HOST_ALERT.ToString();
                objEventLog.AddLog(objEventLogInfo);
                Logger.Error(ex);

            }
            return CacheKeys;
        }

        private static void GenerateAdminTab(int PortalId)
        {


            var tabID = TabController.GetTabByTabPath(PortalId, @"//Admin//OpenUrlRewriter", Null.NullString);
            if (tabID == Null.NullInteger)
            {
                var adminTabID = TabController.GetTabByTabPath(PortalId, @"//Admin", Null.NullString);

                /* dont work on dnn 7 -  generate new section "SEO Features" in admin menu
                 
                var tabName = "SEO Features";
                var tabPath = Globals.GenerateTabPath(adminTabID, tabName);
                tabID = TabController.GetTabByTabPath(PortalId, tabPath, Null.NullString);
                if (tabID == Null.NullInteger)
                {
                    //Create a new page
                    var newParentTab = new TabInfo();
                    newParentTab.TabName = tabName;
                    newParentTab.ParentId = adminTabID;
                    newParentTab.PortalID = PortalId;
                    newParentTab.IsVisible = true;
                    newParentTab.DisableLink = true;
                    newParentTab.TabID = new TabController().AddTab(newParentTab);
                    tabID = newParentTab.TabID;
                }
                 */

                // create new page "Url Rules Cache"
                int parentTabID = adminTabID;
                var tabName = "Open Url Rewriter";
                var tabPath = Globals.GenerateTabPath(parentTabID, tabName);
                tabID = TabController.GetTabByTabPath(PortalId, tabPath, Null.NullString);
                if (tabID == Null.NullInteger)
                {
                    //Create a new page
                    var newTab = new TabInfo();
                    newTab.TabName = tabName;
                    newTab.ParentId = parentTabID;
                    newTab.PortalID = PortalId;
                    newTab.IsVisible = true;
#if DNN71
                    newTab.IconFile = "~/Icons/Sigma/AdvancedUrlMngmt_16x16.png";
                    newTab.IconFileLarge = "~/Icons/Sigma/AdvancedUrlMngmt_32x32.png";
#else
                    newTab.IconFile = "~/Images/icon_search_16px.gif";
                    newTab.IconFileLarge = "~/Images/icon_search_32px.gif";
#endif
                    newTab.TabID = new TabController().AddTab(newTab, false);
                    tabID = newTab.TabID;


                }
            }
            // create new module "OpenUrlRewriter"
            var moduleCtl = new ModuleController();
            if (moduleCtl.GetTabModules(tabID).Count == 0)
            {
                //var dmc = new DesktopModuleController();
                //var dm = dmc.GetDesktopModuleByModuleName("OpenUrlRewriter");
                var dm = DesktopModuleController.GetDesktopModuleByModuleName("OpenUrlRewriter", PortalId);
                //var mdc = new ModuleDefinitionController();
                //var md = mdc.GetModuleDefinitionByName(dm.DesktopModuleID, "OpenUrlRewriter");
                var md = ModuleDefinitionController.GetModuleDefinitionByFriendlyName("OpenUrlRewriter");

                var objModule = new ModuleInfo();
                //objModule.Initialize(PortalId);
                objModule.PortalID = PortalId;
                objModule.TabID = tabID;
                objModule.ModuleOrder = Null.NullInteger;
                objModule.ModuleTitle = "Open Url Rewriter";
                objModule.PaneName = Globals.glbDefaultPane;
                objModule.ModuleDefID = md.ModuleDefID;
                objModule.InheritViewPermissions = true;
                objModule.AllTabs = false;
#if DNN71
                objModule.IconFile = "~/Icons/Sigma/AdvancedUrlMngmt_16x16.png";
#else
                objModule.IconFile = "~/Images/icon_search_32px.gif";
#endif
                moduleCtl.AddModule(objModule);
            }
        }

    }
}