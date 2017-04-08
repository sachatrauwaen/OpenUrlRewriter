#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2012
// by DotNetNuke Corporation
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
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;

using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Framework.Providers;
using Satrabel.HttpModules.Config;
using Satrabel.HttpModules.Provider;
using Satrabel.HttpModules;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Localization;


#endregion

namespace Satrabel.Services.Url.FriendlyUrl
{
    public class OpenFriendlyUrlProvider : DotNetNuke.Services.Url.FriendlyUrl.FriendlyUrlProvider
    {
        private const string ProviderType = "friendlyUrl";
        private const string RegexMatchExpression = "[^a-zA-Z0-9 ]";
        private readonly string _fileExtension;

        private readonly bool _includePageName;

        private readonly ProviderConfiguration _providerConfiguration = ProviderConfiguration.GetProviderConfiguration(ProviderType);

        private readonly string _regexMatch;

        private readonly string _regexMatchDefaultPortal;


        public OpenFriendlyUrlProvider()
        {
            //Read the configuration specific information for this provider
            var objProvider = (Provider)_providerConfiguration.Providers[_providerConfiguration.DefaultProvider];

            //Read the attributes for this provider
            if (!String.IsNullOrEmpty(objProvider.Attributes["includePageName"]))
            {
                _includePageName = bool.Parse(objProvider.Attributes["includePageName"]);
            }
            else
            {
                _includePageName = true;
            }
            if (!String.IsNullOrEmpty(objProvider.Attributes["regexMatch"]))
            {
                _regexMatch = objProvider.Attributes["regexMatch"];
            }
            else
            {
                _regexMatch = RegexMatchExpression;
            }
            if (objProvider.Attributes["fileExtension"] != null)
            {
                _fileExtension = objProvider.Attributes["fileExtension"];
            }
            else
            {
                _fileExtension = ".aspx";
            }

            if (!String.IsNullOrEmpty(objProvider.Attributes["regexMatchDefaultPortal"]))
            {
                _regexMatchDefaultPortal = objProvider.Attributes["regexMatchDefaultPortal"];
            }
            else
            {
                _regexMatchDefaultPortal = "";
            }
        }

        public string FileExtension
        {
            get
            {
                return _fileExtension;
            }
        }

        public bool IncludePageName
        {
            get
            {
                return _includePageName;
            }
        }

        public string RegexMatch
        {
            get
            {
                return _regexMatch;
            }
        }

        public string RegexMatchDefaultPortal
        {
            get
            {
                return _regexMatchDefaultPortal;
            }
        }

        public override string FriendlyUrl(TabInfo tab, string path)
        {
            PortalSettings _portalSettings = PortalController.GetCurrentPortalSettings();
            return FriendlyUrl(tab, path, Globals.glbDefaultPage, _portalSettings);
        }

        public override string FriendlyUrl(TabInfo tab, string path, string pageName)
        {
            PortalSettings _portalSettings = PortalController.GetCurrentPortalSettings();
            return FriendlyUrl(tab, path, pageName, _portalSettings);
        }

        public override string FriendlyUrl(TabInfo tab, string path, string pageName, PortalSettings settings)
        {
            if (settings == null)
            {
                return FriendlyUrl(tab, path, pageName);
            }
            return FriendlyUrl(tab, path, pageName, settings.PortalAlias.HTTPAlias);
        }

