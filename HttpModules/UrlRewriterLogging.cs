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
using DotNetNuke.Instrumentation;
using DotNetNuke.Entities.Host;
using Satrabel.Services.Log.UrlLog;
using DotNetNuke.Entities.Users;
using DotNetNuke.Entities.Portals;
using System.Web.Configuration;
using System.Net;
using DotNetNuke.Security;
using DotNetNuke.Services.Localization;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Common;
using System.Collections.Specialized;
using DotNetNuke.Entities.Modules;
using System.Collections.Generic;
using DotNetNuke.Security.Permissions;
using System.Diagnostics;
using Satrabel.OpenUrlRewriter.HttpModules;

namespace Satrabel.HttpModules
{
    public class UrlRewriterLogging
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(UrlRewriterLogging));

        public void OnPreRequestHandlerExecute(object sender, EventArgs e)
        {
            try
            {
                //First check if we are upgrading/installing or if it is a non-page request
                var app = (HttpApplication)sender;
                HttpRequest request = app.Request;

                //First check if we are upgrading/installing
                if (request.Url.LocalPath.ToLower().EndsWith("install.aspx")
                        || request.Url.LocalPath.ToLower().EndsWith("upgradewizard.aspx")
                        || request.Url.LocalPath.ToLower().EndsWith("installwizard.aspx"))
                {
                    return;
                }

                //exit if a request for a .net mapping that isn't a content page is made i.e. axd
                if (request.Url.LocalPath.ToLower().EndsWith(".aspx") == false && request.Url.LocalPath.ToLower().EndsWith(".asmx") == false &&
                    request.Url.LocalPath.ToLower().EndsWith(".ashx") == false)
                {
                    return;
                }


                if (request.HttpMethod != "GET")
                {
                    return;
                }

                if (HttpContext.Current != null)
                {
                    HttpContext context = HttpContext.Current;
                    if ((context == null))
                    {
                        return;
                    }
                    var page = context.Handler as DotNetNuke.Framework.CDefault;
                    if ((page == null))
                    {
                        return;
                    }
                    page.Load += OnPageLoad;



                    try
                    {
                        int PortalId = (int)HttpContext.Current.Items["UrlRewrite:PortalId"];
                        if (UrlRewiterSettings.IsW3C(PortalId))
                        {
                            //app.Response.Filter = new PageFilter(app.Response.Filter);

                            ResponseFilterStream filter = new ResponseFilterStream(app.Response.Filter);
                            filter.TransformString += W3CTransform.filter_TransformString;
                            app.Response.Filter = filter;


                            app.Response.AddHeader("X-UA-Compatible", "IE=edge");
                        }
                    }
                    catch { }




                }
            }
            catch (Exception ex)
            {
                /*
                var objEventLog = new EventLogController();
                var objEventLogInfo = new LogInfo();
                objEventLogInfo.AddProperty("Analytics.AnalyticsModule", "OnPreRequestHandlerExecute");
                objEventLogInfo.AddProperty("ExceptionMessage", ex.Message);
                objEventLogInfo.LogTypeKey = EventLogController.EventLogType.HOST_ALERT.ToString();
                objEventLog.AddLog(objEventLogInfo);
                 
                Logger.Error(objEventLogInfo);
                */
                Logger.Error(ex);
            }
        }





        private void OnPageInit(object sender, EventArgs e)
        {
            try
            {
                var page = (System.Web.UI.Page)sender;
                if ((page == null))
                {
                    return;
                }

                //ManageRequest(page, PortalSettings.Current.PortalId, PortalSettings.Current.ActiveTab.TabID);
            }
            catch (Exception ex)
            {
                /*
                var objEventLog = new EventLogController();
                var objEventLogInfo = new LogInfo();
                objEventLogInfo.AddProperty("Analytics.AnalyticsModule", "OnPageLoad");
                objEventLogInfo.AddProperty("ExceptionMessage", ex.Message);
                objEventLogInfo.LogTypeKey = EventLogController.EventLogType.HOST_ALERT.ToString();
                objEventLog.AddLog(objEventLogInfo);
                 */
                Logger.Error(ex);

            }
        }


        private void OnPageLoad(object sender, EventArgs e)
        {
            try
            {
                //var page = (System.Web.UI.Page)sender;
                var page = sender as DotNetNuke.Framework.CDefault;
                if ((page == null))
                {
                    return;
                }

                HtmlMeta MetaRobots = (HtmlMeta)page.FindControl("MetaRobots");
                if (MetaRobots != null)
                {
                    if (UrlRewiterSettings.IsDisableSiteIndex(page.PortalSettings.PortalId))
                    {
                        MetaRobots.Content = "NOINDEX, NOFOLLOW";
                    }
                    else if (page.Request.QueryString["ctl"] != null)
                    {
                        string ctlQueryString = page.Request.QueryString["ctl"].ToLower();
                        if (ctlQueryString == "terms" &&
                            UrlRewiterSettings.IsDisableTermsIndex(page.PortalSettings.PortalId))
                        {
                            MetaRobots.Content = "NOINDEX, NOFOLLOW";
                            /*
                            var Cache = page.Response.Cache;
                            Cache.SetCacheability(HttpCacheability.Public);
                            Cache.SetExpires(DateTime.Now.AddSeconds(20));
                            Cache.SetMaxAge(TimeSpan.FromSeconds(20));
                            Cache.SetValidUntilExpires(true);
                            Cache.SetLastModified(DateTime.Now);
                            Cache.VaryByParams.IgnoreParams = true;
                            */

                        }
                        else if (ctlQueryString == "privacy" &&
                                UrlRewiterSettings.IsDisablePrivacyIndex(page.PortalSettings.PortalId))
                        {
                            MetaRobots.Content = "NOINDEX, NOFOLLOW";
                        }
                        if (ctlQueryString == "login" ||
                            ctlQueryString == "register" ||
                            ctlQueryString == "sendpassword")
                        {
                            MetaRobots.Content = "NOINDEX, NOFOLLOW";
                        }
                    }
                }
                if (page.Request.QueryString["ctl"] != null)
                {
                    string ctlQueryString = page.Request.QueryString["ctl"].ToLower();
                    if (ctlQueryString == "login" ||
                                ctlQueryString == "register" ||
                                ctlQueryString == "sendpassword")
                    {
                        var url = HttpContext.Current.Items["UrlRewrite:OriginalUrl"].ToString();
                        //string url = page.Request.RawUrl;
                        if (url.Contains('?'))
                        {
                            url = url.Remove(url.IndexOf('?'));
                        }

                        //Add Canonical <link>
                        var canonicalLink = new HtmlLink();
                        canonicalLink.Href = url;
                        canonicalLink.Attributes["rel"] = "canonical";

                        //todo check if there is already a canonicalLink

                        // Add the HtmlLink to the Head section of the page.
                        page.Header.Controls.Add(canonicalLink);


                    }
                }
                /*
                var CanonicalCtrl = new  HtmlMeta();
                CanonicalCtrl.Name = "";
                CanonicalCtrl.Content = sContent
                page.Header.Controls.Add(CanonicalCtrl)
                 */


                Dictionary<string, Locale>.ValueCollection Locales;

                if (PortalSettings.Current.ContentLocalizationEnabled)
                    Locales = LocaleController.Instance.GetPublishedLocales(PortalSettings.Current.PortalId).Values;
                else
                    Locales = LocaleController.Instance.GetLocales(PortalSettings.Current.PortalId).Values;

                if (Locales.Count > 1)
                {
                    foreach (Locale loc in Locales)
                    {
                        //locales.Add(loc.Code, loc);
                        // <link rel="alternate" hreflang="en-gb" href="http://en-gb.example.com/page.html" />
                        string LocaleUrl;
                        if (Locales.Count(l => l.Code.Substring(0, 2) == loc.Code.Substring(0, 2)) > 1)
                            LocaleUrl = loc.Code;
                        else
                            LocaleUrl = loc.Code.Substring(0, 2);

                        bool CanViewPage;
                        var altLink = new HtmlLink();
                        altLink.Href = newUrl(loc.Code, out CanViewPage);
                        altLink.Attributes["rel"] = "alternate";
                        altLink.Attributes["hreflang"] = LocaleUrl;

                        // Add the HtmlLink to the Head section of the page.
                        if (CanViewPage)
                        {
                            page.Header.Controls.Add(altLink);
                        }
                    }
                    /*
                    if (PortalSettings.Current.ActiveTab.TabID == PortalSettings.Current.HomeTabId)
                    {
                        var altLink = new HtmlLink();
                        altLink.Href = page.Request.Url.Scheme + "://" + PortalSettings.Current.PortalAlias.HTTPAlias;
                        altLink.Attributes["rel"] = "alternate";
                        altLink.Attributes["hreflang"] = "x-default";
                        page.Header.Controls.Add(altLink);
                    }
                    */
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }



        private void ManageRequest(System.Web.UI.Page page, int PortalId, int TabId)
        {
            HttpRequest Request = page.Request;

            //site logging
            int SiteLogHistory = 10;
            if (SiteLogHistory != 0)
            {
                //get User ID

                //URL Referrer
                string urlReferrer = "";
                try
                {



                }
                catch (Exception exc)
                {
                    Logger.Error(exc);

                }




            }
        }

        public void OnEndRequest(object s, EventArgs e)
        {

            if (s == null)
            {
                return;
            }
            var app = (HttpApplication)s;
#if DEBUG
            try
            {
                Stopwatch timer = (Stopwatch)HttpContext.Current.Items["UrlRewrite:Timer"];
                app.Response.AddHeader("X-OpenUrlRewriter-OnEndRequest", timer.Elapsed.TotalMilliseconds.ToString());
            }
            catch
            {
            }
#endif
            int PortalId = -1;
            try
            {
                PortalId = (int)HttpContext.Current.Items["UrlRewrite:PortalId"];
            }
            catch { }

            if (PortalId == -1)
            {
                return;
            }

            if (!UrlRewiterSettings.IsLogEnabled(PortalId))
            {
                return;
            }



            var server = app.Server;
            var request = app.Request;
            var response = app.Response;
            var requestedPath = app.Request.Url.AbsoluteUri;

            if (RewriterUtils.OmitFromRewriteProcessing(request.Url.LocalPath))
            {
                return;
            }

            if (request.Url.LocalPath.ToLower().EndsWith("/install/install.aspx")
                || request.Url.LocalPath.ToLower().EndsWith("/install/upgradewizard.aspx")
                || request.Url.LocalPath.ToLower().EndsWith("/install/installwizard.aspx")
                || request.Url.LocalPath.ToLower().EndsWith("captcha.aspx")
                || request.Url.LocalPath.ToLower().EndsWith("scriptresource.axd")
                || request.Url.LocalPath.ToLower().EndsWith("webresource.axd"))
            {
                return;
            }

            try
            {
                string originalurl = "";
                try
                {
                    originalurl = HttpContext.Current.Items["UrlRewrite:OriginalUrl"].ToString();
                }
                catch { }

                int TabId = -1;
                try
                {
                    TabId = (int)HttpContext.Current.Items["UrlRewrite:TabId"];
                }
                catch
                {
                }
                string RedirectUrl = "";
                try
                {
                    RedirectUrl = HttpContext.Current.Items["UrlRewrite:RedirectUrl"].ToString();
                }
                catch
                {
                }

                string RewriteUrl = request.Url.ToString();
                try
                {
                    RewriteUrl = HttpContext.Current.Items["UrlRewrite:RewriteUrl"].ToString();
                }
                catch
                {
                }

                var statusCode = response.StatusCode;
                var httpContext = HttpContext.Current;
                if (httpContext.Server.GetLastError() != null && httpContext.Server.GetLastError().GetBaseException() != null)
                {
                    var lastException = HttpContext.Current.Server.GetLastError().GetBaseException();
                    var httpException = lastException as HttpException;
                    if (httpException != null)
                    {
                        statusCode = httpException.GetHttpCode();
                    }
                }
                //Logger.Error("{0} : {1}", httpContext.Request.Url.AbsoluteUri, statusCode); 
                //System.Diagnostics.Debug.WriteLine(originalurl + " : " + statusCode);

                string strSiteLogStorage = "D"; // Host.SiteLogStorage;
                int intSiteLogBuffer = 1; //  Host.SiteLogBuffer;

                //int PortalId = PortalSettings.Current.PortalId;
                //int TabId = PortalSettings.Current.ActiveTab.TabID;

                string urlReferrer = "";
                if (request.UrlReferrer != null)
                {
                    urlReferrer = request.UrlReferrer.ToString();
                }

                //log visit
                bool DoLog = true;
                if (PortalId == -1)
                {
                    DoLog = false;
                }

                //UserInfo objUserInfo = UserController.GetCurrentUserInfo();
                UserInfo objUserInfo = UserController.Instance.GetCurrentUserInfo();
                if (!UrlRewiterSettings.IsLogAuthentificatedUsers(PortalId) && objUserInfo != null && objUserInfo.UserID != -1)
                {
                    DoLog = false;
                }

                if (!UrlRewiterSettings.IsLogStatusCode200(PortalId) && statusCode == 200)
                {
                    DoLog = false;
                }

                if (DoLog)
                {
                    if (PortalId != -1 && UrlRewiterSettings.IsLogEachUrlOneTime(PortalId))
                    {
                        //var UrlLogLst = UrlLogController.GetUrlLogByOriginalUrl(PortalId, originalurl);
                        UrlLogController.DeleteUrlLogByOriginalUrl(PortalId, originalurl);
                    }

                    var objSiteLogs = new UrlLogController();
                    objSiteLogs.AddUrlLog(PortalId, objUserInfo.UserID, urlReferrer, RewriteUrl, originalurl, RedirectUrl,
                                       request.UserAgent, request.UserHostAddress, request.UserHostName,
                                       TabId, statusCode,
                                       intSiteLogBuffer, strSiteLogStorage);
                }
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
            }
        }

        private static void OnError(object source, EventArgs e)
        {
            var httpContext = HttpContext.Current;
            var lastException = HttpContext.Current.Server.GetLastError().GetBaseException();
            var httpException = lastException as HttpException;
            var statusCode = (int)HttpStatusCode.InternalServerError;

            if (httpException != null)
            {
                if (httpException.Message == "File does not exist.")
                {
                    //httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    //httpContext.ClearError();
                    return;
                }

                statusCode = httpException.GetHttpCode();
            }

            if ((statusCode != (int)HttpStatusCode.NotFound) && (statusCode != (int)HttpStatusCode.ServiceUnavailable))
            {
                // TODO : Your error logging code here.
            }


            Logger.Error(string.Format("{0} : {1}", httpContext.Request.Url.AbsoluteUri, statusCode));


            var redirectUrl = string.Empty;

            if (!httpContext.IsCustomErrorEnabled)
            {
                return;
            }



            return;
            var errorsSection = WebConfigurationManager.GetSection("system.web/customErrors") as CustomErrorsSection;
            if (errorsSection != null)
            {
                redirectUrl = errorsSection.DefaultRedirect;

                if (httpException != null && errorsSection.Errors.Count > 0)
                {
                    var item = errorsSection.Errors[statusCode.ToString()];

                    if (item != null)
                    {
                        redirectUrl = item.Redirect;
                    }
                }
            }

            httpContext.Response.Clear();
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.TrySkipIisCustomErrors = true;
            httpContext.ClearError();

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                try
                {
                    HttpTransfer(redirectUrl, httpContext);
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        private static void HttpTransfer(string url, HttpContext currentContext)
        {
            currentContext.Server.TransferRequest(url);
        }

        /// <summary>
        /// getQSParams builds up a new querystring. This is necessary
        /// in order to prep for navigateUrl.
        /// we don't ever want a tabid, a ctl and a language parameter in the qs
        /// also, the portalid param is not allowed when the tab is a supertab
        /// (because NavigateUrl adds the portalId param to the qs)
        /// </summary>
        /// <history>
        ///     [erikvb]   20070814    added
        /// </history>
        private string[] getQSParams(string newLanguage, bool isLocalized)
        {
            string returnValue = "";
            NameValueCollection coll = HttpContext.Current.Request.QueryString;
            string[] arrKeys;
            string[] arrValues;
            PortalSettings settings = PortalController.Instance.GetCurrentPortalSettings();
            arrKeys = coll.AllKeys;

            for (int i = 0; i <= arrKeys.GetUpperBound(0); i++)
            {
                if (arrKeys[i] != null)
                {
                    switch (arrKeys[i].ToLowerInvariant())
                    {
                        case "tabid":
                        case "ctl":
                        case "language": //skip parameter
                            break;
                        case "mid":
                        case "moduleid": //start of patch (Manzoni Fausto)
                            if (isLocalized)
                            {
                                string ModuleIdKey = arrKeys[i].ToLowerInvariant();
                                int ModuleID = 0;
                                int tabid = 0;

                                int.TryParse(coll[ModuleIdKey], out ModuleID);
                                int.TryParse(coll["tabid"], out tabid);
                                ModuleInfo localizedModule = new ModuleController().GetModuleByCulture(ModuleID, tabid, settings.PortalId, LocaleController.Instance.GetLocale(newLanguage));
                                if (localizedModule != null)
                                {
                                    if (!string.IsNullOrEmpty(returnValue))
                                    {
                                        returnValue += "&";
                                    }
                                    returnValue += ModuleIdKey + "=" + localizedModule.ModuleID;
                                }
                            }
                            break;
                        default:
                            if ((arrKeys[i].ToLowerInvariant() == "portalid") && PortalSettings.Current.ActiveTab.IsSuperTab)
                            {
                                //skip parameter
                                //navigateURL adds portalid to querystring if tab is superTab
                            }
                            else
                            {
                                arrValues = coll.GetValues(i);
                                for (int j = 0; j <= arrValues.GetUpperBound(0); j++)
                                {
                                    if (!String.IsNullOrEmpty(returnValue))
                                    {
                                        returnValue += "&";
                                    }
                                    var qsv = arrKeys[i];
                                    qsv = qsv.Replace("\"", "");
                                    qsv = qsv.Replace("'", "");
                                    returnValue += qsv.ToString() + "=" + HttpUtility.UrlEncode(arrValues[j]);
                                }
                            }
                            break;
                    }
                }
            }

            if (!settings.ContentLocalizationEnabled && LocaleController.Instance.GetLocales(settings.PortalId).Count > 1 && !settings.EnableUrlLanguage)
            {
                //because useLanguageInUrl is false, navigateUrl won't add a language param, so we need to do that ourselves
                if (returnValue != "")
                {
                    returnValue += "&";
                }
                returnValue += "language=" + newLanguage.ToLower();
            }

            //return the new querystring as a string array
            return returnValue.Split('&');
        }

        /// <summary>
        /// newUrl returns the new URL based on the new language.
        /// Basically it is just a call to NavigateUrl, with stripped qs parameters
        /// </summary>
        /// <param name="newLanguage"></param>
        /// <history>
        ///     [erikvb]   20070814    added
        /// </history>
        private string newUrl(string newLanguage, out bool CanViewPage)
        {
            CanViewPage = true;
            var objSecurity = new PortalSecurity();
            Locale newLocale = LocaleController.Instance.GetLocale(newLanguage);

            //Ensure that the current ActiveTab is the culture of the new language
            int tabId = PortalSettings.Current.ActiveTab.TabID;
            bool islocalized = false;

            TabInfo localizedTab = new TabController().GetTabByCulture(tabId, PortalSettings.Current.PortalId, newLocale);
            if (localizedTab != null)
            {
                islocalized = true;
                tabId = localizedTab.TabID;
                if (localizedTab.IsDeleted || localizedTab.TabType != TabType.Normal || !TabPermissionController.CanViewPage(localizedTab))
                {
                    CanViewPage = false;
                }
            }
            else
            {
                CanViewPage = false;
            }
            /*
             return
                 objSecurity.InputFilter(
                     Globals.NavigateURL(tabId, PortalSettings.Current.ActiveTab.IsSuperTab, PortalSettings.Current, HttpContext.Current.Request.QueryString["ctl"], newLanguage, getQSParams(newLocale.Code, islocalized)),
                     PortalSecurity.FilterFlag.NoScripting);
             * 
             * for performance
             * 
             */

            var qs = getQSParams(newLocale.Code, islocalized);
            if (islocalized && qs.Length > 0 && !string.IsNullOrEmpty(qs[0]))
            {
                // url with query parameters are bad if page is not neutral (in most of the cases)
                CanViewPage = false;
            }

            return Globals.NavigateURL(tabId, PortalSettings.Current.ActiveTab.IsSuperTab, PortalSettings.Current, HttpContext.Current.Request.QueryString["ctl"], newLanguage, qs);

        }




    }




}
