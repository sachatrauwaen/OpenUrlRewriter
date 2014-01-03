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

namespace Satrabel.Modules.OpenUrlRewriter
{
    public partial class UrlRuleCache_View : PortalModuleBase, IActionable 
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            
            ShowCache();
            GridView1.HeaderRow.TableSection = TableRowSection.TableHeader;
        }

        private void ShowCache()
        {
            GridView1.DataSource = UrlRuleConfiguration.GetConfig(PortalId).Rules;
            GridView1.DataBind();

        }
        protected void ClearCache_Click(object sender, EventArgs e)
        {

            //DataCache.RemoveCache("UrlRuleConfig");
            DataCache.ClearCache();
            ShowCache();


        }

        #region IActionable Membres

        public ModuleActionCollection ModuleActions
        {
            get
            {
                ModuleActionCollection Actions = new ModuleActionCollection();
                Actions.Add(this.GetNextActionID(), "Url rules", ModuleActionType.EditContent, "", "", this.EditUrl("urlrule_view"), false, SecurityAccessLevel.View, true, false);
                return Actions;
            }
        }

        #endregion
    }
}