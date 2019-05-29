using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using Satrabel.HttpModules;
using System.Text.RegularExpressions;
using DotNetNuke.Entities.Tabs;
using Satrabel.OpenUrlRewriter.Components;
using Satrabel.HttpModules.Provider;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Common.Internal;
using DotNetNuke.Common;
using DotNetNuke.Services.Localization;

#if DNN71
using DotNetNuke.Entities.Urls.Config;
using DotNetNuke.Entities.Host;
#else
using DotNetNuke.HttpModules.Config;
using System.Diagnostics;
using System.Text;
#endif

namespace Satrabel.HttpModules
{
    public class UrlRewriteModuleUtils
    {

        #region Public methods
        public static void RewriteUrl(HttpApplication app, out string portalAlias, out PortalAliasInfo objPortalAlias, out RewriterAction action)
        {
            HttpRequest request = app.Request;
            HttpResponse response = app.Response;

            RewriteUrl(app, request.Url, out portalAlias, out objPortalAlias, out action,
                        request.ApplicationPath, request.IsSecureConnection, request.HttpMethod, request.RawUrl, request.PhysicalPath);

        }

        public static void RewriteUrl(Uri url, out string portalAlias, out PortalAliasInfo objPortalAlias, out RewriterAction action)
        {
            RewriteUrl(null, url, out portalAlias, out objPortalAlias, out action,
                         HttpContext.Current.Request.ApplicationPath, false, "GET", url.AbsoluteUri, "");
        }

