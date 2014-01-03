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
using DotNetNuke.Entities.Users;

namespace Satrabel.HttpModules.Provider
{

    /// <summary>
    /// Description résumée de TabProvider
    /// </summary>
    public class UserUrlRuleProvider : UrlRuleProvider
    {
        public UserUrlRuleProvider()
        {
            
        }

        public override List<UrlRule> GetRules(int PortalId)
        {
            List<UrlRule> Rules = new List<UrlRule>();

            UserController uc = new UserController();
            ArrayList users = uc.GetUsers(PortalId,false, false);
            foreach (UserInfo user in users)
            {
                if (true)
                {
                    var rule = new UrlRule
                    {
                        RuleType = UrlRuleType.Module,
                        PortalId = user.PortalID,
                        Parameters = "userid=" + user.UserID.ToString(),
                        Action = UrlRuleAction.Rewrite,
                        Url = CleanupUrl(user.Username)
                    };

                    //System.Diagnostics.Debug.WriteLine(rule.Url);
                    Rules.Add(rule);
                }
            }
           
            return Rules;
        }

    }

}