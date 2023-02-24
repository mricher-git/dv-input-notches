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
        public static UnityModManager.ModEntry Mod;
        internal static Harmony HarmonyInst;
        public static Settings settings;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
            HarmonyInst = new Harmony(modEntry.Info.Id);

            settings = Settings.Load<Settings>(modEntry);

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
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

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }

    [HarmonyPatch(typeof(ControlsInstantiator), nameof(ControlsInstantiator.Spawn))]
    public static class ControlsInstantiatorPatch
    {
        public static void Postfix(ControlSpec spec)
        {
            EvaluateLever(spec);
        }

        private static bool? CCLactive = null;
        private static System.Type CabInputRelayType;
        private static FieldInfo Binding;
        private static bool HasCCLInputBinding(ControlSpec spec, string targetBinding)
        {
            CCLactive = CCLactive ?? UnityModManager.FindMod("DVCustomCarLoader")?.Active ?? false;
            if (CCLactive != true)
            {
                return false;
            }
            CabInputRelayType = CabInputRelayType ?? AccessTools.TypeByName("DVCustomCarLoader.LocoComponents.CabInputRelay");
            Binding = Binding ?? AccessTools.Field(CabInputRelayType, "Binding");

            if (spec.GetComponent(CabInputRelayType) is Component cabInputRelay)
                    return Binding.GetValue(cabInputRelay).ToString() == targetBinding;

            return false;
        }

        private static bool IsThrottleControl(Lever spec)
        {
            if (spec.name == "C throttle" || spec.name == "C throttle regulator")
                return true;
            if (HasCCLInputBinding(spec, "Throttle"))
                return true;
            return false;
        }
        private static bool IsTrainBrakeControl(Lever spec)
        {
            if (spec.name == "C train_brake_lever" || spec.name == "C brake")
                return true;
            if (HasCCLInputBinding(spec, "TrainBrake"))
                return true;
            return false;
        }

        private static bool IsIndependentBrakeControl(Lever spec)
        {
            if (spec.name == "C independent_brake_lever" || spec.name == "C independent brake")
                return true;
            if (HasCCLInputBinding(spec, "IndependentBrake"))
                return true;
            return false;
        }


        public static void EvaluateLever(ControlSpec spec)
        {
            if (spec is Lever lever)
            {
                TrainCar car = TrainCar.Resolve(spec.gameObject);
                if (car != null && car.IsLoco)
                {
                    var loco = car.gameObject;

                    if (IsThrottleControl(lever))
                    {
                        ThrottleControls[loco] = new LocoControls(lever, lever.GetComponent<IMouseWheelHoverScrollable>());
                    }
                    else if (IsTrainBrakeControl(lever)) 
                    {
                        BrakeControls[loco] = new LocoControls(lever, lever.GetComponent<IMouseWheelHoverScrollable>());
                    }
                    else if (IsIndependentBrakeControl(lever))
                    {
                        IndBrakeControls[loco] = new LocoControls(lever, lever.GetComponent<IMouseWheelHoverScrollable>());
                    }
                }
            }
        }

        public static readonly Dictionary<GameObject, LocoControls> ThrottleControls = new Dictionary<GameObject, LocoControls>();
        public static readonly Dictionary<GameObject, LocoControls> BrakeControls = new Dictionary<GameObject, LocoControls>();
        public static readonly Dictionary<GameObject, LocoControls> IndBrakeControls = new Dictionary<GameObject, LocoControls>();

        public static void FindLevers(TrainCar loco)
        {
            foreach (var lever in loco.interior?.GetComponentsInChildren<Lever>())
            {
                EvaluateLever(lever);
            }
        }
    }

    public static class KeyboardNotchPatch
    {
        public static bool TryApplyInput(KeyCode[] keyIncrease, KeyCode[] keyDecrease, Lever lever, IMouseWheelHoverScrollable scroller, ref float timer)
        {
            if (!SingletonBehaviour<InputFocusManager>.Instance.hasKeyboardFocus)
            {
                if (keyIncrease.IsPressed())
                {
                    if (keyIncrease.IsDown())
                    {
                        if (lever.scrollWheelHoverScroll > 0f)
                        {
                            scroller.OnHoverScrolledUp();
                        }
                        else
                        {
                            scroller.OnHoverScrolledDown();
                        }
                        timer = 0.3f;
                        return false;
                    }
                    else
                    {
                        timer -= Time.deltaTime;

                        if (timer <= 0f) return true;
                    }
                    return false;
                }
                else if (keyDecrease.IsPressed())
                {
                    if (keyDecrease.IsDown())
                    {
                        if (lever.scrollWheelHoverScroll > 0f)
                        {
                            scroller.OnHoverScrolledDown();
                        }
                        else
                        {
                            scroller.OnHoverScrolledUp();
                        }
                        timer = 0.3f;
                        return false;
                    }
                    else
                    {
                        timer -= Time.deltaTime;

                        if (timer <= 0f) return true;
                    }
                    return false;
                }
            }

            if (timer > 0f)
            {
                timer -= Time.deltaTime;
            }
            if (timer < 0f)
            {
                scroller.OnHoverScrollReleased();
                timer = 0;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(LocoKeyboardInputDiesel), "TryApplyThrottleInput")]
    public static class DieselTryApplyThrottlePatch
    {
        public static bool Prefix(LocoKeyboardInputDiesel __instance, ref float ___throttleVelo)
        {
            if (!(Main.settings.EnableDiesel == true && Main.settings.DieselSettings?.Throttle == true)) return true;
            if (___throttleVelo == 0) ___throttleVelo = 0.001f;

            if (!ControlsInstantiatorPatch.ThrottleControls.TryGetValue(__instance.gameObject, out LocoControls controls)) return true;

            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseThrottleKeys, 
                                                    KeyBindings.decreaseThrottleKeys,
                                                    controls.Lever,
                                                    controls.Scroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputDiesel), "TryApplyBrakeInput")]
    public static class DieselTryApplyBrakePatch
    {
        public static bool Prefix(LocoKeyboardInputDiesel __instance)
        {
            if (!(Main.settings.EnableDiesel == true && Main.settings.DieselSettings?.Brake == true)) return true;

            if (!ControlsInstantiatorPatch.BrakeControls.TryGetValue(__instance.gameObject, out LocoControls controls)) return true;

            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseBrakeKeys,
                                                    KeyBindings.decreaseBrakeKeys,
                                                    controls.Lever,
                                                    controls.Scroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputDiesel), "TryApplyIndependentBrakeInput")]
    public static class DieselTryApplyIndBrakePatch
    {
        public static bool Prefix(LocoKeyboardInputDiesel __instance)
        {
            if (!(Main.settings.EnableDiesel == true && Main.settings.DieselSettings?.IndBrake == true)) return true;

            if (!ControlsInstantiatorPatch.IndBrakeControls.TryGetValue(__instance.gameObject, out LocoControls controls)) return true;

            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseIndependentBrakeKeys,
                                                    KeyBindings.decreaseIndependentBrakeKeys,
                                                    controls.Lever,
                                                    controls.Scroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputShunter), "TryApplyThrottleInput")]
    public static class ShunterTryApplyThrottlePatch
    {
        public static bool Prefix(LocoKeyboardInputShunter __instance, ref float ___throttleVelo)
        {
            if (!(Main.settings.EnableShunter == true && Main.settings.ShunterSettings?.Throttle == true)) return true;

            if (___throttleVelo == 0) ___throttleVelo = 0.001f;

            if (!ControlsInstantiatorPatch.ThrottleControls.TryGetValue(__instance.gameObject, out LocoControls controls)) return true;
            
            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseThrottleKeys,
                                                    KeyBindings.decreaseThrottleKeys,
                                                    controls.Lever,
                                                    controls.Scroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputShunter), "TryApplyBrakeInput")]
    public static class ShunterTryApplyBrakePatch
    {
        public static bool Prefix(LocoKeyboardInputShunter __instance)
        {
            if (!(Main.settings.EnableShunter == true && Main.settings.ShunterSettings?.Brake == true)) return true;

            if (!ControlsInstantiatorPatch.BrakeControls.TryGetValue(__instance.gameObject, out LocoControls controls)) return true;

            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseBrakeKeys,
                                                    KeyBindings.decreaseBrakeKeys,
                                                    controls.Lever,
                                                    controls.Scroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputShunter), "TryApplyIndependentBrakeInput")]
    public static class ShunterTryApplyIndBrakePatch
    {
        public static bool Prefix(LocoKeyboardInputShunter __instance)
        {
            if (!(Main.settings.EnableShunter == true && Main.settings.ShunterSettings?.IndBrake == true)) return true;

            if (!ControlsInstantiatorPatch.IndBrakeControls.TryGetValue(__instance.gameObject, out LocoControls controls)) return true;

            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseIndependentBrakeKeys,
                                                    KeyBindings.decreaseIndependentBrakeKeys,
                                                    controls.Lever,
                                                    controls.Scroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputSteam), "TryApplyThrottleInput")]
    public static class SteamTryApplyThrottlePatch
    {
        public static bool Prefix(LocoKeyboardInputSteam __instance)
        {
            if (!(Main.settings.EnableSteam == true && Main.settings.SteamSettings?.Throttle == true)) return true;

            if (!ControlsInstantiatorPatch.ThrottleControls.TryGetValue(__instance.gameObject, out LocoControls controls)) return true;

            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseThrottleKeys,
                                                    KeyBindings.decreaseThrottleKeys,
                                                    controls.Lever,
                                                    controls.Scroll,
                                                    ref DelayTimer);
        }

        private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputSteam), "TryApplyBrakeInput")]
    public static class SteamTryApplyBrakePatch
    {
        public static bool Prefix(LocoKeyboardInputSteam __instance)
        {
            if (!(Main.settings.EnableSteam == true && Main.settings.SteamSettings?.Brake == true)) return true;

            if (!ControlsInstantiatorPatch.BrakeControls.TryGetValue(__instance.gameObject, out LocoControls controls)) return true;

            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseBrakeKeys,
                                                    KeyBindings.decreaseBrakeKeys,
                                                    controls.Lever,
                                                    controls.Scroll,
                                                    ref DelayTimer);
        }
         private static float DelayTimer;
    }

    [HarmonyPatch(typeof(LocoKeyboardInputSteam), "TryApplyIndependentBrakeInput")]
    public static class SteamTryApplyIndBrakePatch
    {
        public static bool Prefix(LocoKeyboardInputSteam __instance)
        {
            if (!(Main.settings.EnableSteam == true && Main.settings.SteamSettings?.IndBrake == true)) return true;

            if (!ControlsInstantiatorPatch.IndBrakeControls.TryGetValue(__instance.gameObject, out LocoControls controls)) return true;
            
            return KeyboardNotchPatch.TryApplyInput(KeyBindings.increaseIndependentBrakeKeys,
                                                    KeyBindings.decreaseIndependentBrakeKeys,
                                                    controls.Lever,
                                                    controls.Scroll,
                                                    ref DelayTimer);
        }
         private static float DelayTimer;
    }

    //[HarmonyPatch(typeof(LocoControllerBase), "PairRemoteController")]
    //public static class LeverNotches
    //{
    //    public static void Postfix(TrainCar ___train)
    //    {
    //        ThrottleNotches.Add(___train, ControlsInstantiatorPatch.ThrottleLever.notches);
    //        BrakeNotches.Add(___train, ControlsInstantiatorPatch.BrakeLever.notches);
    //        IndBrakeNotches.Add(___train, ControlsInstantiatorPatch.IndependantBrakeLever.notches);
    //    }

    //    public static readonly Dictionary<TrainCar, int> ThrottleNotches = new Dictionary<TrainCar, int>();
    //    public static readonly Dictionary<TrainCar, int> BrakeNotches = new Dictionary<TrainCar, int>();
    //    public static readonly Dictionary<TrainCar, int> IndBrakeNotches = new Dictionary<TrainCar, int>();
    //}

    [HarmonyPatch(typeof(LocoControllerBase), "UpdateThrottle")]
    public static class RemoteThrottlePatch
    {
        public static bool Prefix(float factor, LocoControllerBase __instance)
        {
            int notches = 20;
            if (ControlsInstantiatorPatch.ThrottleControls.TryGetValue(__instance.gameObject, out LocoControls controls))
            {
                notches = controls.Lever.notches - 1;
            }

            if (factor != 0)
            {
                if (RemoteThrottlePatch.Pressed == false)
                {
                    RemoteThrottlePatch.Pressed = true;
                    var notchedTarget = Mathf.Floor(__instance.targetThrottle * notches + 0.25f) / notches;
                    notchedTarget += (factor > 0 ? 1.0f : -1.0f) / notches;
                    __instance.SetThrottle(notchedTarget);
                    RemoteThrottlePatch.DelayTimer = Time.time + 0.3f;
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
            int notches = 20;
            if (ControlsInstantiatorPatch.BrakeControls.TryGetValue(__instance.gameObject, out LocoControls controls))
            {
                notches = controls.Lever.notches - 1;
            }

            if (factor != 0)
            {
                if (RemoteBrakePatch.Pressed == false)
                {
                    RemoteBrakePatch.Pressed = true;
                    var notchedTarget = Mathf.Floor(__instance.targetBrake * notches + 0.25f) / notches;
                    notchedTarget += (factor > 0 ? 1.0f : -1.0f) / notches;
                    __instance.SetBrake(notchedTarget);
                    RemoteBrakePatch.DelayTimer = Time.time + 0.3f;
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
            int notches = 20;
            if (ControlsInstantiatorPatch.IndBrakeControls.TryGetValue(__instance.gameObject, out LocoControls controls))
            {
                notches = controls.Lever.notches - 1;
            }

            if (factor != 0)
            {
                if (RemoteIndependantBrakePatch.Pressed == false)
                {
                    Pressed = true;
                    var notchedTarget = Mathf.Floor(__instance.targetIndependentBrake * notches + 0.25f) / notches;
                    notchedTarget += (factor > 0 ? 1.0f : -1.0f) / notches;
                    __instance.SetIndependentBrake(notchedTarget);
                    RemoteIndependantBrakePatch.DelayTimer = Time.time + 0.3f;
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

    public struct LocoControls
    {
        public LocoControls(Lever lever, IMouseWheelHoverScrollable scroll)
        {
            Lever = lever;
            Scroll = scroll;
        }

        public Lever Lever { get; }
        public IMouseWheelHoverScrollable Scroll { get; }
    }
}