        #endregion
        private static void RewriteUrl(HttpApplication app, Uri url, out string portalAlias, out PortalAliasInfo objPortalAlias, out RewriterAction action,
            string applicationPath, bool isSecureConnection, string httpMethod, string rawUrl, string PhysicalPath)
        {

            /*
             string applicationPath = "";

            bool isSecureConnection = false;
            string httpMethod = "GET";
            string rawUrl = "";
            if (app != null)
            {
                HttpRequest request = app.Request;
                HttpResponse response = app.Response;
                url = request.Url;
                rawUrl = request.RawUrl;
                isSecureConnection = request.IsSecureConnection;
                applicationPath = request.ApplicationPath;
                //NameValueCollection queryString = null;
                //queryString = request.QueryString;
                httpMethod = request.HttpMethod;
            }
            else
            {
                httpMethod = "GET";
                rawUrl = url.AbsoluteUri;
                applicationPath = HttpContext.Current.Request.ApplicationPath;
            }
            */
#if DEBUG
            var IISversion = GetIISVersion();
            if (app != null)
            {
                app.Context.Response.AppendHeader("X-OpenUrlRewriter-IIS-Version", IISversion.Major + "." + IISversion.Minor);
            }
#endif
            portalAlias = "";
            //determine portal alias looking for longest possible match
            objPortalAlias = GetPortalAlias(url, out portalAlias);



            // action is the object containing all info about rewiting and redirection
            action = new RewriterAction();
            if (objPortalAlias != null && portalAlias != objPortalAlias.HTTPAlias)
            {
                action.Alias = objPortalAlias.HTTPAlias;
                action.DoRedirect = true;
                action.Raison += "+www";
                action.RedirectHomePage = true;
                ProcessRedirect(app, httpMethod, url, action);
                return;
            }
            action.LocalPath = url.LocalPath;
            action.HostPort = GetHostPort(url, isSecureConnection);
            action.OriginalUrl = action.HostPort + action.LocalPath;
            action.Alias = portalAlias;
#if DNN71
            action.CultureCode = objPortalAlias.CultureCode;
#endif
            action.QueryUrl = url.Query;
            action.ModuleUrl = "";
            //Work variable
            action.WorkUrl = action.HostPort + action.LocalPath;
            if (action.WorkUrl.StartsWith(action.Alias))
            {
                action.WorkUrl = action.WorkUrl.Remove(0, action.Alias.Length);
            }

            if (IsSpecialPage(url, PhysicalPath, rawUrl, action.WorkUrl)) return;

            CacheController cacheCtrl = null;
            if (objPortalAlias != null)
            {
                cacheCtrl = new CacheController(objPortalAlias.PortalID);
#if DEBUG
                app.Context.Response.AppendHeader("X-OpenUrlRewriter-Rules-count", cacheCtrl.GetUrlRuleConfig().Rules.Count.ToString());
                cacheCtrl.CheckCache();
#endif
            }
#if !DNN71
            // check for language as parameter in the url (OpenUrlRewriter use for public urls, the language after the portal alias)
            if (action.LocalPath.ToLower().Contains("language")) { // for performance
                Match langMatch = Regex.Match(action.LocalPath, "/language/(?:.[^/]+)(?:/|$)", RegexOptions.IgnoreCase); //searches for a string like language/en-US/ in the url            
                if (langMatch.Success)
                {
                    string langParms = langMatch.Value.TrimEnd('/');//in the format of /language/en-US only                
                    action.CultureCode = langParms.ToLower().Replace("/language/", "");
                    // construct the standard dnn path format /alias/culture/rest of the path
                    action.LocalPath = applicationPath + "/" + action.CultureCode + "/"+ action.LocalPath.Substring(applicationPath.Length).Replace(langParms, "");
                    action.WorkUrl = action.WorkUrl.Replace(langParms, "");
                    //langParms = langParms.ToLower().Replace("/language", "");
                    //action.RedirectUrl = action.HostPort + action.LocalPath;
                    action.DoRedirect = true;
                    action.Raison += "+language present";
                }
            }
#endif

            // if tabid present in url redirect to the friendlyurl
            bool RuleMatch = CheckTabIdRedirect(applicationPath, url, objPortalAlias, action, cacheCtrl);

            // Process SiteUrls.config rules 
            if (!RuleMatch)
            {
                if (ProcessRules(applicationPath, objPortalAlias, url, action))
                {
                    return;
                }
            }

            if (cacheCtrl != null)
            {
                // custom redirects            
                if (ProcessCustomRules(cacheCtrl, applicationPath, url, objPortalAlias, action)) return;
            }
            /*
            Globals.SetApplicationName(result.portalId);
            // load the PortalSettings into current context 
            portalSettings = new PortalSettings(result.tabId, result.portalAlias);
            */


            if (!String.IsNullOrEmpty(action.Alias) && objPortalAlias != null)
            {
#if DEBUG
                if (app != null)
                {
                    app.Context.Response.AppendHeader("X-OpenUrlRewriter-RawUrl", rawUrl);
                }
#endif
                int portalID = objPortalAlias.PortalID;

                Dictionary<string, Locale> dicLocales = LocaleController.Instance.GetLocales(portalID);

                #region Default Page has been Requested

                if (!action.DoRedirect && (
                    (action.WorkUrl.ToLower() == "/" + Globals.glbDefaultPage.ToLower()) ||
                    (action.WorkUrl.ToLower() == "/") ||
                    (action.WorkUrl.ToLower() == ""))
                    )
                {
                    // 
                    if (GetCurrentTrustLevel() == AspNetHostingPermissionLevel.Unrestricted &&
                        GetIISVersion().Major >= 7 &&
                        rawUrl.ToLower().EndsWith(Globals.glbDefaultPage.ToLower()))
                    {
                        action.DoRedirect = true;
                        action.Raison += "+Remove /Default.aspx";
                        action.RedirectHomePage = true;
                        ProcessRedirect(app, httpMethod, url, action);
                        return;
                    }
#if DNN71
                    else if (!string.IsNullOrEmpty(objPortalAlias.CultureCode))
                    {
                        var primaryAliases = PortalAliasController.Instance.GetPortalAliasesByPortalId(objPortalAlias.PortalID).ToList().Where(a => a.IsPrimary == true);
                        var alias = primaryAliases.FirstOrDefault(a => string.IsNullOrEmpty(a.CultureCode));
                        if (alias != null)
                        {
                            action.Alias = alias.HTTPAlias;
                            action.DoRedirect = true;
                            action.Raison += "+Remove language";
                            action.RedirectHomePage = true;
                            ProcessRedirect(app, httpMethod, url, action);
                            return;
                        }
                        // for url www.mysite.com/nl : redirect to www.mysite.com/nl/home
                        Locale DefaultLocale = LocaleController.Instance.GetDefaultLocale(portalID);
                        if (!string.IsNullOrEmpty(objPortalAlias.CultureCode) && (objPortalAlias.CultureCode != DefaultLocale.Code))
                        {
                            PortalController pc = new PortalController();

                            PortalInfo objPortal = pc.GetPortal(portalID, objPortalAlias.CultureCode);
                            if (objPortal.HomeTabId > 0)
                            {
                                var portalSettings = new PortalSettings(objPortal.HomeTabId, objPortalAlias);
                                app.Context.Items.Add("UrlRewrite:OriginalUrl", app.Request.Url.AbsoluteUri);
                                string HomeUrl = Globals.NavigateURL(objPortal.HomeTabId, false, portalSettings, "", objPortalAlias.CultureCode);
                                app.Context.Items.Remove("UrlRewrite:OriginalUrl");
                                action.DoRedirect = true;
                                action.Raison += "+Home page missing";
                                action.Status = 301;
                                action.RedirectHomePage = true;
                                action.RedirectUrl = HomeUrl;
                                return;
                            }
                        }

                    }

#endif
                    {
                        PortalController pc = new PortalController();
                        Locale DefaultLocale = LocaleController.Instance.GetDefaultLocale(portalID);
                        // Browser language detection 
                        if (HttpContext.Current.Request["language"] == null && EnableBrowserLanguageInDefault(portalID))
                        {
                            string ActiveLanguage = PortalController.GetActivePortalLanguage(portalID);
                            if (ActiveLanguage != DefaultLocale.Code)
                            {
                                PortalInfo objPortal = pc.GetPortal(portalID, ActiveLanguage);
                                if (objPortal.HomeTabId > 0)
                                {
                                    var portalSettings = new PortalSettings(objPortal.HomeTabId, objPortalAlias);

                                    app.Context.Items.Add("UrlRewrite:OriginalUrl", app.Request.Url.AbsoluteUri);
                                    string HomeUrl = Globals.NavigateURL(objPortal.HomeTabId, false, portalSettings, "", ActiveLanguage);
                                    app.Context.Items.Remove("UrlRewrite:OriginalUrl");
                                    action.DoRedirect = true;
                                    action.Raison += "+Active language";
                                    action.Status = 302;
                                    action.RedirectHomePage = true;
                                    action.RedirectUrl = HomeUrl;
                                    return;
                                }
                            }
                        }
                        {
                            PortalInfo objPortal = pc.GetPortal(portalID, DefaultLocale.Code);


                            string sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + objPortal.HomeTabId;
                            if (dicLocales.Count > 1)
                            {
                                sendToUrl = sendToUrl + "&language=" + objPortal.DefaultLanguage;
                            }
                            if (!string.IsNullOrEmpty(action.QueryUrl))
                            {
                                sendToUrl = sendToUrl + "&" + action.QueryUrl.TrimStart('?', '&');
                            }
                            if (app != null)
                            {
                                app.Context.Items.Add("UrlRewrite:RewriteUrl", RewriterUtils.ResolveUrl(applicationPath, sendToUrl));
                            }
                            //RewriterUtils.RewriteUrl(app.Context, sendToUrl);
                            action.DoReWrite = true;
                            action.RewriteUrl = sendToUrl;
                            return;
                        }
                    }
#if DNN71
                    if (!string.IsNullOrEmpty(action.CultureCode))
                    {

                        PortalInfo objPortal = new PortalController().GetPortal(portalID, action.CultureCode);
                        string sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + objPortal.HomeTabId;
                        sendToUrl = sendToUrl + "&language=" + action.CultureCode;
                        if (app != null)
                        {
                            app.Context.Items.Add("UrlRewrite:RewriteUrl", RewriterUtils.ResolveUrl(applicationPath, sendToUrl));
                        }
                        action.DoReWrite = true;
                        action.RewriteUrl = sendToUrl;
                        return;
                    }
#endif
                }
                #endregion

#if !DNN71                
                // Find culture                
                GetCulture(cacheCtrl, dicLocales, action, portalID);
#endif
                if (action.WorkUrl.ToLower().StartsWith("/host"))
                    cacheCtrl = new CacheController(Null.NullInteger);

                // remove default.aspx                
                RemoveDefaultAndExtention(action);

                // Find tabid
                GetTab(cacheCtrl, action);

                if (app != null && Host.DebugMode)
                {
                    app.Context.Response.AppendHeader("X-OpenUrlRewriter-Info", action.CultureUrl + "*" + action.PageUrl + "*" + action.ModuleUrl);
                }

                /* old stuff find tab by path (standard dnn)
                if (tabID == Null.NullInteger)
                {
                    myTabPath = TabPath;
                    tabID = GetTabByTabPath(portalID, tabPath, cultureCode);
                }
                 */

                if ((action.TabId != Null.NullInteger))
                {

                    string sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + action.TabId;
                    if (!string.IsNullOrEmpty(action.CultureCode))
                    {
                        sendToUrl = sendToUrl + "&language=" + action.CultureCode;
                    }

                    //process the parameters
                    ProcessParameters(cacheCtrl, action, objPortalAlias.PortalID);

                    if (action.ModuleNotFound)
                    {
                        action.Raison = "ModuleNotFound";
                        action.DoNotFound = true;
                        return;
                        //DotNetNuke.Services.Exceptions.Exceptions.ProcessHttpException(request);
                    }

                    ProcessQuery(cacheCtrl, action, objPortalAlias.PortalID);

                    if (!string.IsNullOrEmpty(action.ModuleParameters))
                    {
                        sendToUrl += "&" + action.ModuleParameters;
                    }
                    if (!string.IsNullOrEmpty(action.QueryParameters))
                    {
                        sendToUrl += action.QueryParameters;
                    }

                    if (!String.IsNullOrEmpty(action.QueryUrl))
                    {
                        sendToUrl = sendToUrl + "&" + action.QueryUrl.TrimStart('?', '&');
                    }


                    if (ProcessRedirect(app, httpMethod, url, action))
                    {
                        return;
                    }

                    if (app != null)
                    {
                        app.Context.Items.Add("UrlRewrite:RewriteUrl", RewriterUtils.ResolveUrl(app.Context.Request.ApplicationPath, sendToUrl));
                    }
                    //RewriterUtils.RewriteUrl(app.Context, sendToUrl);
                    action.DoReWrite = true;
                    action.RewriteUrl = sendToUrl;
                    return;
                }
                else
                {
                    // tabid is retrived from UrlRule
                    ProcessParametersWithoutPage(cacheCtrl, action);
                    if (action.TabId != -1)
                    {
                        string sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + action.TabId;
                        if (!string.IsNullOrEmpty(action.CultureCode))
                        {
                            sendToUrl += "&language=" + action.CultureCode;
                        }
                        if (!string.IsNullOrEmpty(action.ModuleParameters))
                        {
                            sendToUrl += "&" + action.ModuleParameters;
                        }
                        //ProcessLowerCaseRedirect(portalID, app);
                        if (ProcessRedirect(app, httpMethod, url, action)) return;
                        if ((!String.IsNullOrEmpty(app.Request.Url.Query)))
                        {
                            sendToUrl = sendToUrl + "&" + app.Request.Url.Query.TrimStart('?');
                        }
                        if (ProcessRedirect(app, httpMethod, url, action)) return;
                        if (app != null)
                        {
                            app.Context.Items.Add("UrlRewrite:RewriteUrl", RewriterUtils.ResolveUrl(app.Context.Request.ApplicationPath, sendToUrl));
                        }
                        //RewriterUtils.RewriteUrl(app.Context, sendToUrl);
                        action.DoReWrite = true;
                        action.RewriteUrl = sendToUrl;
                        return;
                    }
                }
                // urls not included in the UrlRules                                                       
                string tabPath = "/" + action.WorkUrl.ToLower();
                if ((tabPath.IndexOf('?') != -1))
                {
                    tabPath = tabPath.Substring(0, tabPath.IndexOf('?'));
                }
                // process special urls login, register, terms, privacy
                if (ProccessCtl(url, portalID, tabPath, action))
                {
                    return;
                }
                //if (UrlRewiterSettings.IsManage404(objPortalAlias.PortalID))
                {
                    action.Raison = "Page not found";
                    action.DoNotFound = true;
                }

                /*
                tabPath = tabPath.Replace("/", "//");
                var objTabController = new TabController();
                TabCollection objTabs;
                if (tabPath.StartsWith("//host"))
                {
                    objTabs = objTabController.GetTabsByPortal(Null.NullInteger);
                }
                else
                {
                    objTabs = objTabController.GetTabsByPortal(portalID);
                }
                foreach (KeyValuePair<int, TabInfo> kvp in objTabs)
                {
                    if ((kvp.Value.IsDeleted == false && kvp.Value.TabPath.ToLower() == tabPath))
                    {
                        if ((!String.IsNullOrEmpty(app.Request.Url.Query)))
                        {
                            RewriterUtils.RewriteUrl(app.Context, "~/" + Globals.glbDefaultPage + "?TabID=" + kvp.Value.TabID + "&" + app.Request.Url.Query.TrimStart('?'));
                        }
                        else
                        {
                            RewriterUtils.RewriteUrl(app.Context, "~/" + Globals.glbDefaultPage + "?TabID=" + kvp.Value.TabID);
                        }
                        return;
                    }
                } 
                 */
            }
            else
            {
                //Should always resolve to something
                //RewriterUtils.RewriteUrl(app.Context, "~/" & glbDefaultPage)
                return;
            }

        }

