using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using Satrabel.HttpModules.Config;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules.Actions;
using DotNetNuke.Security;
using Satrabel.Services.Log.UrlRule;

namespace Satrabel.Modules.OpenUrlRewriter
{
    public partial class UrlRule_View : PortalModuleBase, IActionable 
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack) {

                GridView1.DataSource = UrlRuleController.GetUrlRules(PortalId).OrderByDescending(r=> r.RuleType);
                GridView1.DataBind();


            }
            GridView1.HeaderRow.TableSection = TableRowSection.TableHeader;
        }

        protected string EditUrlRule(object oUrlRuleId)
        {
            var editUrl = ModuleContext.EditUrl("UrlRuleId", oUrlRuleId.ToString(), "rules_edit");
            if (ModuleContext.PortalSettings.EnablePopUps)
            {
                //editUrl = UrlUtils.PopUpUrl(editUrl, this, ModuleContext.PortalSettings, true, false);
            }
            return editUrl;

        }

        

        #region IActionable Membres

        public ModuleActionCollection ModuleActions
        {
            get
            {
                ModuleActionCollection Actions = new ModuleActionCollection();
                Actions.Add(this.GetNextActionID(), "Host Settings", ModuleActionType.EditContent, "", "", this.EditUrl("HostSettings"), false, SecurityAccessLevel.Host, true, false);
                return Actions;
            }
        }

        #endregion
    }
}