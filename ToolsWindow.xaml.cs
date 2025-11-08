// ToolsWindow.xaml.cs
using Microsoft.VisualBasic.Logging;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Trident.MITM
{
    public partial class ToolsWindow : Window
    {
        private readonly string _profilesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Trident_Profiles");

        // évite que le Checked/Unchecked se déclenche pendant l'init
        private bool _ready;

        public ToolsWindow()
        {
            InitializeComponent();
            Loaded += ToolsWindow_Loaded; // toute l’init UI ici
        }

        private void ToolsWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                _ready = false;

                EnsureThemeResources();
                Directory.CreateDirectory(_profilesDir);

                // état par défaut de l’overlay (on tente de le lire côté MainWindow si dispo)
                bool overlayWanted = ReadOverlayStateFromOwner();
                try { ChkOverlay.IsChecked = overlayWanted; } catch { /* évite l’exception Set IsChecked */ }

                Log("Outils prêts.");
            }
            catch (Exception ex)
            {
                Err("Initialisation : " + ex.Message);
            }
            finally
            {
                _ready = true;
            }
        }

        // --------------------- Import / Export ---------------------
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "Profils Trident (*.json)|*.json|Tous les fichiers|*.*",
                    InitialDirectory = _profilesDir
                };
                if (ofd.ShowDialog(this) != true) return;

                if (!CallOwnerMethod("ImportProfiles", ofd.FileName))
                {
                    var dst = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trident_mitm_state.json");
                    File.Copy(ofd.FileName, dst, overwrite: true);
                }
                Log($"Import : {Path.GetFileName(ofd.FileName)}");
            }
            catch (Exception ex) { Err(ex.Message); }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "Profils Trident (*.json)|*.json",
                    FileName = "trident_mitm_state.json",
                    InitialDirectory = _profilesDir
                };
                if (sfd.ShowDialog(this) != true) return;

                if (!CallOwnerMethod("ExportProfiles", sfd.FileName))
                {
                    var src = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trident_mitm_state.json");
                    if (!File.Exists(src)) throw new FileNotFoundException("Aucun état trouvé à exporter.", src);
                    File.Copy(src, sfd.FileName, overwrite: true);
                }
                Log($"Export : {Path.GetFileName(sfd.FileName)}");
            }
            catch (Exception ex) { Err(ex.Message); }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(_profilesDir);
                Process.Start(new ProcessStartInfo { FileName = _profilesDir, UseShellExecute = true });
            }
            catch (Exception ex) { Err(ex.Message); }
        }

        // --------------------- Raccourcis / Overlay ---------------------
        private void ChkOverlay_Checked(object sender, RoutedEventArgs e)
        {
            if (!_ready) return; // ignore pendant l’init

            try
            {
                bool enabled = ChkOverlay.IsChecked == true;
                if (CallOwnerMethod("ToggleOverlay", enabled))
                    Log($"Overlay {(enabled ? "activé" : "désactivé")}");
                else
                    Log("Overlay : méthode indisponible (placeholder).");
            }
            catch (Exception ex)
            {
                Err("Overlay : " + ex.Message);
                try { ChkOverlay.IsChecked = false; } catch { }
            }
        }

        // --------------------- Thème sombre/clair ---------------------
        private void Theme_Checked(object sender, RoutedEventArgs e)
        {
            ApplyTheme(RDark.IsChecked == true);
        }

        private void ApplyTheme(bool dark)
        {
            var bg0 = (Color)ColorConverter.ConvertFromString(dark ? "#0F1116" : "#F3F5F8");
            var bg1 = (Color)ColorConverter.ConvertFromString(dark ? "#151824" : "#FFFFFF");
            var fg = (Color)ColorConverter.ConvertFromString(dark ? "#F2F8FF" : "#111318");
            var sub = (Color)ColorConverter.ConvertFromString(dark ? "#94C3EA" : "#2F6AA8");

            SetRes("Bg0", bg0); SetRes("Bg1", bg1); SetRes("Fg", fg); SetRes("Sub", sub);
            Log($"Thème {(dark ? "sombre" : "clair")} appliqué.");
        }

        private void EnsureThemeResources()
        {
            if (!Application.Current.Resources.Contains("Bg0"))
            {
                SetRes("Bg0", (Color)ColorConverter.ConvertFromString("#0F1116"));
                SetRes("Bg1", (Color)ColorConverter.ConvertFromString("#151824"));
                SetRes("Fg", (Color)ColorConverter.ConvertFromString("#F2F8FF"));
                SetRes("Sub", (Color)ColorConverter.ConvertFromString("#94C3EA"));
            }
        }

        private static void SetRes(string key, Color value) => Application.Current.Resources[key] = value;


        private MainWindow MW => (MainWindow)(this.Owner ?? Application.Current.MainWindow);

        private async void Pulse_Click(object s, RoutedEventArgs e)
        {
            MW.LastMessage = $"Pulse {Sml.Value}/{Lrg.Value} {Dur.Value}ms";
            MW.SetRumble((byte)Sml.Value, (byte)Lrg.Value);
            await Task.Delay(Math.Max(50, (int)Dur.Value));
            MW.SetRumble(0, 0);
        }


        private async void Ramp_Click(object sender, RoutedEventArgs e)
        {
            byte sMax = (byte)Sml.Value, lMax = (byte)Lrg.Value;
            for (int t = 0; t <= 1000; t += 20)
            {
                double p = t < 500 ? t / 500.0 : 1.0 - (t - 500) / 500.0;
                MW.SetRumble((byte)(sMax * p), (byte)(lMax * p));
                await Task.Delay(20);
            }
            MW.SetRumble(0, 0);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            MW.SetRumble(0, 0);
        }




        private bool RunRumbleTest(byte small, byte large, int ms)
        {
            try
            {
                var mw = GetMainWindow();
                if (mw == null) { Err("MainWindow introuvable."); return false; }

                var mi = mw.GetType().GetMethod("RunRumbleTest",
                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mi == null)
                {
                    MessageBox.Show(
                        "Méthode RunRumbleTest(...) absente dans MainWindow.\n" +
                        "Ajoute une méthode qui envoie les moteurs au périphérique physique.",
                        "Test vibrations", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                mi.Invoke(mw, new object[] { small, large, ms });
                return true;
            }
            catch (Exception ex) { Err(ex.Message); return false; }
        }

        // --------------------- Utilitaires ---------------------
        private Window? GetMainWindow() =>
            Owner as Window ?? Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.GetType().Name == "MainWindow");

        private bool CallOwnerMethod(string method, object? arg = null)
        {
            try
            {
                var mw = GetMainWindow();
                if (mw == null) return false;

                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var mi = arg is null
                    ? mw.GetType().GetMethod(method, flags, null, Type.EmptyTypes, null)
                    : mw.GetType().GetMethod(method, flags, null, new[] { arg.GetType() }, null);
                if (mi == null) return false;

                _ = arg is null ? mi.Invoke(mw, null) : mi.Invoke(mw, new[] { arg });
                return true;
            }
            catch { return false; }
        }

        private bool ReadOverlayStateFromOwner()
        {
            try
            {
                var mw = GetMainWindow();
                if (mw == null) return false;

                var prop = mw.GetType().GetProperty("OverlayEnabled",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop?.PropertyType == typeof(bool))
                    return (bool)(prop.GetValue(mw) ?? false);

                var mi = mw.GetType().GetMethod("IsOverlayEnabled",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                if (mi != null && mi.ReturnType == typeof(bool))
                    return (bool)(mi.Invoke(mw, null) ?? false);
            }
            catch { /* ignore */ }
            return false;
        }

        private void Log(string msg) => Dispatcher.Invoke(() =>
        {
            try { TxtLastAction.Text = msg; LogList?.Items.Add($"{DateTime.Now:HH:mm:ss}  {msg}"); }
            catch { }
        });

        private void Err(string msg)
        {
            Log("Erreur : " + msg);
            Debug.WriteLine("[ToolsWindow] " + msg);
        }

        private void CharacterGenerator_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var charGenWindow = new CharacterGeneratorWindow
                {
                    Owner = this
                };
                charGenWindow.ShowDialog();
                Log("Générateur de personnage Call of Duty ouvert.");
            }
            catch (Exception ex)
            {
                Err("Ouverture du générateur : " + ex.Message);
            }
        }
    }
}
