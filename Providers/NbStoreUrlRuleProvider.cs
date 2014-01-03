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


using DotNetNuke.Entities.Modules;
using NEvoWeb.Modules.NB_Store;
using DotNetNuke.Services.Localization;

namespace Satrabel.HttpModules.Provider
{
    public class NbStoreUrlRuleProvider : UrlRuleProvider
    {
        public NbStoreUrlRuleProvider() { }

        public override List<UrlRule> GetRules(int PortalId)
        {
            List<UrlRule> Rules = new List<UrlRule>();
            Dictionary<string, Locale> dicLocales = LocaleController.Instance.GetLocales(PortalId);
            foreach (KeyValuePair<string, Locale> key in dicLocales)
            {
                string CultureCode = key.Value.Code;
                CategoryController cc = new CategoryController();
                ProductController pc = new ProductController();
                // products alone
                var prodLst = pc.GetProductList(PortalId, Null.NullInteger, CultureCode, false);
                foreach (ProductListInfo prod in prodLst)
                {
                    var rule = new UrlRule
                    {
                        RuleType = UrlRuleType.Module,
                        CultureCode = CultureCode,
                        PortalId = PortalId,
                        Parameters = "ProdID=" + prod.ProductID ,
                        Action = UrlRuleAction.Rewrite,
                        Url = CleanupUrl(prod.SEOName == "" ? prod.ProductName : prod.SEOName)
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
                // products and categories
                var catLst = cc.GetCategories(PortalId, CultureCode);
                foreach (NB_Store_CategoriesInfo cat in catLst)
                {
                    var CatRule = new UrlRule
                    {
                        RuleType = UrlRuleType.Module,
                        CultureCode = CultureCode,
                        PortalId = PortalId,
                        Parameters = "CatID=" + cat.CategoryID,
                        Action = UrlRuleAction.Rewrite,
                        Url = CleanupUrl(cat.SEOName == "" ? cat.CategoryName : cat.SEOName)
                    };
                    Rules.Add(CatRule);

                    var productLst = pc.GetProductList(PortalId, cat.CategoryID, CultureCode, false);
                    foreach (ProductListInfo prod in productLst)
                    {
                        var rule = new UrlRule
                        {
                            RuleType = UrlRuleType.Module,
                            CultureCode = CultureCode,
                            PortalId = PortalId,
                            Parameters = "ProdID=" + prod.ProductID + "&" + "CatID=" + cat.CategoryID,
                            Action = UrlRuleAction.Rewrite,
                            Url = CleanupUrl(cat.SEOName == "" ? cat.CategoryName : cat.SEOName) +"/"+CleanupUrl(prod.SEOName == "" ? prod.ProductName : prod.SEOName)
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