        public override string FriendlyUrl(TabInfo tab, string path, string pageName, string portalAlias)
        {
            /*
            var dnn = new DotNetNuke.Services.Url.FriendlyUrl.DNNFriendlyUrlProvider();
            return dnn.FriendlyUrl(tab, path, pageName, portalAlias);
            */

            string friendlyPath = path;
            bool isPagePath = (tab != null);
            int PortalId = -1;

#if DNN71
            string DefaultPortalAlias = portalAlias;
            if (tab != null)
            {
                string CultureCode = "";
                PortalId = tab.PortalID;
                var queryStringDic = GetQueryStringDictionary(path);
                if (queryStringDic.ContainsKey("portalid"))
                {
                    int.TryParse(queryStringDic.GetValue("portalid", ""), out PortalId);
                }
                if (queryStringDic.ContainsKey("language"))
                {
                    CultureCode = queryStringDic.GetValue("language", "");
                }
                else if (!string.IsNullOrEmpty(tab.CultureCode))
                {
                    CultureCode = tab.CultureCode;
                }
                if (!string.IsNullOrEmpty(CultureCode))
                {
                    var primaryAliases = DotNetNuke.Entities.Portals.Internal.TestablePortalAliasController.Instance.GetPortalAliasesByPortalId(PortalId).AsQueryable();
                    if (PortalSettings.Current != null && PortalSettings.Current.PortalAliasMappingMode == PortalSettings.PortalAliasMapping.Redirect)
                    {
                        primaryAliases = primaryAliases.Where(a => a.IsPrimary == true);
                    }
                    else
                    {
                        var mainAlias = primaryAliases.FirstOrDefault(a => string.IsNullOrEmpty(a.CultureCode) && portalAlias.StartsWith(a.HTTPAlias));
                        if (mainAlias != null)
                        {
                            primaryAliases = primaryAliases.Where(a => a.HTTPAlias.StartsWith(mainAlias.HTTPAlias));
                        }
                    }
                    var alias = primaryAliases.FirstOrDefault(a => string.Equals(a.CultureCode, CultureCode, StringComparison.InvariantCultureIgnoreCase));
                    if (alias != null)
                    {
                        portalAlias = alias.HTTPAlias;
                        DefaultPortalAlias = portalAlias;
                    }
                    var DefaultAlias = primaryAliases.FirstOrDefault(a => string.IsNullOrEmpty(a.CultureCode));
                    if (DefaultAlias != null)
                    {
                        DefaultPortalAlias = DefaultAlias.HTTPAlias;
                    }

                    if (!string.IsNullOrEmpty(RegexMatchDefaultPortal))
                    {
                        var re = new Regex(RegexMatchDefaultPortal, RegexOptions.IgnoreCase);
                        if (re.IsMatch(tab.TabPath))
                        {
                            portalAlias = DefaultPortalAlias;
                        }
                    }

                }
            }
#endif
            /*
            if ((UrlFormat == UrlFormatType.HumanFriendly))
            {
                if ((tab != null))
                {
                    var queryStringDic = GetQueryStringDictionary(path);
                    // only tabid
                    if ((queryStringDic.Count == 0 || (queryStringDic.Count == 1 && queryStringDic.ContainsKey("tabid"))))
                    {
                        string TabPath;
                        var rule = GetTabUrl(tab.TabID);
                        if (rule != null) {
                            TabPath = rule.Url.TrimStart('/');
                        } else {
                            TabPath = tab.TabPath.Replace("//", "/").TrimStart('/');
                        }
                        friendlyPath = GetFriendlyAlias("~/" + TabPath + FileExtension, portalAlias, isPagePath);
                    }
                    // tabid & language
                    else if ((queryStringDic.Count == 2 && queryStringDic.ContainsKey("tabid") && queryStringDic.ContainsKey("language")))
                    {
                        string language;
                        if (!tab.IsNeutralCulture)
                            language = tab.CultureCode;
                        else
                            language = queryStringDic["language"];

                        var rule = GetLanguageUrlByCulture(language);
                        if (rule != null)
                            language = rule.Url;

                        string TabPath;
                        rule = GetTabUrl(tab.TabID);
                        if (rule != null)
                            TabPath = rule.Url.TrimStart('/');
                        else
                            TabPath = tab.TabPath.Replace("//", "/").TrimStart('/');

                        friendlyPath = GetFriendlyAlias("~/" + language + "/" + TabPath + FileExtension, portalAlias, isPagePath).ToLower();
                    }
                    // more then tabid & language
                    else
                    {
                        if (queryStringDic.ContainsKey("ctl") && !queryStringDic.ContainsKey("language"))
                        {
                            switch (queryStringDic["ctl"].ToLowerInvariant())
                            {
                                case "terms":
                                    friendlyPath = GetFriendlyAlias("~/terms" + FileExtension, portalAlias, isPagePath);
                                    break;
                                case "privacy":
                                    friendlyPath = GetFriendlyAlias("~/privacy"+FileExtension, portalAlias, isPagePath);
                                    break;
                                case "login":
                                    if ((queryStringDic.ContainsKey("returnurl")))
                                    {
                                        friendlyPath = GetFriendlyAlias("~/login"+FileExtension+"?ReturnUrl=" + queryStringDic["returnurl"], portalAlias, isPagePath);
                                    }
                                    else
                                    {
                                        friendlyPath = GetFriendlyAlias("~/login" + FileExtension, portalAlias, isPagePath);
                                    }
                                    break;
                                case "register":
                                    if ((queryStringDic.ContainsKey("returnurl")))
                                    {
                                        friendlyPath = GetFriendlyAlias("~/register"+FileExtension+"?returnurl=" + queryStringDic["returnurl"], portalAlias, isPagePath);
                                    }
                                    else
                                    {
                                        friendlyPath = GetFriendlyAlias("~/register" + FileExtension, portalAlias, isPagePath);
                                    }
                                    break;
                                default:
                                    //Return Search engine friendly version
                                    return GetFriendlyQueryString(tab, GetFriendlyAlias(path, portalAlias, isPagePath), pageName);
                            }
                        }
                        else
                        {
                            //Return Search engine friendly version
                            return GetFriendlyQueryString(tab, GetFriendlyAlias(path, portalAlias, isPagePath), pageName);
                        }
                    }
                }
            }
            else
            */
            {
                //Return Search engine friendly version
                //friendlyPath = GetFriendlyQueryString(tab, GetFriendlyAlias(path, portalAlias, isPagePath), pageName);
                //bool DefaultPage;
                friendlyPath = GetFriendlyQueryString(tab, path, pageName, PortalId);

#if DNN71
                if (friendlyPath == "~/")
                    portalAlias = DefaultPortalAlias;
#endif

                friendlyPath = GetFriendlyAlias(friendlyPath, portalAlias, isPagePath);
            }

            friendlyPath = CheckPathLength(Globals.ResolveUrl(friendlyPath), path);

            if (IsUrlToLowerCase(tab, friendlyPath) && !friendlyPath.Contains("tabid"))
            {
                friendlyPath = friendlyPath.ToLower();

            }

            return friendlyPath;
        }