        private static bool EnableBrowserLanguageInDefault(int portalId)
        {
            bool retValue = Null.NullBoolean;
            try
            {
                var setting = Null.NullString;
                PortalController.Instance.GetPortalSettings(portalId).TryGetValue("EnableBrowserLanguage", out setting);
                if (string.IsNullOrEmpty(setting))
                {
                    retValue = DotNetNuke.Entities.Host.Host.EnableBrowserLanguage;
                }
                else
                {
                    retValue = (setting.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase) || setting.ToUpperInvariant() == "TRUE");
                }
            }
            catch (Exception exc)
            {
                // Logger.Error(exc);
            }
            return retValue;
        }


        private static bool IsSpecialPage(Uri Url, string PhysicalPath, string RawUrl, string WorkUrl)
        {
            if (
                  Url.LocalPath.ToLower().EndsWith("/install/install.aspx")
               || Url.LocalPath.ToLower().EndsWith("/install/upgradewizard.aspx")
               || Url.LocalPath.ToLower().EndsWith("/install/installwizard.aspx")
               || Url.LocalPath.ToLower().EndsWith("captcha.aspx")
               || Url.LocalPath.ToLower().EndsWith("sitemap.aspx")
               || Url.LocalPath.ToLower().EndsWith("sitemap.xml")
               || Url.LocalPath.ToLower().EndsWith("logoff.aspx")
               || Url.LocalPath.ToLower().EndsWith("rss.aspx")
               || Url.LocalPath.ToLower().EndsWith("linkclick.aspx")
               || Url.LocalPath.ToLower().EndsWith("user.aspx")
               || Url.LocalPath.ToLower().EndsWith(".axd")
               || Url.LocalPath.ToLower().EndsWith(".ashx")
               || Url.LocalPath.ToLower().EndsWith(".asmx")
               || Url.LocalPath.ToLower().EndsWith(".svc")
               ) return true;

            /*
            if ( !string.IsNullOrEmpty(PhysicalPath) && Directory.Exists(PhysicalPath) )
            {
                return true;
            }
            */
            if (WorkUrl.ToLower().StartsWith("/desktopmodules/") || WorkUrl.StartsWith("/API/"))
            {
                return true;
            }
            //if (Url.LocalPath.ToLower().EndsWith(".aspx") && !Url.LocalPath.ToLower().EndsWith(Globals.glbDefaultPage.ToLower()))

            if (File.Exists(PhysicalPath) && !Url.LocalPath.ToLower().EndsWith(Globals.glbDefaultPage.ToLower()))
            {
                return true;
            }
            return false;
        }


        private static bool CheckTabIdRedirect(string ApplicationPath, Uri Url, PortalAliasInfo objPortalAlias, RewriterAction action, CacheController cacheCtrl)
        {
            bool RuleMatch = false;
            if (cacheCtrl != null && action.LocalPath.ToLower().Contains("tabid")) // for performance
            {
                // check for tabid in the url (OpenUrlRewriter use for public urls, the pagename after the language in the url)
                string pattern = "^" + RewriterUtils.ResolveUrl(ApplicationPath, "[^?]*/TabId/(\\d+)(.*)") + "$";
                Match objMatch = Regex.Match(Url.LocalPath, pattern, RegexOptions.IgnoreCase);
                //if there is a match
                if (objMatch.Success)
                {
                    action.TabId = int.Parse(objMatch.Groups[1].Value);
                    // if there is a rule for the tabid, the OpenUrlRewriter manage the rewriter otherwise the SiteUrls rules discard all processing
                    var rule = cacheCtrl.GetRewriteTabRule(action.CultureCode, action.TabId);
                    if (rule == null && objPortalAlias != null)
                    // if rule not found maybe because culture missing in url
                    {
                        TabController tc = new TabController();
                        var tab = tc.GetTab(action.TabId, objPortalAlias.PortalID, false);
                        if (tab != null)
                        {
                            action.CultureCode = tab.CultureCode;
#if DNN71
                            var primaryAliases = PortalAliasController.Instance.GetPortalAliasesByPortalId(objPortalAlias.PortalID).ToList();
                            var alias = primaryAliases.FirstOrDefault(a => a.CultureCode == action.CultureCode);
                            if (alias != null)
                            {
                                action.Alias = alias.HTTPAlias;
                            }
#endif
                            rule = cacheCtrl.GetRewriteTabRule(action.CultureCode, action.TabId);
                        }
                    }
                    if (rule != null)
                    {
                        // construct the standard dnn path format /alias/culture/pageurl/parameters
#if DNN71
                        string langParms = "";
#else
                        string langParms = string.IsNullOrEmpty(action.CultureCode) ? "" : "/" + action.CultureCode;
#endif

                        action.LocalPath = RewriterUtils.ResolveUrl(ApplicationPath, Regex.Replace(action.LocalPath, pattern, "~" + langParms + "/" + rule.Url + "$2", RegexOptions.IgnoreCase));
                        action.WorkUrl = RewriterUtils.ResolveUrl(ApplicationPath, Regex.Replace(action.WorkUrl, pattern, "~" + langParms + "/" + rule.Url + "$2", RegexOptions.IgnoreCase));
                        //action.RedirectUrl = action.HostPort + action.LocalPath;
                        action.DoRedirect = true;
                        action.Raison += "+tabid present";
                        action.RedirectPage = rule.Url;
                        RuleMatch = true;
                    }
                    //  ~/Default.aspx?TabId=$1                
                }
            }
            else if (cacheCtrl != null && action.QueryUrl.ToLower().Contains("tabid") && action.LocalPath.ToLower().Contains("default.aspx")) // for performance
            {
                // check for tabid in the url (OpenUrlRewriter use for public urls, the pagename after the language in the url)
                string pattern = "^" + RewriterUtils.ResolveUrl(ApplicationPath, "[?&]TabId=(\\d+)(.*)") + "$";
                Match objMatch = Regex.Match(action.QueryUrl, pattern, RegexOptions.IgnoreCase);
                //if there is a match
                if (objMatch.Success)
                {

                    action.TabId = int.Parse(objMatch.Groups[1].Value);
                    // if there is a rule for the tabid, the OpenUrlRewriter manage the rewriter otherwise the SiteUrls rules discard all processing
                    var rule = cacheCtrl.GetRewriteTabRule(action.CultureCode, action.TabId);
                    if (rule == null && objPortalAlias != null)
                    // if rule not found maybe because culture missing in url
                    {
                        TabController tc = new TabController();
                        var tab = tc.GetTab(action.TabId, objPortalAlias.PortalID, false);
                        if (tab != null)
                        {
                            action.CultureCode = tab.CultureCode;

#if DNN71
                            var primaryAliases = PortalAliasController.Instance.GetPortalAliasesByPortalId(objPortalAlias.PortalID).ToList();
                            var alias = primaryAliases.FirstOrDefault(a => a.CultureCode == action.CultureCode);
                            if (alias != null)
                            {
                                action.Alias = alias.HTTPAlias;
                            }
#endif

                            rule = cacheCtrl.GetRewriteTabRule(action.CultureCode, action.TabId);
                        }
                    }
                    if (rule != null)
                    {
                        // construct the standard dnn path format /alias/culture/pageurl/parameters

                        action.QueryUrl = Regex.Replace(action.QueryUrl, pattern, "$2", RegexOptions.IgnoreCase);
#if DNN71
                        string langParms = "";
#else
                        string langParms = string.IsNullOrEmpty(action.CultureCode) ? "" : "/" + action.CultureCode;
#endif
                        if (string.IsNullOrEmpty(action.LocalPath) || action.LocalPath == "/")
                        {
                            action.LocalPath = RewriterUtils.ResolveUrl(ApplicationPath, "~" + langParms + "/" + rule.Url);
                            action.WorkUrl = RewriterUtils.ResolveUrl(ApplicationPath, "~" + langParms + "/" + rule.Url);
                        }
                        //action.RedirectUrl = action.HostPort + action.LocalPath;

                        action.DoRedirect = true;
                        action.Raison += "+tabid present";
                        action.RedirectPage = rule.Url;
                        RuleMatch = true;

                    }

                    //  ~/Default.aspx?TabId=$1                
                }
            }
            else if (cacheCtrl != null && action.QueryUrl.ToLower().Contains("tabid") && action.QueryUrl.ToLower().Contains("passwordreset")) // for performance
            {

                string pattern = "^" + RewriterUtils.ResolveUrl(ApplicationPath, "[?&]TabId=(\\d+)(.*)") + "$";
                Match objMatch = Regex.Match(action.QueryUrl, pattern, RegexOptions.IgnoreCase);
                //if there is a match
                if (objMatch.Success)
                {

                    action.QueryUrl = Regex.Replace(action.QueryUrl, pattern, "$2", RegexOptions.IgnoreCase);

                }
            }


            return RuleMatch;
        }

