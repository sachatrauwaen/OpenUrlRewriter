using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace Satrabel.OpenUrlRewriter.HttpModules
{
    public class W3CTransform
    {

        public static string filter_TransformString(string output)
        {
            string finalHtml = output;
            
            //Regex re = new Regex(@"( <input.*type=""hidden"".*)(autocomplete=""off"")(.*/>)", RegexOptions.IgnoreCase);
            //finalHtml = re.Replace(finalHtml, new MatchEvaluator(FormNameMatch));

            string DebugInfo = ""; //  " <!-- $0 --> ";
            Regex re;
            re = new Regex(@"<!DOCTYPE[^>]*XHTML 1.0 Transitional[^>]*>");

            bool XHTML10Transitional = re.IsMatch(finalHtml);

            re = new Regex(@"<!DOCTYPE html>");
            bool HTML5 = re.IsMatch(finalHtml);

            re = new Regex(@"(<input name=""__dnnVariable"" type=""hidden"" id=""__dnnVariable"")( autocomplete=""off"")([^>]*/>)", RegexOptions.IgnoreCase);                            
            finalHtml = re.Replace(finalHtml, "$1 $3");


            re = new Regex(@"(<a[^>]*)(target="""")([^>]*>)", RegexOptions.IgnoreCase);
            finalHtml = re.Replace(finalHtml, "$1 $3");

            /*
            re = new Regex(@"<img[^>]*src=""([^""]*[ &][^""]*)""[^>]*>");
            finalHtml = re.Replace(finalHtml, new MatchEvaluator(ImgSrcMatch));

            re = new Regex(@"<link[^>]*href=""([^""]*[ &][^""]*)""[^>]*>");
            finalHtml = re.Replace(finalHtml, new MatchEvaluator(ImgSrcMatch));

            re = new Regex(@"<a[^>]*href=""([^""]*[ &][^""]*)""[^>]*>");
            finalHtml = re.Replace(finalHtml, new MatchEvaluator(ImgSrcMatch));
             */

            re = new Regex(@"(<td align=""center"">)", RegexOptions.IgnoreCase);
            finalHtml = re.Replace(finalHtml, @"<td style=""text-align:center"">" + DebugInfo);

            re = new Regex(@"(<table[^>]*)(cellspacing=""0"")([^>]*>)", RegexOptions.IgnoreCase);
            finalHtml = re.Replace(finalHtml, "$1 $3" + DebugInfo);

            re = new Regex(@"(<table[^>]*)(cellpadding=""0"")([^>]*>)", RegexOptions.IgnoreCase);
            finalHtml = re.Replace(finalHtml, "$1 $3" + DebugInfo);
            

            if (HTML5)
            {
                re = new Regex(@"<a name=""\d*""></a>", RegexOptions.IgnoreCase);
                finalHtml = re.Replace(finalHtml, DebugInfo);
                re = new Regex(@"<meta content=""text/javascript"" http-equiv=""Content-Script-Type"" />");
                finalHtml = re.Replace(finalHtml, DebugInfo);
                re = new Regex(@"<meta content=""text/css"" http-equiv=""Content-Style-Type"" />");
                finalHtml = re.Replace(finalHtml, DebugInfo);
                

                re = new Regex(@"<meta id=""MetaCopyright"" name=""COPYRIGHT"" content=""[^""]*"" />");
                finalHtml = re.Replace(finalHtml, DebugInfo);

                re = new Regex(@"<meta name=""RESOURCE-TYPE"" content=""DOCUMENT"" />");
                finalHtml = re.Replace(finalHtml, DebugInfo);
                re = new Regex(@"<meta name=""DISTRIBUTION"" content=""GLOBAL"" />");
                finalHtml = re.Replace(finalHtml, DebugInfo);
                re = new Regex(@"<meta http-equiv=""PAGE-ENTER"" content=""[^""]*"" />");
                finalHtml = re.Replace(finalHtml, DebugInfo);
            }

            re = new Regex(@"<meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />");
            finalHtml = re.Replace(finalHtml, DebugInfo);

            return finalHtml;
       
        }
        private static string ImgSrcMatch(Match m)
        {

            string GroupOk = m.Groups[1].Value;
            //GroupOk = GroupOk.Replace("&", "&amp;");
            GroupOk = GroupOk.Replace(" ", "%20");


            return m.ToString().Replace(m.Groups[1].Value, GroupOk);
        }
    }
}