        public static bool IsUrlToLowerCase(TabInfo tab, string url)
        {
            return UrlRewiterSettings.IsUrlToLowerCase() && tab != null && !UrlRewiterSettings.ExcludeFromLowerCase(tab.PortalID, url);
        }
        
        private static IEnumerable<UrlRule> getRules(int PortalId)
        {
            /*
            if (PortalId < 0) // host menu
                return new List<UrlRule>();
            else
             */
            return UrlRuleConfiguration.GetConfig(PortalId).Rules.Where(r => r.Action == UrlRuleAction.Rewrite);
        }

        private static IEnumerable<UrlRule> getRules(int PortalId, string CultureCode)
        {
            if (CultureCode == "") CultureCode = null;
            return getRules(PortalId).Where(r => r.CultureCode == CultureCode );
        }

        private static UrlRule GetTabUrl(int PortalId, string CultureCode, int TabId)
        {
            var rule = getRules(PortalId, CultureCode).FirstOrDefault(r => r.RuleType == UrlRuleType.Tab && r.Parameters == "tabid=" + TabId.ToString());
            if (rule == null && !string.IsNullOrEmpty(CultureCode))
            {
                rule = getRules(PortalId, null).FirstOrDefault(r => r.RuleType == UrlRuleType.Tab && r.Parameters == "tabid=" + TabId.ToString());
            }
            return rule;
        }

        private static UrlRule GetTabUrl(int PortalId, string CultureCode, string parameter)
        {
            parameter = parameter.ToLower();
            var rule = getRules(PortalId, CultureCode).FirstOrDefault(r => r.RuleType == UrlRuleType.Tab && r.Parameters == parameter);
            if (rule == null && !string.IsNullOrEmpty(CultureCode))
            {
                rule = getRules(PortalId, null).FirstOrDefault(r => r.RuleType == UrlRuleType.Tab && r.Parameters == parameter);
            }
            return rule;
        }


        private static UrlRule GetLanguageUrlByCulture(int PortalId, string CultureCode)
        {
            return getRules(PortalId).FirstOrDefault(r => r.RuleType == UrlRuleType.Culture && r.Parameters == "language=" + CultureCode);
        }

        private static UrlRule GetLanguageUrl(int PortalId, string parameter)
        {
            return getRules(PortalId).FirstOrDefault(r => r.RuleType == UrlRuleType.Culture && r.Parameters.ToLower() == parameter.ToLower());
        }