        private static void RemoveDefaultAndExtention(RewriterAction action)
        {
            string Ext = "";
            if (action.WorkUrl.Contains('.'))
            {
                int ExtIndex = action.WorkUrl.IndexOf('.');
                Ext = action.WorkUrl.Substring(ExtIndex);
                action.WorkUrl = action.WorkUrl.Remove(ExtIndex);
            }
            else if (action.WorkUrl.EndsWith("/"))
            {
                Ext = "/";
                action.WorkUrl = action.WorkUrl.TrimEnd('/');
            }


            if (Ext != UrlRewiterSettings.Current().FileExtension)
            {
                //action.RedirectUrl = action.RedirectUrl.Replace(".aspx", "");
                action.DoRedirect = true;
                action.Raison += "+wrong extention";
            }


            if (action.WorkUrl.EndsWith("/Default", StringComparison.InvariantCultureIgnoreCase))
            {
                //action.RedirectUrl = action.RedirectUrl.Replace("/Default", "");
                action.DoRedirect = true;
                action.Raison += "+Remove /Default";

                action.WorkUrl = action.WorkUrl.Remove(action.WorkUrl.Length - 8);
            }
            action.WorkUrl = action.WorkUrl.TrimStart('/');
        }

        private static bool ProcessRedirect(HttpApplication app, string HttpMethod, Uri Url, RewriterAction redirect)
        {
            if (redirect.DoRedirect && HttpMethod == "GET")
            {
                string RedirectTo = Url.Scheme + "://" + redirect.Alias;

                if (!redirect.RedirectHomePage)
                {
                    if (!string.IsNullOrEmpty(redirect.RedirectCulture))
                    {
                        RedirectTo += "/" + redirect.RedirectCulture;
                    }
                    else if (!string.IsNullOrEmpty(redirect.CultureUrl))
                    {
                        RedirectTo += "/" + redirect.CultureUrl;
                    }
                    if (!string.IsNullOrEmpty(redirect.RedirectPage))
                    {
                        RedirectTo += "/" + redirect.RedirectPage;
                    }
                    else if (!string.IsNullOrEmpty(redirect.PageUrl))
                    {
                        RedirectTo += "/" + redirect.PageUrl;
                    }
                    if (!string.IsNullOrEmpty(redirect.RedirectModule))
                    {
                        RedirectTo += "/" + redirect.RedirectModule;
                    }
                    else if (!string.IsNullOrEmpty(redirect.ModuleUrl))
                    {
                        RedirectTo += "/" + redirect.ModuleUrl;
                    }

                    RedirectTo += UrlRewiterSettings.Current().FileExtension;
                }

                if (!string.IsNullOrEmpty(redirect.QueryUrl))
                {
                    redirect.QueryUrl = redirect.QueryUrl.TrimStart('?', '&');
                    RedirectTo += (RedirectTo.Contains('?') ? "&" : "?") + redirect.QueryUrl;
                }

                redirect.RedirectUrl = RedirectTo;
                /*
                response.AppendHeader("X-Redirect-Raison", redirect.Raison);
                if (redirect.Status == 302)
                {
                    response.Redirect(RedirectTo, true);
                }
                else
                {                    
                    response.Status = "301 Moved Permanently";
                    response.AddHeader("Location", RedirectTo);
                    response.End();
                }
                 */
                // normaly we dont come here because redirect && End stop the thread
                return true;
            }
            else
            {
                redirect.DoRedirect = false; // not GET
                return false;
            }
        }

        private static void ProcessLowerCaseRedirect(int PortalId, Uri Url, RewriterAction action)
        {
            // redirect if requested path is not lowercase

            string requestedPath = Url.AbsoluteUri;
            string strQueryString = "";
            if ((!String.IsNullOrEmpty(Url.Query)))
            {
                //strQueryString = QueryString.ToString();
                strQueryString = Url.Query.Replace("?", "");
                requestedPath = requestedPath.Replace(Url.Query, "");
            }
            if (UrlRewiterSettings.IsUrlToLowerCase() &&
                !UrlRewiterSettings.ExcludeFromRedirect(PortalId, requestedPath) &&
                !UrlRewiterSettings.ExcludeFromLowerCase(PortalId, requestedPath))
            {


                if (requestedPath != requestedPath.ToLower())
                {
                    string sendTo = requestedPath.ToLower();
                    if (strQueryString != "")
                    {
                        sendTo = sendTo + "?" + strQueryString;
                    }

                    /*
                    app.Context.Items.Add("UrlRewrite:RedirectUrl", sendTo);                    
                    app.Response.Status = "301 Moved Permanently";
                    app.Response.AppendHeader("X-Redirect-Reason", "Not lowercase");
                    app.Response.AddHeader("Location", sendTo);
                    app.Response.End();
                     */
                    action.DoRedirect = true;
                    action.RedirectUrl = sendTo;
                    action.Raison = "Not lowercase";
                }
            }
        }


