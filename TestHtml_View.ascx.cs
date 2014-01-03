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
using Microsoft.ApplicationBlocks.Data;

using System.Configuration;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotNetNuke.Entities.Portals;
using Satrabel.HttpModules;
using System.Data.SqlClient;

namespace Satrabel.Modules.OpenUrlRewriter
{
    public partial class TestHtml_View : PortalModuleBase
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            Uri uri = HttpContext.Current.Request.Url;
            Uri baseUri = new Uri(uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.PathAndQuery.Length));
            tbBaseUrl.Text = baseUri.ToString();
            
            //GridView1.HeaderRow.TableSection = TableRowSection.TableHeader;
        }

        private void ShowCache()
        {
            //GridView1.DataSource = UrlRuleConfiguration.GetConfig(PortalId).Rules;
            //GridView1.DataBind();

        }

        protected void lbTest_Click(object sender, EventArgs e)
        {
            SearchReplace(false);
        }

        private void SearchReplace(bool Replace)
        {
            string connectionstr = System.Configuration.ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString;

            var reader = SqlHelper.ExecuteReader(connectionstr, CommandType.Text, "select * from " + tbTableName.Text);

            List<TestHtml> lst = new List<TestHtml>();

            while (reader.Read())
            {

                string html = reader[tbHtmlField.Text].ToString();
                if (cbDecodeEncode.Checked)
                {
                    html = HttpUtility.HtmlDecode(html);
                }
                string NewHtml = html;

                Uri baseUri = new Uri(tbBaseUrl.Text);

                Uri baseUriWithPath = new Uri(baseUri.Scheme + "://" + PortalAlias.HTTPAlias + "/");


                MatchCollection m1 = Regex.Matches(html, @"(<a.*?>.*?</a>)", RegexOptions.Singleline);
                foreach (Match m in m1)
                {
                    string value = m.Groups[1].Value;

                    string Url = "";
                    bool External = false;

                    // Get href attribute.
                    Match m2 = Regex.Match(value, @"href=\""(.*?)\""", RegexOptions.Singleline);
                    if (m2.Success)
                    {
                        Url = m2.Groups[1].Value;

                        if (Url.StartsWith("javascript:"))
                        {
                            continue;
                        }
                        else if (Url.StartsWith("#"))
                        {
                            continue;
                        }


                        try
                        {
                            Uri linkUri = new Uri(Url);

                            if (!baseUri.IsBaseOf(linkUri) && linkUri.IsAbsoluteUri)
                            {
                                External = true;
                            }
                        }
                        catch { }

                        if (!External)
                        {
                            try
                            {

                            
                            TestHtml test = new TestHtml()
                            {
                                ID = (int)reader[tbPrimaryKeyField.Text],
                                Url = Url,
                                Redirect = GetRedirectUrl(baseUri, baseUriWithPath, Url),
                            };

                            if (Url != "" && test.Redirect != "")
                            {
                                if (!test.Redirect.StartsWith("/")) {
                                    test.Redirect = "/" + test.Redirect;
                                }
                                string href = m2.Value.Replace(Url, test.Redirect);
                                string link = m.Value.Replace(m2.Value, href);
                                test.Search = m.Value;
                                test.Replace = link;

                                NewHtml = NewHtml.Replace(m.Value, link);
                                lst.Add(test);
                            }

                            }
                            catch (Exception ex )
                            {

                                throw new ArgumentException( ex.Message + " : " + Url + "/" + reader[tbPrimaryKeyField.Text].ToString(), ex);
                            }

                        }

                    }
                    else
                    {
                        continue;
                    }
                }
                if (html != NewHtml && Replace)
                {
                    if (cbDecodeEncode.Checked)
                    {
                        NewHtml = HttpUtility.HtmlEncode(NewHtml);
                    }


                    SqlHelper.ExecuteNonQuery(connectionstr, CommandType.Text, "update " + tbTableName.Text + " set " + tbHtmlField.Text + " = @HTML where " + tbPrimaryKeyField.Text + " = @ID", new SqlParameter("@HTML", NewHtml), new SqlParameter("@ID", (int)reader[tbPrimaryKeyField.Text]));
                }

            }
            if (reader != null)
            {
                GridView1.DataSource = lst;
                GridView1.DataBind();


            }
        }

        private string GetRedirectUrl(Uri baseUri, Uri baseUriWithPath, string url)
        {
            string portalAlias;
            PortalAliasInfo objPortalAlias;
            RewriterAction action;
            string baseUrl = "";
            Uri uri;
            if (url.Contains("http"))
            {
                // absolut url
                uri = new Uri(url);                
            }
            else
            {
                // relative url
                if (url.Contains('/'))
                {
                    uri = new Uri(baseUri, url);
                    baseUrl = baseUri.ToString();
                }
                else
                {
                    // the browser add the full path when no path present in url
                    uri = new Uri(baseUriWithPath, url);
                    baseUrl = baseUriWithPath.ToString();
                }

            }
            UrlRewriteModuleUtils.RewriteUrl(uri, out portalAlias, out objPortalAlias, out action);
            if (!action.DoRedirect || string.IsNullOrEmpty(action.RedirectUrl) )
                return "";

            string RedirectUrl = action.RedirectUrl;
            if (RedirectUrl + "/" == baseUri.ToString())
                RedirectUrl = "/";
            else if (RedirectUrl.Length > baseUrl.Length)
                RedirectUrl = RedirectUrl.Remove(0, baseUrl.Length);

            string Anchor = "";
            if (url.Contains('#')){
                Anchor = url.Substring(url.IndexOf('#'));
            }
            
            return RedirectUrl + Anchor;


            return "";
        }

        protected void lbReplace_Click(object sender, EventArgs e)
        {
            SearchReplace(true);
        }
    }

    class TestHtml {
        public int ID { get; set; }
        public string Url { get; set; }
        public string Redirect { get; set; }
        public string Search { get; set; }
        public string Replace { get; set; }

    }

}