        private static UrlRule GetModuleUrl(int PortalId, string CultureCode, int TabId, string ModuleQueryString)
        {
            var rules = getRules(PortalId).Where(r => r.RuleType == UrlRuleType.Module && r.IsMatch(ModuleQueryString));
            // with tabid
            var rule = rules.FirstOrDefault(r => r.CultureCode == CultureCode && r.TabId == TabId);
            if (rule == null && !string.IsNullOrEmpty(CultureCode))
            {
                rule = rules.FirstOrDefault(r => r.CultureCode == null && r.TabId == TabId);
            }
            // without tabid
            if (rule == null)
            {
                rule = rules.FirstOrDefault(r => r.CultureCode == CultureCode);
            }
            if (rule == null && !string.IsNullOrEmpty(CultureCode))
            {
                rule = rules.FirstOrDefault(r => r.CultureCode == null);
            }

            return rule;
        }


        private static UrlRule GetCustomModuleUrl(int PortalId, string CultureCode, int TabId, string ModuleQueryString)
        {

            var rules = getRules(PortalId).Where(r => r.RuleType == UrlRuleType.Custom && r.IsMatch(ModuleQueryString));

            // with tabid
            var rule = rules.FirstOrDefault(r => r.CultureCode == CultureCode && r.TabId == TabId);
            if (rule == null && !string.IsNullOrEmpty(CultureCode))
            {
                rule = rules.FirstOrDefault(r => r.CultureCode == null && r.TabId == TabId);
            }
            // without tabid
            if (rule == null)
            {
                rule = rules.FirstOrDefault(r => r.CultureCode == CultureCode);
            }
            if (rule == null && !string.IsNullOrEmpty(CultureCode))
            {
                rule = rules.FirstOrDefault(r => r.CultureCode == null);
            }

            return rule;
        }

        /*
        private static UrlRule GetModuleUrl(int PortalId, string CultureCode, int TabId, string TabModuleQueryString)
        {
            var rules = getRules(PortalId).Where(r => r.RuleType == UrlRuleType.TabModule && r.Parameters.ToLower() == TabModuleQueryString.ToLower());                        
            var rule = rules.FirstOrDefault(r => r.CultureCode == CultureCode);
            
            if (rule == null && !string.IsNullOrEmpty(CultureCode))
            {
                rule = rules.FirstOrDefault(r => r.CultureCode == null);
            }

            return rule;
        }
        */

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// AddPage adds the page to the friendly url
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="path">The path to format.</param>
        /// <param name="pageName">The page name.</param>
        /// <returns>The formatted url</returns>
        /// <history>
        ///		[cnurse]	12/16/2004	created
        /// </history>
        /// -----------------------------------------------------------------------------
        private string AddPage(string path, string pageName)
        {
            string friendlyPath = path;


            if (pageName == Globals.glbDefaultPage)
            {
                if (friendlyPath != "~/")
                {
                    if (friendlyPath.EndsWith("/"))
                    {
                        friendlyPath = friendlyPath.TrimEnd('/');
                    }
                    friendlyPath = friendlyPath + FileExtension;
                }
            }
            else
            {
                pageName = pageName.Replace(".aspx", FileExtension).ToLower();
                if ((friendlyPath.EndsWith("/")))
                {
                    friendlyPath = friendlyPath + pageName;
                }
                else
                {
                    friendlyPath = friendlyPath + "/" + pageName;
                }
            }
            return friendlyPath;
        }

