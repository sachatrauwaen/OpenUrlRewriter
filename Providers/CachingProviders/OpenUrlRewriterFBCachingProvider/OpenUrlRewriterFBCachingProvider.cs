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
//using DotNetNuke.Services.Cache.FileBasedCachingProvider;
using System.Web.Caching;
using DotNetNuke.Services.Cache;
using DotNetNuke.Common;
using Satrabel.HttpModules.Config;
using System.Collections.Generic;
using DotNetNuke.Instrumentation;

namespace Satrabel.Services.Cache.FileBasedCachingProvider
    
{
    public class OpenUrlRewriterFBCachingProvider : CachingProvider //FBCachingProvider
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(OpenUrlRewriterFBCachingProvider));
        public override void Insert(string cacheKey, object itemToCache, DNNCacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority,
                                   CacheItemRemovedCallback onRemoveCallback)
        {

            //onRemoveCallback += ItemRemovedCallback;           

            //Call base class method to add obect to cache
            base.Insert(cacheKey, itemToCache, dependency, absoluteExpiration, slidingExpiration, priority, onRemoveCallback);
        }

        internal void ItemRemovedCallback(string key, object value, CacheItemRemovedReason removedReason)
        {
            try
            {
                if (Globals.Status == Globals.UpgradeStatus.None)
                {
                    // track data removed from cache to synchonize UrlRule cache
                    string[] CacheKeys = UrlRuleConfiguration.GetCacheKeys();
                    if (CacheKeys != null)
                    {
                        foreach (string CacheKey in CacheKeys)
                        {
                            if (key.Contains(CacheKey))
                            {
                                if (DotNetNuke.Entities.Portals.PortalSettings.Current != null)
                                {
                                    int PortalId = DotNetNuke.Entities.Portals.PortalSettings.Current.PortalId;
                                    Remove(GetCacheKey("UrlRuleConfig" + PortalId));
                                    Logger.Trace("Clear cache " + key + " portal "+ PortalId + " raison "+ removedReason.ToString());
                                }
                                else
                                {
                                   // Logger.Trace("Clear cache not executed " + key + " raison " + removedReason.ToString());
                                }
                            }
                        }
                    }
                    if (key.StartsWith("UrlRuleConfig"))
                    {
                        Logger.Trace("cache " + key + "claired : " + removedReason.ToString());
                    }
                }
            }
            catch (Exception exc)
            {
                //Swallow exception            
                Logger.Error(exc);
            }
        }

        public override void Remove(string Key)
        {
            base.Remove(Key);
        }




    }

    


}
