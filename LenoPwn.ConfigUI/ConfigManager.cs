using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LenoPwn.ConfigUI
{
    public class AppConfig
    {
        public string Theme { get; set; } = "Dark";
        public List<HotkeyMapping> Mappings { get; set; } = new();
        public List<UserProfile> Profiles { get; set; } = new();
    }

    public class UserProfile
    {
        public string Name { get; set; } = "";
        public AppConfig Config { get; set; } = new();
    }

    public class HotkeyMapping : INotifyPropertyChanged
    {
        private string _action = "Unassigned";
        private bool _showPopup = false;
        private object? _payload;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public uint KeyCode { get; set; }
        public string Description { get; set; } = "";

        public object? Payload
        {
            get => _payload;
            set
            {
                if (_payload != value)
                {
                    _payload = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Action
        {
            get => _action;
            set
            {
                if (_action != value)
                {
                    _action = value;
                    OnPropertyChanged();

                    if (_action.ToLower() != "special")
                    {
                        ShowPopup = false;
                    }

                    if (_action.ToLower() == "sendkeys" && Payload is not SendKeysPayload)
                    {
                        Payload = new SendKeysPayload();
                    }
                }
            }
        }

        public bool ShowPopup
        {
            get => _showPopup;
            set
            {
                if (_showPopup != value)
                {
                    _showPopup = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    public class SendKeysPayload : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName != nameof(KeyComboText))
            {
                OnPropertyChanged(nameof(KeyComboText));
            }
        }

        private List<string> _modifiers = new();
        [JsonPropertyName("Modifiers")]
        public List<string> Modifiers
        {
            get => _modifiers;
            set
            {
                if (_modifiers != value)
                {
                    _modifiers = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _key = "";
        [JsonPropertyName("Key")]
        public string Key
        {
            get => _key;
            set
            {
                if (_key != value)
                {
                    _key = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public string KeyComboText
        {
            get
            {
                if (string.IsNullOrEmpty(Key))
                {
                    return "(Not set)";
                }

                var parts = new List<string>();
                if (Modifiers.Contains("control", StringComparer.OrdinalIgnoreCase)) parts.Add("Ctrl");
                if (Modifiers.Contains("alt", StringComparer.OrdinalIgnoreCase)) parts.Add("Alt");
                if (Modifiers.Contains("shift", StringComparer.OrdinalIgnoreCase)) parts.Add("Shift");
                if (Modifiers.Contains("win", StringComparer.OrdinalIgnoreCase)) parts.Add("Win");

                parts.Add(Key);

                return string.Join(" + ", parts);
            }
        }
    }

    public static class ConfigManager
    {
        public static string ConfigPath { get; }

        static ConfigManager()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configFolder = Path.Combine(appDataPath, "LenoPwn");
            Directory.CreateDirectory(configFolder);
            ConfigPath = Path.Combine(configFolder, "hotkey_map.json");
        }

        public static AppConfig LoadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaultConfig = new AppConfig();
                AddDefaultProfiles(defaultConfig);
                SaveConfig(defaultConfig);
                return defaultConfig;
            }
            try
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();

                foreach (var mapping in config.Mappings)
                {
                    if (mapping.Action?.ToLower() == "sendkeys" && mapping.Payload is JsonElement element)
                    {
                        try
                        {
                            mapping.Payload = JsonSerializer.Deserialize<SendKeysPayload>(element.GetRawText());
                        }
                        catch
                        {
                            mapping.Payload = new SendKeysPayload();
                        }
                    }
                }

                if (config.Profiles == null || !config.Profiles.Any())
                {
                    AddDefaultProfiles(config);
                }

                return config;
            }
            catch
            {
                var defaultConfig = new AppConfig();
                AddDefaultProfiles(defaultConfig);
                return defaultConfig;
            }
        }

        public static void SaveConfig(AppConfig config)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            var newJson = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigPath, newJson);
        }

        private static void AddDefaultProfiles(AppConfig config)
        {
            config.Profiles = new List<UserProfile>
            {
                new UserProfile
                {
                    Name = "Lenovo Yoga Slim 7x",
                    Config = new AppConfig
                    {
                        Mappings = new List<HotkeyMapping>
                        {
                            new HotkeyMapping { KeyCode = 1, Description = "Insert / Star", Action = "launch", Payload = "notepad", ShowPopup = false },
                            new HotkeyMapping { KeyCode = 72, Description = "F11 / Phone Link", Action = "launch", Payload = "explorer.exe shell:appsFolder\\Microsoft.YourPhone_8wekyb3d8bbwe!App", ShowPopup = false },
                            new HotkeyMapping { KeyCode = 62, Description = "Toggle Microphone", Action = "special", Payload = "toggle_mic_mute", ShowPopup = true },
                            new HotkeyMapping { KeyCode = 2, Description = "Function Lock On", Action = "special", Payload = "fn_lock_on", ShowPopup = true },
                            new HotkeyMapping { KeyCode = 3, Description = "Function Lock Off", Action = "special", Payload = "fn_lock_off", ShowPopup = true },
                            new HotkeyMapping { KeyCode = 12, Description = "Camera On", Action = "special", Payload = "camera_on", ShowPopup = true },
                            new HotkeyMapping { KeyCode = 13, Description = "Camera Off", Action = "special", Payload = "camera_off", ShowPopup = true },
                            new HotkeyMapping { KeyCode = 64, Description = "Keyboard Backlight Off", Action = "special", Payload = "kb_backlight_off", ShowPopup = true },
                            new HotkeyMapping { KeyCode = 67, Description = "Keyboard Backlight High", Action = "special", Payload = "kb_backlight_high", ShowPopup = true },
                            new HotkeyMapping { KeyCode = 65, Description = "Keyboard Backlight Low", Action = "special", Payload = "kb_backlight_low", ShowPopup = true },
                            new HotkeyMapping { KeyCode = 66, Description = "Keyboard Backlight Auto", Action = "special", Payload = "kb_backlight_auto", ShowPopup = true }
                        }
                    }
                }
            };
        }
    }
}