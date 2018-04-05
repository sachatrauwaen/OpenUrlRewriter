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
using DotNetNuke.Security.Roles;
using DotNetNuke.Security.Roles.Internal;

namespace Satrabel.HttpModules.Provider
{

    /// <summary>
    /// Description résumée de TabProvider
    /// </summary>
    public class UserUrlRuleProvider : UrlRuleProvider
    {
        public UserUrlRuleProvider()
        {
            Settings = new UrlRuleSetting[] { 
                new UrlRuleSetting("UseDisplayName", false)
            };
        }

        public override List<UrlRule> GetRules(int PortalId)
        {
            bool UseDisplayName = GetPortalSettingAsBoolean(PortalId, "UseDisplayName");

            List<UrlRule> Rules = new List<UrlRule>();

            UserController uc = new UserController();
            //ArrayList users = uc.GetUsers(PortalId,false, false);

            ArrayList users = UserController.GetUsers(PortalId);            

            foreach (UserInfo user in users)
            {
                if (true)
                {
                    var rule = new UrlRule
                    {
                        RuleType = UrlRuleType.Module,
                        Parameters = "userid=" + user.UserID.ToString(),
                        Action = UrlRuleAction.Rewrite,
                        Url = CleanupUrl(UseDisplayName ? user.DisplayName : user.Username)
                    };
#if DNN71
                    if (!string.IsNullOrEmpty(user.VanityUrl)) {
                        rule.Url = CleanupUrl(user.VanityUrl);
                    }
#endif                    
                    
                    Rules.Add(rule);
                }
            }
            var roles = RoleController.Instance.GetRoles(PortalId, r => r.SecurityMode != SecurityMode.SecurityRole);
            foreach (RoleInfo role in roles)
            {
                if (true)
                {
                    var rule = new UrlRule
                    {
                        RuleType = UrlRuleType.Module,
                        Parameters = "groupid=" + role.RoleID.ToString(),
                        Action = UrlRuleAction.Rewrite,
                        Url = CleanupUrl(role.RoleName)
                    };

                    Rules.Add(rule);
                }
            }
           
            return Rules;
        }

    }

}