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
using System.Text.RegularExpressions;

#endregion

namespace Satrabel.HttpModules.Provider
{
    public enum UrlRuleAction
    {
        Rewrite = 0,
        Redirect = 1,
    }

    public enum UrlRuleType
    {
        Culture = 0,
        Tab = 1,
        Module = 2,
        Custom = 3,
        TabModule = 4

    }


    [Serializable]
    public class UrlRule
    {
        public UrlRule()
        {
            InSitemap = true;
        }


        public UrlRuleType RuleType { get; set; }

        public string RuleTypeString
        {
            get { return RuleType.ToString(); }
        }
        public string CultureCode { get; set; }
        public int TabId { get; set; }
        public string Parameters { get; set; }
        public bool RemoveTab { get; set; }
        public UrlRuleAction Action { get; set; }
        public string ActionString
        {
            get { return Action.ToString(); }
        }
        public string Url { get; set; }
        public string RedirectDestination { get; set; }
        public int RedirectStatus { get; set; }
        public bool InSitemap { get; set; }
        public bool Patern { get; set; }


        private static string GenerateRegExp(string patern)
        {
            return "^" + patern.Replace("+", "\\+").Replace("[", "(?'").Replace("]", "'.*)").Replace("{", "(?'").Replace("}", "'\\d*)") + "$";
        }

        public bool IsMatch(string ModuleQueryString)
        {
            if (RuleType == UrlRuleType.Custom || Patern)
            {
                string internRegExp = GenerateRegExp(Parameters);
                Regex regex = new Regex(internRegExp, RegexOptions.IgnoreCase);
                return regex.IsMatch(ModuleQueryString);
            }
            else
            {
                return Parameters.ToLower() == ModuleQueryString.ToLower();
            }

        }

        public bool IsMatchUrl(string ModuleUrl)
        {
            if (RuleType == UrlRuleType.Custom || Patern)
            {
                string internRegExp = GenerateRegExp(Url);
                Regex regex = new Regex(internRegExp, RegexOptions.IgnoreCase);
                return regex.IsMatch(ModuleUrl);
            }
            else
            {
                return Url == ModuleUrl;
            }
        }

        public bool IsMatchRedirectDestination(string ModuleUrl)
        {
            if (RuleType == UrlRuleType.Custom || Patern)
            {
                string internRegExp = GenerateRegExp(RedirectDestination);
                Regex regex = new Regex(internRegExp, RegexOptions.IgnoreCase);
                return regex.IsMatch(ModuleUrl);
            }
            else
            {
                return RedirectDestination == ModuleUrl;
            }
        }

        public string Replace(string ModuleQueryString, string PageName)
        {

            string internRegExp = GenerateRegExp(Parameters.ToLower());
            string externRegExp = Url.Replace("[pagename]", PageName).Replace("{", "${").Replace("[", "${").Replace("]", "}");
            Regex regex = new Regex(internRegExp, RegexOptions.IgnoreCase);
            if (regex.IsMatch(ModuleQueryString))
            {
                return regex.Replace(ModuleQueryString, externRegExp);

            }
            return ModuleQueryString;
        }

        public string ReplaceUrl(string ModuleUrl)
        {
            string externRegExp = GenerateRegExp(Url);
            string internRegExp = Parameters.Replace("{", "${").Replace("[", "${").Replace("]", "}");
            Regex regex = new Regex(externRegExp, RegexOptions.IgnoreCase);
            if (regex.IsMatch(ModuleUrl))
            {
                string NewUrl = regex.Replace(ModuleUrl, internRegExp);

                return NewUrl;
            }
            return ModuleUrl;
        }

        public string ReplaceRedirectDestination(string ModuleUrl)
        {
            string externRegExp = GenerateRegExp(RedirectDestination);
            string internRegExp = Url.Replace("{", "${").Replace("[", "${").Replace("]", "}");
            Regex regex = new Regex(externRegExp, RegexOptions.IgnoreCase);
            if (regex.IsMatch(ModuleUrl))
            {
                string NewUrl = regex.Replace(ModuleUrl, internRegExp);

                return NewUrl;
            }
            return ModuleUrl;
        }
    }
}