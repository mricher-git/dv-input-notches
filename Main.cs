using DV.CabControls;
using DV.CabControls.Spec;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityModManagerNet;

namespace KeyboardNotches
{
    [EnableReloading]
    public static class Main
    {
        public static bool Enabled;
        internal static UnityModManager.ModEntry Mod;
        internal static Harmony HarmonyInst;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
            HarmonyInst = new Harmony(modEntry.Info.Id);
            modEntry.OnToggle = OnToggle;

            return true;
        }

        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                HarmonyInst.PatchAll(Assembly.GetExecutingAssembly());
            }
            else
            {
                HarmonyInst.UnpatchAll(modEntry.Info.Id);
            }

            Enabled = value;

            return true;
        }
    }

    [HarmonyPatch(typeof(ControlsInstantiator), nameof(ControlsInstantiator.Spawn))]
    public static class ControlsInstantiatorPatch
    {
        public static void Postfix(ControlSpec spec)
        {
            if (spec is Lever lever)
            {
                switch (lever.name)
                {
                    case "C throttle":
                    case "C throttle regulator":
                        ThrottleLever = lever;
                        ThrottleScroll = lever.GetComponent<IMouseWheelHoverScrollable>();
                        break;
                    case "C independent_brake_lever":
                    case "C independent brake":
                        IndependantBrakeLever = lever;
                        IndependantBrakeeScroll = lever.GetComponent<IMouseWheelHoverScrollable>();
                        break;
                    case "C train_brake_lever":
                    case "C brake":
                        BrakeLever = lever;
                        BrakeScroll = lever.GetComponent<IMouseWheelHoverScrollable>();
                        break;
                }
            }
        }

        public static Lever ThrottleLever;
        public static Lever IndependantBrakeLever;
        public static Lever BrakeLever;
        public static IMouseWheelHoverScrollable ThrottleScroll;
        public static IMouseWheelHoverScrollable IndependantBrakeeScroll;
        public static IMouseWheelHoverScrollable BrakeScroll;
    }

    public static class KeyboardNotchPatch
    {
        public static bool TryApplyInput(KeyCode[] keyIncrease, KeyCode[] keyDecrease, Lever lever, IMouseWheelHoverScrollable scroller, ref float timer)
        {
            if (keyIncrease.IsPressed())
            {
                if (keyIncrease.IsDown())
                {
                    if (lever.scrollWheelHoverScroll > 0)
                    {
                        scroller.OnHoverScrolledUp();
                    }
                    else
                    {
                        scroller.OnHoverScrolledDown();
                    }
                    timer = 0.016f;
                    return false;
                }
                else
                {
                    timer += Time.deltaTime;

                    if (timer > 0.3f) return true;
                }
                return false;
            }
            if (keyDecrease.IsPressed())
            {
                if (keyDecrease.IsDown())
                {
                    if (lever.scrollWheelHoverScroll > 0)
                    {
                        scroller.OnHoverScrolledDown();
                    }
                    else
                    {
                        scroller.OnHoverScrolledUp();
                    }
                    timer = 0f;
                    return false;
                }
                else
                {
                    timer += Time.deltaTime;

                    if (timer > 0.3f) return true;
                }
                return false;
            }
            if (timer > 0)
            {
                timer += Time.deltaTime;
            }
            if (timer > 0.3f)
            {
                scroller.OnHoverScrollReleased();
                timer = 0;
            }
            
            return false;
        }
    }

    [HarmonyPatch(typeof(LocoKeyboardInputDiesel), "TryApplyThrottleInput")]
    public static class DieselPatch01
    {
        public static bool Prefix()
        {
            if (ControlsInstantiatorPatch.ThrottleLever == null)
            {
                return true;
            }
            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseThrottleKeys, 
                                                    KeyBindings.decreaseThrottleKeys, 
                                                    ControlsInstantiatorPatch.ThrottleLever, 
                                                    ControlsInstantiatorPatch.ThrottleScroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputDiesel), "TryApplyBrakeInput")]
    public static class DieselPatch02
    {
        public static bool Prefix()
        {
            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseBrakeKeys,
                                                    KeyBindings.decreaseBrakeKeys,
                                                    ControlsInstantiatorPatch.BrakeLever,
                                                    ControlsInstantiatorPatch.BrakeScroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputDiesel), "TryApplyIndependentBrakeInput")]
    public static class DieselPatch03
    {
        public static bool Prefix()
        {
            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseIndependentBrakeKeys,
                                                    KeyBindings.decreaseIndependentBrakeKeys,
                                                    ControlsInstantiatorPatch.IndependantBrakeLever,
                                                    ControlsInstantiatorPatch.IndependantBrakeeScroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputShunter), "TryApplyThrottleInput")]
    public static class ShunterPatch01
    {
        public static bool Prefix()
        {
            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseThrottleKeys,
                                                    KeyBindings.decreaseThrottleKeys,
                                                    ControlsInstantiatorPatch.ThrottleLever,
                                                    ControlsInstantiatorPatch.ThrottleScroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputShunter), "TryApplyBrakeInput")]
    public static class ShunterPatch02
    {
        public static bool Prefix()
        {
            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseBrakeKeys,
                                                    KeyBindings.decreaseBrakeKeys,
                                                    ControlsInstantiatorPatch.BrakeLever,
                                                    ControlsInstantiatorPatch.BrakeScroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputShunter), "TryApplyIndependentBrakeInput")]
    public static class ShunterPatch03
    {
        public static bool Prefix()
        {
            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseIndependentBrakeKeys,
                                                    KeyBindings.decreaseIndependentBrakeKeys,
                                                    ControlsInstantiatorPatch.IndependantBrakeLever,
                                                    ControlsInstantiatorPatch.IndependantBrakeeScroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputSteam), "TryApplyThrottleInput")]
    public static class SteamPatch01
    {
        public static bool Prefix()
        {
            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseThrottleKeys,
                                                    KeyBindings.decreaseThrottleKeys,
                                                    ControlsInstantiatorPatch.ThrottleLever,
                                                    ControlsInstantiatorPatch.ThrottleScroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputSteam), "TryApplyBrakeInput")]
    public static class SteamPatch02
    {
        public static bool Prefix()
        {
            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseBrakeKeys, KeyBindings.decreaseBrakeKeys, ControlsInstantiatorPatch.BrakeLever, ControlsInstantiatorPatch.BrakeScroll, ref DelayTimer);
        }
         private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputSteam), "TryApplyIndependentBrakeInput")]
    public static class SteamPatch03
    {
        public static bool Prefix()
        {
            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseIndependentBrakeKeys, KeyBindings.decreaseIndependentBrakeKeys, ControlsInstantiatorPatch.IndependantBrakeLever, ControlsInstantiatorPatch.IndependantBrakeeScroll, ref DelayTimer);
        }
         private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoControllerBase), "PairRemoteController")]
    public static class LeverNotches
    {
        public static void Postfix()
        {
            ThrottleNotches = ControlsInstantiatorPatch.ThrottleLever.notches;
            IndBrakeNotches = ControlsInstantiatorPatch.IndependantBrakeLever.notches;
            BrakeNotches = ControlsInstantiatorPatch.BrakeLever.notches;
        }

        public static int ThrottleNotches = 20;
        public static int BrakeNotches = 20;
        public static int IndBrakeNotches = 20;
    }

    [HarmonyPatch(typeof(LocoControllerBase), "UpdateThrottle")]
    public static class RemoteThrottlePatch
    {
        public static bool Prefix(float factor, LocoControllerBase __instance)
        {
            var notches = LeverNotches.ThrottleNotches - 1;
            if (factor != 0)
            {
                if (RemoteThrottlePatch.Pressed == false)
                {
                    RemoteThrottlePatch.Pressed = true;
                    var notchedTarget = Mathf.Floor(__instance.targetThrottle * notches + 0.25f) / notches;
                    notchedTarget += (factor > 0 ? 1.0f : -1.0f) / notches;
                    __instance.SetThrottle(notchedTarget);
                    RemoteThrottlePatch.DelayTimer = Time.time + 0.2f;
                }
                else
                {
                    if (RemoteThrottlePatch.DelayTimer < Time.time)
                    {
                        __instance.SetThrottle(__instance.targetThrottle + factor * 0.01f);
                    }
                }
                return false;
            }

            if (factor == 0)
            {
                RemoteThrottlePatch.Pressed = false;
            }

            return false;
        }
        private static bool Pressed = false;
        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoControllerBase), "UpdateBrake")]
    public static class RemoteBrakePatch
    {
        public static bool Prefix(float factor, LocoControllerBase __instance)
        {
            var notches = LeverNotches.BrakeNotches - 1;
            if (factor != 0)
            {
                if (RemoteBrakePatch.Pressed == false)
                {
                    RemoteBrakePatch.Pressed = true;
                    var notchedTarget = Mathf.Floor(__instance.targetBrake * notches + 0.25f) / notches;
                    notchedTarget += (factor > 0 ? 1.0f : -1.0f) / notches;
                    __instance.SetBrake(notchedTarget);
                    RemoteBrakePatch.DelayTimer = Time.time + 0.2f;
                }
                else
                {
                    if (RemoteBrakePatch.DelayTimer < Time.time)
                    {
                        __instance.SetBrake(__instance.targetBrake + factor * 0.01f);
                    }
                }
                return false;
            }

            if (factor == 0)
            {
                RemoteBrakePatch.Pressed = false;
            }
            return false;
        }
        private static bool Pressed = false;
        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoControllerBase), "UpdateIndependentBrake")]
    public static class RemoteIndependantBrakePatch
    {
        public static bool Prefix(float factor, LocoControllerBase __instance)
        {
            var notches = LeverNotches.IndBrakeNotches - 1;
            if (factor != 0)
            {
                if (RemoteIndependantBrakePatch.Pressed == false)
                {
                    Pressed = true;
                    var notchedTarget = Mathf.Floor(__instance.targetIndependentBrake * notches + 0.25f) / notches;
                    notchedTarget += (factor > 0 ? 1.0f : -1.0f) / notches;
                    __instance.SetIndependentBrake(notchedTarget);
                    RemoteIndependantBrakePatch.DelayTimer = Time.time + 0.2f;
                }
                else
                {
                    if (RemoteIndependantBrakePatch.DelayTimer < Time.time)
                    {
                        __instance.SetIndependentBrake(__instance.targetIndependentBrake + factor * 0.01f);
                    }
                }
                return false;
            }

            if (factor == 0)
            {
                RemoteIndependantBrakePatch.Pressed = false;
            }
            return false;
        }
        private static bool Pressed = false;
        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(JoystickDriver), "OnEnable")]
    public static class JoystickDriverMemoryLeakPatch
    {
        public static bool Prefix(JoystickDriver __instance, ControlImplBase ___control, ref Coroutine ___UpdaterCoroutine)
        {
            if (___control == null)
            {
                return true;
            }

            ___UpdaterCoroutine = __instance.StartCoroutine(AccessTools.Method(typeof(JoystickDriver), "BehaviorUpdater").Invoke(__instance, null) as IEnumerator);
            return false;
        }
    }

    // Change "return yield WaitFor.Seconds(0.1)" to "yield return null"
    [HarmonyPatch(typeof(JoystickDriver), "BehaviorUpdater", MethodType.Enumerator)]
    public static class BehaviourUpdaterPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions)
                                  .MatchStartForward(
                                    new CodeMatch(OpCodes.Ldc_R4, 0.1f),
                                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(WaitFor), "Seconds")))
                                  .ThrowIfNotMatch("Could not find return WaitFor.Seconds(0.1)")
                                  .SetAndAdvance(OpCodes.Ldnull, null)
                                  .RemoveInstruction();

            return codeMatcher.InstructionEnumeration();
        }
    }

    // Change yield return WaitFor.SecondsRealtime(0.02f); to yield return Waitfor.FixedUpdate
    [HarmonyPatch(typeof(LeverBase), "CheckValueChange", MethodType.Enumerator)]
    public static class CheckValueChangePatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions)
                                  .MatchStartForward(
                                    new CodeMatch(OpCodes.Ldc_R4, 0.02f),
                                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(WaitFor), "SecondsRealtime")))
                                  .ThrowIfNotMatch("Could not find return WaitFor.Seconds(0.02)")
                                  .RemoveInstruction()
                                  .Set(OpCodes.Ldsfld, AccessTools.Field(typeof(WaitFor), nameof(WaitFor.FixedUpdate)));

            return codeMatcher.InstructionEnumeration();
        }
    }
}