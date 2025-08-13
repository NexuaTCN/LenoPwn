using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LenoPwn.ConfigUI
{
    public class ActionInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string InternalName { get; set; } = string.Empty;
    }

    public static class PayloadOptions
    {
        public static List<ActionInfo> Actions { get; } = new List<ActionInfo>
        {
            new ActionInfo { DisplayName = "Unassigned", InternalName = "unassigned" },
            new ActionInfo { DisplayName = "Launch Application", InternalName = "launch" },
            new ActionInfo { DisplayName = "Send Keystrokes", InternalName = "sendkeys" },
            new ActionInfo { DisplayName = "Special Action", InternalName = "special" }
        };

        public static List<ActionInfo> SpecialActions { get; } = new List<ActionInfo>
        {
            new ActionInfo { DisplayName = "Toggle Microphone Mute", InternalName = "toggle_mic_mute" },
            new ActionInfo { DisplayName = "Toggle Speaker Mute", InternalName = "toggle_speaker_mute" },
            new ActionInfo { DisplayName = "Function Lock On", InternalName = "fn_lock_on" },
            new ActionInfo { DisplayName = "Function Lock Off", InternalName = "fn_lock_off" },
            new ActionInfo { DisplayName = "Camera On", InternalName = "camera_on" },
            new ActionInfo { DisplayName = "Camera Off", InternalName = "camera_off" },
            new ActionInfo { DisplayName = "Keyboard Backlight Off", InternalName = "kb_backlight_off" },
            new ActionInfo { DisplayName = "Keyboard Backlight Low", InternalName = "kb_backlight_low" },
            new ActionInfo { DisplayName = "Keyboard Backlight High", InternalName = "kb_backlight_high" },
            new ActionInfo { DisplayName = "Keyboard Backlight Auto", InternalName = "kb_backlight_auto" }
        };
    }
}