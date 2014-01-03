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
using DotNetNuke.Entities.Portals;
using Satrabel.HttpModules;
using DotNetNuke.Common;

namespace Satrabel.Modules.OpenUrlRewriter
{
    public partial class UrlLog_Settings : PortalModuleBase
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            hlCancel.NavigateUrl = Globals.NavigateURL();

        }

               
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                cbLogAuthentificatedUsers.Checked = UrlRewiterSettings.IsLogAuthentificatedUsers(PortalId);
                cbLogEachUrlOneTime.Checked = UrlRewiterSettings.IsLogEachUrlOneTime(PortalId);
                cbLogStatusCode200.Checked = UrlRewiterSettings.IsLogStatusCode200(PortalId);
                cbLogEnabled.Checked = UrlRewiterSettings.IsLogEnabled(PortalId);
            }
        }

        protected void lbSave_Click(object sender, EventArgs e)
        {
            UrlRewiterSettings.SetLogAuthentificatedUsers(PortalId, cbLogAuthentificatedUsers.Checked);
            UrlRewiterSettings.SetLogEachUrlOneTime(PortalId, cbLogEachUrlOneTime.Checked);
            UrlRewiterSettings.SetLogStatusCode200(PortalId, cbLogStatusCode200.Checked);
            UrlRewiterSettings.SetLogEnabled(PortalId, cbLogEnabled.Checked);

            this.Response.Redirect(Globals.NavigateURL(this.TabId), true);


            /*
            string webUrl = Globals.NavigateURL();
            webUrl = (PortalSettings.EnablePopUps) ? UrlUtils.ClosePopUp(true, webUrl, false) : webUrl;
            Response.Redirect(webUrl, true);
            */
        }

        
    }
}