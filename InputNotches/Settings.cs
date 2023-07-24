using UnityModManagerNet;

namespace InputNotches
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Enable Remote Control Notches", Box = true)] 
        public ControlSettings RemoteSettings = new ControlSettings();
        [Draw("Deadzone for Remote Controller Joysticks")]
        public float DeadZone = 0.75f;

        override public void Save(UnityModManager.ModEntry entry)
        {
            Save<Settings>(this, entry);
        }

        public void OnChange()
        {
        }

        public class ControlSettings
        {
            [Draw("Throttle")] public bool Throttle = true;
            [Draw("Brake")] public bool Brake = true;
            [Draw("Independent Brake")] public bool IndBrake = true;
        }
    }
}