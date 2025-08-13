using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace LenoPwn.Service
{
    [SupportedOSPlatform("windows")]
    public class LenoPwnService : ServiceBase
    {
        private const string PipeName = "LenoPwnPipe";
        private const string ConfigFileName = "hotkey_map.json";
        private ManagementEventWatcher? _watcher;
        private CancellationTokenSource? _cancellationTokenSource;
        private NamedPipeClientStream? _pipeClient;
        private StreamWriter? _pipeWriter;
        private MMDeviceEnumerator? _deviceEnumerator;
        private MMDevice? _micDevice;
        private MMDevice? _speakerDevice;
        private string _currentTheme = "Dark";

        private AppConfig? _cachedConfig;
        private FileSystemWatcher? _configWatcher;
        private readonly object _configLock = new object();

        public LenoPwnService()
        {
            this.ServiceName = "LenoPwn.Service";
            this.CanStop = true;
            this.CanPauseAndContinue = false;
            this.AutoLog = false;
        }

        private void Log(string message, EventLogEntryType type = EventLogEntryType.Information)
        {
            try { EventLog.WriteEntry(ServiceName, message, type); } catch { }
        }

        public static void Main() => ServiceBase.Run(new LenoPwnService());

        protected override void OnStart(string[] args)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ServiceWorker(_cancellationTokenSource.Token));
        }

        protected override void OnStop()
        {
            _cancellationTokenSource?.Cancel();
            _pipeWriter?.Dispose();
            _pipeClient?.Dispose();
            _watcher?.Stop();
            _watcher?.Dispose();
            _configWatcher?.Dispose();
            CleanupAudioMonitors();
        }

        private async Task ServiceWorker(CancellationToken token)
        {
            InitializeAudioMonitors();
            WmiController.SyncMicMuteLed();
            WmiController.SyncSpeakerMuteLed();

            InitializeConfigAndWatcher();

            _watcher = new ManagementEventWatcher(@"\\.\root\WMI", "SELECT * FROM LENOVO_UTILITY_EVENT");
            _watcher.EventArrived += OnEventArrived;
            _watcher.Start();
            Log("WMI watcher started. Service is now listening for key presses.");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    _pipeClient?.Dispose();
                    _pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                    Log("Attempting to connect to agent pipe...");

                    await _pipeClient.ConnectAsync(5000, token);
                    _pipeWriter = new StreamWriter(_pipeClient, Encoding.UTF8) { AutoFlush = true };
                    Log("Successfully connected to agent pipe!");

                    await token.AsTask().ContinueWith(t => { }, TaskContinuationOptions.OnlyOnCanceled);
                }
                catch (OperationCanceledException)
                {
                    Log("Service worker cancellation requested.");
                    break;
                }
                catch (System.TimeoutException)
                {
                    Log("Timeout connecting to agent pipe. Will retry in 5 seconds...", EventLogEntryType.Warning);
                    _pipeClient?.Dispose();
                    await Task.Delay(5000, token);
                }
                catch (Exception ex)
                {
                    Log($"Pipe client error: {ex.Message}. Will retry in 5 seconds...", EventLogEntryType.Warning);
                    _pipeClient?.Dispose();
                    await Task.Delay(5000, token);
                }
            }

            Log("Service worker loop ended.");
        }

        private void OnEventArrived(object sender, EventArrivedEventArgs e)
        {
            if (e.NewEvent.GetPropertyValue("PressTypeDataVal") is not uint keyCode) return;

            AppConfig? config;
            lock (_configLock)
            {
                config = _cachedConfig;
            }

            if (config == null)
            {
                Log("Configuration not loaded, skipping execution.", EventLogEntryType.Warning);
                return;
            }

            _currentTheme = string.IsNullOrWhiteSpace(config.Theme) ? "Dark" : config.Theme;
            var mapping = config.Mappings.FirstOrDefault(m => m.KeyCode == keyCode);
            if (mapping != null) ExecuteAction(mapping);
        }

        private void ExecuteAction(HotkeyMapping mapping)
        {
            string? payloadStr = (mapping.Payload as JsonElement?)?.GetString() ?? mapping.Payload as string;
            switch (mapping.Action.ToLower())
            {
                case "launch":
                    if (string.IsNullOrEmpty(payloadStr)) return;
                    SendCommandToAgent($"launch::{payloadStr}");
                    break;

                case "sendkeys":
                    if (mapping.Payload is SendKeysPayload sendKeysPayload)
                    {
                        string key = sendKeysPayload.Key ?? "";
                        string modifiersStr = string.Join(",", sendKeysPayload.Modifiers ?? new List<string>());

                        if (!string.IsNullOrEmpty(key))
                        {
                            SendCommandToAgent($"sendkeys::{modifiersStr}::{key}");
                        }
                    }
                    else if (mapping.Payload is JsonElement jsonPayload)
                    {
                        try
                        {
                            if (jsonPayload.TryGetProperty("Key", out var keyProp) && keyProp.ValueKind == JsonValueKind.String)
                            {
                                string key = keyProp.GetString() ?? "";
                                string modifiersStr = "";

                                if (jsonPayload.TryGetProperty("Modifiers", out var modProp) && modProp.ValueKind == JsonValueKind.Array)
                                {
                                    var modifiers = modProp.EnumerateArray().Select(m => m.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                                    modifiersStr = string.Join(",", modifiers);
                                }

                                if (!string.IsNullOrEmpty(key))
                                {
                                    SendCommandToAgent($"sendkeys::{modifiersStr}::{key}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Failed to execute sendkeys action: {ex.Message}", EventLogEntryType.Error);
                        }
                    }
                    break;

                case "special":
                    if (string.IsNullOrEmpty(payloadStr)) return;
                    string iconName = payloadStr;
                    if (payloadStr == "toggle_mic_mute")
                    {
                        ToggleCoreAudioMuteAndLed();
                        iconName = (_micDevice?.AudioEndpointVolume.Mute ?? false) ? "microphone_mute" : "microphone_unmute";
                    }
                    else if (payloadStr == "toggle_speaker_mute")
                    {
                        ToggleCoreAudioSpeakerMuteAndLed();
                        iconName = (_speakerDevice?.AudioEndpointVolume.Mute ?? false) ? "speaker_mute" : "speaker_unmute";
                    }
                    if (mapping.ShowPopup) ShowPopup(iconName);
                    break;
            }
        }

        private void ShowPopup(string iconName) => SendCommandToAgent($"show_icon::{iconName}::{_currentTheme}");

        private void SendCommandToAgent(string command)
        {
            if (_pipeClient?.IsConnected == true && _pipeWriter != null)
            {
                try
                {
                    _pipeWriter.WriteLine(command);
                }
                catch (IOException ex) { Log($"Pipe write error: {ex.Message}", EventLogEntryType.Warning); }
            }
        }

        #region User Config and Audio
        private void InitializeConfigAndWatcher()
        {
            var configPath = GetConfigPathForActiveUser();
            if (configPath == null)
            {
                Log("Could not determine config path for active user. Caching will be disabled.", EventLogEntryType.Warning);
                return;
            }

            LoadConfiguration(configPath);

            var configDirectory = Path.GetDirectoryName(configPath);
            if (configDirectory != null)
            {
                _configWatcher = new FileSystemWatcher(configDirectory, ConfigFileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
                };
                _configWatcher.Changed += (s, e) => LoadConfiguration(e.FullPath);
                _configWatcher.Created += (s, e) => LoadConfiguration(e.FullPath);
                _configWatcher.Deleted += (s, e) =>
                {
                    lock (_configLock)
                    {
                        _cachedConfig = new AppConfig();
                    }
                    Log("Config file deleted. Cache cleared.");
                };
                _configWatcher.EnableRaisingEvents = true;
                Log($"Watching for config changes at: {configPath}");
            }
        }

        private void LoadConfiguration(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    Log($"Config file not found at {configPath}. Awaiting creation.", EventLogEntryType.Warning);
                    return;
                }

                var json = File.ReadAllText(configPath);
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

                lock (_configLock)
                {
                    _cachedConfig = config;
                }
                Log("Configuration reloaded and cached successfully.");
            }
            catch (Exception ex)
            {
                Log($"Failed to load or parse configuration: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private string? GetConfigPathForActiveUser()
        {
            uint sessionId = WTSGetActiveConsoleSessionId();
            if (sessionId == 0xFFFFFFFF) return null;
            IntPtr userToken = IntPtr.Zero;
            try
            {
                if (!WTSQueryUserToken(sessionId, out userToken)) return null;
                uint size = 260;
                var profileDir = new StringBuilder((int)size);
                if (!GetUserProfileDirectory(userToken, profileDir, ref size)) return null;
                var configFolder = Path.Combine(profileDir.ToString(), "AppData", "Local", "LenovoHotkeyService");
                return Path.Combine(configFolder, ConfigFileName);
            }
            catch { return null; }
            finally { if (userToken != IntPtr.Zero) CloseHandle(userToken); }
        }

        private void InitializeAudioMonitors()
        {
            try
            {
                _deviceEnumerator = new MMDeviceEnumerator();
                _micDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                _micDevice.AudioEndpointVolume.OnVolumeNotification += (data) => WmiController.SetMicMuteLedState(data.Muted);
                _speakerDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                _speakerDevice.AudioEndpointVolume.OnVolumeNotification += (data) => WmiController.SetSpeakerMuteLedState(data.Muted);
            }
            catch (Exception ex) { Log($"Could not initialize audio monitors: {ex.Message}.", EventLogEntryType.Warning); }
        }

        private void CleanupAudioMonitors()
        {
            _micDevice?.Dispose();
            _speakerDevice?.Dispose();
            _deviceEnumerator?.Dispose();
        }

        private void ToggleCoreAudioMuteAndLed() { if (_micDevice != null) _micDevice.AudioEndpointVolume.Mute = !_micDevice.AudioEndpointVolume.Mute; }
        private void ToggleCoreAudioSpeakerMuteAndLed() { if (_speakerDevice != null) _speakerDevice.AudioEndpointVolume.Mute = !_speakerDevice.AudioEndpointVolume.Mute; }
        #endregion

        #region P/Invoke
        [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetUserProfileDirectory(IntPtr hToken, StringBuilder lpProfileDir, ref uint lpcchSize);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(uint SessionId, out IntPtr phToken);

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);
        #endregion
    }

    #region Helper Classes
    public class AppConfig { public string Theme { get; set; } = "Dark"; public List<HotkeyMapping> Mappings { get; set; } = new(); }
    public class HotkeyMapping { public uint KeyCode { get; set; } public string Description { get; set; } = ""; public string Action { get; set; } = "Not Assigned"; public object? Payload { get; set; } public bool ShowPopup { get; set; } = false; }

    public class SendKeysPayload
    {
        public List<string> Modifiers { get; set; } = new();
        public string Key { get; set; } = "";
    }

    public class HotkeyMappingPayloadConverter : JsonConverter<object> { public override object? Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => r.TokenType switch { JsonTokenType.String => r.GetString(), _ => JsonDocument.ParseValue(ref r).RootElement.Clone(), }; public override void Write(Utf8JsonWriter w, object v, JsonSerializerOptions o) => JsonSerializer.Serialize(w, v, v.GetType(), o); }

    [SupportedOSPlatform("windows")]
    public static class WmiController
    {
        private const uint MIC_MUTE_LED_ON = 1, MIC_MUTE_LED_OFF = 2, SPEAKER_MUTE_LED_ON = 4, SPEAKER_MUTE_LED_OFF = 5;
        private static void SetLedFeature(uint fc) { try { using var mc = new ManagementClass(@"\\.\root\WMI", "LENOVO_UTILITY_DATA", null); using var mi = mc.GetInstances().Cast<ManagementObject>().FirstOrDefault(); if (mi == null) return; var ip = mc.GetMethodParameters("SetFeature"); ip["featuretype"] = fc; mi.InvokeMethod("SetFeature", ip, null); } catch { } }
        public static void SetMicMuteLedState(bool isMuted) => SetLedFeature(isMuted ? MIC_MUTE_LED_ON : MIC_MUTE_LED_OFF);
        public static void SetSpeakerMuteLedState(bool isMuted) => SetLedFeature(isMuted ? SPEAKER_MUTE_LED_ON : SPEAKER_MUTE_LED_OFF);
        public static void SyncMicMuteLed() { try { using var e = new MMDeviceEnumerator(); using var m = e.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications); if (m != null) SetMicMuteLedState(m.AudioEndpointVolume.Mute); } catch { } }
        public static void SyncSpeakerMuteLed() { try { using var e = new MMDeviceEnumerator(); using var s = e.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console); if (s != null) SetSpeakerMuteLedState(s.AudioEndpointVolume.Mute); } catch { } }
    }

    public static class CancellationTokenExtensions
    {
        public static Task AsTask(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object>();
            cancellationToken.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);
            return tcs.Task;
        }
    }
    #endregion
}