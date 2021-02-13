using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BTTimeSkip
{
    [HarmonyPatch(typeof(SimGameState), "SetSimRoomState")]
    class SimGameState_SetSimRoomState
    {
        public static bool Prefix(SimGameState __instance, DropshipLocation state)
        {
            if (state == DropshipLocation.CPT_QUARTER && Input.GetKey(KeyCode.LeftShift))
            {
                __instance.InterruptQueue.AddInterrupt(new TimeSkip_InterruptManager_AskTimeSkip(__instance), true);
                return false;
            }
            return true;
        }

        public class TimeSkip_InterruptManager_AskTimeSkip : SimGameInterruptManager.Entry
        {
            SimGameState state;

            public TimeSkip_InterruptManager_AskTimeSkip(SimGameState s)
            {
                type = SimGameInterruptManager.InterruptType.GenericPopup;
                state = s;
            }

            public override bool IsUnique()
            {
                return false;
            }

            public override bool IsVisible()
            {
                return true;
            }

            public override bool NeedsFader()
            {
                return false;
            }

            public override void Render()
            {
                if (!state.CanAdvance())
                {
                    GenericPopupBuilder.Create("Skip Time?", "Currently impossible!\nYou have to be in a stable orbit and nothing to work on.").AddButton("Cancel", NewClose, true, null)
                        .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                    return;
                }
                GenericPopupBuilder pop = GenericPopupBuilder.Create("Skip Time?", "Skip To a predefined year, skip By an amount of years, or skip to a Custom date?\nSkipping time can take a moment.");
                pop.AddButton("Cancel", NewClose, true, null);
                pop.AddButton("Skip To", RenderTo, true, null);
                pop.AddButton("Skip By", RenderBy, true, null);
                pop.AddButton("Skip Custom", RenderCustom, true, null);
                pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
                pop.Render();
            }

            public void RenderTo()
            {
                GenericPopupBuilder pop = GenericPopupBuilder.Create("Skip Time?", "Choose when you want to resume playing.");
                pop.AddButton("now", NewClose, true, null);
                foreach (string t in BTTimeSkip_Main.Settings.SkipTo)
                {
                    DateTime t2 = DateTime.Parse(t);
                    if (state.GetDayDiff(t2) > 0)
                        pop.AddButton(t, delegate { state.AdvanceTo(t2); NewClose(); }, true, null);
                }
                pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
                pop.Render();
            }

            public void RenderBy()
            {
                GenericPopupBuilder pop = GenericPopupBuilder.Create("Skip Time?", "Choose how many years to skip.");
                pop.AddButton("0", NewClose, true, null);
                foreach (int t in BTTimeSkip_Main.Settings.SkipBy)
                {
                    DateTime t2 = state.CurrentDate.AddYears(t);
                    pop.AddButton(t.ToString(), delegate { state.AdvanceTo(t2); NewClose(); }, true, null);
                }
                pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
                pop.Render();
            }

            public void RenderCustom()
            {
                GenericPopupBuilder pop = GenericPopupBuilder.Create("Skip Time?", "Input the date you want to skip to. Format is YYYY-M-D .");
                pop.AddButton("Cancel", NewClose, true, null);
                pop.AddInput("Date", (str) => { 
                    if (DateTime.TryParse(str, out DateTime t) && state.GetDayDiff(t) > 0)
                    {
                        state.AdvanceTo(t);
                        NewClose();
                    }
                    else
                    {
                        GenericPopupBuilder.Create("Skip Time?", "Invalid Date!").AddButton("Cancel", NewClose, true, null)
                        .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                    }
                }, "3030-1-1", false, false);
                pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
                pop.Render();
            }

            public void NewClose()
            {
                Traverse.Create(this).Method("Close").GetValue();
            }
        }
    }
}
