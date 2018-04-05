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
using System.Collections;

using DotNetNuke.Data;
using DotNetNuke.Instrumentation;

#endregion

namespace Satrabel.Services.Log.UrlLog
{
    public class BufferedUrlLog
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(BufferedUrlLog));
        public ArrayList UrlLog;
        public string UrlLogStorage;

        public void AddUrlLog()
        {
            try
            {
                UrlLogInfo objUrlLog;
                var objUrlLogs = new UrlLogController();

				//iterate through buffered UrlLog items and insert into database
                int intIndex;
                for (intIndex = 0; intIndex <= UrlLog.Count - 1; intIndex++)
                {
                    objUrlLog = (UrlLogInfo) UrlLog[intIndex];
                    switch (UrlLogStorage)
                    {
                        case "D": //database
                            DataProvider.Instance().ExecuteNonQuery("AddUrlLog",
                                                               objUrlLog.DateTime,
                                                               objUrlLog.PortalId,
                                                               objUrlLog.UserId,
                                                               objUrlLog.Referrer,
                                                               objUrlLog.URL,
                                                               objUrlLog.OriginalURL,
                                                               objUrlLog.RedirectURL,
                                                               objUrlLog.UserAgent,
                                                               objUrlLog.UserHostAddress,
                                                               objUrlLog.UserHostName,
                                                               objUrlLog.TabId,
                                                               objUrlLog.StatusCode);
                            break;
                        case "F": //file system
                            objUrlLogs.W3CExtendedLog(objUrlLog.DateTime,
                                                       objUrlLog.PortalId,
                                                       objUrlLog.UserId,
                                                       objUrlLog.Referrer,
                                                       objUrlLog.URL,
                                                       objUrlLog.OriginalURL,
                                                       objUrlLog.RedirectURL,
                                                       objUrlLog.UserAgent,
                                                       objUrlLog.UserHostAddress,
                                                       objUrlLog.UserHostName,
                                                       objUrlLog.TabId,
                                                       objUrlLog.StatusCode);
                            break;
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Error(exc);

            }
        }
    }
}