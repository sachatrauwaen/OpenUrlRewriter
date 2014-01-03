/*
' Copyright (c) 2012 DotNetNuke Corporation
'  All rights reserved.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
' 
*/

using System;
using DotNetNuke.Services.Exceptions;
using Satrabel.Services.Log.UrlRule;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Tabs;

namespace DotNetNuke.Modules.OpenUrlRewriter
{

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The EditOpenUrlRewriter class is used to manage content
    /// 
    /// Typically your edit control would be used to create new content, or edit existing content within your module.
    /// The ControlKey for this control is "Edit", and is defined in the manifest (.dnn) file.
    /// 
    /// Because the control inherits from OpenUrlRewriterModuleBase you have access to any custom properties
    /// defined there, as well as properties from DNN such as PortalId, ModuleId, TabId, UserId and many more.
    /// 
    /// </summary>
    /// -----------------------------------------------------------------------------
    public partial class UrlRule_Edit : PortalModuleBase
    {

        #region Event Handlers

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            hlCancel.NavigateUrl = Globals.NavigateURL();

        }

        int ItemId = Null.NullInteger;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                if (Page.Request.QueryString["UrlRuleId"] != null) 
                {
                    ItemId = int.Parse(Page.Request.QueryString["UrlRuleId"]);
                    
                }

                
                ddlTab.DataSource = TabController.GetPortalTabs(PortalId, -1, false, true);
                ddlTab.DataBind();

        

                lbDelete.Visible = ItemId != Null.NullInteger;

                if (!Page.IsPostBack) {
                    if (ItemId != Null.NullInteger)
                    {
                        var rule = UrlRuleController.GetUrlRule(ItemId);

                        ddlRuleType.SelectedValue = rule.RuleType.ToString();
                
                        //CultureCode = ddlCultureCode.SelectedValue,
                        if (rule.TabId > 0)
                        ddlTab.SelectedValue = rule.TabId.ToString();
                        tbParameters.Text = rule.Parameters;
                        ddlAction.SelectedValue = rule.RuleAction.ToString();
                        cbRemoveTab.Checked = rule.RemoveTab;
                        tbUrl.Text = rule.Url;
                        tbRedirectDestination.Text = rule.RedirectDestination;
                        ddlRedirectStatus.SelectedValue = rule.RedirectStatus.ToString();
                
                    }
                }



            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        #endregion

        protected void lbSave_Click(object sender, EventArgs e)
        {
            UrlRuleInfo rule = new UrlRuleInfo()
            {
                RuleType = int.Parse(ddlRuleType.SelectedValue),
                PortalId = PortalId,
                //CultureCode = ddlCultureCode.SelectedValue,
                TabId = int.Parse(ddlTab.SelectedValue),
                Parameters = tbParameters.Text,                
                RuleAction = int.Parse(ddlAction.SelectedValue),
                RemoveTab = cbRemoveTab.Checked,
                Url = tbUrl.Text,                
                RedirectDestination = tbRedirectDestination.Text,
                RedirectStatus = int.Parse(ddlRedirectStatus.SelectedValue),
                UserId = UserId,
                DateTime = DateTime.Now
            };
            if (ItemId == Null.NullInteger)
            {                
                var UrlRule = UrlRuleController.AddUrlRule(rule);
            }
            else
            {
                rule.UrlRuleId = ItemId;
                UrlRuleController.UpdateUrlRule(rule);
            }

            DataCache.ClearCache();

            this.Response.Redirect(Globals.NavigateURL(this.TabId), true);
        }

        protected void lbDelete_Click(object sender, EventArgs e)
        {
            
            if (ItemId != Null.NullInteger)
            {
            
                UrlRuleController.DeleteUrlRule(ItemId);
            }
            DataCache.ClearCache();
            this.Response.Redirect(Globals.NavigateURL(this.TabId), true);
        }

    }

}