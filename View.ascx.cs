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
using Satrabel.Services.Log.UrlRule;
using DotNetNuke.Common;
using DotNetNuke.Services.Localization;
using Satrabel.HttpModules;
using Satrabel.HttpModules.Provider;
using DotNetNuke.Entities.Portals;
using System.Xml;
using DotNetNuke.Services.Installer;
using System.Runtime.Serialization.Formatters.Binary;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Urls;


namespace Satrabel.Modules.OpenUrlRewriter
{
    public partial class View : PortalModuleBase, IActionable 
    {
        string cssSucces = "dnnFormMessage dnnFormSuccess";
        string cssError = "dnnFormMessage dnnFormWarning";

        protected void Page_Load(object sender, EventArgs e)
        {

                MyEditUrl(lbTestHtml, "TestHtml");

                MyEditUrl(lbViewRules, "rules_view");
                MyEditUrl(lbAddRule, "rules_edit");
                MyEditUrl(lbViewCache, "cache_view");
                MyEditUrl(lbViewLog, "log_view");
                //lbLogSettings.Attributes.Add("onclick", "return " + MyEditUrl("log_settings"));
           
            ShowCache();
            ShowLogs();
            ShowRules();

            ShowProviders();
            //ShowPortals();

            
            ddlTab.DataSource = TabController.GetPortalTabs(PortalId, -1, false, true);
            ddlTab.DataBind();

            if (!Page.IsPostBack)
            {
                cbDisableSiteIndex.Checked = UrlRewiterSettings.IsDisableSiteIndex(PortalId);
                cbDisableTermsIndex.Checked = UrlRewiterSettings.IsDisableTermsIndex(PortalId);
                cbDisablePivacyIndex.Checked = UrlRewiterSettings.IsDisablePrivacyIndex(PortalId);

                cbLogAuthentificatedUsers.Checked = UrlRewiterSettings.IsLogAuthentificatedUsers(PortalId);
                cbLogEachUrlOneTime.Checked = UrlRewiterSettings.IsLogEachUrlOneTime(PortalId);
                cbLogStatusCode200.Checked = UrlRewiterSettings.IsLogStatusCode200(PortalId);
                cbLogEnabled.Checked = UrlRewiterSettings.IsLogEnabled(PortalId);
                cbW3C.Checked = UrlRewiterSettings.IsW3C(PortalId);
                cbEnhanced404.Checked = UrlRewiterSettings.IsManage404(PortalId);
                ddlTab.SelectedValue = PortalController.GetPortalSetting(FriendlyUrlSettings.ErrorPage404Setting, PortalId, "-1");

                XmlDocument xmlConfig = Config.Load();
                
                XmlNode xmlSitemap = xmlConfig.SelectSingleNode("/configuration/dotnetnuke/sitemap/providers/add[@name='openUrlRewriterSitemapProvider']");
                XmlNode xmlFriendlyUrl = xmlConfig.SelectSingleNode("/configuration/dotnetnuke/friendlyUrl/providers/add[@name='OpenFriendlyUrl']");
                XmlNode xmlFriendlyUrl2 = xmlConfig.SelectSingleNode("/configuration/dotnetnuke/friendlyUrl");

                XmlNode xmlUrlRewriter1 = xmlConfig.SelectSingleNode("/configuration/system.webServer/modules/add[@name='UrlRewrite']");
                XmlNode xmlUrlRewriter2 = xmlConfig.SelectSingleNode("/configuration/system.web/httpModules/add[@name='UrlRewrite']");
                
                //XmlNode xmlCaching = xmlConfig.SelectSingleNode("/configuration/dotnetnuke/caching/providers/add[@name='OpenUrlRewriterFBCachingProvider']");

                XmlNode xmlSitemapHandler1 = xmlConfig.SelectSingleNode("/configuration/system.webServer/handlers/add[@name='SitemapHandler']");
                XmlNode xmlSitemapHandler2 = xmlConfig.SelectSingleNode("/configuration/system.web/httpHandlers/add[@path='Sitemap.aspx']");
                
                //cbSitemapProvider.Checked = xmlSitemap != null;

                if (xmlFriendlyUrl != null && xmlFriendlyUrl2.Attributes["defaultProvider"].Value == "OpenFriendlyUrl")
                    lFriendlyUrlProvider.CssClass = cssSucces;
                else
                    lFriendlyUrlProvider.CssClass = cssError;

                if ( HttpRuntime.UsingIntegratedPipeline)
                {
                    if (xmlUrlRewriter1 != null && xmlUrlRewriter1.Attributes["type"].Value == "Satrabel.HttpModules.UrlRewriteModule, Satrabel.OpenUrlRewriter")
                        lUrlRewriter.CssClass = cssSucces;
                    else
                        lUrlRewriter.CssClass = cssError;
                }
                else {
                    if (xmlUrlRewriter2 != null && xmlUrlRewriter2.Attributes["type"].Value == "Satrabel.HttpModules.UrlRewriteModule, Satrabel.OpenUrlRewriter")
                        lUrlRewriter.CssClass = cssSucces;
                    else
                        lUrlRewriter.CssClass = cssError;
                }
                if (xmlSitemap != null)
                    lSitemapProvider.CssClass = cssSucces;
                else
                    lSitemapProvider.CssClass = cssError;

                if (xmlSitemap != null)
                    lSitemapProvider.CssClass = cssSucces;
                else
                    lSitemapProvider.CssClass = cssError;

                /*
                if (xmlCaching != null)
                    lCachingProvider.CssClass = cssSucces;
                else
                    lCachingProvider.CssClass = cssError;
                */

                if (HttpRuntime.UsingIntegratedPipeline)
                {
                    if (xmlSitemapHandler1 != null && xmlSitemapHandler1.Attributes["type"].Value == "Satrabel.Services.Sitemap.OpenSitemapHandler, Satrabel.OpenUrlRewriter")
                        lSitemapHandler.CssClass = cssSucces;
                    else
                        lSitemapHandler.CssClass = cssError;
                }
                else 
                {
                    if (xmlSitemapHandler2 != null && xmlSitemapHandler2.Attributes["type"].Value == "Satrabel.Services.Sitemap.OpenSitemapHandler, Satrabel.OpenUrlRewriter")
                        lSitemapHandler.CssClass = cssSucces;
                    else
                        lSitemapHandler.CssClass = cssError;                
                }

                lbSaveMeta.Enabled = EditMode;
                lbTestHtml.Visible = UserInfo.IsSuperUser;
            }
            /*
            Locale DefaultLocale = LocaleController.Instance.GetDefaultLocale(PortalId);
            PortalInfo objPortal = new PortalController().GetPortal(PortalId, DefaultLocale.Code);
            int DefaultHomeTabId = -1;
            if (objPortal != null)
                DefaultHomeTabId = objPortal.HomeTabId;

            bool RemoveHomePage = PortalController.GetPortalSettingAsBoolean( "TabUrlRuleProvider" + "_"+ "RemoveHomePage", PortalId, false);

            lTrace.Text = PortalId + "/" + DefaultLocale.Code + "/" + DefaultHomeTabId + "/" + RemoveHomePage;
            */

        }

