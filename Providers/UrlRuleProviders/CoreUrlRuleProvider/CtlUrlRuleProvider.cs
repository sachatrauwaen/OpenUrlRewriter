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
using DotNetNuke.Entities.Users;

namespace Satrabel.HttpModules.Provider
{
    public class CtlUrlRuleProvider : UrlRuleProvider
    {
        public CtlUrlRuleProvider() {}

        public override List<UrlRule> GetRules(int PortalId)
        {
            List<UrlRule> Rules = new List<UrlRule>();
            Rules.AddRange(getRules(PortalId, "terms"));           
            Rules.AddRange(getRules(PortalId, "privacy"));
            
            Rules.Add(getRule(PortalId, "login"));
            Rules.Add(getRule(PortalId, "register"));
            return Rules;
        }



        private static UrlRule getRule(int PortalId, string ctlName)
        {
            var rule = new UrlRule
            {
                TabId = -1,
                RuleType = UrlRuleType.Module,
                Parameters = "ctl="+ctlName,
                RemoveTab = true,
                Action = UrlRuleAction.Rewrite,
                Url = ctlName
            };                                            
            return rule;
        }

        private static UrlRule getRedirect(int PortalId, string ctlName)
        {
            var rule = new UrlRule
            {
                TabId = -1,
                RuleType = UrlRuleType.Module,
                Parameters = "ctl=" + ctlName,
                RemoveTab = true,
                Action = UrlRuleAction.Redirect,
                Url = "ctl/"+ctlName,
                RedirectStatus = 301
                //RedirectDestination = ctlName
            };
            return rule;
        }

        private static List<UrlRule> getRules(int PortalId, string ctlName)
        {
            List<UrlRule> Rules = new List<UrlRule>();
            var rule = new UrlRule
            {
                TabId = -1,
                RuleType = UrlRuleType.Module,
                Parameters = "ctl=" + ctlName,
                RemoveTab = true,
                Action = UrlRuleAction.Rewrite,
                Url = ctlName
            };
            Rules.Add(rule);
            /*
            rule = new UrlRule
            {
                TabId = -1,
                RuleType = UrlRuleType.Module,
                Parameters = "ctl=" + ctlName + "&portalid=" + PortalId.ToString(),
                RemoveTab = true,
                Action = UrlRuleAction.Rewrite,
                Url = ctlName
            };
            Rules.Add(rule);
        
            rule = new UrlRule
            {
                TabId = -1,
                RuleType = UrlRuleType.Module,
                Parameters = "ctl=" + ctlName,
                RemoveTab = true,
                Action = UrlRuleAction.Redirect,
                Url = "ctl/" + ctlName,
                RedirectStatus = 301
                
            };
            Rules.Add(rule);
             */
            return Rules;
        }
    }
}