        private static bool ProccessCtl(Uri Url, int portalID, string tabPath, RewriterAction action)
        {

            //Get the Portal
            PortalInfo portal = new PortalController().GetPortal(portalID);
            var requestQuery = Url.Query;
            if (!string.IsNullOrEmpty(requestQuery))
            {
                requestQuery = Regex.Replace(requestQuery, "&?tabid=\\d+", string.Empty, RegexOptions.IgnoreCase);
                requestQuery = Regex.Replace(requestQuery, "&?portalid=\\d+", string.Empty, RegexOptions.IgnoreCase);
                requestQuery = requestQuery.TrimStart('?', '&');
            }

            tabPath = tabPath.Replace(".aspx", "");
            if (tabPath == "/login")
            {
                ProcessLowerCaseRedirect(portalID, Url, action);
                string sendToUrl;
                if (portal.LoginTabId > Null.NullInteger && Globals.ValidateLoginTabID(portal.LoginTabId))
                {
                    if (!string.IsNullOrEmpty(requestQuery))
                    {
                        sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.LoginTabId + "&" + requestQuery;
                    }
                    else
                    {
                        sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.LoginTabId;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(requestQuery))
                    {
                        sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.HomeTabId + "&portalid=" + portalID + "&ctl=login&" + requestQuery;
                    }
                    else
                    {
                        sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.HomeTabId + "&portalid=" + portalID + "&ctl=login";
                    }
                }
                if (!string.IsNullOrEmpty(action.CultureCode))
                {
                    sendToUrl = sendToUrl + "&language=" + action.CultureCode;
                }
                //RewriterUtils.RewriteUrl(app.Context, sendToUrl);
                action.DoReWrite = true;
                action.RewriteUrl = sendToUrl;
                return true;
            }
            if (tabPath == "/register")
            {
                ProcessLowerCaseRedirect(portalID, Url, action);
                string sendToUrl;
                if (portal.RegisterTabId > Null.NullInteger)
                {
                    if (!string.IsNullOrEmpty(requestQuery))
                    {
                        sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.RegisterTabId + "&portalid=" + portalID + "&" + requestQuery;
                    }
                    else
                    {
                        sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.RegisterTabId + "&portalid=" + portalID;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(requestQuery))
                    {
                        sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.HomeTabId + "&portalid=" + portalID + "&ctl=Register&" + requestQuery;
                    }
                    else
                    {
                        sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.HomeTabId + "&portalid=" + portalID + "&ctl=Register";
                    }
                }
                if (!string.IsNullOrEmpty(action.CultureCode))
                {
                    sendToUrl = sendToUrl + "&language=" + action.CultureCode;
                }
                //RewriterUtils.RewriteUrl(app.Context, sendToUrl);
                action.DoReWrite = true;
                action.RewriteUrl = sendToUrl;
                return true;
            }
            if (tabPath == "/terms")
            {
                ProcessLowerCaseRedirect(portalID, Url, action);
                string sendToUrl;
                if (!string.IsNullOrEmpty(requestQuery))
                {
                    sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.HomeTabId /*+ "&portalid=" + portalID*/ + "&ctl=Terms&" + requestQuery;
                }
                else
                {
                    sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.HomeTabId /*+ "&portalid=" + portalID */ + "&ctl=Terms";
                }
                if (!string.IsNullOrEmpty(action.CultureCode))
                {
                    sendToUrl = sendToUrl + "&language=" + action.CultureCode;
                }
                //RewriterUtils.RewriteUrl(app.Context, sendToUrl);
                action.DoReWrite = true;
                action.RewriteUrl = sendToUrl;
                return true;
            }
            if (tabPath == "/privacy")
            {
                ProcessLowerCaseRedirect(portalID, Url, action);
                string sendToUrl;
                if (!string.IsNullOrEmpty(requestQuery))
                {
                    sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.HomeTabId /*+ "&portalid=" + portalID*/ + "&ctl=Privacy&" + requestQuery;
                }
                else
                {
                    sendToUrl = "~/" + Globals.glbDefaultPage + "?TabID=" + portal.HomeTabId /*+ "&portalid=" + portalID*/ + "&ctl=Privacy";
                }
                if (!string.IsNullOrEmpty(action.CultureCode))
                {
                    sendToUrl = sendToUrl + "&language=" + action.CultureCode;
                }
                //RewriterUtils.RewriteUrl(app.Context, sendToUrl);
                action.DoReWrite = true;
                action.RewriteUrl = sendToUrl;
                return true;
            }

            return false;
        }


        private static void GetCulture(CacheController cacheCtrl, Dictionary<string, Locale> dicLocales, RewriterAction action, int portalID)
        {
            string cultureCode = string.Empty;
            //cultureUrl = string.Empty;
            // check if multi language site
            if (dicLocales.Count > 1)
            {
                String[] splitUrl = action.WorkUrl.TrimStart('/').Split('/');
                // asume culture is always first part of url afetr alias
                string culturePart = splitUrl[0];
                // try to find UrlRule                            
                var ruleL = cacheCtrl.GetLanguageRule(culturePart.ToLower());
                if (ruleL != null)
                {
                    if (ruleL.Action == UrlRuleAction.Redirect)
                    {
                        //action.RedirectUrl = redirect.RedirectUrl.Replace("/" + culturePart + "/", "/" + ruleL.RedirectDestination + "/");
                        action.RedirectCulture = ruleL.RedirectDestination;
                        action.Status = ruleL.RedirectStatus;
                        action.DoRedirect = true;
                        action.Raison += "+Culture:" + ruleL.Url + ">" + ruleL.RedirectDestination;
                    }
                    else if (culturePart != ruleL.Url) // because different case
                    {
                        action.RedirectCulture = ruleL.Url;
                        action.DoRedirect = true;
                        action.Raison += "+Culture Wrong case";
                    }
                    culturePart = ruleL.CultureCode;
                }

                foreach (KeyValuePair<string, Locale> key in dicLocales)
                {
                    if (key.Key.ToLower().Equals(culturePart.ToLower()))
                    {
                        action.CultureCode = key.Value.Code;
                        action.CultureUrl = splitUrl[0];
                        action.WorkUrl = action.WorkUrl.Substring(action.CultureUrl.Length + 1);
                        break;
                    }
                }
                if (string.IsNullOrEmpty(action.CultureUrl))
                {
                    Locale DefaultLocale = LocaleController.Instance.GetDefaultLocale(portalID);
                    action.RedirectCulture = DefaultLocale.Code;
                    var ruleD = cacheCtrl.GetLanguageRule(culturePart.ToLower());
                    if (ruleD != null && ruleL.Action == UrlRuleAction.Redirect)
                    {
                        action.RedirectCulture = ruleD.RedirectDestination;
                        action.Status = ruleD.RedirectStatus;
                    }
                    action.DoRedirect = true;
                    action.Raison += "+Language empty";
                }


            }
        }