        private void ShowProviders()
        {
            var builder = new UrlBuilder();

            gvProviders.DataSource = builder.Providers;
            gvProviders.DataBind();

        }

        private void ShowPortals()
        {
#if DEBUG
            gvPortals.Columns[2].Visible = true;
#endif
            PortalController pc = new PortalController();            
            gvPortals.DataSource = pc.GetPortals();
            gvPortals.DataBind();

        }

        protected void gvPortals_RowDataBound(Object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                PortalInfo portal = (PortalInfo)e.Row.DataItem;
                int RulesCount = UrlRuleConfiguration.GetConfig(portal.PortalID).Rules.Count();
                e.Row.Cells[1].Text = RulesCount.ToString();
                /*
                int Memsize = 0;
                foreach (var rule in UrlRuleConfiguration.GetConfig(portal.PortalID).Rules) {
                    Memsize += sizeof(UrlRuleType);
                    if (rule.CultureCode != null)
                    Memsize += sizeof(char) * rule.CultureCode.Length;
                    Memsize += sizeof(int); // tabid
                    if (rule.Parameters != null)
                    Memsize += sizeof(char) * rule.Parameters.Length; // Parameters
                    Memsize += sizeof(Boolean); //RemoveTab                    
                    Memsize += sizeof(UrlRuleAction);
                    if (rule.Url != null)
                    Memsize += sizeof(char) * rule.Url.Length; // Url
                    if (rule.RedirectDestination != null)
                    Memsize += sizeof(char) * rule.RedirectDestination.Length; // 
                    Memsize += sizeof(int); // RedirectionStatus

                }
                */
                
#if DEBUG
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                BinaryFormatter objFormatter = new BinaryFormatter();
                objFormatter.Serialize(stream, UrlRuleConfiguration.GetConfig(portal.PortalID));
                long Memsize = stream.Length;
                
                            
                

                e.Row.Cells[2].Text = Memsize / 1000 + " kb"; //(RulesCount * sizeof(UrlRule) / 1000) + "";
#endif                
                
            }
        }


