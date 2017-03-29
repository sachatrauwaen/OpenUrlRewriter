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
using DotNetNuke.Entities.Portals;
using System.Text.RegularExpressions;
using DotNetNuke.Framework.Providers;

namespace Satrabel.HttpModules
{
    public class UrlRewiterSettings
    {
        public const string ModuleQualifier = "OpenUrlRewriter_";
        private const string LogAuthentificatedUsers = ModuleQualifier + "LogAuthentificatedUsers";

        public static bool IsLogAuthentificatedUsers(int PortalId)
        {
            return bool.Parse(PortalController.GetPortalSetting(LogAuthentificatedUsers, PortalId, "False"));
        }

        public static void SetLogAuthentificatedUsers(int PortalId, bool value)
        {
            PortalController.UpdatePortalSetting(PortalId, LogAuthentificatedUsers, value.ToString());
        }

        private const string LogEachUrlOneTime = ModuleQualifier + "LogEachUrlOneTime";

        public static bool IsLogEachUrlOneTime(int PortalId)
        {
            return bool.Parse(PortalController.GetPortalSetting(LogEachUrlOneTime, PortalId, "True"));
        }

        public static void SetLogEachUrlOneTime(int PortalId, bool value)
        {
            PortalController.UpdatePortalSetting(PortalId, LogEachUrlOneTime, value.ToString());
        }

        private const string LogStatusCode200 = ModuleQualifier + "LogStatusCode200";

        public static bool IsLogStatusCode200(int PortalId)
        {
            return bool.Parse(PortalController.GetPortalSetting(LogStatusCode200, PortalId, "False"));
        }

        public static void SetLogStatusCode200(int PortalId, bool value)
        {
            PortalController.UpdatePortalSetting(PortalId, LogStatusCode200, value.ToString());
        }


        private const string LogEnabled = ModuleQualifier + "LogEnabled";
        public static bool IsLogEnabled(int PortalId)
        {
            return bool.Parse(PortalController.GetPortalSetting(LogEnabled, PortalId, "False"));
        }

        public static void SetLogEnabled(int PortalId, bool value)
        {
            PortalController.UpdatePortalSetting(PortalId, LogEnabled, value.ToString());
        }


        private const string DisableSiteIndex = ModuleQualifier + "DisableSiteIndex";
        public static bool IsDisableSiteIndex(int PortalId)
        {
            return bool.Parse(PortalController.GetPortalSetting(DisableSiteIndex, PortalId, "False"));
        }

        public static void SetDisableSiteIndex(int PortalId, bool value)
        {
            PortalController.UpdatePortalSetting(PortalId, DisableSiteIndex, value.ToString());
        }

        private const string DisableTermsIndex = ModuleQualifier + "DisableTermsIndex";
        public static bool IsDisableTermsIndex(int PortalId)
        {
            return bool.Parse(PortalController.GetPortalSetting(DisableTermsIndex, PortalId, "False"));
        }

        public static void SetDisableTermsIndex(int PortalId, bool value)
        {
            PortalController.UpdatePortalSetting(PortalId, DisableTermsIndex, value.ToString());
        }

        private const string DisablePrivacyIndex = ModuleQualifier + "DisablePrivacyIndex";
        public static bool IsDisablePrivacyIndex(int PortalId)
        {
            return bool.Parse(PortalController.GetPortalSetting(DisablePrivacyIndex, PortalId, "False"));
        }

        public static void SetDisablePrivacyIndex(int PortalId, bool value)
        {
            PortalController.UpdatePortalSetting(PortalId, DisablePrivacyIndex, value.ToString());
        }

        private const string W3C = ModuleQualifier + "W3C";
        public static bool IsW3C(int PortalId)
        {
            return bool.Parse(PortalController.GetPortalSetting(W3C, PortalId, "False"));
        }

        public static void SetW3C(int PortalId, bool value)
        {
            PortalController.UpdatePortalSetting(PortalId, W3C, value.ToString());
        }


        public static bool IsUrlToLowerCase()
        {
            return false;
        }

        public const string ErrorPage404Setting = "AUM_ErrorPage404";

        private const string Manage404 = ModuleQualifier + "Manage404";
        public static bool IsManage404(int PortalId)
        {
            try
            {
                return bool.Parse(PortalController.GetPortalSetting(Manage404, PortalId, "False"));
            }
            catch (Exception)
            {

                return false;
            }
            
        }

        public static void SetManage404(int PortalId, bool value)
        {
            PortalController.UpdatePortalSetting(PortalId, Manage404, value.ToString());
        }

        public static bool ExcludeFromRedirect(int PortalId, string path)
        {
            return  Regex.IsMatch(path, @"/LinkClick\.aspx|/Providers/|/DesktopModules/", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        public static bool ExcludeFromLowerCase(int PortalId, string path)
        {
            return Regex.IsMatch(path, "/ctl/", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        
        private static UrlRewiterSettings current = null;
        public static UrlRewiterSettings Current()
        {
            if (current == null)
            {                
                ProviderConfiguration providerConfiguration = ProviderConfiguration.GetProviderConfiguration("friendlyUrl");
                var objProvider = (DotNetNuke.Framework.Providers.Provider)providerConfiguration.Providers[providerConfiguration.DefaultProvider];

                current = new UrlRewiterSettings(objProvider);
            }
            return current;
        }

        public UrlRewiterSettings(DotNetNuke.Framework.Providers.Provider objProvider)
        {
            if (objProvider.Attributes["fileExtension"] != null)
            {
                _fileExtension = objProvider.Attributes["fileExtension"];
            }
            else
            {
                _fileExtension = ".aspx";
            }
    
        
        }

        private readonly string _fileExtension;
        public string FileExtension
        {
            get
            {
                return _fileExtension;
            }
        }
    }
}