        private static bool ProcessRules(string ApplicationPath, PortalAliasInfo objPortalAlias, Uri Url, RewriterAction action)
        {
            //Friendly URLs are exposed externally using the following format
            //http://www.domain.com/tabid/###/mid/###/ctl/xxx/default.aspx
            //and processed internally using the following format
            //http://www.domain.com/default.aspx?tabid=###&mid=###&ctl=xxx
            //The system for accomplishing this is based on an extensible Regex rules definition stored in /SiteUrls.config


            //save and remove the querystring as it gets added back on later
            //path parameter specifications will take precedence over querystring parameters
            string requestedPath = Url.AbsoluteUri;
            string strQueryString = "";
            if ((!String.IsNullOrEmpty(Url.Query)))
            {
                strQueryString = Url.Query.Replace("?", "");
                requestedPath = requestedPath.Replace(Url.Query, "");
            }


            string sendTo = "";
            //get url rewriting rules 
            RewriterRuleCollection rules = RewriterConfiguration.GetConfig().Rules;

            //iterate through list of rules
            int matchIndex = -1;
            for (int ruleIndex = 0; ruleIndex <= rules.Count - 1; ruleIndex++)
            {
                //check for the existence of the LookFor value 
                string pattern = "^" + RewriterUtils.ResolveUrl(ApplicationPath, rules[ruleIndex].LookFor) + "$";
                Match objMatch = Regex.Match(requestedPath, pattern, RegexOptions.IgnoreCase);

                //if there is a match
                if ((objMatch.Success))
                {
                    //create a new URL using the SendTo regex value
                    sendTo = RewriterUtils.ResolveUrl(ApplicationPath, Regex.Replace(requestedPath, pattern, rules[ruleIndex].SendTo, RegexOptions.IgnoreCase));

                    string parameters = objMatch.Groups[2].Value;
                    //process the parameters
                    //ProcessParameters(objPortalAlias.PortalID, "", -1, "",ref sendTo, parameters, null);
                    if ((parameters.Trim().Length > 0))
                    {
                        //split the value into an array based on "/" ( ie. /tabid/##/ )
                        parameters = parameters.Replace("\\", "/");

                        parameters = parameters.Replace("/" + Globals.glbDefaultPage, "").Replace("/" + Globals.glbDefaultPage.ToLower(), "").Replace(".aspx", "");

                        string[] splitParameters = parameters.Split('/');
                        string parameterName;
                        string parameterValue;
                        //icreate a well formed querystring based on the array of parameters
                        for (int parameterIndex = 0; parameterIndex < splitParameters.Length; parameterIndex++)
                        {
                            //ignore the page name 
                            if (splitParameters[parameterIndex].IndexOf(".aspx", StringComparison.InvariantCultureIgnoreCase) == -1)
                            {
                                //get parameter name
                                parameterName = splitParameters[parameterIndex].Trim();
                                if (parameterName.Length > 0)
                                {
                                    //add parameter to SendTo if it does not exist already  
                                    if (sendTo.IndexOf("?" + parameterName + "=", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                                        sendTo.IndexOf("&" + parameterName + "=", StringComparison.InvariantCultureIgnoreCase) == -1)
                                    {
                                        //get parameter delimiter
                                        string parameterDelimiter;
                                        if (sendTo.IndexOf("?") != -1)
                                        {
                                            parameterDelimiter = "&";
                                        }
                                        else
                                        {
                                            parameterDelimiter = "?";
                                        }
                                        sendTo = sendTo + parameterDelimiter + parameterName;
                                        //get parameter value
                                        parameterValue = "";
                                        if (parameterIndex < splitParameters.Length - 1)
                                        {
                                            parameterIndex += 1;
                                            if (!String.IsNullOrEmpty(splitParameters[parameterIndex].Trim()))
                                            {
                                                parameterValue = splitParameters[parameterIndex].Trim();
                                            }
                                        }
                                        //add the parameter value
                                        if (parameterValue.Length > 0)
                                        {
                                            sendTo = sendTo + "=" + parameterValue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    matchIndex = ruleIndex;
                    break; //exit as soon as it processes the first match
                }
            }


            //if a match was found to the urlrewrite rules
            if (matchIndex != -1)
            {
                if (!String.IsNullOrEmpty(strQueryString))
                {
                    //add querystring parameters back to SendTo
                    string[] parameters = strQueryString.Split('&');
                    string parameterName;
                    //iterate through the array of parameters
                    for (int parameterIndex = 0; parameterIndex <= parameters.Length - 1; parameterIndex++)
                    {
                        //get parameter name
                        parameterName = parameters[parameterIndex];
                        if (parameterName.IndexOf("=") != -1)
                        {
                            parameterName = parameterName.Substring(0, parameterName.IndexOf("="));
                        }
                        //check if parameter already exists
                        if (sendTo.IndexOf("?" + parameterName + "=", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                            sendTo.IndexOf("&" + parameterName + "=", StringComparison.InvariantCultureIgnoreCase) == -1)
                        {
                            //add parameter to SendTo value
                            if (sendTo.IndexOf("?") != -1)
                            {
                                sendTo = sendTo + "&" + parameters[parameterIndex];
                            }
                            else
                            {
                                sendTo = sendTo + "?" + parameters[parameterIndex];
                            }
                        }
                    }
                }

                if (rules[matchIndex].SendTo.StartsWith("~"))
                {
                    //rewrite the URL for internal processing
                    //RewriterUtils.RewriteUrl(app.Context, sendTo);
                    action.DoReWrite = true;
                    action.RewriteUrl = sendTo;
                }
                else
                {
                    //it is not possible to rewrite the domain portion of the URL so redirect to the new URL
                    //app.Context.Items.Add("UrlRewrite:RedirectUrl", sendTo);
                    //app.Response.Redirect(sendTo, true);
                    action.DoRedirect = true;
                    action.RedirectUrl = sendTo;
                }
                return true;
            }
            else
            {
                return false;
            }


        }


        private static bool ProcessCustomRules(CacheController cacheCtrl, string ApplicationPath, Uri Url, PortalAliasInfo objPortalAlias, RewriterAction action)
        {
            //save and remove the querystring as it gets added back on later
            //path parameter specifications will take precedence over querystring parameters
            string requestedPath = Url.AbsoluteUri;
            string strQueryString = "";
            if ((!String.IsNullOrEmpty(Url.Query)))
            {
                //strQueryString = QueryString.ToString();
                strQueryString = Url.Query.Replace("?", "");
                requestedPath = requestedPath.Replace(Url.Query, "");
            }


            string sendTo = "";
            //get url rewriting rules 
            //RewriterRuleCollection rules = RewriterConfiguration.GetConfig().Rules;
            var rules = cacheCtrl.GetRules().Where(r => r.RuleType == UrlRuleType.Custom);

            //iterate through list of rules
            UrlRule ruleFound = null;
            foreach (UrlRule rule in rules)
            {
                //check for the existence of the LookFor value 
                string pattern = "^" + RewriterUtils.ResolveUrl(ApplicationPath, rule.Url) + "$";
                Match objMatch = Regex.Match(requestedPath, pattern, RegexOptions.IgnoreCase);

                //if there is a match
                if (objMatch.Success /* && (string.IsNullOrEmpty(rule.Parameters) || rule.Parameters == strQueryString) */ )
                {
                    //create a new URL using the SendTo regex value
                    sendTo = RewriterUtils.ResolveUrl(ApplicationPath, Regex.Replace(requestedPath, pattern, rule.RedirectDestination, RegexOptions.IgnoreCase));

                    string parameters = objMatch.Groups[2].Value;
                    //process the parameters
                    //ProcessParameters(objPortalAlias.PortalID, "", -1,  ref sendTo, parameters);
                    ruleFound = rule;
                    break; //exit as soon as it processes the first match
                }
            }

            //if a match was found to the urlrewrite rules
            if (ruleFound != null /*&& string.IsNullOrEmpty(ruleFound.Parameters)*/ )
            {
                if (!String.IsNullOrEmpty(strQueryString))
                {
                    //add querystring parameters back to SendTo
                    string[] parameters = strQueryString.Split('&');
                    string parameterName;
                    //iterate through the array of parameters
                    for (int parameterIndex = 0; parameterIndex <= parameters.Length - 1; parameterIndex++)
                    {
                        //get parameter name
                        parameterName = parameters[parameterIndex];
                        if (parameterName.IndexOf("=") != -1)
                        {
                            parameterName = parameterName.Substring(0, parameterName.IndexOf("="));
                        }
                        //check if parameter already exists
                        if (sendTo.IndexOf("?" + parameterName + "=", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                            sendTo.IndexOf("&" + parameterName + "=", StringComparison.InvariantCultureIgnoreCase) == -1)
                        {
                            //add parameter to SendTo value
                            if (sendTo.IndexOf("?") != -1)
                            {
                                sendTo = sendTo + "&" + parameters[parameterIndex];
                            }
                            else
                            {
                                sendTo = sendTo + "?" + parameters[parameterIndex];
                            }
                        }
                    }
                }

                if (ruleFound.Action == UrlRuleAction.Rewrite)
                {
                    action.DoReWrite = true;
                    action.RewriteUrl = sendTo;
                    action.Raison = "Custom rule";
                }
                else
                {
                    if (ruleFound.RedirectStatus == 404)
                    {
                        action.DoNotFound = true;
                        action.Raison = "Custom rule";
                        action.Status = 404;
                    }
                    else
                    {
                        action.DoRedirect = true;
                        action.RedirectUrl = sendTo;
                        action.Raison = "Custom rule";
                        action.Status = ruleFound.RedirectStatus;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        /*
        private static void ProcessParametersRedirects(HttpApplication app, HttpResponse response, int portalId, string CultureCode, String myAlias, string cultureUrl, string tabUrl, string parameters, RedirectAction redirect)
        {
            if ((parameters.Trim().Length > 0))
            {
                UrlRule CtlRule = GetRedirectModuleRule(portalId, CultureCode, parameters.TrimStart('/').ToLower());
                if (CtlRule != null)
                {
                    if (CtlRule.RemoveTab)
                    {
                        redirect.RedirectUrl = redirect.RedirectUrl.Replace("/" + tabUrl, "");
                    }
                    redirect.RedirectUrl = redirect.RedirectUrl.Replace(parameters.TrimStart('/'), CtlRule.RedirectDestination);
                    redirect.DoRedirect = true;
                    redirect.Raison += "+ModuleRule:" + CtlRule.Url + ">" + CtlRule.RedirectDestination;
                }
            }
        }
        */
        private static void GetTab(CacheController cacheCtrl, RewriterAction action)
        {
            if (string.IsNullOrEmpty(action.WorkUrl))
                return;

            int tabID = Null.NullInteger;
            string TabPath = action.WorkUrl;
            string Parameters = "";
            UrlRule TabRule;
            do
            {
                TabRule = cacheCtrl.GetTabRule(action.CultureCode, TabPath);
                if (TabRule != null)
                {
                    action.TabId = TabRule.TabId;
                    action.WorkUrl = "";
                    action.PageUrl = TabPath;
                    action.ModuleUrl = Parameters.TrimStart('/');

                    if (TabRule.RemoveTab && string.IsNullOrEmpty(action.ModuleUrl) && string.IsNullOrEmpty(action.QueryUrl))
                    { // redirect to alias
                        action.RedirectHomePage = true;
                        action.Status = TabRule.RedirectStatus;
                        action.DoRedirect = true;
                        action.Raison += "+Tab:" + TabRule.Url + "> alias";
                    }
                    else if (TabRule.Action == UrlRuleAction.Redirect)
                    {
                        //action.RedirectUrl = action.RedirectUrl.Replace("/" + TabRule.Url, "/" + TabRule.RedirectDestination);
                        action.RedirectPage = TabRule.RedirectDestination;
                        action.Status = TabRule.RedirectStatus;
                        action.DoRedirect = true;
                        action.Raison += "+Tab:" + TabRule.Url + ">" + TabRule.RedirectDestination;

                    }
                    else if (TabPath != TabRule.Url) // because different case
                    {
                        action.RedirectPage = TabRule.Url;
                        action.DoRedirect = true;
                        action.Raison += "+Wrong case";
                    }
                    break;
                }

                int slashIndex = TabPath.LastIndexOf('/');
                if (slashIndex > 0)
                {
                    Parameters = TabPath.Substring(slashIndex) + Parameters;
                    TabPath = TabPath.Substring(0, slashIndex);
                }
                else
                {
                    TabPath = "";
                }
            } while (TabPath.Length > 0);
        }

        private static PortalAliasInfo GetPortalAlias(Uri url, out string portalAlias)
        {
            PortalAliasInfo objPortalAlias = null;
            //string myAlias = Globals.GetDomainName(app.Request, true);

#if DNN71
            string myAlias = TestableGlobals.Instance.GetDomainName(url, true);
#else
            string myAlias = GetDomainName(url, true);
#endif


            portalAlias = "";
            do
            {
                //objPortalAlias = PortalAliasController.GetPortalAliasInfo(myAlias);
                objPortalAlias = PortalAliasController.Instance.GetPortalAlias(myAlias);


                if (objPortalAlias != null)
                {
                    portalAlias = myAlias;
                    break;
                }

                int slashIndex = myAlias.LastIndexOf('/');
                if (slashIndex > 1)
                {
                    myAlias = myAlias.Substring(0, slashIndex);
                }
                else
                {
                    myAlias = "";
                }
            } while (myAlias.Length > 0);

            return objPortalAlias;
        }

        private static void ProcessParameters(CacheController cacheCtrl, RewriterAction action, int PortalId)
        {
            if ((action.ModuleUrl.Trim().Length > 0))
            {
                //split the value into an array based on "/" ( ie. /tabid/##/ )
                string parameters = action.ModuleUrl.Replace("\\", "/").TrimStart('/');
                var rule = cacheCtrl.GetModuleRule(action.CultureCode, action.TabId, parameters);

                if (rule == null)
                {
                    rule = cacheCtrl.GetCustomModuleRule(action.CultureCode, action.TabId, parameters);
                    if (rule != null)
                    {
                        if (rule.Action == UrlRuleAction.Redirect)
                        {
                            if (rule.RemoveTab)
                            {
                                action.RedirectPage = "";
                            }
                            action.RedirectModule = rule.ReplaceRedirectDestination(parameters);
                            action.DoRedirect = true;
                            action.Raison += "+CustomModuleRule:" + parameters + ">" + action.RedirectModule;
                        }
                        else
                        {
                            action.ModuleParameters = rule.ReplaceUrl(parameters);
                            action.WorkUrl = "";
                        }
                        return;
                    }
                }

                if (rule != null)
                {
                    if (rule.Action == UrlRuleAction.Redirect)
                    {
                        if (rule.RemoveTab)
                        {
                            //action.RedirectUrl = redirect.RedirectUrl.Replace("/" + tabUrl, "");
                            action.RedirectPage = "";
                        }
                        //action.RedirectUrl = redirect.RedirectUrl.Replace(parameters.TrimStart('/'), rule.RedirectDestination);
                        action.RedirectModule = rule.RedirectDestination;
                        action.DoRedirect = true;
                        action.Raison += "+ModuleRule:" + rule.Url + ">" + rule.RedirectDestination + " (" + action.TabId + ")";

                    }
                    else if (parameters != rule.Url) // because different case
                    {
                        action.RedirectPage = rule.Url;
                        action.DoRedirect = true;
                        action.Raison += "+Wrong case or url";
                    }
                    action.ModuleParameters = rule.Parameters;
                    action.WorkUrl = "";
                    return;
                }
                else
                {
                    //if (!HttpContext.Current.Request.IsAuthenticated)
                    if (UrlRewiterSettings.IsManage404(PortalId))
                    {
                        action.ModuleNotFound = true;
                    }
                }
                action.QueryParameters = "";
                string[] splitParameters = parameters.Split('/');
                string parameterName;
                string parameterValue;
                //create a well formed querystring based on the array of parameters
                for (int parameterIndex = 0; parameterIndex < splitParameters.Length; parameterIndex++)
                {
                    //ignore the page name 
                    if (splitParameters[parameterIndex].IndexOf(".aspx", StringComparison.InvariantCultureIgnoreCase) == -1)
                    {
                        //get parameter name
                        parameterName = splitParameters[parameterIndex].Trim();
                        if (parameterName.Length > 0)
                        {
                            //rule = GetModuleRule(portalId, CultureCode, TabId, parameterName);
                            rule = null;

                            //add parameter to SendTo if it does not exist already  && not pagename
                            if (action.QueryParameters.IndexOf("?" + parameterName + "=", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                                action.QueryParameters.IndexOf("&" + parameterName + "=", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                                (parameterIndex != splitParameters.Length - 1 || rule != null))
                            {
                                //get parameter value
                                parameterValue = "";
                                if (rule != null)
                                {
                                    action.QueryParameters += "&" + rule.Parameters;
                                }
                                else
                                {
                                    action.QueryParameters += "&" + parameterName;
                                    if (parameterIndex < splitParameters.Length - 1)
                                    {
                                        parameterIndex += 1;
                                        if (!String.IsNullOrEmpty(splitParameters[parameterIndex].Trim()))
                                        {
                                            parameterValue = splitParameters[parameterIndex].Trim();
                                            parameterValue = parameterValue.Replace(".aspx", "");

                                        }
                                    }
                                    //add the parameter value
                                    if (parameterValue.Length > 0)
                                    {
                                        action.QueryParameters += "=" + parameterValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void ProcessQuery(CacheController cacheCtrl, RewriterAction action, int PortalId)
        {
            if ((action.QueryUrl.Trim().Length > 0))
            {
                //split the value into an array based on "/" ( ie. /tabid/##/ )
                string parameters = action.QueryUrl.TrimStart('?', '&').Trim();
                var rule = cacheCtrl.GetModuleRuleByParameters(action.CultureCode, action.TabId, parameters);

                if (rule != null)
                {
                    if (rule.RemoveTab)
                    {
                        action.RedirectPage = "";
                    }
                    action.RedirectModule = rule.Url;
                    action.DoRedirect = true;
                    action.Raison += "+ModuleRule:" + rule.Parameters + ">" + rule.Url;
                    action.QueryUrl = "";
                    return;
                }


                rule = cacheCtrl.GetCustomModuleRuleByParameters(action.CultureCode, action.TabId, parameters);
                if (rule != null)
                {

                    if (rule.RemoveTab)
                    {
                        action.RedirectPage = "";
                    }
                    action.RedirectModule = rule.Replace(parameters, "");
                    action.DoRedirect = true;
                    action.Raison += "+ModuleRule:" + parameters + ">" + action.RedirectModule;
                    action.QueryUrl = "";
                    return;
                }
            }
        }

        private static int ProcessParametersWithoutPage(CacheController cacheCtrl, RewriterAction action)
        {
            if ((action.WorkUrl.Trim().Length > 0))
            {
                //split the value into an array based on "/" ( ie. /tabid/##/ )
                string parameters = action.WorkUrl.Replace("\\", "/").TrimStart('/');
                var rule = cacheCtrl.GetModuleRule(action.CultureCode, parameters);
                if (rule != null)
                {
                    if (rule.Action == UrlRuleAction.Redirect)
                    {
                        /*
                        if (rule.RemoveTab)
                        {
                            redirect.RedirectUrl = redirect.RedirectUrl.Replace("/" + tabUrl, "");
                        }
                         */
                        //action.RedirectUrl = action.RedirectUrl.Replace(parameters.TrimStart('/'), rule.RedirectDestination);
                        action.RedirectModule = rule.RedirectDestination;
                        action.DoRedirect = true;
                        action.Raison += "+ModuleRule:" + rule.Url + ">" + rule.RedirectDestination;

                    }
                    if (rule.TabId != Null.NullInteger)
                    {
                        action.ModuleUrl = parameters;
                        action.ModuleParameters = rule.Parameters;
                        action.TabId = rule.TabId;
                        action.WorkUrl = "";
                    }
                }

            }
            return -1;
        }

        private static string GetHostPort(Uri url, bool IsSecureConnection)
        {
            string MyHostPort;
            if (Globals.UsePortNumber() && ((url.Port != 80 && !IsSecureConnection) || (url.Port != 443 && IsSecureConnection)))
            {
                MyHostPort = url.Host + ":" + url.Port;
            }
            else
            {
                MyHostPort = url.Host;
            }
            return MyHostPort;
        }

        private static Version GetIISVersion()
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\InetStp", false))
            {
                if (key == null) return new Version(0, 0);
                int majorVersion = (int)key.GetValue("MajorVersion", -1);
                int minorVersion = (int)key.GetValue("MinorVersion", -1);
                if (majorVersion == -1 || minorVersion == -1) return new Version(0, 0);
                return new Version(majorVersion, minorVersion);
            }
        }

        private static AspNetHostingPermissionLevel GetCurrentTrustLevel()
        {
            foreach (AspNetHostingPermissionLevel trustLevel in
                    new AspNetHostingPermissionLevel[] {
            AspNetHostingPermissionLevel.Unrestricted,
            AspNetHostingPermissionLevel.High,
            AspNetHostingPermissionLevel.Medium,
            AspNetHostingPermissionLevel.Low,
            AspNetHostingPermissionLevel.Minimal
            })
            {
                try
                {
                    new AspNetHostingPermission(trustLevel).Demand();
                }
                catch (System.Security.SecurityException)
                {
                    continue;
                }

                return trustLevel;
            }

            return AspNetHostingPermissionLevel.None;
        }

#if !DNN71
        private static string GetDomainName(Uri requestedUri, bool parsePortNumber)
        {
            var domainName = new StringBuilder();

            // split both URL separater, and parameter separator
            // We trim right of '?' so test for filename extensions can occur at END of URL-componenet.
            // Test:   'www.aspxforum.net'  should be returned as a valid domain name.
            // just consider left of '?' in URI
            // Binary, else '?' isn't taken literally; only interested in one (left) string
            string uri = requestedUri.ToString();
            string hostHeader =  DotNetNuke.Common.Utilities.Config.GetSetting("HostHeader");
            if (!String.IsNullOrEmpty(hostHeader))
            {
                uri = uri.ToLower().Replace(hostHeader.ToLower(), "");
            }
            int queryIndex = uri.IndexOf("?", StringComparison.Ordinal);
            if (queryIndex > -1)
            {
                uri = uri.Substring(0, queryIndex);
            }
            string[] url = uri.Split('/');
            for (queryIndex = 2; queryIndex <= url.GetUpperBound(0); queryIndex++)
            {
                bool needExit = false;
                switch (url[queryIndex].ToLower())
                {
                    case "":
                        continue;
                    case "admin":
                    case "controls":
                    case "desktopmodules":
                    case "mobilemodules":
                    case "premiummodules":
                    case "providers":
                        needExit = true;
                        break;
                    default:
                        // exclude filenames ENDing in ".aspx" or ".axd" --- 
                        //   we'll use reverse match,
                        //   - but that means we are checking position of left end of the match;
                        //   - and to do that, we need to ensure the string we test against is long enough;
                        if ((url[queryIndex].Length >= ".aspx".Length))
                        {
                            if (url[queryIndex].ToLower().LastIndexOf(".aspx", StringComparison.Ordinal) == (url[queryIndex].Length - (".aspx".Length)) ||
                                url[queryIndex].ToLower().LastIndexOf(".axd", StringComparison.Ordinal) == (url[queryIndex].Length - (".axd".Length)) ||
                                url[queryIndex].ToLower().LastIndexOf(".ashx", StringComparison.Ordinal) == (url[queryIndex].Length - (".ashx".Length)))
                            {
                                break;
                            }
                        }
                        // non of the exclusionary names found
                        domainName.Append((!String.IsNullOrEmpty(domainName.ToString()) ? "/" : "") + url[queryIndex]);
                        break;
                }
                if (needExit)
                {
                    break;
                }
            }
            if (parsePortNumber)
            {
                if (domainName.ToString().IndexOf(":", StringComparison.Ordinal) != -1)
                {
                    if (!Globals.UsePortNumber())
                    {
                        domainName = domainName.Replace(":" + requestedUri.Port, "");
                    }
                }
            }
            return domainName.ToString();
        } 
#endif

    }

    public class RewriterAction
    {

        public RewriterAction()
        {
            TabId = Null.NullInteger;
            CultureCode = null;
        }

        public string LocalPath { get; set; }
        public String OriginalUrl { get; set; }
        public string HostPort { get; set; }
        public string Alias { get; set; }

        // Url parts
        public string CultureUrl { get; set; }
        public string PageUrl { get; set; }
        public string ModuleUrl { get; set; }
        public string QueryUrl { get; set; }

        public string WorkUrl { get; set; }

        // Rewrite information
        public string CultureCode { get; set; }
        public int TabId { get; set; }
        public string ModuleParameters { get; set; }
        public string QueryParameters { get; set; }

        // Force ReWrite
        public bool DoReWrite { get; set; }
        public string RewriteUrl { get; set; }

        // redirection information
        public String RedirectCulture { get; set; }
        public String RedirectPage { get; set; }
        public String RedirectModule { get; set; }

        //public String RedirectUrl { get; set; }
        public int Status { get; set; }
        public bool DoRedirect { get; set; }
        public string Raison { get; set; }
        public bool RedirectHomePage { get; set; }
        public string RedirectUrl { get; set; }

        // not found
        public bool ModuleNotFound { get; set; }
        public bool DoNotFound { get; set; }

    }


}