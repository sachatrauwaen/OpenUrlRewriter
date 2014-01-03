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
using System.IO;
using System.Xml.Serialization;
using System.Xml.XPath;

using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Cache;
using DotNetNuke.Services.Log.EventLog;
using DotNetNuke.Entities.Tabs;
using System.Collections;
using System.Text;
using DotNetNuke.Entities.Users;
using DotNetNuke.Entities.Portals;
using Satrabel.HttpModules.Provider;
using DotNetNuke.ComponentModel;
using System.Collections.Generic;

#endregion

namespace Satrabel.HttpModules.Config
{
    [Serializable, XmlRoot("RewriterConfig")]
    public class UrlRuleConfiguration
    {
        private List<UrlRule> _rules;

        public List<UrlRule> Rules
        {
            get
            {
                return _rules;
            }
            set
            {
                _rules = value;
            }
        }

        public static UrlRuleConfiguration GetConfig()
        {

            var config = new UrlRuleConfiguration();
            config.Rules = new List<UrlRule>();
            FileStream fileReader = null;
            string filePath = "";
            try
            {
                config = (UrlRuleConfiguration)DataCache.GetCache("UrlRuleConfig");
                if ((config == null))
                {

                    config = new UrlRuleConfiguration { Rules = new List<UrlRule>() };
                    
                    if (ComponentFactory.GetComponents<UrlRuleProvider>().Count == 0)
                        ComponentFactory.InstallComponents(new DotNetNuke.ComponentModel.ProviderInstaller("urlRule", typeof(UrlRuleProvider)));



                    PortalController pc = new PortalController();
                    foreach (PortalInfo portal in pc.GetPortals()) {
                        
                        var builder = new UrlBuilder(portal.PortalID);
                        var Rules = builder.BuildUrlMap();

                        
                        config.Rules.AddRange(Rules);
                        
                    }
                           
                    /*

                  
                    */
                    DataCache.SetCache("UrlRuleConfig", config);

                    /*
                    if (File.Exists(filePath))
                    {
                        //Set back into Cache
                        DataCache.SetCache("RewriterConfig", config, new DNNCacheDependency(filePath));
                    }
                     */
                }
            }
            catch (Exception ex)
            {
                //log it
                var objEventLog = new EventLogController();
                var objEventLogInfo = new LogInfo();
                objEventLogInfo.AddProperty("UrlRewriter.RewriterConfiguration", "GetConfig Failed");
                objEventLogInfo.AddProperty("FilePath", filePath);
                objEventLogInfo.AddProperty("ExceptionMessage", ex.Message);
                objEventLogInfo.LogTypeKey = EventLogController.EventLogType.HOST_ALERT.ToString();
                objEventLog.AddLog(objEventLogInfo);
                DnnLog.Error(objEventLogInfo);

            }
            finally
            {
                if (fileReader != null)
                {
                    //Close the Reader
                    fileReader.Close();
                }
            }
            return config;
        }

        

      
    }
}