#region Copyright
// 
// DotNetNuke� - http://www.dotnetnuke.com
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;

using DotNetNuke.Common;
using DotNetNuke.ComponentModel;
using DotNetNuke.Entities.Portals;

#endregion

namespace Satrabel.HttpModules.Provider
{
    public class UrlBuilder
    {
        
        private readonly int _PortalId;

        public UrlBuilder(int PortalId)
        {
            _PortalId = PortalId;
            LoadProviders();
        }

        #region "Urlmap Building"


        public List<UrlRule> BuildUrlMap()
        {
            var allUrls = new List<UrlRule>();
            // get all urls
            foreach (UrlRuleProvider _provider in Providers)
            {
                bool isProviderEnabled = true;//bool.Parse(PortalController.GetPortalSetting(_provider.Name + "Enabled", PortalSettings.PortalId, "True"));
                if (isProviderEnabled)
                {
                    // Get all urls from provider
                    List<UrlRule> urls = _provider.GetRules(_PortalId);
                    foreach (UrlRule url in urls)
                    {                      
                        allUrls.Add(url);                       
                    }
                }
            }
            return allUrls;
        }

        #endregion

        #region "Provider configuration and setup"

        private static List<UrlRuleProvider> _providers;

        private static readonly object _lock = new object();

        public List<UrlRuleProvider> Providers
        {
            get
            {
                return _providers;
            }
        }

        private static void LoadProviders()
        {
            // Avoid claiming lock if providers are already loaded
            if (_providers == null)
            {
                lock (_lock)
                {
                    _providers = new List<UrlRuleProvider>();


                    foreach (KeyValuePair<string, UrlRuleProvider> comp in ComponentFactory.GetComponents<UrlRuleProvider>())
                    {
                        //comp.Value.Name = comp.Key;
                        //comp.Value.Description = comp.Value.Description;
                        _providers.Add(comp.Value);
                                                
                        
                    }
                    //'ProvidersHelper.InstantiateProviders(section.Providers, _providers, GetType(SiteMapProvider))
                }
            }
        }

        #endregion
    }
}
