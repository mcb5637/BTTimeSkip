using BattleTech;
using BattleTech.UI;
using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Timeline.Features;
using Timeline.Resources;

[assembly:AssemblyVersion("0.4.0")]

namespace BTTimeSkip
{
    static class BTTimeSkip_Main
    {
        public static Settings Settings;

        public static void Init(string directory, string settingsJSON)
        {
            try
            {
                Settings = JsonConvert.DeserializeObject<Settings>(settingsJSON);
            }
            catch (Exception e)
            {
                FileLog.Log(e.Message);
                FileLog.Log(e.StackTrace);
            }
            HarmonyInstance harmony = HarmonyInstance.Create("com.github.mcb5637.BTTimeSkip");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            SGEventPanel_SetEvent.ApplyPatch(harmony);
        }

        public static bool CanAdvance(this SimGameState s)
        {
            if (s.TravelState != SimGameTravelStatus.IN_SYSTEM)
                return false;
            if (Traverse.Create(s.RoomManager).Field("timelineWidget").Field("ActiveItems").GetValue<Dictionary<WorkOrderEntry, TaskManagementElement>>().Count > 1)
                return false;
            return true;
        }

        public static void AdvanceBy(this SimGameState s, int days, bool skipNews)
        {
            if (days <= 0)
            {
                FileLog.Log("days <= 0");
                return;
            }
            //int quartercost = s.GetExpenditures(false);
            int daysremain = s.DayRemainingInQuarter;
            daysremain -= days;
            int qpassed = -(daysremain / s.Constants.Finances.QuarterLength) + 1;
            //s.AddFunds(qpassed * quartercost, null, true, false);
            //s.Constants.CareerMode.GameLength += days; // not saved/loaded
            object[] para = new object[] { days };
            Traverse.Create(s).Property("DayRemainingInQuarter").SetValue(s.DayRemainingInQuarter + qpassed * s.Constants.Finances.QuarterLength);
            List<ForcedTimelineEvent> ev = ForcedEvents.ForcedTimelineEvents;
            if (skipNews)
                ForcedEvents.ForcedTimelineEvents = new List<ForcedTimelineEvent>();
            Traverse.Create(s).Method("OnDayPassed", para).GetValue(para);
            ForcedEvents.ForcedTimelineEvents = ev;
            s.RoomManager.RefreshCareerCountdown();
            s.RoomManager.RemoveWorkQueueEntry(s.FinancialReportNotification, false);
            Traverse.Create(s).Property("FinancialReportNotification").SetValue(new WorkOrderEntry_Notification(WorkOrderType.FinancialReport, "Financial Report", "Financial Report", ""));
            s.FinancialReportNotification.SetCost(s.DayRemainingInQuarter);
            s.FinancialReportItem = s.RoomManager.AddWorkQueueEntry(s.FinancialReportNotification);
            s.RoomManager.SortTimeline();
        }

        public static void AdvanceTo(this SimGameState s, DateTime d, bool skipNews)
        {
            int days = s.GetDayDiff(d);
            s.AdvanceBy(days, skipNews);
        }

        public static int GetDayDiff(this SimGameState s, DateTime d)
        {
            return (int)d.Subtract(s.CurrentDate).TotalDays;
        }
    }
}
