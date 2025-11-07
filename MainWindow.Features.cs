// MainWindow.Features.cs — Ajouts pro (hotkeys, overlay, import/export, logs, test rumble, thème, outils inline-safe)
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;          // <— pour CheckBox/RadioButton/Slider/TextBlock
using System.Windows.Interop;

namespace Trident.MITM
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        // ==================== Journal (logs) ====================
        public ObservableCollection<string> LogLines { get; } = new();
        void Log(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogLines.Add(line);
                while (LogLines.Count > 2000) LogLines.RemoveAt(0);
                LastMessage = msg;
            });
        }

        // ==================== Hotkeys (globaux) ====================
        const int HOTKEY_TOGGLE = 0xA001;
        const int HOTKEY_NEXT = 0xA002;
        const int HOTKEY_PREV = 0xA003;
        const int HOTKEY_RF = 0xA004;

        const uint MOD_ALT = 0x0001;
        const uint MOD_CONTROL = 0x0002;
        const int VK_F1 = 0x70;
        // ⚠️ NE PAS redéclarer VK_LEFT/VK_RIGHT/VK_R ici (ils existent dans MainWindow.xaml.cs)

        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        HwndSource? _src;

        

      

        // ==================== Overlay (facultatif) ====================
        OverlayWindow? _overlay;
        public bool OverlayEnabled
        {
            get => _overlay?.IsVisible == true;
            set { if (value) ShowOverlay(); else HideOverlay(); N(nameof(OverlayEnabled)); }
        }
        void ShowOverlay()
        {
            if (_overlay == null) { _overlay = new OverlayWindow { Owner = this }; }
            _overlay.Left = SystemParameters.WorkArea.Right - 260;
            _overlay.Top = SystemParameters.WorkArea.Top + 30;
            _overlay.Show();
            UpdateOverlay();
            Log("Overlay: ON");
        }
        void HideOverlay() { try { _overlay?.Hide(); } catch { } Log("Overlay: OFF"); }
        void UpdateOverlay()
        {
            _overlay?.SetStatus(SelectedWeapon, RapidFire, _loopCts != null, VigemStatus, PadStatus);
        }

        // ==================== Import / Export profils ====================
        

        // ==================== Testeur de vibrations (inline-safe) ====================
        CancellationTokenSource? _rumbleCts;

        void RumbleOnce(byte small, byte large, int ms)
        {
            try
            {
                ForwardRumbleUSB(small, large);
                Task.Delay(ms).ContinueWith(_ => ForwardRumbleUSB(0, 0));
                Log($"Rumble test: small={small}, large={large}, {ms}ms");
            }
            catch (Exception ex) { Log("Rumble test erreur: " + ex.Message); }
        }

        async void RumbleRamp(byte fromSmall, byte fromLarge, byte toSmall, byte toLarge, int durationMs)
        {
            _rumbleCts?.Cancel();
            _rumbleCts = new CancellationTokenSource();
            var ct = _rumbleCts.Token;
            int steps = Math.Max(8, durationMs / 30);
            for (int i = 0; i <= steps && !ct.IsCancellationRequested; i++)
            {
                double t = (double)i / steps;
                byte s = (byte)Math.Round(fromSmall + (toSmall - fromSmall) * t);
                byte l = (byte)Math.Round(fromLarge + (toLarge - fromLarge) * t);
                ForwardRumbleUSB(s, l);
                await Task.Delay(30, ct).ContinueWith(_ => { });
            }
            ForwardRumbleUSB(0, 0);
            Log($"Rumble ramp {durationMs}ms terminé.");
        }

        // Méthodes appelées par des boutons éventuels (si présents dans le XAML)
        public void UiRumblePulse(int small, int large, int ms) => RumbleOnce((byte)small, (byte)large, ms);
        public void UiRumbleRamp(int s0, int l0, int s1, int l1, int ms) => RumbleRamp((byte)s0, (byte)l0, (byte)s1, (byte)l1, ms);

        // ==================== Thème (clair/sombre) ====================
        public bool IsDarkTheme { get => _isDarkTheme; set { _isDarkTheme = value; ApplyTheme(); N(nameof(IsDarkTheme)); } }
        bool _isDarkTheme = true;

        void ApplyTheme()
        {
            var res = Application.Current.Resources;
            if (_isDarkTheme)
            {
                res["Bg0"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F0F13");
                res["Bg1"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#14161C");
                res["Fg"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F2F8FF");
                res["Sub"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94C3EA");
            }
            else
            {
                res["Bg0"] = System.Windows.Media.Colors.White;
                res["Bg1"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F3F6FA");
                res["Fg"] = System.Windows.Media.Colors.Black;
                res["Sub"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3A6EA5");
            }
            Log("Thème appliqué: " + (_isDarkTheme ? "Sombre" : "Clair"));
        }

        // ==================== Outils pro INLINE (safe, sans champs générés) ====================
        readonly string _profilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Trident_Profiles");
        bool _uiReadyInline;

        void InitToolsInline()
        {
            _uiReadyInline = false;
            try
            {
                Directory.CreateDirectory(_profilesDir);

                // Overlay checkbox si le contrôle existe
                if (FindName("ChkOverlayInline") is CheckBox chk)
                    chk.IsChecked = OverlayEnabled;

                // Thème radios si présents
                var rDark = FindName("RDarkInline") as RadioButton;
                var rLight = FindName("RLightInline") as RadioButton;
                if (rDark != null && rLight != null)
                {
                    rDark.IsChecked = IsDarkTheme;
                    rLight.IsChecked = !IsDarkTheme;
                }

                ToolsLog("Outils prêts.");
            }
            catch (Exception ex) { Log("Init tools: " + ex.Message); }
            finally { _uiReadyInline = true; }
        }

        void ToolsLog(string msg)
        {
            try
            {
                if (FindName("TxtLastActionInline") is TextBlock tb) tb.Text = msg;
                LogLines.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
            }
            catch { }
        }

        // === Handlers “Outils pro” (branchés seulement si le XAML les déclare) ===
        void ChkOverlayInline_Checked(object sender, RoutedEventArgs e)
        {
            if (!_uiReadyInline) return;
            var chk = (sender as CheckBox) ?? (FindName("ChkOverlayInline") as CheckBox);
            ToggleOverlay(chk?.IsChecked == true);
        }
        void ThemeInline_Checked(object sender, RoutedEventArgs e)
        {
            if (!_uiReadyInline) return;
            var rDark = (sender as RadioButton) ?? (FindName("RDarkInline") as RadioButton);
            IsDarkTheme = (rDark?.IsChecked == true);
        }
        void SmlInline_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_uiReadyInline) ToolsLog($"Small = {(int)e.NewValue}");
        }
        void LrgInline_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_uiReadyInline) ToolsLog($"Large = {(int)e.NewValue}");
        }
        void DurInline_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_uiReadyInline) ToolsLog($"Durée = {(int)e.NewValue} ms");
        }
        async void PulseInline_Click(object? sender, RoutedEventArgs e)
        {
            byte sml = (byte)((FindName("SmlInline") as Slider)?.Value ?? 120);
            byte lrg = (byte)((FindName("LrgInline") as Slider)?.Value ?? 160);
            int dur = (int)((FindName("DurInline") as Slider)?.Value ?? 900);

            if (!UseXInput && (_psStream == null || !_psStream.CanWrite)) RebindIfNeeded();

            SetRumble(sml, lrg);
            await Task.Delay(Math.Max(1, dur));
            SetRumble(0, 0);

            ToolsLog($"Pulse {sml}/{lrg} {dur}ms");
        }

        async void RampInline_Click(object? sender, RoutedEventArgs e)
        {
            for (int a = 0; a <= 255; a += 15)
            {
                SetRumble((byte)a, (byte)a);
                await Task.Delay(60);
            }
            SetRumble(0, 0);
            ToolsLog("Ramp 1s");
        }

        void StopInline_Click(object? sender, RoutedEventArgs e)
        {
            SetRumble(0, 0);
            ToolsLog("Stop vibrations");
        }

        void OpenProfilesFolder_Click(object s, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(_profilesDir);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = _profilesDir, UseShellExecute = true });
            }
            catch (Exception ex) { ToolsLog("Erreur dossier: " + ex.Message); }
        }
        void ToggleOverlay(bool enabled)
        {
            if (enabled) ShowOverlay(); else HideOverlay();
            ToolsLog($"Overlay {(enabled ? "activé" : "désactivé")}");
        }
    }
}
