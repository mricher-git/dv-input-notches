using DV.RemoteControls;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace InputNotches
{
    [EnableReloading]
    public static class Main
    {
        public static bool Enabled;
        public static UnityModManager.ModEntry ModEntry;
        internal static Harmony HarmonyInst;
        public static Settings Settings;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            HarmonyInst = new Harmony(modEntry.Info.Id);

            Settings = Settings.Load<Settings>(modEntry);

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
            Settings.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }
    }
    
    [HarmonyPatch(typeof(RemoteControllerModule), "UpdateThrottle")]
    public static class RemoteThrottlePatch
    {
        public static bool Prefix(float factor, BaseControlsOverrider ___controlsOverrider, float ___throttleStepSize)
        {
            if (!Main.Settings.RemoteSettings.Throttle) return true;
            ThrottleControl throttle = ___controlsOverrider.Throttle;
            if (throttle == null) return false;
            
            throttle.Set(___controlsOverrider.Throttle.Value + ((factor == 0f) ? 0f : (Mathf.Sign(factor) * ___throttleStepSize)));
            return false;
        }
    }
    [HarmonyPatch(typeof(RemoteControllerModule), "UpdateIndependentBrake")]

    public static class RemoteIndBrakePatch
    {
        public static bool Prefix(float factor, BaseControlsOverrider ___controlsOverrider, float ___indBrakeStepSize)
        {
            if (!Main.Settings.RemoteSettings.IndBrake) return true;
            IndependentBrakeControl indBrake = ___controlsOverrider.IndependentBrake;
            if (indBrake == null) return false;

            indBrake.Set(___controlsOverrider.IndependentBrake.Value + ((factor == 0f) ? 0f : (Mathf.Sign(factor) * ___indBrakeStepSize)));
            return false;
        }
    }

    [HarmonyPatch(typeof(RemoteControllerModule), "UpdateBrake")]
    public static class RemoteBrakePatch
    {
        public static bool Prefix(float factor, BaseControlsOverrider ___controlsOverrider, float ___brakeStepSize, TrainCar ___car)
        {
            if (!Main.Settings.RemoteSettings.Brake) return true;
            
            BrakeControl brake = ___controlsOverrider.Brake;
            if (brake == null)
            {
                return false;
            }
            //if (___car.brakeSystem.selfLappingController)
            //{
                brake.Set(___controlsOverrider.Brake.Value + ((factor == 0f) ? 0f : (Mathf.Sign(factor) * ___brakeStepSize)));
            //} 
            //else
            //{
            //    float num = ((factor == 0f) ? 0f : (Mathf.Sign(factor) * 0.15f));
            //    brake.Set(0.5f + num);
            //}
            return false;
        }
    }
    [HarmonyPatch(typeof(JoystickDriver), "Normalize")]
    public static class DeadZonePatch
    {
        public static bool Prefix(JoystickDriver __instance)
        {
            if (__instance.behavior == JoystickDriver.Behavior.Additive)
                __instance.deadZone = Main.Settings.DeadZone;
            
            return true;
        }
    }
}