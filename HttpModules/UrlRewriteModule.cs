#region Copyright
// 
// Satrabel - http://www.satrabel.be
// Copyright (c) 2002-2014
// by Satrabel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion
#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Linq;

using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Entities.Host;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;

using DotNetNuke.Instrumentation;
using DotNetNuke.Services.ClientCapability;
using DotNetNuke.Services.EventQueue;
using DotNetNuke.Services.Localization;
using Satrabel.HttpModules.Config;
using Satrabel.HttpModules.Provider;
using Satrabel.Services.Log.UrlLog;
using DotNetNuke.Entities.Users;
using Satrabel.OpenUrlRewriter.Components;
using DotNetNuke.Framework;
#if DNN71
using DotNetNuke.Entities.Urls;
#endif



#endregion

namespace Satrabel.HttpModules
{
    public class UrlRewriteModule : IHttpModule
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(UrlRewriteModule));
        public string ModuleName
        {
            get
            {
                return "UrlRewriteModule";
            }
        }

        #region IHttpModule Members

        public void Init(HttpApplication application)
        {
            
            application.BeginRequest += OnBeginRequest;
            UrlRewriterLogging logger = new UrlRewriterLogging();
            application.EndRequest += logger.OnEndRequest;
            application.PreRequestHandlerExecute += logger.OnPreRequestHandlerExecute;
            //application.Error += OnError;
            
            
        }

        public void Dispose()
        {
        }

        #endregion

        private string FormatDomain(string url, string replaceDomain, string withDomain)
        {
            if (!String.IsNullOrEmpty(replaceDomain) && !String.IsNullOrEmpty(withDomain))
            {
                if (url.IndexOf(replaceDomain) != -1)
                {
                    url = url.Replace(replaceDomain, withDomain);
                }
            }
            return url;
        }

        private void RewriteUrl(HttpApplication app, out string portalAlias) {
            PortalAliasInfo objPortalAlias;
            RewriterAction action;
            UrlRewriteModuleUtils.RewriteUrl(app, out portalAlias, out objPortalAlias, out action);

            // save OriginalUrl & PortalId for Logging
            if (app != null)
            {
                app.Context.Items.Add("UrlRewrite:OriginalUrl", app.Request.Url.AbsoluteUri);
                app.Context.Items.Add("UrlRewrite:PortalId", (objPortalAlias == null ? -1 : objPortalAlias.PortalID));
            }

            if (action.DoNotFound)
            {
                //string strURL = "ErrorPage.aspx?status=404&error=CustomRule";
                app.Response.Clear();
                app.Response.StatusCode = 404;
                app.Response.Status = "404 Not Found";
                app.Response.AppendHeader("X-OpenUrlRewriter-404-Raison", action.Raison);

                int TabId404 = PortalController.GetPortalSettingAsInteger(UrlRewiterSettings.ErrorPage404Setting, objPortalAlias.PortalID, -1);
                if (TabId404 != -1)
                {
                    //TabInfo errTab = tc.GetTab(errTabId, result.PortalId, true);
                    string strURL = Globals.glbDefaultPage + "?TabId=" + TabId404.ToString();
                    var ps = new PortalSettings(TabId404, objPortalAlias);
                    app.Context.Items.Add("PortalSettings", ps);

                    if (app.Context.User == null)
                    {
                        app.Context.User = Thread.CurrentPrincipal;
                    }
                    app.Response.TrySkipIisCustomErrors = true;
                    //spoof the basePage object so that the client dependency framework
                    //is satisfied it's working with a page-based handler
                    IHttpHandler spoofPage = new CDefault();
                    app.Context.Handler = spoofPage;
                    app.Context.Server.Transfer("~/" + strURL, true);

                }
                else {
                    const string errorPageHtmlHeader = @"<html><head><title>404 Page not found </title></head><body>";
                    const string errorPageHtmlFooter = @"</body></html>";
                    var errorPageHtml = new StringWriter();
                    errorPageHtml.Write("<br> 404 Fle not found ");
                    errorPageHtml.Write("<br> Raison : " + action.Raison);
                    errorPageHtml.Write("<div style='font-weight:bolder'>Administrators</div>");
                    errorPageHtml.Write("<div>Change this message by configuring a specific 404 Error Page.</div>");
                    errorPageHtml.Write(string.Format("<a href=\"//{0}\">Goto website</a>", objPortalAlias.HTTPAlias));
                    app.Response.Write(errorPageHtmlHeader);
                    app.Response.Write(errorPageHtml.ToString());
                    app.Response.Write(errorPageHtmlFooter);                
                }
                
                app.Response.End();
            } else if (action.DoRedirect) {
                app.Context.Items.Add("UrlRewrite:RedirectUrl", action.RedirectUrl);
                app.Response.AppendHeader("X-Redirect-Reason", action.Raison);
                if (action.Status == 302)
                {                    
                    app.Response.Redirect(action.RedirectUrl, true);
                }
                else
                {
                    app.Response.Status = "301 Moved Permanently";                    
                    app.Response.AddHeader("Location", action.RedirectUrl);
                    app.Response.End();
                }
            } else if (action.DoReWrite)
            {
                RewriterUtils.RewriteUrl(app.Context, action.RewriteUrl);
            } 

        }

      
        private static int GetTabByTabPath(int portalID, string tabPath, string cultureCode)
        {
            // Check to see if the tab exists (if localization is enable, check for the specified culture)
            int tabID = TabController.GetTabByTabPath(portalID, tabPath.Replace("/", "//").Replace(".aspx", ""), cultureCode);

            // Check to see if neutral culture tab exists
            if ((tabID == Null.NullInteger && cultureCode.Length > 0))
            {
                tabID = TabController.GetTabByTabPath(portalID, tabPath.Replace("/", "//").Replace(".aspx", ""), "");
            }
            return tabID;
        }


        public void OnBeginRequest(object s, EventArgs e)
        {
            var app = (HttpApplication) s;
            var server = app.Server;
            var request = app.Request;
            var response = app.Response;
            var requestedPath = app.Request.Url.AbsoluteUri;

            if (RewriterUtils.OmitFromRewriteProcessing(request.Url.LocalPath))
            {
                return;
            }
			
            //'Carry out first time initialization tasks
            Initialize.Init(app);
            if (request.Url.LocalPath.ToLower().Contains("/install/install.aspx")
                || request.Url.LocalPath.ToLower().Contains("/install/upgradewizard.aspx")
                || request.Url.LocalPath.ToLower().Contains("/install/installwizard.aspx")
                || request.Url.LocalPath.ToLower().Contains("captcha.aspx")
                || request.Url.LocalPath.ToLower().Contains("scriptresource.axd")
                || request.Url.LocalPath.ToLower().Contains("webresource.axd")
                || request.Url.LocalPath.ToLower().Contains("dmxdav.axd")
                || request.Url.LocalPath.ToLower().Contains("dependencyhandler.axd")
                || request.Url.LocalPath.ToLower().Contains("/api/personabar/")
                )
            {
                return;
            }

           

			
            //URL validation 
            //check for ".." escape characters commonly used by hackers to traverse the folder tree on the server
            //the application should always use the exact relative location of the resource it is requesting
            var strURL = request.Url.AbsolutePath;
            var strDoubleDecodeURL = server.UrlDecode(server.UrlDecode(request.RawUrl));
            if (Regex.Match(strURL, "[\\\\/]\\.\\.[\\\\/]").Success || Regex.Match(strDoubleDecodeURL, "[\\\\/]\\.\\.[\\\\/]").Success)
            {
                DotNetNuke.Services.Exceptions.Exceptions.ProcessHttpException(request);
            }
            try
            {
                //fix for ASP.NET canonicalization issues http://support.microsoft.com/?kbid=887459
                if ((request.Path.IndexOf("\\") >= 0 || Path.GetFullPath(request.PhysicalPath) != request.PhysicalPath))
                {
                    DotNetNuke.Services.Exceptions.Exceptions.ProcessHttpException(request);
                }
            }
            catch (Exception exc)
            {
                //DNN 5479
                //request.physicalPath throws an exception when the path of the request exceeds 248 chars.
                //example to test: http://localhost/dotnetnuke_2/xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx/default.aspx
                Logger.Error("RawUrl:"+request.RawUrl + " / Referrer:" + request.UrlReferrer.AbsoluteUri, exc);

            }
#if DEBUG
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            app.Context.Items.Add("UrlRewrite:Timer", timer);
#endif

            String domainName;
            RewriteUrl(app, out domainName);

            //blank DomainName indicates RewriteUrl couldn't locate a current portal
            //reprocess url for portal alias if auto add is an option
            if(domainName == "" && CanAutoAddPortalAlias())
            {
                domainName = Globals.GetDomainName(app.Request, true);
            }

            //from this point on we are dealing with a "standard" querystring ( ie. http://www.domain.com/default.aspx?tabid=## )
            //if the portal/url was succesfully identified

            var tabId = Null.NullInteger;
            var portalId = Null.NullInteger;
            string portalAlias = null;
            PortalAliasInfo portalAliasInfo = null;
            var parsingError = false;

            // get TabId from querystring ( this is mandatory for maintaining portal context for child portals )
            if (!string.IsNullOrEmpty(request.QueryString["tabid"]))
            {
                if (!Int32.TryParse(request.QueryString["tabid"], out tabId))
                {
                    tabId = Null.NullInteger;
                    parsingError = true;
                }
            }

            // get PortalId from querystring ( this is used for host menu options as well as child portal navigation )
            if (!string.IsNullOrEmpty(request.QueryString["portalid"]))
            {
                if (!Int32.TryParse(request.QueryString["portalid"], out portalId))
                {
                    portalId = Null.NullInteger;
                    parsingError = true;
                }
            }

            if (parsingError)
            {
                //The tabId or PortalId are incorrectly formatted (potential DOS)
                DotNetNuke.Services.Exceptions.Exceptions.ProcessHttpException(request);
            }


            try
            {
                //alias parameter can be used to switch portals
                if (request.QueryString["alias"] != null)
                {
                    // check if the alias is valid
                    var childAlias = request.QueryString["alias"];
                    if (!Globals.UsePortNumber())
                    {
                        childAlias = childAlias.Replace(":" + request.Url.Port, "");
                    }

                    //if (PortalAliasController.GetPortalAliasInfo(childAlias) != null)
                    if (PortalAliasController.Instance.GetPortalAlias(childAlias) != null)
                    {
                        //check if the domain name contains the alias
                        if (childAlias.IndexOf(domainName, StringComparison.OrdinalIgnoreCase) == -1)
                        {
                            //redirect to the url defined in the alias
                            app.Response.AppendHeader("X-Redirect-Reason", "alias parameter");
                            response.Redirect(Globals.GetPortalDomainName(childAlias, request, true), true);
                        }
                        else //the alias is the same as the current domain
                        {
                            portalAlias = childAlias;
                        }
                    }
                }
				
				//PortalId identifies a portal when set
                if (portalAlias == null)
                {
                    if (portalId != Null.NullInteger)
                    {
                        portalAlias = PortalAliasController.GetPortalAliasByPortal(portalId, domainName);
                    }
                }
				
				//TabId uniquely identifies a Portal
                if (portalAlias == null)
                {
                    if (tabId != Null.NullInteger)
                    {
                        //get the alias from the tabid, but only if it is for a tab in that domain
                        portalAlias = PortalAliasController.GetPortalAliasByTab(tabId, domainName);
                        if (String.IsNullOrEmpty(portalAlias))
                        {
                            //if the TabId is not for the correct domain
                            //see if the correct domain can be found and redirect it 
                            //portalAliasInfo = PortalAliasController.GetPortalAliasInfo(domainName);
                            portalAliasInfo = PortalAliasController.Instance.GetPortalAlias(domainName);
                            if (portalAliasInfo != null && !request.Url.LocalPath.ToLower().EndsWith("/linkclick.aspx"))
                            {
                                if (app.Request.Url.AbsoluteUri.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    strURL = "https://" + portalAliasInfo.HTTPAlias.Replace("*.", "");
                                }
                                else
                                {
                                    strURL = "http://" + portalAliasInfo.HTTPAlias.Replace("*.", "");
                                }
                                if (strURL.IndexOf(domainName, StringComparison.InvariantCultureIgnoreCase) == -1)
                                {
                                    strURL += app.Request.Url.PathAndQuery;
                                }
                                app.Response.AppendHeader("X-Redirect-Reason", "not correct domain");
                                response.Redirect(strURL, true);
                            }
                        }
                    }
                }
				
                //else use the domain name
                if (String.IsNullOrEmpty(portalAlias))
                {
                    portalAlias = domainName;
                }
                //using the DomainName above will find that alias that is the domainname portion of the Url
                //ie. dotnetnuke.com will be found even if zzz.dotnetnuke.com was entered on the Url
                //portalAliasInfo = PortalAliasController.GetPortalAliasInfo(portalAlias);
                portalAliasInfo = PortalAliasController.Instance.GetPortalAlias(portalAlias);

                if (portalAliasInfo != null)
                {
                    portalId = portalAliasInfo.PortalID;
                }
				
                //if the portalid is not known
                if (portalId == Null.NullInteger)
                {
                    bool autoAddPortalAlias = CanAutoAddPortalAlias();

                    if (!autoAddPortalAlias && !request.Url.LocalPath.EndsWith(Globals.glbDefaultPage, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // allows requests for aspx pages in custom folder locations to be processed
                        return;
                    }

                    if (autoAddPortalAlias)
                    {
                        var portalAliasController = new PortalAliasController();
                        portalId = Host.HostPortalID;
                        //the domain name was not found so try using the host portal's first alias
                        if (portalId > Null.NullInteger)
                        {
                            portalAliasInfo = new PortalAliasInfo();
                            portalAliasInfo.PortalID = portalId;
                            portalAliasInfo.HTTPAlias = portalAlias;
                            portalAliasController.AddPortalAlias(portalAliasInfo);
                            app.Response.AppendHeader("X-Redirect-Reason", "auto add portalalias");
                            response.Redirect(app.Request.Url.ToString(), true);
                        }
                    }
                }
            }
            catch (ThreadAbortException exc)
            {
                //Do nothing if Thread is being aborted - there are two response.redirect calls in the Try block
                Logger.Debug(exc);

            }
            catch (Exception ex)
            {
				//500 Error - Redirect to ErrorPage
                Logger.Error(ex);

                strURL = "~/ErrorPage.aspx?status=500&error=" + server.UrlEncode(ex.Message);
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Server.Transfer(strURL);
            }
            if (portalId != -1)
            {
                //load the PortalSettings into current context
                var portalSettings = new PortalSettings(tabId, portalAliasInfo);
                app.Context.Items.Add("PortalSettings", portalSettings);

                // load PortalSettings and HostSettings dictionaries into current context
                // specifically for use in DotNetNuke.Web.Client, which can't reference DotNetNuke.dll to get settings the normal way
                app.Context.Items.Add("PortalSettingsDictionary", PortalController.Instance.GetPortalSettings(portalId));
                app.Context.Items.Add("HostSettingsDictionary", HostController.Instance.GetSettingsDictionary());
#if DNN71                
                if (portalSettings.PortalAliasMappingMode == PortalSettings.PortalAliasMapping.Redirect &&
                    portalAliasInfo != null && !portalAliasInfo.IsPrimary && !request.IsLocal)
                {
#else
                if (portalSettings.PortalAliasMappingMode == PortalSettings.PortalAliasMapping.Redirect && 
                    !String.IsNullOrEmpty(portalSettings.DefaultPortalAlias) &&
                    portalAliasInfo != null &&
                    portalAliasInfo.HTTPAlias != portalSettings.DefaultPortalAlias && !request.IsLocal)
                {
#endif

                    //Permanently Redirect
                    response.StatusCode = 301;
                    
                    var redirectAlias = Globals.AddHTTP(portalSettings.DefaultPortalAlias);
                    var checkAlias = Globals.AddHTTP(portalAliasInfo.HTTPAlias);
                    var redirectUrl = redirectAlias + request.RawUrl;
                    if(redirectUrl.StartsWith(checkAlias, StringComparison.InvariantCultureIgnoreCase))
                    {
                        redirectUrl = redirectAlias + redirectUrl.Substring(checkAlias.Length);
                    }
                    app.Response.AppendHeader("X-Redirect-Reason", "alias redirect");
                    response.AppendHeader("Location", redirectUrl);
                }

                //manage page URL redirects - that reach here because they bypass the built-in navigation
                //ie Spiders, saved favorites, hand-crafted urls etc
                if (!String.IsNullOrEmpty(portalSettings.ActiveTab.Url) && request.QueryString["ctl"] == null && request.QueryString["fileticket"] == null)
                {
					//Target Url
                    var redirectUrl = portalSettings.ActiveTab.FullUrl;
                    app.Response.AppendHeader("X-Redirect-Reason", "page url redirect");
                    if (portalSettings.ActiveTab.PermanentRedirect)
                    {
						//Permanently Redirect
                        
                        response.StatusCode = 301;
                        response.AppendHeader("Location", redirectUrl);
                    }
                    else
                    {
						//Normal Redirect
                        response.Redirect(redirectUrl, true);
                    }
                }
				
                //manage secure connections
                if (request.Url.AbsolutePath.EndsWith(".aspx", StringComparison.InvariantCultureIgnoreCase))
                {
					//request is for a standard page
                    strURL = "";
                    //if SSL is enabled
                    if (portalSettings.SSLEnabled)
                    {
						//if page is secure and connection is not secure orelse ssloffload is enabled and server value exists
                        if ((portalSettings.ActiveTab.IsSecure && !request.IsSecureConnection) && (IsSSLOffloadEnabled(request) == false))
                        {
							//switch to secure connection
                            strURL = requestedPath.Replace("http://", "https://");
                            strURL = FormatDomain(strURL, portalSettings.STDURL, portalSettings.SSLURL);
                        }
                    }
                    //if SSL is enforced
                    if (portalSettings.SSLEnforced)
                    {
						//if page is not secure and connection is secure 
                        if ((!portalSettings.ActiveTab.IsSecure && request.IsSecureConnection) )
                        {
                            //check if connection has already been forced to secure orelse ssloffload is disabled
                            if (request.QueryString["ssl"] == null)
                            {
                                strURL = requestedPath.Replace("https://", "http://");
                                strURL = FormatDomain(strURL, portalSettings.SSLURL, portalSettings.STDURL);
                            }
                        }
                    }
					
					//if a protocol switch is necessary
                    if (!String.IsNullOrEmpty(strURL))
                    {
                        if (strURL.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                        {
							//redirect to secure connection
                            app.Response.AppendHeader("X-Redirect-Reason", "redirect to secure");
                            // response.Redirect(strURL, true);
                            response.RedirectPermanent(strURL, true);
                        }
                        else //when switching to an unsecure page, use a clientside redirector to avoid the browser security warning
                        {
                            response.Clear();
                            //add a refresh header to the response 
                            response.AddHeader("Refresh", "0;URL=" + strURL);
                            //add the clientside javascript redirection script
                            response.Write("<html><head><title></title>");
                            response.Write("<!-- <script language=\"javascript\">window.location.replace(\"" + strURL + "\")</script> -->");
                            response.Write("</head><body></body></html>");
                            //send the response
                            response.End();
                        }
                    }
                }
            }
            else
            {
                //alias does not exist in database
                //and all attempts to find another have failed
                //this should only happen if the HostPortal does not have any aliases
                //404 Error - Redirect to ErrorPage
                strURL = "~/ErrorPage.aspx?status=404&error=" + domainName;
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Server.Transfer(strURL);
            }

            if (app.Context.Items["FirstRequest"] != null)
            {
                app.Context.Items.Remove("FirstRequest");

                //Process any messages in the EventQueue for the Application_Start_FirstRequest event
                EventQueueController.ProcessMessages("Application_Start_FirstRequest");
            }
#if DEBUG
            app.Response.AddHeader("X-OpenUrlRewriter-OnBeginRequest", timer.Elapsed.TotalMilliseconds.ToString());
#endif
        }


       

        private bool IsSSLOffloadEnabled(HttpRequest request)
        {

            var ssloffloadheader = HostController.Instance.GetString("SSLOffloadHeader", "");
            //if the ssloffloadheader variable has been set check to see if a request header with that type exists
            if (!string.IsNullOrEmpty(ssloffloadheader.ToString()))
            {
                var ssloffload = request.Headers[ssloffloadheader.ToString()];
                if (!string.IsNullOrEmpty(ssloffload))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private bool CanAutoAddPortalAlias()
        {
            var autoAddPortalAlias = HostController.Instance.GetBoolean("AutoAddPortalAlias");
            autoAddPortalAlias = autoAddPortalAlias && (new PortalController().GetPortals().Count == 1);
            return autoAddPortalAlias;
        }


    }

}