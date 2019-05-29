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
using System.Data;
using System.IO;
using System.Text;
using System.Threading;

using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;
using DotNetNuke.Entities.Host;
using DotNetNuke.Instrumentation;
using DotNetNuke.Security;
using System.Collections.Generic;

#endregion

namespace Satrabel.Services.Log.UrlLog
{

    

    public class UrlLogController
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(UrlLogController));

        private const string ModuleQualifier = "OpenUrlRewriter_";

        public void AddUrlLog(int PortalId, int UserId, string Referrer, string URL, string OriginalURL, string RedirectURL, string UserAgent, string UserHostAddress, string UserHostName, int TabId, int Statuscode, int UrlLogBuffer,
                               string UrlLogStorage)
        {
            var objSecurity = new PortalSecurity();
            try
            {
                if (Host.PerformanceSetting == Globals.PerformanceSettings.NoCaching)
                {
                    UrlLogBuffer = 1;
                }
                switch (UrlLogBuffer)
                {
                    case 0: //logging disabled
                        break;
                    case 1: //no buffering
                        switch (UrlLogStorage)
                        {
                            case "D": //database
                                DataProvider.Instance().ExecuteNonQuery(ModuleQualifier + "AddUrlLog",
                                                                   DateTime.Now,
                                                                   PortalId,
                                                                   UserId,
                                                                   objSecurity.InputFilter(Referrer, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                                                   objSecurity.InputFilter(URL, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                                                   objSecurity.InputFilter(OriginalURL, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                                                   objSecurity.InputFilter(RedirectURL, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                                                   objSecurity.InputFilter(UserAgent, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                                                   objSecurity.InputFilter(UserHostAddress, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                                                   objSecurity.InputFilter(UserHostName, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                                                   TabId, Statuscode);
                                break;
                            case "F": //file system
                                W3CExtendedLog(DateTime.Now,
                                               PortalId,
                                               UserId,
                                               objSecurity.InputFilter(Referrer, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                               objSecurity.InputFilter(URL, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                               objSecurity.InputFilter(OriginalURL, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                               objSecurity.InputFilter(RedirectURL, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                               objSecurity.InputFilter(UserAgent, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                               objSecurity.InputFilter(UserHostAddress, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                               objSecurity.InputFilter(UserHostName, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup),
                                               TabId, Statuscode);
                                break;
                        }
                        break;
                    default: //buffered logging
                        string key = "UrlLog" + PortalId;
                        var arrUrlLog = (ArrayList) DataCache.GetCache(key);
                        
						//get buffered site log records from the cache
						if (arrUrlLog == null)
                        {
                            arrUrlLog = new ArrayList();
                            DataCache.SetCache(key, arrUrlLog);
                        }
						
                        //create new UrlLog object
                        var objUrlLog = new UrlLogInfo();
                        objUrlLog.DateTime = DateTime.Now;
                        objUrlLog.PortalId = PortalId;
                        objUrlLog.UserId = UserId;
                        objUrlLog.Referrer = objSecurity.InputFilter(Referrer, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup);
                        objUrlLog.URL = objSecurity.InputFilter(URL, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup);
                        objUrlLog.OriginalURL = objSecurity.InputFilter(OriginalURL, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup);
                        objUrlLog.RedirectURL = objSecurity.InputFilter(RedirectURL, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup);
                        objUrlLog.UserAgent = objSecurity.InputFilter(UserAgent, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup);
                        objUrlLog.UserHostAddress = objSecurity.InputFilter(UserHostAddress, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup);
                        objUrlLog.UserHostName = objSecurity.InputFilter(UserHostName, PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoMarkup);
                        objUrlLog.TabId = TabId;
                        objUrlLog.StatusCode = Statuscode;

                        

                        //add UrlLog object to cache
                        arrUrlLog.Add(objUrlLog);

                        if (arrUrlLog.Count >= UrlLogBuffer)
                        {
							//create the buffered UrlLog object
                            var objBufferedUrlLog = new BufferedUrlLog();
                            objBufferedUrlLog.UrlLogStorage = UrlLogStorage;
                            objBufferedUrlLog.UrlLog = arrUrlLog;

                            //clear the current UrlLogs from the cache
                            DataCache.RemoveCache(key);

                            //process buffered UrlLogs on a background thread
                            var objThread = new Thread(objBufferedUrlLog.AddUrlLog);
                            objThread.Start();
                        }
                        break;
                }
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
            }
        }

        public static List<UrlLogInfo> GetUrlLog(int PortalId, DateTime StartDate, DateTime EndDate)
        {
            return CBO.FillCollection<UrlLogInfo>(DataProvider.Instance().ExecuteReader(ModuleQualifier+"GetUrlLog",
                                                                            PortalId, StartDate, EndDate));
        }

        public static List<UrlLogInfo> GetUrlLogByOriginalUrl(int PortalId, string OriginalUrl)
        {
            return CBO.FillCollection<UrlLogInfo>(DataProvider.Instance().ExecuteReader(ModuleQualifier + "GetUrlLogByOriginalUrl",
                                                                            PortalId, OriginalUrl));
        }

        public static void DeleteUrlLog(DateTime DateTime, int PortalId)
        {
            DataProvider.Instance().ExecuteNonQuery(ModuleQualifier + "DeleteUrlLog", DateTime, PortalId);
        }

        public static void DeleteUrlLogByOriginalUrl(int PortalId, string OriginalUrl)
        {
            DataProvider.Instance().ExecuteNonQuery(ModuleQualifier + "DeleteUrlLogByOriginalUrl", PortalId, OriginalUrl);
        }

        public void W3CExtendedLog(DateTime DateTime, int PortalId, int UserId, string Referrer, string URL, string OriginalURL, string RedirectURL, string UserAgent, string UserHostAddress, string UserHostName, int TabId, int StatusCode)
        {
            StreamWriter objStream;

            //create log file path
            string LogFilePath = Globals.ApplicationMapPath + "\\Portals\\" + PortalId + "\\Logs\\";
            string LogFileName = "ex" + DateTime.Now.ToString("yyMMdd") + ".log";

            //check if log file exists
            if (!File.Exists(LogFilePath + LogFileName))
            {
                try
                {
					//create log file
                    Directory.CreateDirectory(LogFilePath);

                    //open log file for append ( position the stream at the end of the file )
                    objStream = File.AppendText(LogFilePath + LogFileName);

                    //add standard log file headers
                    objStream.WriteLine("#Software: Microsoft Internet Information Services 6.0");
                    objStream.WriteLine("#Version: 1.0");
                    objStream.WriteLine("#Date: " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                    objStream.WriteLine("#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) sc-status sc-substatus sc-win32-status");

                    //close stream
                    objStream.Flush();
                    objStream.Close();
                }
				catch (Exception ex) //can not create file
				{
					Logger.Error(ex);
				}
            }
            try
            {
				//open log file for append ( position the stream at the end of the file )
                objStream = File.AppendText(LogFilePath + LogFileName);

                //declare a string builder
                var objStringBuilder = new StringBuilder(1024);

                //build W3C extended log item
                objStringBuilder.Append(DateTime.ToString("yyyy-MM-dd hh:mm:ss") + " ");
                objStringBuilder.Append(UserHostAddress + " ");
                objStringBuilder.Append("GET" + " ");
                objStringBuilder.Append(URL + " ");
                objStringBuilder.Append("-" + " ");
                objStringBuilder.Append("80" + " ");
                objStringBuilder.Append("-" + " ");
                objStringBuilder.Append(UserHostAddress + " ");
                objStringBuilder.Append(UserAgent.Replace(" ", "+") + " ");
                objStringBuilder.Append("200" + " ");
                objStringBuilder.Append("0" + " ");
                objStringBuilder.Append("0");

                //write to log file
                objStream.WriteLine(objStringBuilder.ToString());

                //close stream
                objStream.Flush();
                objStream.Close();
            }
			catch (Exception ex) //can not open file
			{
				Logger.Error(ex);
			}
        }
    }
}