        private void MyEditUrl(LinkButton lb, string ctl)
        {
            var editUrl = ModuleContext.EditUrl(ctl);
            if (ModuleContext.PortalSettings.EnablePopUps)
            {
                editUrl = UrlUtils.PopUpUrl(editUrl, this, ModuleContext.PortalSettings, true, false);
                lb.Attributes.Add("onclick", "return " + editUrl);
            }
            else {
                lb.Click += delegate (object sender, EventArgs e) {
                    Response.Redirect(editUrl, true);
                };

            }
            
        }

        private void ShowCache()
        {
            var Rules = UrlRuleConfiguration.GetConfig(PortalId).Rules;

            var stats = from r in Rules

                        group r by new { r.RuleTypeString, r.ActionString } into g
                        orderby g.Key.RuleTypeString, g.Key.ActionString
                        select new { Type = g.Key.RuleTypeString, Action = g.Key.ActionString, Count = g.Count() };

            gvCache.DataSource = stats;
            gvCache.DataBind();

            var clashes = from r in Rules

                        group r by new { r.RuleTypeString, r.CultureCode, r.TabId, r.Url } into g
                        where g.Count() > 1
                        select new { Type = g.Key.RuleTypeString, Culture = g.Key.CultureCode, g.Key.TabId, g.Key.Url, Count = g.Count() };

            //clashes = clashes.Where(c => c.Count > 1);

            gvClashes.DataSource = clashes;
            gvClashes.DataBind();

        }

        private void ShowRules()
        {
            var Rules = UrlRuleController.GetUrlRules(PortalId);

            var stats = from r in Rules

                        group r by new { r.RuleTypeString, r.RuleActionString } into g
                        orderby g.Key.RuleTypeString, g.Key.RuleActionString
                        select new { Type = g.Key.RuleTypeString, Action = g.Key.RuleActionString, Count = g.Count() };



            gvRules.DataSource = stats;
            gvRules.DataBind();
        }

        private void ShowLogs()
        {
            var Logs = UrlLogController.GetUrlLog(PortalId, DateTime.Now.AddDays(-30), DateTime.Now);

            var stats = from l in Logs

                        group l by new { l.StatusCode } into g
                        select new { Status = g.Key.StatusCode, Count = g.Count() };



            gvUrlLogs.DataSource = stats;
            gvUrlLogs.DataBind();


            var logsByURL = from l in Logs

                             group l by new { l.OriginalURL, l.URL} into g
                             
                             select new { g.Key.OriginalURL, g.Key.URL };


            var duplicates = from l in logsByURL

                             group l by new { l.OriginalURL } into g
                             where g.Count() > 1
                             select new { g.Key.OriginalURL, Count = g.Count() };


            gvDuplicates.DataSource = duplicates;
            gvDuplicates.DataBind();

        }

        

       

