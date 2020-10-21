using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Features;
using Timeline.Resources;
using System.Reflection;

namespace BTTimeSkip
{
    internal class SGEventPanel_SetEvent
    {
        internal static void ApplyPatch(HarmonyInstance i)
        {
            HarmonyMethod x = new HarmonyMethod(typeof(SGEventPanel_SetEvent).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic));
            x.after = new string[] { "io.github.mpstark.Timeline" };
            i.Patch(typeof(SGEventPanel).GetMethod("SetEvent"), null, x, null);
        }

        internal static void Postfix(SGEventPanel __instance, SimGameEventDef evt)
        {
            ForcedTimelineEvent e = ForcedEvents.ForcedTimelineEvents.Find((ev) => ev.EventID.Equals(evt.Description.Id));
            if (e != null)
                Traverse.Create(__instance).Field("eventTime").GetValue<LocalizableText>().SetText($"{e.DateToFire:yyyy M d}", new object[] { });
        }
    }
}
