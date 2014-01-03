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
using Satrabel.Services.Log.UrlLog;

namespace Satrabel.Modules.OpenUrlRewriter
{
    public partial class UrlLog_View : PortalModuleBase
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            ShowCache();          
        }

        private void ShowCache()
        {
            var Logs = UrlLogController.GetUrlLog(PortalId, DateTime.Now.AddDays(-30), DateTime.Now);
            gvLogs.DataSource = Logs;
            gvLogs.DataBind();
            if (gvLogs.HeaderRow != null)
                gvLogs.HeaderRow.TableSection = TableRowSection.TableHeader;
        }
        protected void ClearCache_Click(object sender, EventArgs e)
        {

            //DataCache.RemoveCache("UrlRuleConfig");
            DataCache.ClearCache();
            ShowCache();
        }

    }
}