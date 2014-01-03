
#region Usings

using System;
using System.Collections;

using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Scheduling;
using DotNetNuke.Services.Exceptions;

#endregion

namespace Satrabel.Services.Log.UrlLog
{
    public class PurgeUrlLog : SchedulerClient
    {
        public PurgeUrlLog(ScheduleHistoryItem objScheduleHistoryItem)
        {
            ScheduleHistoryItem = objScheduleHistoryItem;
        }

        public override void DoWork()
        {
            try
            {
				//notification that the event is progressing
                Progressing(); //OPTIONAL

                DoPurgeUrlLog();

                ScheduleHistoryItem.Succeeded = true; //REQUIRED

                ScheduleHistoryItem.AddLogNote("Url Log purged.");
            }
            catch (Exception exc) //REQUIRED
            {
                ScheduleHistoryItem.Succeeded = false; //REQUIRED

                ScheduleHistoryItem.AddLogNote("Url Log purge failed. " + exc); //OPTIONAL

				//notification that we have errored
                Errored(ref exc);
				
				//log the exception
                Exceptions.LogException(exc); //OPTIONAL
            }
        }

        private void DoPurgeUrlLog()
        {
            //var objUrlLog = new UrlLogController();
            var objPortals = new PortalController();
            ArrayList arrPortals = objPortals.GetPortals();
            PortalInfo objPortal;
            DateTime PurgeDate;
            int intIndex;
            for (intIndex = 0; intIndex <= arrPortals.Count - 1; intIndex++)
            {
                objPortal = (PortalInfo) arrPortals[intIndex];
                int UrlLogHistory = 10;
                if (UrlLogHistory > 0)
                {
                    PurgeDate = DateTime.Now.AddDays(-(UrlLogHistory));
                    UrlLogController.DeleteUrlLog(PurgeDate, objPortal.PortalID);
                }
            }
        }
    }
}