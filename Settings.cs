﻿using UnityModManagerNet;

namespace KeyboardNotches
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Shunter")] public bool EnableShunter;
        [Draw("", Box = true, VisibleOn = "EnableShunter|true")] public ControlSettings ShunterSettings = new ControlSettings();
        [Draw("Steamer")] public bool EnableSteam;
        [Draw("", Box = true, VisibleOn = "EnableSteam|true")] public ControlSettings SteamSettings = new ControlSettings();
        [Draw("Diesel")] public bool EnableDiesel;
        [Draw("", Box = true, VisibleOn = "EnableDiesel|true")] public ControlSettings DieselSettings = new ControlSettings();



        override public void Save(UnityModManager.ModEntry entry)
        {
            Save<Settings>(this, entry);
        }

        public void OnChange()
        {
        }

        public class ControlSettings
        {
            [Draw("Throttle")] public bool Throttle;
            [Draw("Brake")] public bool Brake;
            [Draw("Independent Brake")] public bool IndBrake;
        }
    }
}