        private string CheckPathLength(string friendlyPath, string originalpath)
        {
            if (friendlyPath.Length >= 260)
            {
                return Globals.ResolveUrl(originalpath);
            }
            else
            {
                return friendlyPath;
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// GetFriendlyAlias gets the Alias root of the friendly url
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="path">The path to format.</param>
        /// <param name="portalAlias">The portal alias of the site.</param>
        /// <param name="isPagePath">Whether is a relative page path.</param>
        /// <returns>The formatted url</returns>
        /// <history>
        ///		[cnurse]	12/16/2004	created
        /// </history>
        /// -----------------------------------------------------------------------------
        private string GetFriendlyAlias(string path, string portalAlias, bool isPagePath)
        {
            string friendlyPath = path;
            string matchString = "";
            if (portalAlias != Null.NullString)
            {
                if (HttpContext.Current.Items["UrlRewrite:OriginalUrl"] != null)
                {
                    string httpAlias = Globals.AddHTTP(portalAlias).ToLowerInvariant();
                    string originalUrl = HttpContext.Current.Items["UrlRewrite:OriginalUrl"].ToString().ToLowerInvariant();
                    httpAlias = Globals.AddPort(httpAlias, originalUrl);
                    if (originalUrl.StartsWith(httpAlias))
                    {
                        matchString = httpAlias;
                    }
                    if ((String.IsNullOrEmpty(matchString)))
                    {
                        //Manage the special case where original url contains the alias as
                        //http://www.domain.com/Default.aspx?alias=www.domain.com/child"
                        Match portalMatch = Regex.Match(originalUrl, "^?alias=" + portalAlias, RegexOptions.IgnoreCase);
                        if (!ReferenceEquals(portalMatch, Match.Empty))
                        {
                            matchString = httpAlias;
                        }
                    }

                    if ((String.IsNullOrEmpty(matchString)))
                    {
                        //Manage the special case of child portals 
                        //http://www.domain.com/child/default.aspx
                        string tempurl = HttpContext.Current.Request.Url.Host + Globals.ResolveUrl(friendlyPath);
                        if (!tempurl.Contains(portalAlias))
                        {
                            matchString = httpAlias;
                        }
                    }

                    if ((String.IsNullOrEmpty(matchString)))
                    {
                        // manage the case where the current hostname is www.domain.com and the portalalias is domain.com
                        // (this occurs when www.domain.com is not listed as portal alias for the portal, but domain.com is)
                        string wwwHttpAlias = Globals.AddHTTP("www." + portalAlias);
                        if (originalUrl.StartsWith(wwwHttpAlias))
                        {
                            matchString = wwwHttpAlias;
                        }
                    }
                }
            }
            if ((!String.IsNullOrEmpty(matchString)))
            {
                if ((path.IndexOf("~") != -1))
                {
                    if (matchString.EndsWith("/"))
                    {
                        friendlyPath = friendlyPath.Replace("~/", matchString);
                    }
                    else
                    {
                        friendlyPath = friendlyPath.Replace("~", matchString);
                    }
                }
                else
                {
                    friendlyPath = matchString + friendlyPath;
                }
            }
            else
            {
                friendlyPath = Globals.ResolveUrl(friendlyPath);
            }
            if (friendlyPath.StartsWith("//") && isPagePath)
            {
                friendlyPath = friendlyPath.Substring(1);
            }
            return friendlyPath;
        }

        //static string internPatern = "agentType=View&PropertyID=[id]";
        //static string externPatern = "PropertyID-[id]";

        private string GetFriendlyQueryString(TabInfo tab, string path, string pageName, int CurrentPortalId)
        {
            bool DoAddPage = true;
            string friendlyPath = path;
            string friendlyParams = "";
            Match queryStringMatch = Regex.Match(friendlyPath, "(.[^\\\\?]*)\\\\?(.*)", RegexOptions.IgnoreCase);
            string queryStringSpecialChars = "";
            if (!ReferenceEquals(queryStringMatch, Match.Empty))
            {
                friendlyPath = queryStringMatch.Groups[1].Value;
                friendlyPath = Regex.Replace(friendlyPath, Globals.glbDefaultPage, "", RegexOptions.IgnoreCase);
                string queryString = queryStringMatch.Groups[2].Value.Replace("&amp;", "&");
                if ((queryString.StartsWith("?")))
                {
                    queryString = queryString.TrimStart(Convert.ToChar("?")); //.ToLower();
                }
                string[] nameValuePairs = queryString.Split(Convert.ToChar("&"));
                if (tab != null)
                {
                    string LanguageQueryString = "";
                    string TabQueryString = "";
                    string ModuleQueryString = "";
                    string OtherQueryString = "";

                    string CultureCode = null;
                    int PortalId = tab.PortalID;

                    // find the different part of the url
                    foreach (string nameValuePair in nameValuePairs)
                    {
                        string[] pair = nameValuePair.Split(Convert.ToChar("="));
                        if ((pair.Length > 1))
                        {
                            if (pair[0].ToLower() == "tabid")
                            {
                                TabQueryString = pair[0] + "=" + pair[1];
                            }
                            else if (pair[0].ToLower() == "language")
                            {
                                LanguageQueryString = pair[0] + "=" + pair[1];
                                CultureCode = pair[1];
                            }
                            else if (pair[0].ToLower() == "returnurl")
                            {
                                OtherQueryString = OtherQueryString + "&" + nameValuePair;
                            }
                            else if (pair[0].ToLower() == "portalid")
                            {
                                int.TryParse(pair[1], out PortalId);
                                ModuleQueryString = ModuleQueryString + "&" + nameValuePair;
                            }
                            else
                            {
                                ModuleQueryString = ModuleQueryString + "&" + nameValuePair;
                            }
                        }
                        else
                        {
                            OtherQueryString = OtherQueryString + "&" + nameValuePair;
                        }
                    }
                    ModuleQueryString = ModuleQueryString.TrimStart('&');
                    OtherQueryString = OtherQueryString.TrimStart('&');

                    queryString = "";

                    if (LanguageQueryString != "")
                    {
#if !DNN71
                        var Rule = GetLanguageUrl(PortalId, LanguageQueryString);
                        if (Rule != null)
                            LanguageQueryString = Rule.Url;
                        else
                            LanguageQueryString = CultureCode;

                        queryString += "&" + LanguageQueryString;
#endif
                    }
                    else if (!string.IsNullOrEmpty(tab.CultureCode))
                    {
                        CultureCode = tab.CultureCode;
#if !DNN71
                        var Rule = GetLanguageUrlByCulture(PortalId, tab.CultureCode);
                        if (Rule != null)
                            LanguageQueryString = Rule.Url;
                        else
                            LanguageQueryString = tab.CultureCode;

                        
                        queryString += "&" + LanguageQueryString;
#endif
                    }




                    /*
                    if (TabQueryString != "" && ModuleQueryString != "")
                    {
                        string TabModuleQueryString = TabQueryString + "&" + ModuleQueryString;
                        var ModuleRule = GetTabModuleUrl(tab.PortalID, CultureCode, tab.TabID, TabModuleQueryString);
                        if (ModuleRule != null)
                        {
                            ModuleQueryString = ModuleRule.Url;
                            TabQueryString = "";
                            ModuleQueryString = "";
                            queryString = queryString + "&" + TabModuleQueryString;
                            // if exist module rewrite rule, dont add pagename at the end
                            DoAddPage = false;
                        }
                    }
                    */
                    if (ModuleQueryString != "")
                    {
                        var ModuleRule = GetModuleUrl(tab.PortalID, CultureCode, tab.TabID, ModuleQueryString);
                        if (ModuleRule != null)
                        {
                            ModuleQueryString = ModuleRule.Url;
                            if (ModuleRule.RemoveTab)
                                TabQueryString = "";

                            // if exist module rewrite rule, dont add pagename at the end
                            DoAddPage = false;
                        }
                        else
                        {
                            ModuleRule = GetCustomModuleUrl(tab.PortalID, CultureCode, tab.TabID, ModuleQueryString);
                            if (ModuleRule != null)
                            {
                                string pn = pageName.Replace(".aspx", FileExtension).ToLower();
                                ModuleQueryString = ModuleRule.Replace(ModuleQueryString, pn);
                                if (ModuleRule.RemoveTab)
                                    TabQueryString = "";

                                // if exist module rewrite rule, dont add pagename at the end
                                DoAddPage = false;
                            }
                            /*
                            string internRegExp = "^" + internPatern.Replace("[", "(?'").Replace("]", "'.*)") + "$";
                            string externRegExp = externPatern.Replace("[", "${").Replace("]", "}");
                            Regex regex = new Regex(internRegExp);
                            if (regex.IsMatch(ModuleQueryString))
                            {
                                ModuleQueryString = regex.Replace(ModuleQueryString, externRegExp);
                                //DoAddPage = false;
                            }
                             */
                        }
                    }

                    if (TabQueryString != "")
                    {
                        var Rule = GetTabUrl(tab.PortalID, CultureCode, TabQueryString);
                        if (Rule != null)
                            TabQueryString = Rule.Url;

                        queryString = queryString + "&" + TabQueryString;

                        if (Rule != null && Rule.RemoveTab && ModuleQueryString == "" && OtherQueryString == "") //for home page only
                            queryString = "";

                    }


                    if (ModuleQueryString != "")
                        queryString = queryString + "&" + ModuleQueryString;

                    if (OtherQueryString != "")
                        queryString = queryString + "&" + OtherQueryString;

                    queryString = queryString.TrimStart('&');

                    // no urlrules for admin pages because the rewriter dont process UrlRules when tabid is present
                    if (!queryString.ToLower().Split(Convert.ToChar("&")).Any(p => p.StartsWith("tabid=")))
                        nameValuePairs = queryString.Split(Convert.ToChar("&"));
                }

                for (int i = 0; i <= nameValuePairs.Length - 1; i++)
                {
                    string pathToAppend = "";
                    bool atBegin = false;
                    string[] pair = nameValuePairs[i].Split(Convert.ToChar("="));

                    //Add name part of name/value pair
                    pathToAppend = pathToAppend + "/" + pair[0];

                    if ((pair.Length > 1))
                    {
                        if ((!String.IsNullOrEmpty(pair[1])))
                        {
                            if ((Regex.IsMatch(pair[1], _regexMatch) == false))
                            {
                                //Contains Non-AlphaNumeric Characters
                                if ((pair[0].ToLower() == "tabid")) // only for admin & host tabs
                                {
                                    if ((Regex.IsMatch(pair[1], "^\\d+$")))
                                    {
                                        if (tab != null)
                                        {
                                            int tabId = Convert.ToInt32(pair[1]);
                                            if ((tab.TabID == tabId))
                                            {
                                                if ((tab.TabPath != Null.NullString) && IncludePageName)
                                                {
                                                    pathToAppend = tab.TabPath.Replace("//", "/") + pathToAppend;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (UrlRewiterSettings.IsManage404(CurrentPortalId))
                                {
                                    if (String.IsNullOrEmpty(queryStringSpecialChars))
                                    {
                                        queryStringSpecialChars = pair[0] + "=" + pair[1];
                                    }
                                    else
                                    {
                                        queryStringSpecialChars = queryStringSpecialChars + "&" + pair[0] + "=" + pair[1];
                                    }
                                    pathToAppend = "";
                                    DoAddPage = false;
                                }
                                else
                                {
                                    pathToAppend = pathToAppend + "/" + HttpUtility.UrlPathEncode(pair[1]);
                                }
                            }
                            else
                            {
                                //Rewrite into URL, contains only alphanumeric and the % or space
                                if (String.IsNullOrEmpty(queryStringSpecialChars))
                                {
                                    queryStringSpecialChars = pair[0] + "=" + pair[1];
                                }
                                else
                                {
                                    queryStringSpecialChars = queryStringSpecialChars + "&" + pair[0] + "=" + pair[1];
                                }
                                pathToAppend = "";
                            }
                        }
                        else
                        {
                            pathToAppend = pathToAppend + "/" + HttpUtility.UrlPathEncode((' ').ToString());
                        }
                    }
                    if (atBegin)
                        friendlyParams = pathToAppend + friendlyParams;
                    else
                        friendlyParams = friendlyParams + pathToAppend;
                }
                friendlyParams = friendlyParams.TrimStart('/');
                friendlyPath = friendlyPath + friendlyParams;
            }

            if (DoAddPage /*|| !string.IsNullOrEmpty( FileExtension)*/ )
            {

                friendlyPath = AddPage(friendlyPath, pageName);

            }
            else
            {
                friendlyPath = friendlyPath + FileExtension;
            }
            if ((!String.IsNullOrEmpty(queryStringSpecialChars)))
            {
                friendlyPath = friendlyPath + "?" + queryStringSpecialChars;
            }
            /*            
                        if (HttpContext.Current.Request.IsAuthenticated) {
                            if (!String.IsNullOrEmpty(queryStringSpecialChars))
                            {
                                friendlyPath = friendlyPath + "&nocache=true";
                            }
                            else {
                                friendlyPath = friendlyPath + "?nocache=true";
                            }
                        }
            */
            return friendlyPath;
        }

        private Dictionary<string, string> GetQueryStringDictionary(string path)
        {
            string[] parts = path.Split('?');
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if ((parts.Length == 2))
            {
                foreach (string part in parts[1].Split('&'))
                {
                    string[] keyvalue = part.Split('=');
                    if ((keyvalue.Length == 2))
                    {
                        results[keyvalue[0]] = keyvalue[1];
                    }
                }
            }
            return results;
        }
    }
}
