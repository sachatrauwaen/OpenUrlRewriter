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

using System.Collections;

using System.Collections.Generic;
using DotNetNuke.Common.Utilities;

using Ventrian.PropertyAgent;
using DotNetNuke.Entities.Modules;

namespace Satrabel.HttpModules.Provider
{
    public class PropertyAgentUrlRuleProvider : UrlRuleProvider
    {
        public PropertyAgentUrlRuleProvider() {}

        public override List<UrlRule> GetRules(int PortalId)
        {
            List<UrlRule> Rules = new List<UrlRule>();
            PropertyController pc = new PropertyController();
            PropertyTypeController ptc = new PropertyTypeController();            
            ModuleController mc = new ModuleController();
            ArrayList modules = mc.GetModulesByDefinition(PortalId, "Property Agent");
            foreach (ModuleInfo module in modules.OfType<ModuleInfo>().GroupBy(m => m.ModuleID).Select(g => g.First())){                

                PropertySettings settings = new  PropertySettings(module.ModuleSettings);

                if (settings.SEOAgentType.ToLower() == "agenttype" && settings.SEOViewPropertyTitle == "")
                {

                    var properties = pc.List(module.ModuleID, Null.NullInteger, SearchStatusType.Any, Null.NullInteger, Null.NullInteger, false, SortByType.Published, Null.NullInteger, SortDirectionType.Ascending, "", "", 0, 9999, true);
                    foreach (PropertyInfo prop in properties)
                    {
                        CustomFieldController cfc = new CustomFieldController();
                        PropertyValueController pvc = new PropertyValueController();
                        CustomFieldInfo cfi = cfc.List(module.ModuleID, false).OrderBy(c => c.SortOrder).First();
                        if (cfi != null)
                        {
                            PropertyValueInfo pvi = pvc.GetByCustomField(prop.PropertyID, cfi.CustomFieldID, module.ModuleID);
                            if (pvi != null)
                            {
                                var rule = new UrlRule
                                {
                                    RuleType = UrlRuleType.Module,
                                    PortalId = PortalId,
                                    Parameters = settings.SEOAgentType+"=View&" + settings.SEOPropertyID + "=" + prop.PropertyID.ToString(),
                                    Action = UrlRuleAction.Rewrite,
                                    Url = CleanupUrl(pvi.CustomValue)
                                };
                                Rules.Add(rule);
                                /*
                                rule = new UrlRule
                                {
                                    RuleType = "module",
                                    PortalId = PortalId,
                                    Parameters = rule.parameters,
                                    Action = "redirect",
                                    Url = "agenttype/view/" + settings.SEOPropertyID.ToLower() + "/" + prop.PropertyID.ToString() + "/" + pvi.CustomValue
                                };
                                 Rules.Add(rule);
                                 */
                            }
                        }
                    }
                    var propertyTypes = ptc.List(module.ModuleID, false);
                    foreach (PropertyTypeInfo propType in propertyTypes)
                    {
                        var rule = new UrlRule
                        {
                            RuleType = UrlRuleType.Module,
                            PortalId = PortalId,
                            Parameters = settings.SEOAgentType + "=View&" + settings.SEOPropertyTypeID + "=" + propType.PropertyTypeID.ToString(),
                            Action = UrlRuleAction.Rewrite,
                            Url = CleanupUrl(propType.Name)
                            //Url = settings.SEOPropertyID.ToLower() +"-"+ prop.PropertyID.ToString()
                            //Url = CleanupUrl(prop.PropertyTypeName) + "-" + CleanupUrl(pvi.CustomValue)
                        };
                        //System.Diagnostics.Debug.WriteLine(rule.Url);
                        Rules.Add(rule);
                        /*
                        rule = new UrlRule
                        {
                            RuleType = "module",
                            PortalId = PortalId,
                            Parameters = "agenttype=view&" + settings.SEOPropertyID.ToLower() + "=" + prop.PropertyID.ToString(),
                            Action = "redirect",
                            Url = "agenttype/view/" + settings.SEOPropertyID.ToLower() + "/" + prop.PropertyID.ToString() + "/" + pvi.CustomValue,
                            RedirectDestination = settings.SEOPropertyID.ToLower() + "-" + prop.PropertyID.ToString() + "/" + pvi.CustomValue
                        };
                         Rules.Add(rule);
                         */
                    }
                }
            }           
            return Rules;
        }
    }
}