        protected void ClearCache_Click(object sender, EventArgs e)
        {

            //DataCache.RemoveCache("UrlRuleConfig");
            DataCache.ClearCache();
            ShowCache();
            lStatus.Text = "The cache is cleared!";
            lStatus.Visible = true;

        }

        protected void ViewRules_Click(object sender, EventArgs e)
        {
            //Response.Redirect(this.ModuleContext.EditUrl("view_rules"));

            string redirectUrl = UrlUtils.PopUpUrl(EditUrl("urlrule_view"), this, PortalSettings, false, true);
            Response.Redirect(redirectUrl, true);
        }

        protected void ClearLogs_Click(object sender, EventArgs e)
        {
            UrlLogController.DeleteUrlLog(DateTime.Now, PortalId);
            ShowLogs();
        }

        protected void ClearRules_Click(object sender, EventArgs e)
        {
            var Rules = UrlRuleController.GetUrlRules(PortalId).Where(r=> r.RuleTypeEnum != UrlRuleType.Custom);
            foreach (var rule in Rules) {
                UrlRuleController.DeleteUrlRule(rule.UrlRuleId);
            }
            ShowRules();
        }

        protected void SaveMeta_Click(object sender, EventArgs e)
        {
            UrlRewiterSettings.SetDisableSiteIndex(PortalId, cbDisableSiteIndex.Checked);
            UrlRewiterSettings.SetDisableTermsIndex(PortalId, cbDisableTermsIndex.Checked);
            UrlRewiterSettings.SetDisablePrivacyIndex(PortalId, cbDisablePivacyIndex.Checked);

            UrlRewiterSettings.SetLogAuthentificatedUsers(PortalId, cbLogAuthentificatedUsers.Checked);
            UrlRewiterSettings.SetLogEachUrlOneTime(PortalId, cbLogEachUrlOneTime.Checked);
            UrlRewiterSettings.SetLogStatusCode200(PortalId, cbLogStatusCode200.Checked);
            UrlRewiterSettings.SetLogEnabled(PortalId, cbLogEnabled.Checked);
            UrlRewiterSettings.SetW3C(PortalId, cbW3C.Checked);
            UrlRewiterSettings.SetManage404(PortalId, cbEnhanced404.Checked);

            PortalController.UpdatePortalSetting(PortalId, FriendlyUrlSettings.ErrorPage404Setting, ddlTab.SelectedValue);

            foreach (GridViewRow row in gvProviders.Rows)
            {
                CheckBox cbEnabled = (CheckBox)row.FindControl("cbEnabled");
                PortalController.UpdatePortalSetting(PortalId, row.Cells[1].Text + "_Enabled", cbEnabled.Checked.ToString());

                CheckBoxList cblSettings = (CheckBoxList)row.FindControl("cblSettings");
            
                foreach (ListItem li in cblSettings.Items)
                {
                    PortalController.UpdatePortalSetting(PortalId, row.Cells[1].Text + "_" + li.Value, li.Selected.ToString());
                }
            

            }

            /*
            XmlDocument xmlConfig = Config.Load();
            XmlNode xmlSitemap = xmlConfig.SelectSingleNode("/configuration/dotnetnuke/sitemap/providers/add[@name='openUrlRewriterSitemapProvider']");
            if (cbSitemapProvider.Checked && xmlSitemap == null) {

            string install =
            @"<configuration>
                <nodes>
                  <node path=""/configuration/dotnetnuke/sitemap/providers""
                        action=""update"" key=""name"" collision=""ignore"">
                    <add name=""openUrlRewriterSitemapProvider"" type=""Satrabel.SitemapProviders.OpenUrlRewriterSitemapProvider, Satrabel.OpenUrlRewriter"" providerPath=""~\Providers\MembershipProviders\Sitemap\OpenUrlRewriterSitemapProvider\""/>
                  </node>
                  <node path=""/configuration/dotnetnuke/sitemap"" action=""updateattribute"" name=""defaultProvider"" value=""openUrlRewriterSitemapProvider"" />
                  <node path=""/configuration/dotnetnuke/sitemap/providers/add[@name='coreSitemapProvider']"" action=""remove"" />                  
                </nodes>
              </configuration>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(install);

                var app = DotNetNuke.Application.DotNetNukeContext.Current.Application;
                var merge = new XmlMerge(doc, Globals.FormatVersion(app.Version), app.Description);



                merge.UpdateConfig(xmlConfig);

                
                //XmlNode xmlSitemapProviders = xmlConfig.SelectSingleNode("/configuration/dotnetnuke/sitemap/providers");
                //xmlSitemap = xmlConfig.CreateElement("add");
                //XmlUtils.CreateAttribute(xmlConfig, xmlSitemap, "name", "openUrlRewriterSitemapProvider");
                //XmlUtils.CreateAttribute(xmlConfig, xmlSitemap, "type", "Satrabel.SitemapProviders.OpenUrlRewriterSitemapProvider, Satrabel.OpenUrlRewriter");
                //XmlUtils.CreateAttribute(xmlConfig, xmlSitemap, "providerPath", @"~\Providers\MembershipProviders\Sitemap\OpenUrlRewriterSitemapProvider\");
                //xmlSitemapProviders.AppendChild(xmlSitemap);
                
                Config.Save(xmlConfig);
            }
            else if (!cbSitemapProvider.Checked && xmlSitemap != null)
            {

            }
            */

            DataCache.ClearCache();
            this.Response.Redirect(Globals.NavigateURL(this.TabId), true);
        }
        protected void gvProviders_RowDataBound(Object sender, GridViewRowEventArgs e){
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                UrlRuleProvider provider = (UrlRuleProvider)e.Row.DataItem;
                e.Row.Cells[2].Text = provider.GetRules(PortalId).Count().ToString();
                bool isProviderEnabled = PortalController.GetPortalSettingAsBoolean(provider.Name + "_Enabled", PortalId, true);
                CheckBox cbEnabled = (CheckBox)e.Row.FindControl("cbEnabled");
                cbEnabled.Checked = isProviderEnabled;

                CheckBoxList cblSettings = (CheckBoxList)e.Row.FindControl("cblSettings");
                if (provider.Settings != null)
                {
                    foreach (var set in provider.Settings)
                    {
                        cblSettings.Items.Add(new ListItem()
                        {
                            Value = set.Name,
                            Text = set.Name,
                            Selected = provider.GetPortalSettingAsBoolean(PortalId, set.Name)                            
                        });
                    }
                }
            }
        }

        protected void cbEnabled_OnCheckedChanged(Object sender, EventArgs e) { 
            CheckBox cbEnabled = (CheckBox)sender;
            PortalController.UpdatePortalSetting(PortalId, cbEnabled.ToolTip + "_Enabled", cbEnabled.Checked.ToString());
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

        protected void lbShowPortals_Click(object sender, EventArgs e)
        {
            ShowPortals();
            lbShowPortals.Visible = false;
        }

        protected void lbTestUrl_Click(object sender, EventArgs e)
        {
            string portalAlias;
            PortalAliasInfo objPortalAlias;
            RewriterAction action;
            try
            {
                Uri url = new Uri(tbUrl.Text);
                UrlRewriteModuleUtils.RewriteUrl(url, out portalAlias, out objPortalAlias, out action);

                if (action.DoRedirect)
                {
                    if (action.Status == 302)
                    {
                        lUrlResult.Text = "Redirect " + action.Status + " (" + action.Raison + ") : " + action.RedirectUrl;
                    }
                    else
                    {
                        lUrlResult.Text = "Redirect 301 (" + action.Raison + ") : " + action.RedirectUrl;
                    }
                }
                else if (action.DoNotFound)
                {
                    lUrlResult.Text = "NotFound 404 (" + action.Raison + ")";
                }
                else if (action.DoReWrite)
                {
                    lUrlResult.Text = "Rewrite to : " + action.RewriteUrl;
                }

                lUrlResult.Text = action.RedirectUrl;
            }
            catch (Exception ex) {
                lUrlResult.Text = "Error : " + ex.Message;
            }
        }

       
    }
}