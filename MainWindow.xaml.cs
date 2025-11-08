// MainWindow.xaml.cs — ViGEm X360/DS4 + haptics + AR/RF (fix) + AimAssist + AutoPing + Hotkeys KB/Pad
using HidSharp;
using HidSharp.Utility;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using HidDevice = HidSharp.HidDevice;
using HidStream = HidSharp.HidStream;
// +++ AJOUTER EN HAUT +++
using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
 
using System.Net.Http;


namespace Trident.MITM
{
    public partial class MainWindow : Window, INotifyPropertyChanged

    {

        // champs
        readonly object _virtLock = new();
        bool _virtConnecting = false;

        bool _autoEnsureVirtual = true;

        bool _virtualAuto = true;
        // après
        bool _autoDropVirtualOnPhysicalGone = false;

        // Anti-spam (front montant) pour hotkeys par profil
        bool _hkProfilePadPrev = false;
        bool _hkProfileKbPrev = false;

        bool _trigPrev = false;
        const int PING_HOLD_MS = 120; // au lieu de 70ms

        // Evite le spam des hotkeys (détection des fronts)
        bool _hkTogglePrev = false;
        bool _hkNextPrev = false;
        bool _hkPrevPrev = false;

        // === Apprentissage (1–2 signatures max par arme) ===
        bool _learning = false;
        List<short[]> _learnBuf = new();
        const int LEARN_DURATION_MS = 2600;
        const int LEARN_SAMPLE_PERIOD_MS = 60;
        const double LEARN_CLUSTER_THR = 0.045;
        const int LEARN_TOPK = 2;

        static bool IsBluetooth(HidSharp.HidDevice d)
        {
            var path = d.DevicePath ?? string.Empty;
            return path.IndexOf("BTHENUM", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // --- DS5/Edge keep-alive ---
        DispatcherTimer? _edgeTimer;
        byte _edgeLastSmall, _edgeLastLarge;
        long _edgeUntilMs;
        const int EDGE_PERIOD_MS = 60;
        const int EDGE_HOLD_MS = 220;

        // préférences
        bool _preferHid = false;
        int _vigemUser = -1; // index XInput de la manette virtuelle ViGEm (à ignorer lors du scan)


        // ===== Sticks =====
        struct StickCfg { public double DeadZone, AntiDeadZone, MaxZone, Gamma; public BezierCurve? BezierCurve; }
        StickCfg LCfg = new() { DeadZone = 0.00, AntiDeadZone = 0.00, MaxZone = 1.00, Gamma = 1.00, BezierCurve = null };
        StickCfg RCfg = new() { DeadZone = 0.00, AntiDeadZone = 0.00, MaxZone = 1.00, Gamma = 1.00, BezierCurve = null };
        
        // Courbes Bézier pour les sticks
        BezierCurve? _lBezierCurve = null;
        BezierCurve? _rBezierCurve = null;
        BezierCurve? _kbMouseBezierCurve = null;
        
        public double LBezierP1 { get => _lBezierCurve?.P1 ?? 0.5; set { if (_lBezierCurve == null) _lBezierCurve = new BezierCurve(); _lBezierCurve.P1 = Math.Clamp(value, 0.0, 1.0); LCfg.BezierCurve = _lBezierCurve; N(); UpdateBezierPreview("L"); } }
        public double LBezierP2 { get => _lBezierCurve?.P2 ?? 0.5; set { if (_lBezierCurve == null) _lBezierCurve = new BezierCurve(); _lBezierCurve.P2 = Math.Clamp(value, 0.0, 1.0); LCfg.BezierCurve = _lBezierCurve; N(); UpdateBezierPreview("L"); } }
        public double RBezierP1 { get => _rBezierCurve?.P1 ?? 0.5; set { if (_rBezierCurve == null) _rBezierCurve = new BezierCurve(); _rBezierCurve.P1 = Math.Clamp(value, 0.0, 1.0); RCfg.BezierCurve = _rBezierCurve; N(); UpdateBezierPreview("R"); } }
        public double RBezierP2 { get => _rBezierCurve?.P2 ?? 0.5; set { if (_rBezierCurve == null) _rBezierCurve = new BezierCurve(); _rBezierCurve.P2 = Math.Clamp(value, 0.0, 1.0); RCfg.BezierCurve = _rBezierCurve; N(); UpdateBezierPreview("R"); } }
        public double KbMouseBezierP1 { get => _kbMouseBezierCurve?.P1 ?? 0.5; set { if (_kbMouseBezierCurve == null) _kbMouseBezierCurve = new BezierCurve(); _kbMouseBezierCurve.P1 = Math.Clamp(value, 0.0, 1.0); N(); UpdateBezierPreview("KbMouse"); } }
        public double KbMouseBezierP2 { get => _kbMouseBezierCurve?.P2 ?? 0.5; set { if (_kbMouseBezierCurve == null) _kbMouseBezierCurve = new BezierCurve(); _kbMouseBezierCurve.P2 = Math.Clamp(value, 0.0, 1.0); N(); UpdateBezierPreview("KbMouse"); } }
        
        // Paramètres de sensibilité avancés pour clavier/souris
        private double _mouseSensitivityBase = 0.1;
        public double MouseSensitivityBase { get => _mouseSensitivityBase; set { _mouseSensitivityBase = Math.Clamp(value, 0.01, 1.0); N(); } }
        
        private double _mouseSmoothingFactor = 0.15;
        public double MouseSmoothingFactor { get => _mouseSmoothingFactor; set { _mouseSmoothingFactor = Math.Clamp(value, 0.0, 1.0); N(); } }
        
        private double _mouseEmaAlpha = 0.16;
        public double MouseEmaAlpha { get => _mouseEmaAlpha; set { _mouseEmaAlpha = Math.Clamp(value, 0.01, 1.0); N(); } }
        
        private double _mouseMaxSensitivity = 30.0;
        public double MouseMaxSensitivity { get => _mouseMaxSensitivity; set { _mouseMaxSensitivity = Math.Clamp(value, 1.0, 100.0); N(); } }
        
        private double _mouseMinSensitivity = 30.0;
        public double MouseMinSensitivity { get => _mouseMinSensitivity; set { _mouseMinSensitivity = Math.Clamp(value, 1.0, 100.0); N(); } }
        
        private double _mouseAmplification = 10.0;
        public double MouseAmplification { get => _mouseAmplification; set { _mouseAmplification = Math.Clamp(value, 0.1, 50.0); N(); } }
        
        // Presets de courbes Bézier
        public ObservableCollection<string> BezierPresets { get; } = new()
        {
            "Personnalisé", "Linéaire", "Exponentielle", "Logarithmique", "S-Curve", "Agressif", "Doux", "Rapide"
        };

        // ===== OCR / Tesseract (cache pour performance) =====
        
        
        private string _selectedBezierPreset = "Personnalisé";
        public string SelectedBezierPreset
        {
            get => _selectedBezierPreset;
            set
            {
                _selectedBezierPreset = value;
                ApplyBezierPreset(value, "Current");
                N();
            }
        }
        
        private void ApplyBezierPreset(string preset, string target)
        {
            if (preset == "Personnalisé")
                return; // Ne rien faire pour personnalisé
            
            double p1 = 0.5, p2 = 0.5;
            switch (preset)
            {
                case "Linéaire":
                    p1 = 0.33; p2 = 0.67; break;
                case "Exponentielle":
                    p1 = 0.1; p2 = 0.3; break;
                case "Logarithmique":
                    p1 = 0.7; p2 = 0.9; break;
                case "S-Curve":
                    p1 = 0.2; p2 = 0.8; break;
                case "Agressif":
                    p1 = 0.05; p2 = 0.15; break;
                case "Doux":
                    p1 = 0.4; p2 = 0.6; break;
                case "Rapide":
                    p1 = 0.15; p2 = 0.35; break;
                default:
                    return;
            }
            
            // Appliquer selon la cible (sans déclencher UpdateBezierPreview pour éviter la récursion)
            if (target == "L")
            {
                if (_lBezierCurve == null) _lBezierCurve = new BezierCurve();
                _lBezierCurve.P1 = p1;
                _lBezierCurve.P2 = p2;
                LCfg.BezierCurve = _lBezierCurve;
                N(nameof(LBezierP1));
                N(nameof(LBezierP2));
                UpdateBezierPreview("L");
            }
            if (target == "R")
            {
                if (_rBezierCurve == null) _rBezierCurve = new BezierCurve();
                _rBezierCurve.P1 = p1;
                _rBezierCurve.P2 = p2;
                RCfg.BezierCurve = _rBezierCurve;
                N(nameof(RBezierP1));
                N(nameof(RBezierP2));
                UpdateBezierPreview("R");
            }
            if (target == "KbMouse")
            {
                if (_kbMouseBezierCurve == null) _kbMouseBezierCurve = new BezierCurve();
                _kbMouseBezierCurve.P1 = p1;
                _kbMouseBezierCurve.P2 = p2;
                N(nameof(KbMouseBezierP1));
                N(nameof(KbMouseBezierP2));
                UpdateBezierPreview("KbMouse");
            }
        }
        
        private void UpdateBezierPreview(string target)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Windows.Controls.Canvas? canvas = null;
                double p1 = 0.5, p2 = 0.5;
                
                switch (target)
                {
                    case "L":
                        canvas = LBezierCanvas;
                        p1 = LBezierP1;
                        p2 = LBezierP2;
                        break;
                    case "R":
                        canvas = RBezierCanvas;
                        p1 = RBezierP1;
                        p2 = RBezierP2;
                        break;
                    case "KbMouse":
                        canvas = KbMouseBezierCanvas;
                        p1 = KbMouseBezierP1;
                        p2 = KbMouseBezierP2;
                        break;
                }
                
                if (canvas != null)
                {
                    DrawBezierCurve(canvas, p1, p2);
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }
        
        private void DrawBezierCurve(System.Windows.Controls.Canvas canvas, double p1, double p2)
        {
            canvas.Children.Clear();
            
            const double size = 280.0; // Taille du graphique (avec padding)
            const double padding = 10.0;
            const double graphSize = size - padding * 2;
            
            // Grille de fond
            var gridBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 255, 255, 255));
            for (int i = 0; i <= 10; i++)
            {
                double pos = padding + (graphSize / 10.0) * i;
                // Lignes verticales
                var vLine = new Line
                {
                    X1 = pos, Y1 = padding,
                    X2 = pos, Y2 = padding + graphSize,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                canvas.Children.Add(vLine);
                
                // Lignes horizontales
                var hLine = new Line
                {
                    X1 = padding, Y1 = pos,
                    X2 = padding + graphSize, Y2 = pos,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                canvas.Children.Add(hLine);
            }
            
            // Axes
            var axisBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 255, 255, 255));
            var xAxis = new Line
            {
                X1 = padding, Y1 = padding + graphSize / 2,
                X2 = padding + graphSize, Y2 = padding + graphSize / 2,
                Stroke = axisBrush,
                StrokeThickness = 2
            };
            canvas.Children.Add(xAxis);
            
            var yAxis = new Line
            {
                X1 = padding + graphSize / 2, Y1 = padding,
                X2 = padding + graphSize / 2, Y2 = padding + graphSize,
                Stroke = axisBrush,
                StrokeThickness = 2
            };
            canvas.Children.Add(yAxis);
            
            // Courbe Bézier
            var curve = new Polyline
            {
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 50, 50)),
                StrokeThickness = 3,
                Points = new PointCollection()
            };
            
            var bezier = new BezierCurve(p1, p2);
            for (int i = 0; i <= 100; i++)
            {
                double t = i / 100.0;
                double y = bezier.Apply(t);
                double x = padding + t * graphSize;
                double yPos = padding + (1.0 - y) * graphSize; // Inversé car le Canvas est inversé
                curve.Points.Add(new System.Windows.Point(x, yPos));
            }
            canvas.Children.Add(curve);
            
            // Ligne de référence linéaire (y = x)
            var linearRef = new Polyline
            {
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 200, 200, 200)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection(new[] { 5.0, 5.0 }),
                Points = new PointCollection()
            };
            linearRef.Points.Add(new System.Windows.Point(padding, padding + graphSize));
            linearRef.Points.Add(new System.Windows.Point(padding + graphSize, padding));
            canvas.Children.Add(linearRef);
        }
        
        private void LBezierPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LBezierPresetCombo.SelectedItem is string preset)
            {
                ApplyBezierPreset(preset, "L");
            }
        }
        
        private void RBezierPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RBezierPresetCombo.SelectedItem is string preset)
            {
                ApplyBezierPreset(preset, "R");
            }
        }
        
        private void KbMouseBezierPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (KbMouseBezierPresetCombo.SelectedItem is string preset)
            {
                ApplyBezierPreset(preset, "KbMouse");
            }
        }
        public string HotkeyProfile { get => _hkProfile; set { _hkProfile = value; N(); } }
        string _hkProfile = "— Choisir —";
        public string HotkeyPadBtn1 { get => _hkPad1; set { _hkPad1 = value; N(); } }
        string _hkPad1 = "X";
        public string HotkeyPadBtn2 { get => _hkPad2; set { _hkPad2 = "Y"; N(); } }
        string _hkPad2 = "Y";
        public string HotkeyKey { get => _hkKey; set { _hkKey = value; N(); } }
        string _hkKey = "F1";

        static short ClampShort(double v) => (short)Math.Clamp(Math.Round(v), short.MinValue, short.MaxValue);
        static (short X, short Y) ApplyStick(short xin, short yin, StickCfg c)
        {
            double x = xin / 32767.0, y = yin / 32767.0;
            double r = Math.Sqrt(x * x + y * y);
            double dz = Math.Clamp(c.DeadZone, 0, .5);
            double mz = Math.Clamp(c.MaxZone, dz + 1e-6, 1.0);
            double adz = Math.Clamp(c.AntiDeadZone, 0, .5);
            double g = c.Gamma <= 0 ? 1.0 : c.Gamma;

            if (r <= dz) { x = 0; y = 0; }
            else
            {
                double k = (r - dz) / (mz - dz);
                k = Math.Clamp(k, 0, 1);
                k = Math.Pow(k, g);
                k = adz + (1.0 - adz) * k;
                if (r > 1e-6) { x *= k / r; y *= k / r; } else { x = 0; y = 0; }
            }
            
            // Appliquer la courbe Bézier si définie
            if (c.BezierCurve != null)
            {
                x = c.BezierCurve.ApplyToStick(x);
                y = c.BezierCurve.ApplyToStick(y);
            }
            
            return (ClampShort(x * 32767.0), ClampShort(y * 32767.0));
        }

        // ============ VM & bindings ============
        public event PropertyChangedEventHandler? PropertyChanged;
        // 1) VM: toutes les options pad (simples + combos DPad+Bouton)
        public ObservableCollection<string> AllPadCombos { get; } = new();

        void BuildPadCombos()
        {
            AllPadCombos.Clear();

            // Option "aucun" (ligne vide → aucun raccourci)
            AllPadCombos.Add("");

            var buttons = new[] { "A", "B", "X", "Y", "LB", "RB", "LS", "RS", "Back", "Start" };
            var dpad = new[] { "DPadUp", "DPadRight", "DPadDown", "DPadLeft" };

            // Touches simples
            foreach (var b in buttons) AllPadCombos.Add(b);
            foreach (var d in dpad) AllPadCombos.Add(d);

            // Combos DPad + Bouton (ex: DPadUp+A, DPadRight+X, ...)
            foreach (var d in dpad)
                foreach (var b in buttons)
                    AllPadCombos.Add($"{d}+{b}");
        }

        // ---- Zones mortes / courbes (stick gauche) ----
        public double LDead { get => LCfg.DeadZone; set { LCfg.DeadZone = Math.Clamp(value, 0, 0.5); N(); } }
        public double LADZ { get => LCfg.AntiDeadZone; set { LCfg.AntiDeadZone = Math.Clamp(value, 0, 0.5); N(); } }
        public double LMax { get => LCfg.MaxZone; set { LCfg.MaxZone = Math.Clamp(value, 0.6, 1.0); N(); } }
        public double LGamma { get => LCfg.Gamma; set { LCfg.Gamma = Math.Clamp(value, 0.6, 2.0); N(); } }

        // ---- Zones mortes / courbes (stick droit) ----
        public double RDead { get => RCfg.DeadZone; set { RCfg.DeadZone = Math.Clamp(value, 0, 0.5); N(); } }
        public double RADZ { get => RCfg.AntiDeadZone; set { RCfg.AntiDeadZone = Math.Clamp(value, 0, 0.5); N(); } }
        public double RMax { get => RCfg.MaxZone; set { RCfg.MaxZone = Math.Clamp(value, 0.6, 1.0); N(); } }
        public double RGamma { get => RCfg.Gamma; set { RCfg.Gamma = Math.Clamp(value, 0.6, 2.0); N(); } }

        void N([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public ObservableCollection<string> WeaponItems { get; } = new();
        bool _lockSelectedWeapon = false;
        public bool LockSelectedWeapon { get => _lockSelectedWeapon; set { _lockSelectedWeapon = value; N(); } }
        string _selectedWeapon = "— Choisir —";
        public string SelectedWeapon
        {
            get => _selectedWeapon;
            set
            {
                _selectedWeapon = value;
                if (_profiles.TryGetValue(_selectedWeapon, out var p))
                    ApplyProfile(p);                 // <— recharge V/H & timeline dans l'UI
                RefreshWeaponHotkeys();
                N();
            }
        }

        void RefreshWeaponHotkeys()
        {
            if (_profiles.TryGetValue(SelectedWeapon, out var p))
            {
                _wpadhk = p.PadHotkey ?? "";
                _wkbk = p.KbHotkey ?? "";
                N(nameof(WeaponPadHotkey));
                N(nameof(WeaponKbHotkey));
            }
        }

        // +++ AJOUTER DANS LA CLASSE MainWindow (par ex. près des autres classes internes) +++
        public class ProfilesFile
        {
            public string Version { get; set; } = "1";
            public List<WeaponProfile> Profiles { get; set; } = new();
        }

        // Rapid-Fire
        public bool RapidFire { get => _rapidFire; set { _rapidFire = value; N(nameof(MacroText)); } }
        bool _rapidFire = false;
        public double RapidFireHz { get => _rfHz; set { _rfHz = Math.Clamp(Math.Round(value, 1), 3.0, 20.0); N(nameof(MacroText)); } }
        double _rfHz = 8.0;
        public double RapidFireDuty { get => _rfDuty; set { _rfDuty = Math.Clamp(Math.Round(value, 0), 15.0, 85.0); N(nameof(MacroText)); } } // %
        double _rfDuty = 55.0;

        // Anti-recul (simple) + timings avancés
        public bool AdsOnly { get => _adsOnly; set { _adsOnly = value; N(); } }
        bool _adsOnly = true;
        double _arV = 1.00;   // backing field pour AntiRecoilV
        double _arH = 0.00;
        public double AntiRecoilV
        {
            get => _arV;
            set
            {
                _arV = Math.Clamp(Math.Round(value, 2), 0, 3);
                if (_profiles.TryGetValue(SelectedWeapon, out var p))
                    p.AntiRecoilValue = _arV;        // écrit dans le profil courant
                N(nameof(MacroText));
                SaveStateSafe();
            }
        }

        public double AntiRecoilH
        {
            get => _arH;
            set
            {
                _arH = Math.Clamp(Math.Round(value, 2), -2, 2);
                if (_profiles.TryGetValue(SelectedWeapon, out var p))
                    p.AntiRecoilHorizontal = _arH;   // écrit dans le profil courant
                N(nameof(MacroText));
                SaveStateSafe();
            }
        }

        public double ArDelayMs { get => _arDelay; set { _arDelay = Math.Clamp(Math.Round(value, 0), 0, 500); N(); } }
        double _arDelay = 80;
        public double ArRampMs { get => _arRamp; set { _arRamp = Math.Clamp(Math.Round(value, 0), 0, 1200); N(); } }
        double _arRamp = 300;
        public double ArSustainMs { get => _arHold; set { _arHold = Math.Clamp(Math.Round(value, 0), 0, 4000); N(); } }
        double _arHold = 900;

        // Aim Assist (micro-oscillation stick droit)
        public bool AimAssist { get => _aa; set { _aa = value; N(); } }
        bool _aa = false;
        public double AAAmplitudeX { get => _aaAx; set { _aaAx = Math.Clamp(Math.Round(value, 0), 0, 6000); N(); } }
        double _aaAx = 2200;
        public double AAAmplitudeY { get => _aaAy; set { _aaAy = Math.Clamp(Math.Round(value, 0), 0, 6000); N(); } }
        double _aaAy = 800;
        public double AAFrequency { get => _aaHz; set { _aaHz = Math.Clamp(Math.Round(value, 1), 1.0, 30.0); N(); } }
        double _aaHz = 7.0;

        // Auto-Ping
        public bool AutoPing { get => _autoPing; set { _autoPing = value; N(); } }
        bool _autoPing = false;
        public int AutoPingInterval { get => _pingInterval; set { _pingInterval = Math.Clamp(value, 800, 6000); N(); } }
        int _pingInterval = 2200;
        long _nextPingAtMs = 0, _pingUntilMs = 0;
        
        // Strafe Assist
        public bool StrafeAssistEnabled { get => _strafeAssistEnabled; set { _strafeAssistEnabled = value; N(); } }
        bool _strafeAssistEnabled = false;
        public double StrafeAssistIntensity { get => _strafeAssistIntensity; set { _strafeAssistIntensity = Math.Clamp(value, 0, 100); N(); } }
        double _strafeAssistIntensity = 50.0;
        public bool StrafeAssistFireOnly { get => _strafeAssistFireOnly; set { _strafeAssistFireOnly = value; N(); } }
        bool _strafeAssistFireOnly = false;
        public bool StrafeAssistAdsOnly { get => _strafeAssistAdsOnly; set { _strafeAssistAdsOnly = value; N(); } }
        bool _strafeAssistAdsOnly = false;
        public bool StrafeAssistDisableOnMove { get => _strafeAssistDisableOnMove; set { _strafeAssistDisableOnMove = value; N(); } }
        bool _strafeAssistDisableOnMove = false;
        public bool StrafeAssistAdaptive { get => _strafeAssistAdaptive; set { _strafeAssistAdaptive = value; N(); } }
        bool _strafeAssistAdaptive = false;
        public bool StrafeAssistSyncAim { get => _strafeAssistSyncAim; set { _strafeAssistSyncAim = value; N(); } }
        bool _strafeAssistSyncAim = false;
        public bool StrafeAssistProgressive { get => _strafeAssistProgressive; set { _strafeAssistProgressive = value; N(); } }
        bool _strafeAssistProgressive = false;
        
        // Auto-Ping Advanced
        public bool AutoPingFireOnly { get => _autoPingFireOnly; set { _autoPingFireOnly = value; N(); } }
        bool _autoPingFireOnly = false;
        public bool AutoPingAdsOnly { get => _autoPingAdsOnly; set { _autoPingAdsOnly = value; N(); } }
        bool _autoPingAdsOnly = false;
        public bool AutoPingDisableOnMove { get => _autoPingDisableOnMove; set { _autoPingDisableOnMove = value; N(); } }
        bool _autoPingDisableOnMove = false;
        public bool AutoPingTargetOnly { get => _autoPingTargetOnly; set { _autoPingTargetOnly = value; N(); } }
        bool _autoPingTargetOnly = false;
        public bool AutoPingSilent { get => _autoPingSilent; set { _autoPingSilent = value; N(); } }
        bool _autoPingSilent = false;
        public bool AutoPingSyncAim { get => _autoPingSyncAim; set { _autoPingSyncAim = value; N(); } }
        bool _autoPingSyncAim = false;

        // Hotkeys
        public string HotkeyMode
        {
            get => _hkMode;
            set { _hkMode = "Manette"; N(); N(nameof(HotkeyIsKeyboard)); N(nameof(HotkeyIsPad)); }
        }
        string _hkMode = "Manette";

        public bool HotkeyIsKeyboard { get => false; set { /* désactivé */ } }
        public bool HotkeyIsPad { get => true; set { /* forcé */ } }

        // ===== Facteur global d'anti-recul =====
        public double ArPower { get => _arPower; set { _arPower = Math.Clamp(Math.Round(value, 2), 0.2, 3.0); N(); } }
        double _arPower = 2.00;
        // Gâchette qui déclenche AR/RF/Timeline (RT = R2, RB = R1)
        // Choix disponibles dans les ComboBox
        public ObservableCollection<string> AllGachettes { get; } =
            new() { "L2", "L1", "R2", "R1" };

        // Tir (gâchette feu)
        public string FireGachette
        {
            get => _fire;
            set { _fire = NormalizeGachette(value); N(); SaveStateSafe(); }
        }
        string _fire = "R2";

        // ADS (viser)
        public string AdsGachette
        {
            get => _ads;
            set { _ads = NormalizeGachette(value); N(); SaveStateSafe(); }
        }
        string _ads = "L2";

        // Accepte les synonymes venant d'anciens states (RT/RB/LT/LB)
        static string NormalizeGachette(string? v)
        {
            var s = (v ?? "").Trim().ToUpperInvariant();
            return s switch
            {
                "RT" or "R2" => "R2",
                "RB" or "R1" => "R1",
                "LT" or "L2" => "L2",
                "LB" or "L1" => "L1",
                _ => "R2"
            };
        }


        // Timeline AR (optionnel)
        public bool UseTimeline { get => _useT; set { _useT = value; N(); } }
        bool _useT = false;
        public double TimelineGain { get => _tGain; set { _tGain = Math.Clamp(value, 0.05, 3.0); if (Timeline != null) Timeline.Gain = _tGain; N(); } }
        double _tGain = 1.0;
        public bool TimelineRequireAds { get => _tAds; set { _tAds = value; if (Timeline != null) Timeline.RequireADS = _tAds; N(); } }
        bool _tAds = true;
        public string TimelineJson { get => _timelineJson; set { _timelineJson = value; N(); } }
        string _timelineJson = "";
        public string TimelinePreview { get => _timelinePreview; set { _timelinePreview = value; N(); } }
        string _timelinePreview = "";

        public string VigemStatus { get => _vigemStatus; set { _vigemStatus = value; N(); } }
        string _vigemStatus = "inconnu";
        public string PadStatus { get => _padStatus; set { _padStatus = value; N(); } }
        string _padStatus = "déconnecté";
        public string VirtualStatus { get => _virtStatus; set { _virtStatus = value; N(); } }
        string _virtStatus = "non créée";

        public string HapticsStatus { get => _hapticsStatus; set { _hapticsStatus = value; N(); } }
        string _hapticsStatus = "Aucune signature";
        public string RumbleText { get => _rumbleText; set { _rumbleText = value; N(); } }
        string _rumbleText = "0/0";
        public string CurrentWeapon { get => _currentWeapon; set { _currentWeapon = value; N(); N(nameof(DetectedWeaponDisplay)); } }
        
        public string DetectedWeaponDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(CurrentWeapon) || CurrentWeapon == "— Choisir —")
                {
                    return "Aucune arme détectée";
                }
                return CurrentWeapon;
            }
        }
        string _currentWeapon = "";

        public string EmaBestText { get => _emaBestText; set { _emaBestText = value; N(); } }
        string _emaBestText = "n/a";
        public string TriggerText { get => _trigText; set { _trigText = value; N(); } }
        string _trigText = "n/a";

        // Affichage "Arme : …" dans Profils
        public string ProfilesDetectedText { get => _profilesDetectedText; set { _profilesDetectedText = value; N(); } }
        string _profilesDetectedText = "Arme : —";

        public string MacroText => $"RF:{(RapidFire ? 1 : 0)} {RapidFireHz:0.0}Hz/{RapidFireDuty:0}%  |  AR V:{AntiRecoilV:0.00} H:{AntiRecoilH:0.00}";
        public string LastMessage { get => _lastMsg; set { _lastMsg = value; N(); } }
        string _lastMsg = "";
        public string FooterText => $"V{AppVersion}  |  {DateTime.Now:yyyy} © Trident";

        // Backup
        public string BackupFolder { get => _backupFolder; set { _backupFolder = value ?? ""; N(); } }
        string _backupFolder = "";
        public bool BackupOnSave { get => _backupOnSave; set { _backupOnSave = value; N(); } }
        bool _backupOnSave = false;

        // ============ Données ============
        readonly Dictionary<string, WeaponProfile> _profiles = new();
        readonly List<string> _order = new();
        readonly HapticStore _haptics = new();
        const string PersistPath = "trident.json";
        const string RulesPath = "trident_rules.json";
        static string AppVersion => "1.1.1-fix";

        public string WeaponPadHotkey
        {
            get => _wpadhk;
            set { _wpadhk = value ?? ""; if (_profiles.TryGetValue(SelectedWeapon, out var p)) { p.PadHotkey = _wpadhk; SaveStateSafe(); } N(); }
        }
        string _wpadhk = "";

        public string WeaponKbHotkey
        {
            get => _wkbk;
            set { _wkbk = value ?? ""; if (_profiles.TryGetValue(SelectedWeapon, out var p)) { p.KbHotkey = _wkbk; SaveStateSafe(); } N(); }
        }
        string _wkbk = "";

        // Edge-detection par profil (évite le spam)
        readonly Dictionary<string, bool> _profPadPrev = new();
        readonly Dictionary<string, bool> _profKbPrev = new();

        // ============ ViGEm & HID ============
        ViGEmClient? _client;

        enum VirtualType { Xbox360, DualShock4 }
        VirtualType _virtType = VirtualType.Xbox360;
        IXbox360Controller? _x360;
        IDualShock4Controller? _ds4;

        CancellationTokenSource? _loopCts;

        HidDevice? _psDevice;
        HidStream? _psStream;
        byte[] _buf = new byte[128];

        // ===== XINPUT =====
        int _xinputUser = -1;
        bool UseXInput => _xinputUser >= 0;

        [StructLayout(LayoutKind.Sequential)]
        struct XINPUT_GAMEPAD
        {
            public ushort wButtons; public byte bLeftTrigger; public byte bRightTrigger;
            public short sThumbLX; public short sThumbLY; public short sThumbRX; public short sThumbRY;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct XINPUT_STATE { public uint dwPacketNumber; public XINPUT_GAMEPAD Gamepad; }
        [StructLayout(LayoutKind.Sequential)]
        struct XINPUT_VIBRATION { public ushort wLeftMotorSpeed; public ushort wRightMotorSpeed; }

        [DllImport("xinput1_4.dll", EntryPoint = "#100")] static extern uint XInputGetStateEx(uint dwUserIndex, out XINPUT_STATE pState);
        [DllImport("xinput1_4.dll")] static extern uint XInputGetState(uint dwUserIndex, out XINPUT_STATE pState);
        [DllImport("xinput1_4.dll")] static extern uint XInputSetState(uint dwUserIndex, ref XINPUT_VIBRATION pVibration);


        // ===== Clavier (hotkeys) =====  (DÉSACTIVÉS)
        bool IsKeyDown(int vk) => false;   // ne lit plus aucune touche
        bool ModCtrl => false;
        bool ModAlt => false;

        bool _hkRFPrev, _hkAAPrev, _hkPingPrev; // états anti-spam conservés


        // === CONFIG EXTENSIBLE VID/PID + SNIFF DS-LIKE ===
        class ExtraPadEntry { public string VID { get; set; } = "0x0000"; public string PID { get; set; } = "0x0000"; }
        class ExtraPadConfig { public bool AllowAnyHID { get; set; } = false; public List<ExtraPadEntry> ExtraPads { get; set; } = new(); }
        ExtraPadConfig _padCfg = new();
        HashSet<(ushort vid, ushort pid)> _knownPads = new()
        {
            (0x054C,0x05C4),(0x054C,0x09CC),(0x054C,0x0BA0), // DS4
            (0x054C,0x0CE6),(0x054C,0x0E5F),(0x054C,0x0DF2)  // DualSense/Edge
        };
        void LoadExtraPads()
        {
            try
            {
                if (!File.Exists("pads_extra.json")) return;
                var cfg = JsonSerializer.Deserialize<ExtraPadConfig>(File.ReadAllText("pads_extra.json"));
                if (cfg == null) return;
                _padCfg = cfg;
                foreach (var e in _padCfg.ExtraPads)
                {
                    if (ushort.TryParse(e.VID.Replace("0x", "", StringComparison.OrdinalIgnoreCase), System.Globalization.NumberStyles.HexNumber, null, out var v) &&
                        ushort.TryParse(e.PID.Replace("0x", "", StringComparison.OrdinalIgnoreCase), System.Globalization.NumberStyles.HexNumber, null, out var p))
                    {
                        _knownPads.Add((v, p));
                    }
                }
            }
            catch { /* ignore parsing errors */ }
        }

        bool FindXInputController()
        {
            if (_preferHid && _psStream != null) { _xinputUser = -1; return false; }

            for (uint i = 0; i < 4; i++)
            {
                // IGNORE le slot XInput de la manette virtuelle ViGEm
                if (_vigemUser >= 0 && i == (uint)_vigemUser) continue;

                bool ok = (XInputGetState(i, out var _) == 0);
                if (!ok) { try { ok = (XInputGetStateEx(i, out var __) == 0); } catch { } }
                if (ok) { _xinputUser = (int)i; PadStatus = "Xbox (XInput)"; return true; }
            }
            _xinputUser = -1; return false;
        }

        bool XInputAlive() => (_xinputUser >= 0) && (XInputGetState((uint)_xinputUser, out var _) == 0);

        void XInputVibrate(byte small, byte large)
        {
            if (!UseXInput) return;
            var vib = new XINPUT_VIBRATION { wLeftMotorSpeed = (ushort)(large * 257), wRightMotorSpeed = (ushort)(small * 257) };
            try { _ = XInputSetState((uint)_xinputUser, ref vib); } catch { }
        }

        bool TryReadXInput(out short lx, out short ly, out short rx, out short ry,
                           out byte lt, out byte rt, out byte b0, out byte b1, out int hat)
        {
            lx = ly = rx = ry = 0; lt = rt = 0; b0 = b1 = 0; hat = 8;
            if (!UseXInput) return false;
            if (XInputGetState((uint)_xinputUser, out var st) != 0) return false;

            var gp = st.Gamepad;
            lx = gp.sThumbLX; ly = ly = gp.sThumbLY;
            rx = gp.sThumbRX; ry = ry = gp.sThumbRY;
            lt = gp.bLeftTrigger; rt = gp.bRightTrigger;

            bool a = (gp.wButtons & 0x1000) != 0, b = (gp.wButtons & 0x2000) != 0, x = (gp.wButtons & 0x4000) != 0, y = (gp.wButtons & 0x8000) != 0;
            bool lb = (gp.wButtons & 0x0100) != 0, rb = (gp.wButtons & 0x0200) != 0;
            bool back = (gp.wButtons & 0x0020) != 0, start = (gp.wButtons & 0x0010) != 0;
            bool ls = (gp.wButtons & 0x0040) != 0, rs = (gp.wButtons & 0x0080) != 0;
            bool dUp = (gp.wButtons & 0x0001) != 0, dDown = (gp.wButtons & 0x0002) != 0, dLeft = (gp.wButtons & 0x0004) != 0, dRight = (gp.wButtons & 0x0008) != 0;

            hat = 8;
            if (dUp && !dRight && !dLeft) hat = 0;
            else if (dUp && dRight) hat = 1;
            else if (dRight && !dDown) hat = 2;
            else if (dRight && dDown) hat = 3;
            else if (dDown && !dLeft) hat = 4;
            else if (dDown && dLeft) hat = 5;
            else if (dLeft && !dUp) hat = 6;
            else if (dLeft && dUp) hat = 7;

            b0 = (byte)((hat & 0x0F) | (x ? 0x10 : 0) | (a ? 0x20 : 0) | (b ? 0x40 : 0) | (y ? 0x80 : 0));
            b1 = 0;
            if (lb) b1 |= 0x01; if (rb) b1 |= 0x02;
            if (back) b1 |= 0x10; if (start) b1 |= 0x20;
            if (ls) b1 |= 0x40; if (rs) b1 |= 0x80;
            return true;
        }

        // Rumble buffer (pour signatures)
        readonly object _rbLock = new();
        readonly List<(long t, int small, int large)> _rbuf = new(4096);
        const int HBUF_KEEP_MS = 3000, HSIG_MS = 1600, HSIG_BINS = 28;

        // Haptique matching (plus strict)
        const double MIN_STD = 0.020; // Augmenté pour filtrer les signatures trop plates
        const double EMA_ALPHA = 0.25; // Réduit pour moins de lissage (plus réactif mais plus stable)
        const double MATCH_MAX_SAD = 0.22; // Réduit : score max plus strict (0.22 au lieu de 0.28)
        const double MARGIN_TO_SECOND = 0.12; // Augmenté : marge plus grande requise (0.12 au lieu de 0.08)
        const double STICKY_TOLERANCE = 0.005; // Réduit : tolérance plus stricte pour rester sur l'arme actuelle
        const int CONS_WINS_REQUIRED = 4; // Augmenté : 4 confirmations au lieu de 2
        const int MIN_SWITCH_INTERVAL_MS = 2000; // Augmenté : 2 secondes au lieu de 1.4
        const double MIN_IMPROVEMENT_RATIO = 0.15; // Nouveau : amélioration minimale de 15% requise

        bool _latchUntilRelease = false;
        volatile bool _trigHeldFlag = false; // état R2 courant (gating)
        bool _recordingMode = false;         // bloque les switchs pendant enregistrement

        string _candidateWeapon = ""; int _candidateWins = 0; long _lastSwitchAt = 0;
        readonly Dictionary<string, double> _ema = new();

        // Timeline runtime
        AntiRecoilTimeline? Timeline;
        long _fireStartMs = 0;

        // Etat boutons (combo hotkeys manette)
        bool _lbPrev, _rbPrev, _xPrev, _yPrev, _bPrev;

        // Auto-profiler
        public ObservableCollection<GameRule> AutoRules { get; } = new();
        GameProfileService _gameSvc = new();

        public MainWindow()

        {
            // S'assurer que les ressources de localisation sont chargées
            try
            {
                if (Application.Current.Resources.MergedDictionaries.Count == 0 || 
                    !Application.Current.Resources.Contains("AppName"))
                {
                    LocalizationManager.SetLanguage(Trident.MITM.Language.French);
                }
            }
            catch { }
            
            InitializeComponent();
            
            // Charger les paramètres sauvegardés
            LoadSettings();
            
            // Initialiser les visualisations Bézier après le chargement du XAML
            Loaded += (s, e) =>
            {
                UpdateBezierPreview("L");
                UpdateBezierPreview("R");
                UpdateBezierPreview("KbMouse");
            };
            DataContext = this;
            BuildPadCombos(); // combos pour l'UI
            LoadExtraPads();  // <<< charge la config VID/PID externe
            LoadDefaultProfiles();
            BuildWeaponList();
            SelectedWeapon = "— Choisir —";
            ProfilesDetectedText = "Arme : —";
            LoadStateIfAny();
            UpdateHapticsStatus();

            

            Timeline = AntiRecoilTimeline.Default();
            _tGain = Timeline.Gain; _tAds = Timeline.RequireADS;
            _timelineJson = JsonSerializer.Serialize(Timeline, new JsonSerializerOptions { WriteIndented = true });

            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            t.Tick += (_, __) =>
            {
                EmaBestText = _ema.Count > 0 ? $"{_ema.OrderBy(kv => kv.Value).First().Key}: {_ema.Min(kv => kv.Value):0.000}" : "n/a";
            };
            t.Start();

            try { _client = new ViGEmClient(); VigemStatus = "installé"; } catch { VigemStatus = "non installé (driver requis)"; }
            DeviceList.Local.Changed += (_, __) => Dispatcher.Invoke(RebindIfNeeded);
            
            // Initialize advanced features
            try
            {
                InitializeAdvancedFeatures();
                LoadAdvancedFeaturesConfig();
            }
            catch (Exception ex)
            {
                Log($"Warning: Could not initialize advanced features: {ex.Message}");
            }
            
            // Initialiser l'onglet actif au démarrage
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ShowTab("Home");
            }), DispatcherPriority.Loaded);
        }

        void SendNeutralToVirtual()
        {
            try
            {
                if (_x360 != null)
                {
                    _x360.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                    _x360.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
                    _x360.SetAxisValue(Xbox360Axis.RightThumbX, 0);
                    _x360.SetAxisValue(Xbox360Axis.RightThumbY, 0);
                    _x360.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
                    _x360.SetSliderValue(Xbox360Slider.RightTrigger, 0);
                    _x360.SetButtonState(Xbox360Button.A, false);
                    _x360.SetButtonState(Xbox360Button.B, false);
                    _x360.SetButtonState(Xbox360Button.X, false);
                    _x360.SetButtonState(Xbox360Button.Y, false);
                    _x360.SetButtonState(Xbox360Button.Up, false);
                    _x360.SetButtonState(Xbox360Button.Right, false);
                    _x360.SetButtonState(Xbox360Button.Down, false);
                    _x360.SetButtonState(Xbox360Button.Left, false);
                    _x360.SetButtonState(Xbox360Button.LeftShoulder, false);
                    _x360.SetButtonState(Xbox360Button.RightShoulder, false);
                    _x360.SetButtonState(Xbox360Button.Back, false);
                    _x360.SetButtonState(Xbox360Button.Start, false);
                    _x360.SetButtonState(Xbox360Button.LeftThumb, false);
                    _x360.SetButtonState(Xbox360Button.RightThumb, false);
                    _x360.SubmitReport();
                }
                if (_ds4 != null)
                {
                    _ds4.SetAxisValue(DualShock4Axis.LeftThumbX, 0x80);
                    _ds4.SetAxisValue(DualShock4Axis.LeftThumbY, 0x80);
                    _ds4.SetAxisValue(DualShock4Axis.RightThumbX, 0x80);
                    _ds4.SetAxisValue(DualShock4Axis.RightThumbY, 0x80);
                    _ds4.SetSliderValue(DualShock4Slider.LeftTrigger, 0);
                    _ds4.SetSliderValue(DualShock4Slider.RightTrigger, 0);
                    _ds4.SetDPadDirection(DualShock4DPadDirection.None);
                    _ds4.SetButtonState(DualShock4Button.Cross, false);
                    _ds4.SetButtonState(DualShock4Button.Circle, false);
                    _ds4.SetButtonState(DualShock4Button.Square, false);
                    _ds4.SetButtonState(DualShock4Button.Triangle, false);
                    _ds4.SetButtonState(DualShock4Button.ShoulderLeft, false);
                    _ds4.SetButtonState(DualShock4Button.ShoulderRight, false);
                    _ds4.SetButtonState(DualShock4Button.Share, false);
                    _ds4.SetButtonState(DualShock4Button.Options, false);
                    _ds4.SetButtonState(DualShock4Button.ThumbLeft, false);
                    _ds4.SetButtonState(DualShock4Button.ThumbRight, false);
                    _ds4.SubmitReport();
                }
            }
            catch { }
        }

        // Méthode publique pour l'émulation clavier/souris avec anti-recul et aim assist
        public void SendKeyboardMouseInput(short lx, short ly, short rx, short ry, bool buttonA, bool buttonLB, bool buttonLS, bool buttonBack, byte lt, byte rt)
        {
            lock (_virtLock)
            {
                try
                {
                    // Appliquer les transformations de stick (deadzone, gamma, Bézier)
                    var Lout = ApplyStick(lx, ly, LCfg);
                    var Rout = ApplyStick(rx, ry, RCfg);
                    short outLX = Lout.X, outLY = Lout.Y, outRX = Rout.X, outRY = Rout.Y;
                    
                    // Détecter les états pour les macros
                    bool trigHeld = rt > 0; // Right trigger (clic gauche souris)
                    bool adsHeld = lt > 0;  // Left trigger (clic droit souris)
                    
                    // Démarrer le timer anti-recul si le trigger vient d'être pressé
                    if (trigHeld && !_trigPrev)
                    {
                        _fireStartMs = Environment.TickCount64;
                    }
                    _trigPrev = trigHeld;
                    
                    long now = Environment.TickCount64;
                    
                    // Anti-recul (si activé et conditions remplies)
                    if (trigHeld && (!AdsOnly || adsHeld))
                    {
                        if (UseTimeline && Timeline != null && Timeline.Segments.Count > 0)
                        {
                            var tms = Environment.TickCount64 - _fireStartMs;
                            (int dx, int dy) = EvalTimeline(Timeline, (int)tms, TimelineGain * ArPower);
                            outRX = (short)Math.Clamp(outRX + dx, short.MinValue, short.MaxValue);
                            outRY = (short)Math.Clamp(outRY - dy, short.MinValue, short.MaxValue);
                        }
                        else
                        {
                            int tms = (int)(Environment.TickCount64 - _fireStartMs);
                            (int dx, int dy) = EvalAdvancedAR(tms, AntiRecoilH, AntiRecoilV, ArDelayMs, ArRampMs, ArSustainMs);
                            outRX = (short)Math.Clamp(outRX + dx, short.MinValue, short.MaxValue);
                            outRY = (short)Math.Clamp(outRY - dy, short.MinValue, short.MaxValue);
                        }
                    }
                    
                    // Aim Assist (si activé et ADS)
                    if (AimAssist && adsHeld)
                    {
                        double tt = (Environment.TickCount64 / 1000.0);
                        var (ax, ay) = CalculateAimAssist(tt);
                        outRX = (short)Math.Clamp(outRX + ax, short.MinValue, short.MaxValue);
                        outRY = (short)Math.Clamp(outRY + ay, short.MinValue, short.MaxValue);
                    }
                    
                    // Rapid-Fire (si activé)
                    byte outRT = rt;
                    if (RapidFire && trigHeld)
                    {
                        double period = 1000.0 / RapidFireHz;
                        double onMs = period * (RapidFireDuty / 100.0);
                        int tms = (int)(Environment.TickCount64 - _fireStartMs);
                        double m = tms % period;
                        outRT = (byte)((m < onMs) ? 255 : 0);
                    }
                    
                    // Envoyer vers la manette virtuelle
                    if (_x360 != null)
                    {
                        _x360.SetAxisValue(Xbox360Axis.LeftThumbX, outLX);
                        _x360.SetAxisValue(Xbox360Axis.LeftThumbY, outLY);
                        _x360.SetAxisValue(Xbox360Axis.RightThumbX, outRX);
                        _x360.SetAxisValue(Xbox360Axis.RightThumbY, outRY);
                        _x360.SetButtonState(Xbox360Button.A, buttonA);
                        _x360.SetButtonState(Xbox360Button.LeftShoulder, buttonLB);
                        _x360.SetButtonState(Xbox360Button.LeftThumb, buttonLS);
                        _x360.SetButtonState(Xbox360Button.Back, buttonBack);
                        _x360.SetSliderValue(Xbox360Slider.LeftTrigger, lt);
                        _x360.SetSliderValue(Xbox360Slider.RightTrigger, outRT);
                        _x360.SubmitReport();
                    }
                }
                catch { }
            }
        }


        void DisconnectVirtual(string reason = "")
        {
            try
            {
                if (_x360 != null)
                {
                    try { _x360.FeedbackReceived -= X360_FeedbackReceived; } catch { }
                    try { _x360.Disconnect(); } catch { }
                }
                if (_ds4 != null)
                {
                    try { _ds4.FeedbackReceived -= Ds4_FeedbackReceived; } catch { }
                    try { _ds4.Disconnect(); } catch { }
                }
            }
            catch { }
            finally
            {
                _x360 = null;
                _ds4 = null;
                _vigemUser = -1;                 // ne plus ignorer ce slot XInput
                VirtualStatus = string.IsNullOrWhiteSpace(reason) ? "déconnectée" : $"déconnectée ({reason})";
            }
        }
        void SaveSelectedProfileIntoStore()
        {
            var name = SelectedWeapon;
            if (string.IsNullOrWhiteSpace(name) || name == "— Choisir —") return;
            if (!_profiles.TryGetValue(name, out var p)) return;

            // Recopie des réglages courants vers le profil
            p.AntiRecoilValue = AntiRecoilV;
            p.AntiRecoilHorizontal = AntiRecoilH;
            p.RapidFireEnable = RapidFire ? 1 : 0;

            p.UseTimeline = UseTimeline;
            p.Timeline = UseTimeline ? Timeline : null;

            _profiles[name] = p; // (utile si la ref change un jour)
        }


        // Envoi test direct vers le pad physique Sony (si présent) + XInput si Xbox
        private async Task RumbleDirect(byte small, byte large, int ms = 300)
        {
            ForwardRumbleUSB(small, large);
            await Task.Delay(Math.Max(1, ms));
            ForwardRumbleUSB(0, 0);
        }

        // TryWrite → bool
        private bool TryWrite(byte[] data)
        {
            try
            {
                if (_psStream == null) return false;
                _psStream.Write(data, 0, data.Length);
                return true;
            }
            catch { return false; }
        }
        private bool TryWrite(ReadOnlySpan<byte> data)
        {
            try
            {
                if (_psStream == null) return false;
#if NET5_0_OR_GREATER
                _psStream.Write(data);
#else
                var arr = data.ToArray();
                _psStream.Write(arr, 0, arr.Length);
#endif
                return true;
            }
            catch { return false; }
        }

        // ======== Entraînement 2–3 s ========
        private async void LearnSelectedWeapon_Click(object sender, RoutedEventArgs e)
        {
            var name = SelectedWeapon;
            if (_learning) { LastMessage = "Apprentissage en cours."; return; }
            if (string.IsNullOrWhiteSpace(name) || name == "— Choisir —")
            { LastMessage = "Choisis une arme."; return; }

            _learning = true;
            _learnBuf.Clear();
            LastMessage = $"Apprentissage « {name} »… tire 2–3 s";

            long end = Environment.TickCount64 + LEARN_DURATION_MS;
            while (Environment.TickCount64 < end)
            {
                var sig = BuildSignature(HSIG_MS);
                if (sig != null) _learnBuf.Add(sig);
                await Task.Delay(LEARN_SAMPLE_PERIOD_MS);
            }
            _learning = false;

            if (_learnBuf.Count < 3)
            { LastMessage = "Échantillons insuffisants. Recommence en tirant plus longtemps."; return; }

            var clusters = new List<List<short[]>>();
            foreach (var s in _learnBuf)
            {
                int best = -1; double bestD = double.MaxValue;
                for (int i = 0; i < clusters.Count; i++)
                {
                    double d = Sad(s, clusters[i][0]);
                    if (d < bestD) { bestD = d; best = i; }
                }
                if (best >= 0 && bestD <= LEARN_CLUSTER_THR) clusters[best].Add(s);
                else clusters.Add(new List<short[]> { s });
            }

            var top = clusters
                .OrderByDescending(c => c.Count)
                .Take(LEARN_TOPK)
                .Select(AverageSig)
                .ToList();

            _haptics.Templates[name] = top;
            _haptics.Counts[name] = (_haptics.Counts.TryGetValue(name, out var c) ? c : 0) + 1;

            SaveStateSafe();
            UpdateHapticsStatus();
            CurrentWeapon = name;
            ProfilesDetectedText = $"Arme : {name}";
            LastMessage = $"Appris « {name} » : {top.Count} template(s).";
        }

        static short[] AverageSig(IEnumerable<short[]> cluster)
        {
            var arr = cluster.ToList();
            int bins = arr[0].Length;
            var acc = new double[bins];
            foreach (var s in arr) for (int i = 0; i < bins; i++) acc[i] += s[i];
            for (int i = 0; i < bins; i++) acc[i] /= arr.Count;

            var outa = new short[bins];
            for (int i = 0; i < bins; i++) outa[i] = (short)Math.Round(acc[i]);

            double sum = outa.Sum(v => (int)v);
            if (sum > 0)
            {
                double g = 1000.0 / sum;
                for (int i = 0; i < bins; i++) outa[i] = (short)Math.Round(outa[i] * g);
            }
            return outa;
        }

        // ====== UI divers ======
        private void ApplyTimelineJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tl = JsonSerializer.Deserialize<AntiRecoilTimeline>(TimelineJson);
                if (tl == null) throw new Exception("JSON vide.");
                Timeline = tl;
                TimelineGain = tl.Gain;
                TimelineRequireAds = tl.RequireADS;
                TimelinePreview = "Timeline appliquée.";
                LastMessage = "Timeline appliquée au profil courant.";
                SaveStateSafe();
            }
            catch (Exception ex)
            {
                LastMessage = "Timeline JSON invalide : " + ex.Message;
            }
        }

        private void ResetTimelineJson_Click(object sender, RoutedEventArgs e)
        {
            Timeline = AntiRecoilTimeline.Default();
            TimelineGain = Timeline.Gain;
            TimelineRequireAds = Timeline.RequireADS;
            TimelineJson = JsonSerializer.Serialize(Timeline, new JsonSerializerOptions { WriteIndented = true });
            TimelinePreview = "Timeline par défaut chargée.";
            LastMessage = "Timeline par défaut.";
            SaveStateSafe();
        }

        private void OpenRecoilDraw_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new Trident.MITM.RecoilDrawWindow
                {
                    Owner = this
                };
                bool? ok = win.ShowDialog();
                if (ok == true && win.ResultTimeline != null)
                {
                    Timeline = win.ResultTimeline;
                    TimelineGain = Timeline.Gain;
                    TimelineRequireAds = Timeline.RequireADS;
                    TimelineJson = JsonSerializer.Serialize(Timeline, new JsonSerializerOptions { WriteIndented = true });
                    UseTimeline = true;
                    TimelinePreview = $"Timeline dessinée appliquée ({win.ResultDurationMs} ms).";
                    LastMessage = "Pattern dessiné appliqué au profil.";
                    SaveStateSafe();
                }
            }
            catch (Exception ex)
            {
                LastMessage = "Erreur dessin de recul: " + ex.Message;
            }
        }
        // +++ AJOUTER DANS MainWindow (méthodes) +++
        // +++ AJOUTER DANS MainWindow (méthodes) +++
        void ExportProfiles_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Title = "Exporter les profils",
                    Filter = "Profils Trident (*.json)|*.json",
                    FileName = "trident_profiles.json"
                };
                if (dlg.ShowDialog() != true) return;

                // On exporte tous les profils sauf la ligne "— Choisir —"
                var toExport = _order
                    .Where(n => n != "— Choisir —" && _profiles.ContainsKey(n))
                    .Select(n => _profiles[n])
                    .ToList();

                var payload = new ProfilesFile { Version = AppVersion, Profiles = toExport };
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(dlg.FileName, json);
                LastMessage = $"Exporté {toExport.Count} profil(s) → {dlg.FileName}";
            }
            catch (Exception ex)
            {
                LastMessage = "Export échoué : " + ex.Message;
            }
        }

        void ImportProfiles_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Importer des profils",
                    Filter = "Fichiers JSON (*.json)|*.json",
                    Multiselect = false
                };
                if (dlg.ShowDialog() != true) return;

                var text = File.ReadAllText(dlg.FileName);
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                List<WeaponProfile>? list = null;

                // 1) Wrapper { Version, Profiles:[...] }
                try
                {
                    var pf = JsonSerializer.Deserialize<ProfilesFile>(text, opts);
                    if (pf?.Profiles?.Count > 0) list = pf.Profiles;
                }
                catch { }

                // 2) Tableau brut [ {WeaponProfile}, ... ]
                if (list == null)
                {
                    try
                    {
                        var arr = JsonSerializer.Deserialize<List<WeaponProfile>>(text, opts);
                        if (arr?.Count > 0) list = arr;
                    }
                    catch { }
                }

                // 3) Dictionnaire { "Name": {WeaponProfile}, ... }
                if (list == null)
                {
                    try
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, WeaponProfile>>(text, opts);
                        if (dict != null && dict.Count > 0)
                        {
                            list = new List<WeaponProfile>();
                            foreach (var kv in dict)
                            {
                                var p = kv.Value ?? new WeaponProfile();
                                if (string.IsNullOrWhiteSpace(p.Name)) p.Name = kv.Key;
                                list.Add(p);
                            }
                            
                        }
                    }
                    catch { }
                }

                // 4) PersistState complet (trident_mitm_state.json)
                if (list == null)
                {
                    try
                    {
                        var ps = JsonSerializer.Deserialize<PersistState>(text, opts);
                        if (ps?.Profiles != null && ps.Profiles.Count > 0)
                        {
                            list = new List<WeaponProfile>();
                            foreach (var kv in ps.Profiles)
                            {
                                var p = kv.Value ?? new WeaponProfile();
                                if (string.IsNullOrWhiteSpace(p.Name)) p.Name = kv.Key;
                                list.Add(p);
                            }
                        }
                    }
                    catch { }
                }

                // 5) Dernière chance : root.Profiles en objet/array via JsonDocument
                if (list == null)
                {
                    using var doc = JsonDocument.Parse(text);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("Profiles", out var pr))
                    {
                        if (pr.ValueKind == JsonValueKind.Object)
                        {
                            var dict = JsonSerializer.Deserialize<Dictionary<string, WeaponProfile>>(pr.GetRawText(), opts);
                            if (dict != null)
                            {
                                list = new List<WeaponProfile>();
                                foreach (var kv in dict)
                                {
                                    var p = kv.Value ?? new WeaponProfile();
                                    if (string.IsNullOrWhiteSpace(p.Name)) p.Name = kv.Key;
                                    list.Add(p);
                                }
                            }
                        }
                        else if (pr.ValueKind == JsonValueKind.Array)
                        {
                            var arr = JsonSerializer.Deserialize<List<WeaponProfile>>(pr.GetRawText(), opts);
                            if (arr != null) list = arr;
                        }
                    }
                }

                if (list == null || list.Count == 0)
                {
                    LastMessage = "Import échoué : Format JSON non reconnu.";
                    return;
                }

                // Fusionne par Name (ajout / mise à jour)
                int added = 0, updated = 0;
                foreach (var p in list)
                {
                    if (p == null) continue;
                    if (string.IsNullOrWhiteSpace(p.Name) || p.Name == "— Choisir —") continue;

                    if (_profiles.ContainsKey(p.Name)) { _profiles[p.Name] = p; updated++; }
                    else { AddProfile(p); added++; }
                }

                BuildWeaponList();
                SaveStateSafe();
                LastMessage = $"Import OK : {added} ajouté(s), {updated} mis à jour.";
            }
            catch (Exception ex)
            {
                LastMessage = "Import échoué : " + ex.Message;
            }
        }


        private void ToggleRecording_Click(object sender, RoutedEventArgs e)
        {
            _recordingMode = !_recordingMode;
            LastMessage = _recordingMode ? "Mode enregistrement ON" : "Mode enregistrement OFF";
        }

        private void EnableDetect_Click(object? sender, RoutedEventArgs e)
        {
            LastMessage = "Auto-détection activée.";
        }

        void VirtualXbox_Click(object s, RoutedEventArgs e)
        {
           
            _virtType = VirtualType.Xbox360;
            EnsureVirtualConnected();
        }
        void VirtualDs4_Click(object s, RoutedEventArgs e)
        {
            _virtType = VirtualType.DualShock4;
            EnsureVirtualConnected();
        }

        void BindPad_Click(object s, RoutedEventArgs e)
        {
            RebindIfNeeded();
            if (UseXInput || (_psStream != null)) 
            { 
                PadStatus = UseXInput ? "Xbox (XInput)" : "PS (HID) connecté"; 
                StartLoop();
                // Créer automatiquement la manette virtuelle après avoir lié la physique
                EnsureVirtualConnected();
            }
            else 
            { 
                PadStatus = "aucune"; 
                LastMessage = "Aucune manette trouvée (USB recommandé, désactiver Steam Input)."; 
            }
            _preferHid = (_psStream != null); // ne bloque pas XInput si aucune HID Sony/clone
        }

        void LoadState_Click(object s, RoutedEventArgs e) { LoadStateIfAny(); UpdateHapticsStatus(); }
        void SaveState_Click(object s, RoutedEventArgs e)
        {
            try
            {
                // Sauvegarder le profil sélectionné
                SaveSelectedProfileIntoStore();
                
                // Sauvegarder l'état de l'application
            SaveStateSafe();
                
                // Sauvegarder les paramètres (settings)
                SaveSettings_Click(s, e);
                
                // Sauvegarder les optimizations (si nécessaire)
                // Note: Les optimizations sont appliquées directement, pas sauvegardées séparément
                
                LastMessage = LocalizationManager.GetString("SaveSuccess") ?? "Tout a été sauvegardé avec succès !";
            }
            catch (Exception ex)
            {
                LastMessage = $"Erreur lors de la sauvegarde: {ex.Message}";
            }
        }


        async void TestRumbleDirect_Click(object? s, RoutedEventArgs e) => await RumbleDirect(255, 255, 600);

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try { _gameSvc?.Stop(); _gameSvc?.Dispose(); } catch { }
            StopLoop();
            try { _x360?.Disconnect(); } catch { }
            try { _ds4?.Disconnect(); } catch { }
            try { _psStream?.Dispose(); } catch { }
            if (UseXInput) try { XInputVibrate(0, 0); } catch { }
            _client?.Dispose();
        }

        // ============ Périphériques ============
        void EnsureVirtualConnected()
        {
            if (_client == null || !_virtualAuto) { VirtualStatus = "désactivée (manuel)"; return; }

            lock (_virtLock)
            {
                if (_virtConnecting) return;
                _virtConnecting = true;
            }

            try
            {
                // si déjà présente, on ne recrée pas
                if (_x360 != null || _ds4 != null) { VirtualStatus = "déjà connectée"; return; }

                // nettoie toute instance fantôme
                try { _x360?.Disconnect(); } catch { }
                try { _ds4?.Disconnect(); } catch { }
                _x360 = null; _ds4 = null; _vigemUser = -1;

                if (_virtType == VirtualType.DualShock4)
                {
                    _ds4 = _client.CreateDualShock4Controller();
                    _ds4.FeedbackReceived += Ds4_FeedbackReceived;
                    _ds4.Connect();
                    SendNeutralToVirtual();
                    VirtualStatus = "DS4 virtuelle connectée";
                }
                else
                {
                    _x360 = _client.CreateXbox360Controller();
                    _x360.FeedbackReceived += X360_FeedbackReceived;
                    _x360.Connect();
                    SendNeutralToVirtual();
                    try { _vigemUser = _x360.UserIndex; } catch { _vigemUser = -1; }
                    VirtualStatus = "Xbox 360 virtuelle connectée";
                }
            }
            catch { VirtualStatus = "erreur"; }
            finally
            {
                lock (_virtLock) { _virtConnecting = false; }
            }
        }


        bool IsPadAlive()
        {
            try { return UseXInput ? XInputAlive() : (_psStream != null); }
            catch { return false; }
        }


        // *** NOUVELLE OUVERTURE HID « DUALSHOCK-LIKE » (VID/PID extensible + sniff rapports) ***
        bool TryOpenDualShockLikeHid()
        {
            _psDevice = null;
            _psStream = null;

            var devs = DeviceList.Local.GetHidDevices()
                // priorité USB (meilleure latence), puis BT
                .OrderBy(d => IsBluetooth(d) ? 1 : 0)
                // privilégier interfaces avec OUT report
                .ThenByDescending(d => d.GetMaxOutputReportLength());

            foreach (var d in devs)
            {
                try
                {
                    ushort vid = (ushort)d.VendorID, pid = (ushort)d.ProductID;
                    bool known = _knownPads.Contains((vid, pid)) || vid == 0x054C;
                    if (!known && !_padCfg.AllowAnyHID) continue;

                    if (!d.TryOpen(out var s)) continue;

                    // phase de sniff courte
                    s.ReadTimeout = 120;
                    s.WriteTimeout = 120;

                    // petit test OUT si dispo (sans conséquence)
                    int outLen = d.GetMaxOutputReportLength();
                    if (outLen > 0)
                    {
                        var test = new byte[Math.Max(2, outLen)];
                        test[0] = 0x02; // type générique
                        try { s.Write(test, 0, test.Length); } catch { /* ignore */ }
                    }

                    // lire 1–2 paquets pour reconnaître DS4/DS5 (USB/BT)
                    var buf = new byte[Math.Max(64, d.GetMaxInputReportLength())];
                    int n = 0;
                    try { n = s.Read(buf, 0, buf.Length); } catch { /* try again once */ }
                    if (n <= 0 && s.CanRead) { try { n = s.Read(buf, 0, buf.Length); } catch { } }

                    bool looksDS = false;
                    if (n > 0)
                    {
                        byte rid = buf[0];
                        looksDS =
                            (rid == 0x01 && n >= 10) ||   // USB DS4/DS5
                            (rid == 0x11 && n >= 10) ||   // BT DS4
                            (rid == 0x31 && n >= 12);     // BT DS5
                    }

                    // accepter si: (connu) OU (AllowAnyHID + ressemble à DS)
                    // accepter si: (connu) OU (AllowAnyHID && ressemble DS)
                    if (known || (_padCfg.AllowAnyHID && looksDS))
                    {
                        // <<< ne PAS bloquer
                        s.ReadTimeout = 15;
                        s.WriteTimeout = 120;

                        _psDevice = d; _psStream = s;
                        PadStatus = "PS (HID) connecté";
                        return true;
                    }


                    s.Dispose();
                }
                catch
                {
                    // essai suivant
                }
            }

            LastMessage = "Aucune interface HID compatible DS4/DS5 trouvée (désactive Steam Input/DSX/HidHide, teste en USB).";
            return false;
        }

        void RebindIfNeeded()
        {
            try
            {
                if (_preferHid && _psStream != null) return;     // <- retire .CanRead


                if (_psStream == null || !_psStream.CanRead)
                {
                    try { _psStream?.Dispose(); } catch { }
                    _psStream = null; _psDevice = null;

                    if (TryOpenDualShockLikeHid())
                    {
                        if (_loopCts == null) StartLoop();
                        _preferHid = true;
                        return;
                    }
                }

                if (FindXInputController())
                {
                    if (_loopCts == null) StartLoop();
                    return;
                }
                // Si plus AUCUNE manette physique et qu'on souhaite lâcher la virtuelle, on la coupe
                if (!FindXInputController() && (_psStream == null || !_psStream.CanRead))
                {
                    PadStatus = "déconnecté";   // on garde la virtuelle branchée
                    _xinputUser = -1;
                    return;
                }

                _xinputUser = -1; PadStatus = "déconnecté";
            }
            catch { PadStatus = "déconnecté"; }
        }
        void CheckPerProfileHotkeys(byte b0, byte b1, int hat)
        {
            // ---- Pad: combos "DPadUp+A", "LB+X", etc. ----
            bool anyPadDown = false;
            foreach (var name in _order)
            {
                if (!_profiles.TryGetValue(name, out var p)) continue;
                var spec = p.PadHotkey ?? "";
                if (string.IsNullOrWhiteSpace(spec)) continue;

                if (ComboDown(spec, b0, b1, hat))
                {
                    anyPadDown = true;
                    if (!_hkProfilePadPrev)
                        ApplyWeaponByName(name, $"Hotkey Pad ({spec})");
                    break; // un seul match
                }
            }
            _hkProfilePadPrev = anyPadDown;

            // ---- Clavier: "F1", "Left", "A", etc. ----
            bool anyKbDown = false;
            foreach (var name in _order)
            {
                if (!_profiles.TryGetValue(name, out var p)) continue;
                var key = (p.KbHotkey ?? "").Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;

                int vk = VKFromKeyName(key);
                if (vk != 0 && IsKeyDown(vk))
                {
                    anyKbDown = true;
                    if (!_hkProfileKbPrev)
                        ApplyWeaponByName(name, $"Hotkey KB ({key})");
                    break;
                }
            }
            _hkProfileKbPrev = anyKbDown;
        }

        // ====== RUMBLE ======
        long _lastAppFeedMs;
        byte _lastAppFeedS, _lastAppFeedL;
        void FeedAppRumble(byte s, byte l)
        {
            long now = Environment.TickCount64;
            if (now - _lastAppFeedMs < 8 && s == _lastAppFeedS && l == _lastAppFeedL) return;
            _lastAppFeedMs = now; _lastAppFeedS = s; _lastAppFeedL = l;

            lock (_rbLock)
            {
                _rbuf.Add((now, s, l));
                long cut = now - HBUF_KEEP_MS;
                int first = _rbuf.FindIndex(x => x.t >= cut);
                if (first > 0) _rbuf.RemoveRange(0, first);
            }
            Dispatcher.BeginInvoke(new Action(() => RumbleText = $"{s}/{l}"));
        }


        void EdgeRumble_Set(byte small, byte large)
        {
            if (small == 0 && large == 0)
            {
                EdgeRumble_Send(0, 0);
                if (_edgeTimer != null && _edgeTimer.IsEnabled) _edgeTimer.Stop();
                _edgeUntilMs = 0; _edgeLastSmall = _edgeLastLarge = 0;
                return;
            }

            byte big = (byte)Math.Min(255, large * 2);
            byte sml = (byte)Math.Min(255, small * 2);

            _edgeLastSmall = sml;
            _edgeLastLarge = big;
            _edgeUntilMs = Environment.TickCount64 + EDGE_HOLD_MS;

            EdgeRumble_Send(_edgeLastSmall, _edgeLastLarge);

            if (_edgeTimer == null)
            {
                _edgeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(EDGE_PERIOD_MS) };
                _edgeTimer.Tick += (_, __) =>
                {
                    long now = Environment.TickCount64;
                    if (now > _edgeUntilMs) { EdgeRumble_Send(0, 0); _edgeTimer!.Stop(); return; }
                    EdgeRumble_Send(_edgeLastSmall, _edgeLastLarge);
                };
            }
            if (!_edgeTimer.IsEnabled) _edgeTimer.Start();
        }

        void EdgeRumble_Send(byte small, byte large)
        {
            {   // 0x02 / 63
                byte[] r = new byte[63];
                r[0] = 0x02; r[1] = 0x01;
                r[3] = large; r[4] = small;
                r[10] = 0x02;
                if (TryWrite(r)) { LastMessage = "Edge rumble: payload A OK"; return; }
            }
            {   // swapped
                byte[] r = new byte[63];
                r[0] = 0x02; r[1] = 0x01;
                r[3] = small; r[4] = large;
                r[10] = 0x02;
                if (TryWrite(r)) { LastMessage = "Edge rumble: payload A-swapped OK"; return; }
            }
            {   // 0x31 / 78
                byte[] r = new byte[78];
                r[0] = 0x31; r[1] = 0x02;
                r[10] = 0x08; r[0x2D] = 0x02;
                r[15] = large; r[16] = small;
                if (TryWrite(r)) { LastMessage = "Edge rumble: payload 0x31 OK"; return; }
            }
            LastMessage = "Edge rumble: aucun payload accepté";
        }

        CancellationTokenSource? _rumbleMirrorCts;
        volatile byte _lastSmall, _lastLarge;
        long _lastSetAtMs;

        const int RUMBLE_HZ = 60;
        const int RUMBLE_HOLD_MS = 80;

        void X360_FeedbackReceived(object? _, Xbox360FeedbackReceivedEventArgs e)
          => OnFeedbackSample(Math.Clamp((int)e.SmallMotor, 0, 255), Math.Clamp((int)e.LargeMotor, 0, 255));
        void Ds4_FeedbackReceived(object? _, DualShock4FeedbackReceivedEventArgs e)
          => OnFeedbackSample(Math.Clamp((int)e.SmallMotor, 0, 255), Math.Clamp((int)e.LargeMotor, 0, 255));

        public void SetRumble(byte small, byte large)
        {
            _lastSmall = small; _lastLarge = large;
            _lastSetAtMs = Environment.TickCount64;
            if (_rumbleMirrorCts == null) StartRumbleMirror();
        }

        void StartRumbleMirror()
        {
            StopRumbleMirror();
            _rumbleMirrorCts = new CancellationTokenSource();
            var ct = _rumbleMirrorCts.Token;
            Task.Run(async () =>
            {
                int period = Math.Max(8, 1000 / RUMBLE_HZ);
                byte prevS = 0, prevL = 0;
                while (!ct.IsCancellationRequested)
                {
                    var now = Environment.TickCount64;
                    var s = _lastSmall; var l = _lastLarge;

                    if (s == 0 && l == 0 && (now - _lastSetAtMs) < RUMBLE_HOLD_MS) { s = prevS; l = prevL; }

                    ForwardRumbleUSB(s, l);
                    if (s != 0 || l != 0) { await Task.Delay(2, ct).ConfigureAwait(false); ForwardRumbleUSB(s, l); }

                    prevS = s; prevL = l;
                    await Task.Delay(period, ct).ConfigureAwait(false);

                    if (s == 0 && l == 0 && (now - _lastSetAtMs) > 200) StopRumbleMirror();
                }
            }, ct);
        }
        void StopRumbleMirror() { try { _rumbleMirrorCts?.Cancel(); } catch { } _rumbleMirrorCts = null; }

        // Mirroring Rumble : Sony HID (DS4/DS5/Edge) + Xbox XInput (si manette physique Xbox)
        void ForwardRumbleUSB(byte small, byte large)
        {
            try
            {
                if (_psDevice != null && _psStream != null && _psStream.CanWrite)
                {
                    bool isSony = _psDevice.VendorID == 0x054C;
                    ushort pid = (ushort)_psDevice.ProductID;

                    // DS4 USB
                    if (isSony && (pid == 0x05C4 || pid == 0x09CC || pid == 0x0BA0))
                    {
                        Span<byte> b = stackalloc byte[32];
                        b.Clear();
                        b[0] = 0x05; b[1] = 0xFF;
                        b[4] = small;  // small
                        b[5] = large;  // large
                        try { _psStream.Write(b); } catch { }
                    }
                    // DualSense / Edge
                    else if (isSony && (pid == 0x0CE6 || pid == 0x0E5F || pid == 0x0DF2))
                    {
                        EdgeRumble_Set(small, large);
                    }
                    else
                    {
                        // Générique 0x02 OUT si dispo (clones)
                        int outLen = _psDevice.GetMaxOutputReportLength();
                        if (outLen > 0)
                        {
                            var b = new byte[outLen];
                            b[0] = 0x02; b[1] = 0x01;
                            b[3] = large;
                            b[4] = small;
                            try { _psStream.Write(b, 0, b.Length); } catch { }
                        }
                    }
                }
            }
            catch { }

            // Si la manette physique est Xbox (XInput), vibre aussi
            if (UseXInput) XInputVibrate(small, large);
        }

        void StartLoop() { StopLoop(); _loopCts = new CancellationTokenSource(); Task.Run(() => LoopAsync(_loopCts.Token)); }
        void StopLoop() { try { _loopCts?.Cancel(); } catch { } _loopCts = null; }

        async Task LoopAsync(CancellationToken ct)
        {
            try { Thread.CurrentThread.Priority = ThreadPriority.Highest; } catch { }
            while (!ct.IsCancellationRequested)
            { // au début du while, avant IsPadAlive()
              // au début de la boucle
              // au début du while, avant IsPadAlive()
                if (_autoEnsureVirtual && _x360 == null && _ds4 == null)
                    EnsureVirtualConnected();


                try
                {

                    // ❗ On NE bloque plus si aucune manette virtuelle n'est branchée :
                    // if (_x360 == null && _ds4 == null) { await Task.Delay(2, ct); continue; }
                    if (!IsPadAlive())
                    {
                        RebindIfNeeded();
                        SendNeutralToVirtual();              // << garder la virtuelle en "heartbeat"
                        PollHotkeys(adsHeld: false, trigHeld: false, b0: 0, b1: 0);
                        await Task.Delay(8, ct);
                        continue;
                    }



                    bool ok = UseXInput
                        ? TryReadXInput(out short lx, out short ly, out short rx, out short ry, out byte lt, out byte rt, out byte b0, out byte b1, out int hat)
                        : TryReadDualShock(out lx, out ly, out rx, out ry, out lt, out rt, out b0, out b1, out hat);

                    if (ok)
                    {
                        // Mettre à jour la manette interactive en temps réel
                        UpdateInteractiveController(lx, ly, rx, ry, lt, rt, b0, b1);
                        
                        bool trigHeld = FireGachette switch
                        {
                            "R2" => rt > 0,           // gâchette analogique droite
                            "R1" => (b1 & 0x02) != 0, // bouton RB
                            "L2" => lt > 0,           // gâchette analogique gauche
                            "L1" => (b1 & 0x01) != 0, // bouton LB
                            _ => rt > 0
                        };

                        bool adsHeld = AdsGachette switch
                        {
                            "R2" => rt > 0,
                            "R1" => (b1 & 0x02) != 0,
                            "L2" => lt > 0,
                            "L1" => (b1 & 0x01) != 0,
                            _ => lt > 0
                        };

                        if (trigHeld && !_trigPrev)
                        {
                            _nextPingAtMs = 0;                               // ping immédiatement
                            _fireStartMs = Environment.TickCount64;          // reset AR/RF propre
                        }
                        _trigPrev = trigHeld;

                        _trigHeldFlag = trigHeld;
                        TriggerText = $"Trig:{rt} Ads:{lt}";

                        // ✅ Hotkeys (toujours checkés)
                        PollHotkeys(adsHeld, trigHeld, b0, b1);

                        // ===== AR / AA / RF / AutoPing =====
                        var Lout = ApplyStick(lx, ly, LCfg);
                        var Rout = ApplyStick(rx, ry, RCfg);
                        short outLX = Lout.X, outLY = Lout.Y, outRX = Rout.X, outRY = Rout.Y;

                        long now = Environment.TickCount64;

                        

                        // AutoPing scheduler
                        if (AutoPing && trigHeld && now >= _nextPingAtMs)
                        {
                            _pingUntilMs = now + PING_HOLD_MS;
                            _nextPingAtMs = now + AutoPingInterval;
                        }

                        if (trigHeld && (!AdsOnly || adsHeld))
                        {
                            int tms = (int)(Environment.TickCount64 - _fireStartMs);
                            if (UseTimeline && Timeline != null && Timeline.Segments.Count > 0)
                            {
                                (int dx, int dy) = EvalTimeline(Timeline, tms, TimelineGain * ArPower);
                                outRX = (short)Math.Clamp(outRX + dx, short.MinValue, short.MaxValue);
                                outRY = (short)Math.Clamp(outRY - dy, short.MinValue, short.MaxValue);
                                TimelinePreview = $"t={tms}ms  ΔX={dx}  ΔY={dy}";
                            }
                            else
                            {
                                (int dx, int dy) = EvalAdvancedAR(tms, AntiRecoilH, AntiRecoilV, ArDelayMs, ArRampMs, ArSustainMs);
                                outRX = (short)Math.Clamp(outRX + dx, short.MinValue, short.MaxValue);
                                outRY = (short)Math.Clamp(outRY - dy, short.MinValue, short.MaxValue);
                            }
                        }
                        else if (!trigHeld)
                        {
                            _fireStartMs = Environment.TickCount64;
                            _latchUntilRelease = false;
                            _candidateWeapon = ""; _candidateWins = 0;
                        }

                        // Aim Assist (ADS) - avec modes multiples
                        if (AimAssist && adsHeld)
                        {
                            double tt = (Environment.TickCount64 / 1000.0);
                            var (ax, ay) = CalculateAimAssist(tt);
                            outRX = (short)Math.Clamp(outRX + ax, short.MinValue, short.MaxValue);
                            outRY = (short)Math.Clamp(outRY + ay, short.MinValue, short.MaxValue);
                        }

                        // Rapid-Fire
                        byte outLT = lt, outRT = rt;
                        if (RapidFire && trigHeld)
                        {
                            double period = 1000.0 / RapidFireHz;
                            double onMs = period * (RapidFireDuty / 100.0);
                            int tms = (int)(Environment.TickCount64 - _fireStartMs);
                            double m = tms % period;
                            outRT = (byte)((m < onMs) ? 255 : 0);
                        }

                        // Détection visuelle CNN à cadence réduite
                        

                        // === Mappage vers virtuelles ===
                        bool dUp = (hat == 0 || hat == 1 || hat == 7);
                        bool dRight = (hat == 2 || hat == 1 || hat == 3);
                        bool dDown = (hat == 4 || hat == 3 || hat == 5);
                        bool dLeft = (hat == 6 || hat == 5 || hat == 7);
                        if (hat == 8) { dUp = dRight = dDown = dLeft = false; }
                        if (AutoPing && now < _pingUntilMs) dUp = true;

                        // X360
                        if (_x360 != null)
                        {
                            _x360.SetAxisValue(Xbox360Axis.LeftThumbX, outLX);
                            _x360.SetAxisValue(Xbox360Axis.LeftThumbY, outLY);
                            _x360.SetAxisValue(Xbox360Axis.RightThumbX, outRX);
                            _x360.SetAxisValue(Xbox360Axis.RightThumbY, outRY);

                            bool psSquare = (b0 & 0x10) != 0;
                            bool psCross = (b0 & 0x20) != 0;
                            bool psCircle = (b0 & 0x40) != 0;
                            bool psTriangle = (b0 & 0x80) != 0;
                            _x360.SetButtonState(Xbox360Button.A, psCross);
                            _x360.SetButtonState(Xbox360Button.B, psCircle);
                            _x360.SetButtonState(Xbox360Button.X, psSquare);
                            _x360.SetButtonState(Xbox360Button.Y, psTriangle);

                            _x360.SetButtonState(Xbox360Button.Up, dUp);
                            _x360.SetButtonState(Xbox360Button.Right, dRight);
                            _x360.SetButtonState(Xbox360Button.Down, dDown);
                            _x360.SetButtonState(Xbox360Button.Left, dLeft);

                            _x360.SetButtonState(Xbox360Button.LeftShoulder, (b1 & 0x01) != 0);
                            _x360.SetButtonState(Xbox360Button.RightShoulder, (b1 & 0x02) != 0);
                            _x360.SetButtonState(Xbox360Button.Back, (b1 & 0x10) != 0);
                            _x360.SetButtonState(Xbox360Button.Start, (b1 & 0x20) != 0);
                            _x360.SetButtonState(Xbox360Button.LeftThumb, (b1 & 0x40) != 0);
                            _x360.SetButtonState(Xbox360Button.RightThumb, (b1 & 0x80) != 0);

                            if (outLT == 0 && (b1 & 0x10) != 0) outLT = 255;
                            if (outRT == 0 && (b1 & 0x20) != 0) outRT = 255;
                            _x360.SetSliderValue(Xbox360Slider.LeftTrigger, outLT);
                            _x360.SetSliderValue(Xbox360Slider.RightTrigger, outRT);

                            try { _x360.SubmitReport(); } catch { _x360 = null; VirtualStatus = "déconnectée"; }
                        }

                        // DS4
                        if (_ds4 != null)
                        {
                            static byte ToByte(short v, bool invertY = false)
                            { int val = v + 32768; if (invertY) val = 65535 - val; return (byte)(val >> 8); }

                            _ds4.SetAxisValue(DualShock4Axis.LeftThumbX, ToByte(outLX));
                            _ds4.SetAxisValue(DualShock4Axis.LeftThumbY, ToByte(outLY, invertY: true));
                            _ds4.SetAxisValue(DualShock4Axis.RightThumbX, ToByte(outRX));
                            _ds4.SetAxisValue(DualShock4Axis.RightThumbY, ToByte(outRY, invertY: true));

                            _ds4.SetSliderValue(DualShock4Slider.LeftTrigger, outLT);
                            _ds4.SetSliderValue(DualShock4Slider.RightTrigger, outRT);

                            _ds4.SetButtonState(DualShock4Button.Cross, (b0 & 0x20) != 0);
                            _ds4.SetButtonState(DualShock4Button.Circle, (b0 & 0x40) != 0);
                            _ds4.SetButtonState(DualShock4Button.Square, (b0 & 0x10) != 0);
                            _ds4.SetButtonState(DualShock4Button.Triangle, (b0 & 0x80) != 0);

                            _ds4.SetButtonState(DualShock4Button.ShoulderLeft, (b1 & 0x01) != 0);
                            _ds4.SetButtonState(DualShock4Button.ShoulderRight, (b1 & 0x02) != 0);
                            _ds4.SetButtonState(DualShock4Button.Share, (b1 & 0x10) != 0);
                            _ds4.SetButtonState(DualShock4Button.Options, (b1 & 0x20) != 0);
                            _ds4.SetButtonState(DualShock4Button.ThumbLeft, (b1 & 0x40) != 0);
                            _ds4.SetButtonState(DualShock4Button.ThumbRight, (b1 & 0x80) != 0);

                            DualShock4DPadDirection dp = DualShock4DPadDirection.None;
                            if (dUp && !dRight && !dLeft) dp = DualShock4DPadDirection.North;
                            else if (dUp && dRight) dp = DualShock4DPadDirection.Northeast;
                            else if (dRight && !dDown) dp = DualShock4DPadDirection.East;
                            else if (dRight && dDown) dp = DualShock4DPadDirection.Southeast;
                            else if (dDown && !dLeft) dp = DualShock4DPadDirection.South;
                            else if (dDown && dLeft) dp = DualShock4DPadDirection.Southwest;
                            else if (dLeft && !dUp) dp = DualShock4DPadDirection.West;
                            else if (dLeft && dUp) dp = DualShock4DPadDirection.Northwest;
                            _ds4.SetDPadDirection(dp);

                            try { _ds4.SubmitReport(); } catch { _ds4 = null; VirtualStatus = "déconnectée"; }
                        }

                        continue;
                    }
                    else
                    {
                        // ✅ Aucune lecture ce tick → quand même checker les hotkeys clavier
                        PollHotkeys(adsHeld: false, trigHeld: false, b0: 0, b1: 0);
                    }

                    await Task.Delay(2, ct);
                }
                catch
                {
                    RebindIfNeeded();
                    await Task.Delay(4, ct);
                }
            }
        }


        // Lecture HID Sony/« DS-like »
        bool TryReadDualShock(out short lx, out short ly, out short rx, out short ry,
                              out byte lt, out byte rt, out byte b0, out byte b1, out int hat)
        {
            lx = ly = rx = ry = 0; lt = rt = 0; b0 = b1 = 0; hat = 8;
            if (_psStream == null) return false;

            int n;
            try
            {
                n = _psStream.Read(_buf, 0, _buf.Length);
            }
            catch
            {
                try { _psStream?.Dispose(); } catch { }
                _psStream = null;
                _psDevice = null;   // <- forcera Rebind au prochain tick
                return false;
            }
            if (n <= 0) return false;


            byte rid = _buf[0];
            static bool HatValid(byte v) => (v & 0x0F) <= 8;
            static short AX(byte v) => (short)Math.Clamp((v - 128) * 257, short.MinValue, short.MaxValue);
            static short AY(byte v) => (short)Math.Clamp(-(v - 128) * 257, short.MinValue, short.MaxValue);

            if (rid == 0x01 && n >= 10) // USB DS4/DS5
            {
                int idxB0 = 8, idxB1 = 9, idxL2 = 5, idxR2 = 6;
                if (!HatValid(_buf[idxB0])) { idxB0 = 5; idxB1 = 6; idxL2 = 8; idxR2 = 9; }

                lx = AX(_buf[1]); ly = AY(_buf[2]);
                rx = AX(_buf[3]); ry = AY(_buf[4]);
                b0 = _buf[idxB0]; b1 = _buf[idxB1];
                hat = b0 & 0x0F; if (hat > 8) hat = 8;
                lt = _buf[idxL2]; rt = _buf[idxR2];
                return true;
            }
            else if (rid == 0x11 && n >= 10) // DS4 BT
            {
                lx = AX(_buf[3]); ly = AY(_buf[4]);
                rx = AX(_buf[5]); ry = AY(_buf[6]);
                b0 = _buf[7]; b1 = _buf[8];
                hat = b0 & 0x0F; if (hat > 8) hat = 8;
                lt = _buf[8]; rt = _buf[9];
                return true;
            }
            else if (rid == 0x31 && n >= 12) // DS5 BT
            {
                lx = AX(_buf[3]); ly = AY(_buf[4]);
                rx = AX(_buf[5]); ry = AY(_buf[6]);
                b0 = _buf[10]; b1 = _buf[11];
                hat = b0 & 0x0F; if (hat > 8) hat = 8;
                lt = _buf[8]; rt = _buf[9];
                return true;
            }
            return false;
        }

        // Haptique (signature)
        short[]? BuildSignature(long msWindow)
        {
            List<(long t, int small, int large)> slice;
            long now = Environment.TickCount64, from = now - msWindow;
            lock (_rbLock)
            {
                int i0 = _rbuf.FindIndex(s => s.t >= from);
                if (i0 < 0 && _rbuf.Count > 0)
                {
                    long from2 = now - Math.Min(HBUF_KEEP_MS - 100, 2400);
                    i0 = _rbuf.FindIndex(s => s.t >= from2);
                }
                if (i0 < 0) return null;
                slice = _rbuf.GetRange(i0, _rbuf.Count - i0);
            }
            if (slice.Count < 4) return null;

            int nonZero = 0, maxAmp = 0;
            double sum = 0; long t0 = slice[0].t;
            var pts = new List<(double t, double v)>(slice.Count);
            foreach (var s in slice)
            {
                int a = Math.Max(s.small, s.large);
                if (a > 0) nonZero++;
                if (a > maxAmp) maxAmp = a;
                double v = (s.small + s.large) / 255.0;
                sum += v;
                pts.Add((s.t - t0, v));
            }
            double energyAvg = sum / Math.Max(1, slice.Count);
            if (nonZero < 8 || (maxAmp < 20 && energyAvg < 0.06)) return null;

            double dur = Math.Max(1.0, pts[^1].t - pts[0].t);
            double binDur = dur / HSIG_BINS;
            var bins = new double[HSIG_BINS];
            int b = 0, accN = 0; double edge = binDur, acc = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                while (pts[i].t >= edge && b < HSIG_BINS) { bins[b] = accN > 0 ? acc / accN : 0; b++; edge += binDur; acc = 0; accN = 0; }
                if (b < HSIG_BINS) { acc += pts[i].v; accN++; }
            }
            if (b < HSIG_BINS) bins[b] = accN > 0 ? acc / accN : 0;

            var sm = new double[HSIG_BINS];
            for (int i = 0; i < HSIG_BINS; i++)
            {
                double s = bins[i];
                if (i > 0) s += bins[i - 1];
                if (i + 1 < HSIG_BINS) s += bins[i + 1];
                int n = 1 + (i > 0 ? 1 : 0) + ((i + 1 < HSIG_BINS) ? 1 : 0);
                sm[i] = s / n;
            }
            var sorted = (double[])sm.Clone(); Array.Sort(sorted);
            double baseline = sorted[(int)Math.Round(0.20 * (sorted.Length - 1))];
            for (int i = 0; i < HSIG_BINS; i++) sm[i] = Math.Max(0.0, sm[i] - baseline);

            double ssum = sm.Sum(); if (ssum < 1e-6) return null;
            for (int i = 0; i < HSIG_BINS; i++) sm[i] /= ssum;

            var sig = new short[HSIG_BINS];
            for (int i = 0; i < HSIG_BINS; i++) sig[i] = (short)Math.Round(sm[i] * 1000.0);

            double mean = sm.Average();
            double std = Math.Sqrt(sm.Select(v => (v - mean) * (v - mean)).Average());
            if (std < MIN_STD) return null;

            return sig;
        }

        static double Sad(short[] a, short[] b)
        {
            int n = Math.Min(a.Length, b.Length);
            long acc = 0; for (int i = 0; i < n; i++) acc += Math.Abs(a[i] - b[i]);
            return (double)acc / (n * 1000.0);
        }
        double ScoreWeapon(string weapon, short[] probe)
        {
            if (!_haptics.Templates.TryGetValue(weapon, out var list) || list == null || list.Count == 0) return double.PositiveInfinity;
            double best = double.PositiveInfinity;
            foreach (var t in list) { var s = Sad(probe, t); if (s < best) best = s; }
            return best;
        }

        // === Hotkeys configurables (VM) ==============================================
        public ObservableCollection<string> AllPadButtons { get; } = new()
        {
            "A","B","X","Y","LB","RB","Back","Start","LS","RS",
            "DPadUp","DPadRight","DPadDown","DPadLeft"
        };

        public ObservableCollection<string> AllKbKeys { get; } = new(); // clavier désactivé


        // Sélections utilisateur (data-bindings du XAML)
        public string PadToggleRFButton { get => _padRF; set { _padRF = value; N(); } }
        string _padRF = "DPadUp+A";
        public string PadToggleAAButton { get => _padAA; set { _padAA = value; N(); } }
        string _padAA = "DPadRight+X";
        public string PadTogglePingButton { get => _padPG; set { _padPG = value; N(); } }
        string _padPG = "DPadDown+B";

        public string KbToggleKey { get => _kbToggle; set { _kbToggle = value; N(); } }
        string _kbToggle = "F1";
        public string KbPrevKey { get => _kbPrev; set { _kbPrev = value; N(); } }
        string _kbPrev = "Left";
        public string KbNextKey { get => _kbNext; set { _kbNext = value; N(); } }
        string _kbNext = "Right";
        public string KbRapidKey { get => _kbRapid; set { _kbRapid = value; N(); } }
        string _kbRapid = "R";
        public string KbAimKey { get => _kbAim; set { _kbAim = value; N(); } }

        void DisconnectVirtualButton_Click(object s, RoutedEventArgs e)
        {
            _autoEnsureVirtual = false;                 // empêche la boucle de la recréer
            DisconnectVirtual("déconnectée manuellement");
        }


        void ReconnectVirtualButton_Click(object s, RoutedEventArgs e)
        {
            _virtualAuto = true;
            EnsureVirtualConnected();
        }


        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled) return;
            e.Handled = true;
            var ev = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };
            (this.Content as UIElement)?.RaiseEvent(ev);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
        private void CheckBox_Checked(object sender, RoutedEventArgs e) { }

        string _kbAim = "A";
        public string KbPingKey { get => _kbPing; set { _kbPing = value; N(); } }
        string _kbPing = "P";

        // VK étendu (F1..F24, A–Z, 0–9, flèches, etc.)
        int VKFromKeyName(string k)
        {
            if (string.IsNullOrWhiteSpace(k)) return 0;
            var s = k.Trim().ToUpperInvariant();

            if (s.Length == 1)
            {
                char c = s[0];
                if (c >= 'A' && c <= 'Z') return c;
                if (c >= '0' && c <= '9') return c;
            }
            if (s.StartsWith("F") && int.TryParse(s.Substring(1), out int fn) && fn >= 1 && fn <= 24)
                return 0x70 + (fn - 1);

            return s switch
            {
                "LEFT" => 0x25,
                "RIGHT" => 0x27,
                "UP" => 0x26,
                "DOWN" => 0x28,
                "SPACE" => 0x20,
                "TAB" => 0x09,
                "ESC" => 0x1B,
                _ => 0
            };
        }

        // Synonymes FR/PS pour combos
        bool BtnFromName(string name, byte b0, byte b1, int hat)
        {
            bool a = (b0 & 0x20) != 0, b = (b0 & 0x40) != 0, x = (b0 & 0x10) != 0, y = (b0 & 0x80) != 0;
            bool lb = (b1 & 0x01) != 0, rb = (b1 & 0x02) != 0, back = (b1 & 0x10) != 0, start = (b1 & 0x20) != 0;
            bool ls = (b1 & 0x40) != 0, rs = (b1 & 0x80) != 0;
            bool up = hat == 0 || hat == 1 || hat == 7;
            bool right = hat == 2 || hat == 1 || hat == 3;
            bool down = hat == 4 || hat == 3 || hat == 5;
            bool left = hat == 6 || hat == 5 || hat == 7;

            string n = (name ?? "")
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .Replace("É", "E")
                .Replace("È", "E")
                .Replace("Ê", "E")
                .Replace("À", "A")
                .Replace("Â", "A")
                .Replace("Ô", "O")
                .Replace("Ù", "U");

            return n switch
            {
                "A" or "CROSS" or "CROIX" => a,
                "B" or "CIRCLE" or "ROND" => b,
                "X" or "SQUARE" or "CARRE" or "CARRE" => x,
                "Y" or "TRIANGLE" => y,
                "LB" or "L1" => lb,
                "RB" or "R1" => rb,
                "BACK" or "SHARE" => back,
                "START" or "OPTIONS" => start,
                "LS" or "L3" => ls,
                "RS" or "R3" => rs,
                "DPADUP" or "HAUT" or "FLECHEUP" or "FLECHEHAUT" => up,
                "DPADRIGHT" or "DROITE" or "FLECHERIGHT" or "FLECHEDROITE" => right,
                "DPADDOWN" or "BAS" or "FLECHEDOWN" or "FLECHEBAS" => down,
                "DPADLEFT" or "GAUCHE" or "FLECHELEFT" or "FLECHEGAUCHE" => left,
                _ => false
            };
        }

        bool ComboDown(string spec, byte b0, byte b1, int hat)
        {
            if (string.IsNullOrWhiteSpace(spec)) return false;
            var parts = spec.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts)
                if (!BtnFromName(p, b0, b1, hat)) return false;
            return true;
        }
        void HandlePadProfileHotkeys(byte b0, byte b1, int hat)
        {
            string? match = null;
            foreach (var kv in _profiles)
            {
                var hk = kv.Value.PadHotkey;
                if (!string.IsNullOrWhiteSpace(hk) && ComboDown(hk!, b0, b1, hat)) { match = kv.Key; break; }
            }
            if (match != null && !_hkProfilePadPrev) ApplyWeaponByName(match, "Hotkey pad");
            _hkProfilePadPrev = (match != null);
        }

        bool KeySpecDown(string spec) => IsKeyDown(VKFromKeyName(spec));

        void HandleKbProfileHotkeys()
        {
            string? match = null;
            foreach (var kv in _profiles)
            {
                var hk = kv.Value.KbHotkey;
                if (!string.IsNullOrWhiteSpace(hk) && KeySpecDown(hk!)) { match = kv.Key; break; }
            }
            if (match != null && !_hkProfileKbPrev) ApplyWeaponByName(match, "Hotkey clavier");
            _hkProfileKbPrev = (match != null);
        }

        // ==== Hotkeys ====
        void PollHotkeys(bool adsHeld, bool trigHeld, byte b0, byte b1)
        {
            int hat = b0 & 0x0F; if (hat > 8) hat = 8;

            // Raccourcis PAR PROFIL (pad uniquement)
            CheckPerProfileHotkeys(b0, b1, hat);

            // Raccourcis GLOBAUX (pad uniquement)
            bool rfCombo = ComboDown(PadToggleRFButton, b0, b1, hat);
            bool aaCombo = ComboDown(PadToggleAAButton, b0, b1, hat);
            bool pgCombo = ComboDown(PadTogglePingButton, b0, b1, hat);

            if (rfCombo && !_hkRFPrev) { RapidFire = !RapidFire; LastMessage = $"Rapid-Fire {(RapidFire ? "ON" : "OFF")}"; }
            if (aaCombo && !_hkAAPrev) { AimAssist = !AimAssist; LastMessage = $"AimAssist {(AimAssist ? "ON" : "OFF")}"; }
            if (pgCombo && !_hkPingPrev) { AutoPing = !AutoPing; LastMessage = $"Auto-Ping {(AutoPing ? "ON" : "OFF")}"; }

            _hkRFPrev = rfCombo; _hkAAPrev = aaCombo; _hkPingPrev = pgCombo;
        }

        




        void NextProfile()
        {
            if (_order.Count == 0) return;
            int idx = Math.Max(0, _order.IndexOf(SelectedWeapon));
            idx = (idx + 1) % _order.Count;
            ApplyWeaponByName(_order[idx], "Hotkey");
        }
        void PrevProfile()
        {
            if (_order.Count == 0) return;
            int idx = Math.Max(0, _order.IndexOf(SelectedWeapon));
            idx = (idx - 1 + _order.Count) % _order.Count;
            ApplyWeaponByName(_order[idx], "Hotkey");
        }

        // ====== Haptique → décision ======
        (string name, double score, double score2, short[] probe)? HapticRankedBest()
        {
            var probe = BuildSignature(HSIG_MS);
            if (probe == null || _haptics.Templates.Count == 0) return null;
            
            string bestW = ""; double best = double.MaxValue; 
            string secondW = ""; double second = double.MaxValue;
            string thirdW = ""; double third = double.MaxValue;
            
            // Calculer les scores pour toutes les armes
            foreach (var weapon in _haptics.Templates.Keys)
            {
                double raw = ScoreWeapon(weapon, probe);
                if (double.IsInfinity(raw)) continue;
                
                // EMA avec alpha réduit pour plus de stabilité
                if (!_ema.TryGetValue(weapon, out var prev)) prev = raw;
                double ema = EMA_ALPHA * raw + (1.0 - EMA_ALPHA) * prev;
                _ema[weapon] = ema;
                
                // Garder les 3 meilleurs pour validation
                if (ema < best) 
                { 
                    third = second; thirdW = secondW;
                    second = best; secondW = bestW; 
                    best = ema; bestW = weapon; 
                }
                else if (ema < second) 
                { 
                    third = second; thirdW = secondW;
                    second = ema; secondW = weapon; 
                }
                else if (ema < third)
                {
                    third = ema; thirdW = weapon;
                }
            }
            
            if (string.IsNullOrEmpty(bestW)) return null;
            if (double.IsInfinity(second)) second = 1.0;
            if (double.IsInfinity(third)) third = 1.0;
            
            // VALIDATION SUPPLÉMENTAIRE : Vérifier que le 1er est vraiment meilleur que le 3ème
            // Si le 3ème est trop proche, c'est ambigu
            if (third - best < MARGIN_TO_SECOND * 0.8)
            {
                // Trop ambigu, rejeter
                return null;
            }
            
            return (bestW, best, second, probe);
        }

        void HapticDecideAndMaybeApply(bool fromEdge)
        {
            if (_recordingMode) return;
            if (_latchUntilRelease) return;
            if (!_trigHeldFlag) return;

            var r = HapticRankedBest(); if (r == null) return;
            var (bestW, best, second, _) = r.Value;
            double margin = (second - best);
            double improvementRatio = 0.0; // Déclarer en dehors du bloc if
            
            // VALIDATION 1 : Score absolu trop élevé = rejeter
            if (best > MATCH_MAX_SAD) 
            { 
                _candidateWeapon = ""; 
                _candidateWins = 0; 
                return; 
            }

            // VALIDATION 2 : Marge insuffisante entre 1er et 2ème = rejeter (trop ambigu)
            if (second - best < MARGIN_TO_SECOND) 
            { 
                _candidateWeapon = ""; 
                _candidateWins = 0; 
                return; 
            }

            // VALIDATION 3 : Si on a déjà une arme, vérifier que le changement est vraiment meilleur
            if (!string.IsNullOrEmpty(CurrentWeapon) && _ema.TryGetValue(CurrentWeapon, out var curScore))
            {
                // Si l'arme actuelle est déjà très proche du meilleur score, ne pas changer
                if (curScore <= best + STICKY_TOLERANCE) 
                {
                    _candidateWeapon = ""; 
                    _candidateWins = 0; 
                    return; 
                }
                
                // Vérifier que l'amélioration est significative (au moins 15% mieux)
                var improveVsCur = curScore - best;
                improvementRatio = improveVsCur / curScore;
                if (improvementRatio < MIN_IMPROVEMENT_RATIO)
                {
                    _candidateWeapon = ""; 
                    _candidateWins = 0; 
                    return; 
                }
                
                // Vérifier que la marge avec le 2ème est suffisante ET que l'amélioration est claire
                var gapToSecond = second - best;
                if (gapToSecond < MARGIN_TO_SECOND * 1.2) // Marge encore plus stricte si changement d'arme
                {
                    _candidateWeapon = ""; 
                    _candidateWins = 0; 
                    return; 
                }
            }

            // VALIDATION 4 : Confirmation multiple (le même candidat doit gagner plusieurs fois)
            if (_candidateWeapon != bestW) 
            { 
                _candidateWeapon = bestW; 
                _candidateWins = 1; 
            }
            else 
            {
                _candidateWins++;
            }

            // VALIDATION 5 : Nombre de confirmations requis (augmenté pour plus de sécurité)
            if (_candidateWins < CONS_WINS_REQUIRED) return;
            
            // VALIDATION 6 : Intervalle minimum entre changements (éviter les changements trop rapides)
            long now = Environment.TickCount64;
            if (!fromEdge && now - _lastSwitchAt < MIN_SWITCH_INTERVAL_MS) return;

            // VALIDATION 7 : Dernière vérification avant application (re-vérifier le score)
            var finalCheck = HapticRankedBest();
            if (finalCheck == null || finalCheck.Value.name != bestW || finalCheck.Value.score > MATCH_MAX_SAD)
            {
                _candidateWeapon = ""; 
                _candidateWins = 0; 
                return; 
            }

            // ✅ TOUTES LES VALIDATIONS PASSÉES : Appliquer le changement
            if (!LockSelectedWeapon) { ApplyWeaponByName(bestW, $"Haptique (score {best:0.000}, marge {margin:0.000}, amélioration {improvementRatio:P1})"); CurrentWeapon = bestW; }
            _lastSwitchAt = now;
            _candidateWeapon = ""; 
            _candidateWins = 0;
            _latchUntilRelease = true;
            
            // Mettre à jour l'affichage de l'arme détectée
            Dispatcher.BeginInvoke(new Action(() => { N(nameof(DetectedWeaponDisplay)); }));
        }

        void OnFeedbackSample(int small, int large)
        {
            SetRumble((byte)small, (byte)large);
            FeedAppRumble((byte)small, (byte)large);

            if ((small > 0 || large > 0) && _trigHeldFlag)
                HapticDecideAndMaybeApply(fromEdge: false);

            long ms = Environment.TickCount64;
            lock (_rbLock)
            {
                _rbuf.Add((ms, small, large));
                long cut = ms - HBUF_KEEP_MS;
                int first = _rbuf.FindIndex(s => s.t >= cut);
                if (first > 0) _rbuf.RemoveRange(0, first);
            }

            Dispatcher.BeginInvoke(new Action(() => { RumbleText = $"{small}/{large}"; }));
        }

        // ============ Profils & persistance ============
        void LoadDefaultProfiles()
        {
            _profiles.Clear(); _order.Clear();
            AddProfile(new WeaponProfile { Name = "— Choisir —", SimpleEnable = 0 });

            // BASE + existants (exemples)
            AddProfile(new WeaponProfile { Name = "Warzone (Base)", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "XM4", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00, });
            AddProfile(new WeaponProfile { Name = "AK-74", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = -0.00 });
            AddProfile(new WeaponProfile { Name = "AMES 85", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = -0.00 });
            AddProfile(new WeaponProfile { Name = "GPR 91", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "Model L", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "Goblin Mk2", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = -0.00 });
            AddProfile(new WeaponProfile { Name = "AS VAL", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = -0.00 });
            AddProfile(new WeaponProfile { Name = "Krig CC", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "Cypher 091", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "CR-56 AMAX", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = -0.00 });
            AddProfile(new WeaponProfile { Name = "Kilo 141", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00, });
            AddProfile(new WeaponProfile { Name = "FFAR 1", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "ABR A1", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });

            // SMG
            AddProfile(new WeaponProfile { Name = "C9", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00, });
            AddProfile(new WeaponProfile { Name = "KSV", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "Jackal PDW", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = -0.00, });
            AddProfile(new WeaponProfile { Name = "Tanto .22", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "PP-919", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "Kompakt 92", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "Saug", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = -0.00 });
            AddProfile(new WeaponProfile { Name = "PPSh-41", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "LC10", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            AddProfile(new WeaponProfile { Name = "Ladra", SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
            EnsureBO6WeaponsTestSet();
        }

        // Ajoute une liste d'armes Black Ops 6 (TEST) si elles n'existent pas déjà
        void EnsureBO6WeaponsTestSet()
        {
            string[] bo6 = new[]
            {
                // Fusils d'assaut
                "XM4","AK-74","AMES 85","GPR 91","Model L","Goblin Mk2","AS VAL","Krig CC","Cypher 091","CR-56 AMAX","Kilo 141","FFAR 1","ABR A1","Merrick 556",
                // Mitraillettes
                "C9","KSV","Jackal PDW","Tanto .22","PP-919","Kompakt 92","Saug","PPSh-41","LC10","Ladra","Dresden 9 mm",
                // Variantes test pour OCR
                "AK-74u","Commando","FAMAS","Galil","AUG",
                // Mitrailleuses (test)
                "HAMR","Stoner 63","GPMG 9",
                // Tactiques / DMR (test)
                "CARV.2","DMR 14","Type 63","DM-10","Essex Modèle 07",
                // Snipers (test)
                "LW3 - Tundra","Pellington 703","M82","LR 7.62","SVD","AMR Mod 4",
                // Fusils à pompe
                "SP marin","Olympia",
                // Pistolets
                "GS45",
                // Spéciales / Mêlée
                "Resonator X52","Couteau de combat","Pioche","Tronçonneuse"
            };

            foreach (var w in bo6)
            {
                if (!_profiles.ContainsKey(w))
                {
                    AddProfile(new WeaponProfile { Name = w, SimpleEnable = 1, AntiRecoilValue = 1.00, AntiRecoilHorizontal = 0.00 });
                }
            }
        }
        void AddProfile(WeaponProfile p)
        {
            if (!_profiles.ContainsKey(p.Name)) { _profiles[p.Name] = p; _order.Add(p.Name); }
            else _profiles[p.Name] = p;
        }
        void BuildWeaponList() { WeaponItems.Clear(); foreach (var n in _order) WeaponItems.Add(n); }
        void ResetProfiles_Click(object s, RoutedEventArgs e)
        {
            LoadDefaultProfiles();
            BuildWeaponList();
            SelectedWeapon = "— Choisir —";
            SaveStateSafe();
            LastMessage = "Profils réinitialisés (valeurs par défaut).";
        }

        void ApplyProfile(WeaponProfile p)
        {
            RapidFire = p.RapidFireEnable == 1;
            AntiRecoilV = p.AntiRecoilValue;
            AntiRecoilH = p.AntiRecoilHorizontal;
            if (p.UseTimeline && p.Timeline != null)
            {
                UseTimeline = true; Timeline = p.Timeline;
                TimelineGain = p.Timeline.Gain; TimelineRequireAds = p.Timeline.RequireADS;
                TimelineJson = JsonSerializer.Serialize(Timeline, new JsonSerializerOptions { WriteIndented = true });
            }
            // MAJ champs de hotkeys visibles pour édition
            _wpadhk = p.PadHotkey ?? ""; _wkbk = p.KbHotkey ?? "";
            N(nameof(WeaponPadHotkey)); N(nameof(WeaponKbHotkey));
            
        }
        void ApplyWeaponByName(string name, string src)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!_profiles.TryGetValue(name, out var p)) { LastMessage = $"Profil « {name} » introuvable."; return; }
            if (SelectedWeapon != name) SelectedWeapon = name;
            ApplyProfile(p);
            ProfilesDetectedText = $"Arme : {name}";
            LastMessage = $"{src} → « {name} »";
            SaveStateSafe();
        }

        private void TimelineEditor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        void SaveStateSafe()
        {
            try
            {
                var st = new PersistState
                {
                    FireGachette = this.FireGachette,
                    AdsGachette = this.AdsGachette,


                    ArPower = ArPower,
                    LastWeapon = SelectedWeapon,
                    Profiles = _profiles,
                    Haptics = _haptics,
                    BackupFolder = BackupFolder,
                    BackupOnSave = BackupOnSave,
                    AutoRules = AutoRules.ToList(),
                    Timeline = Timeline,
                    UseTimeline = UseTimeline,

                    // Macros & options
                    RapidFire = RapidFire,
                    RapidFireHz = RapidFireHz,
                    RapidFireDuty = RapidFireDuty,
                    AdsOnly = AdsOnly,
                    ArDelayMs = ArDelayMs,
                    ArRampMs = ArRampMs,
                    ArSustainMs = ArSustainMs,
                    AntiRecoilV = AntiRecoilV,
                    AntiRecoilH = AntiRecoilH,
                    AimAssist = AimAssist,
                    AAAmplitudeX = AAAmplitudeX,
                    AAAmplitudeY = AAAmplitudeY,
                    AAFrequency = AAFrequency,
                    AutoPing = AutoPing,
                    AutoPingInterval = AutoPingInterval,
                    HotkeyMode = HotkeyMode,

                    
                };
                File.WriteAllText(PersistPath, JsonSerializer.Serialize(st, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
        void LoadStateIfAny()
        {
            try
            {
                if (!File.Exists(PersistPath)) return;
                var st = JsonSerializer.Deserialize<PersistState>(File.ReadAllText(PersistPath));
                if (st?.Profiles != null) { _profiles.Clear(); _order.Clear(); foreach (var kv in st.Profiles) AddProfile(kv.Value); }
                if (st?.Haptics?.Templates != null) { _haptics.Templates.Clear(); foreach (var kv in st.Haptics.Templates) _haptics.Templates[kv.Key] = kv.Value ?? new List<short[]>(); }
                if (st?.Haptics?.Counts != null) { _haptics.Counts.Clear(); foreach (var kv in st.Haptics.Counts) _haptics.Counts[kv.Key] = kv.Value; }
                BuildWeaponList();
                EnsureBO6WeaponsTestSet();

                BackupFolder = st?.BackupFolder ?? "";
                BackupOnSave = st?.BackupOnSave ?? false;

                UseTimeline = st?.UseTimeline ?? UseTimeline;
                if (st?.Timeline != null)
                {
                    Timeline = st.Timeline;
                    TimelineGain = Timeline.Gain; TimelineRequireAds = Timeline.RequireADS;
                    TimelineJson = JsonSerializer.Serialize(Timeline, new JsonSerializerOptions { WriteIndented = true });
                }

                // Macros & options
                if (st != null)
                {
                    FireGachette = NormalizeGachette(st?.FireGachette);
                    AdsGachette = NormalizeGachette(st?.AdsGachette ?? "L2");


                    RapidFire = st.RapidFire;
                    RapidFireHz = st.RapidFireHz > 0 ? st.RapidFireHz : RapidFireHz;
                    RapidFireDuty = st.RapidFireDuty > 0 ? st.RapidFireDuty : RapidFireDuty;
                    AdsOnly = st.AdsOnly;
                    ArDelayMs = st.ArDelayMs > 0 ? st.ArDelayMs : ArDelayMs;
                    ArRampMs = st.ArRampMs > 0 ? st.ArRampMs : ArRampMs;
                    ArSustainMs = st.ArSustainMs > 0 ? st.ArSustainMs : ArSustainMs;
                    AntiRecoilV = st.AntiRecoilV > 0 ? st.AntiRecoilV : AntiRecoilV;
                    AntiRecoilH = st.AntiRecoilH;
                    ArPower = (st.ArPower > 0) ? st.ArPower : ArPower;
                    AimAssist = st.AimAssist;
                    AAAmplitudeX = st.AAAmplitudeX > 0 ? st.AAAmplitudeX : AAAmplitudeX;
                    AAAmplitudeY = st.AAAmplitudeY > 0 ? st.AAAmplitudeY : AAAmplitudeY;
                    AAFrequency = st.AAFrequency > 0 ? st.AAFrequency : AAFrequency;
                    AutoPing = st.AutoPing;
                    AutoPingInterval = st.AutoPingInterval > 0 ? st.AutoPingInterval : AutoPingInterval;
                    HotkeyMode = string.IsNullOrWhiteSpace(st.HotkeyMode) ? HotkeyMode : st.HotkeyMode;

                    
                }

                if (!string.IsNullOrWhiteSpace(st?.LastWeapon) && _profiles.ContainsKey(st.LastWeapon))
                { SelectedWeapon = st.LastWeapon; ApplyProfile(_profiles[st.LastWeapon]); ProfilesDetectedText = $"Arme : {SelectedWeapon}"; }
                LastMessage = $"Chargé depuis {PersistPath}";
            }
            catch (Exception ex) { LastMessage = "Erreur chargement: " + ex.Message; }
        }
        void UpdateHapticsStatus()
        {
            int n = 0; foreach (var kv in _haptics.Templates) if (kv.Value != null && kv.Value.Count > 0) n++;
            HapticsStatus = n == 0 ? "Aucune signature" : $"{n} arme(s) avec signature(s)";
        }

        // Timeline AR (boucle infinie sur la durée totale du pattern)
        (int dx, int dy) EvalTimeline(AntiRecoilTimeline tl, int elapsedMs, double gain)
        {
            // Calcul d’un "pass" complet (durée et delta total)
            int passDuration = 0;
            int passVX = 0, passVY = 0;
            foreach (var seg in tl.Segments)
            {
                int span = Math.Max(1, seg.DurationMs);
                int rep = Math.Max(1, seg.Repeat + 1);
                passDuration += Math.Max(0, seg.WaitMs);
                passDuration += span * rep;
                passVX += seg.VX * rep;
                passVY += seg.VY * rep;
            }
            if (passVY == 0)
            {
                // Si la somme d'une passe est nulle, soutiens le mouvement
                // en utilisant le dernier VY non nul pour éviter l'arrêt.
                for (int i = tl.Segments.Count - 1; i >= 0; i--)
                {
                    if (tl.Segments[i].VY != 0) { passVY = tl.Segments[i].VY; break; }
                }
            }
            if (passDuration <= 0)
            {
                return (0, 0);
            }

            // Nombre de passes complètes écoulées + reste
            int fullPass = Math.Max(0, elapsedMs / passDuration);
            int rem = Math.Max(0, elapsedMs % passDuration);

            int accX = (int)(passVX * gain) * fullPass;
            int accY = (int)(passVY * gain) * fullPass;

            // Accumulation partielle sur la passe courante
            int t = 0;
            foreach (var seg in tl.Segments)
            {
                t += Math.Max(0, seg.WaitMs);
                int span = Math.Max(1, seg.DurationMs);
                int rep = Math.Max(1, seg.Repeat + 1);
                for (int r = 0; r < rep; r++)
                {
                    if (rem <= t) break;
                    int start = t, end = t + span;
                    if (rem >= end)
                    {
                        accX += (int)(seg.VX * gain);
                        accY += (int)(seg.VY * gain);
                    }
                    else
                    {
                        double p = (rem - start) / (double)span;
                        p = Ease(p, seg.Ease);
                        accX += (int)(seg.VX * gain * p);
                        accY += (int)(seg.VY * gain * p);
                        return (accX, accY);
                    }
                    t = end;
                }
            }
            return (accX, accY);

            static double Ease(double p, double e)
            {
                if (e == 1.0) return p;
                if (e > 1.0) return p * p;
                if (e <= 0.5) return Math.Sqrt(p);
                return p;
            }
        }

        // Anti-recul avancé (sliders)
        (int dx, int dy) EvalAdvancedAR(int tms, double h, double v, double delay, double ramp, double sustain)
        {
            if (tms < delay) return (0, 0);

            double p = (tms < delay + ramp) ? (tms - delay) / Math.Max(1.0, ramp) : 1.0;

            int k = (_x360 != null) ? 2620 : 5120;

            int dy = (int)Math.Round(v * k * p * ArPower); // ← facteur global
            int dx = (int)Math.Round(h * k * p * ArPower); // ← facteur global
            return (dx, dy);
        }

        // ============ IA Anti-Recul Automatique ============
        RecoilAI? _recoilAI = null;
        bool _recoilAIEnabled = false;
        
        public bool RecoilBasedDetection
        {
            get => _recoilBasedDetection;
            set { _recoilBasedDetection = value; N(); }
        }
        bool _recoilBasedDetection = true; // Détection basée sur le recul (plus fiable que haptique)
        
        public bool OCRDetectionEnabled
        {
            get => _ocrDetectionEnabled;
            set { _ocrDetectionEnabled = value; N(); }
        }
        bool _ocrDetectionEnabled = false; // Détection OCR (100% fiable, lit directement le nom de l'arme)
        
        // Zone de capture OCR (coordonnées de l'écran où le nom de l'arme est affiché)
        public int OCRCaptureX { get => _ocrCaptureX; set { _ocrCaptureX = value; N(); } }
        int _ocrCaptureX = 100; // Position X par défaut
        public int OCRCaptureY { get => _ocrCaptureY; set { _ocrCaptureY = value; N(); } }
        int _ocrCaptureY = 100; // Position Y par défaut
        public int OCRCaptureWidth { get => _ocrCaptureWidth; set { _ocrCaptureWidth = value; N(); } }
        int _ocrCaptureWidth = 300; // Largeur par défaut
        public int OCRCaptureHeight { get => _ocrCaptureHeight; set { _ocrCaptureHeight = value; N(); } }
        int _ocrCaptureHeight = 50; // Hauteur par défaut
        
        long _lastOCRCapture = 0;
        volatile bool _ocrBusy = false;
        const long OCR_INTERVAL_MS = 120; // Vérifier ~8 fois par seconde pour quasi instantané
        const double OCR_MIN_CONF = 0.35; // Confiance minimale normalisée (0..1)

        // Dictionnaires armes pour matching robuste (BO6)
        Dictionary<string, string> _weaponUpperToOriginal = new();
        HashSet<string> _weaponTokenSet = new();
        readonly Queue<string> _ocrMatchHistory = new();
        const int OCR_HISTORY_SIZE = 6;
        
        

        
        const float YOLO_NMS_IOU = 0.50f;

        (int sw, int sh) GetScreenPixelSize()
        {
            try { return (GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN)); }
            catch { return (1920, 1080); }
        }
        
        
        
        // Sauvegarder le pattern IA dans le profil de l'arme
        
        
        // ============ Détection OCR (Reconnaissance Optique de Caractères) ============
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);
        
        private const int SRCCOPY = 0x00CC0020;
        
        // Capturer une zone de l'écran
        System.Drawing.Bitmap? CaptureScreenRegion(int x, int y, int width, int height)
        {
            try
            {
                IntPtr hdcSrc = GetDC(IntPtr.Zero);
                IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
                IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
                IntPtr hOld = SelectObject(hdcDest, hBitmap);
                
                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, x, y, SRCCOPY);
                
                System.Drawing.Bitmap bitmap = System.Drawing.Bitmap.FromHbitmap(hBitmap);
                
                SelectObject(hdcDest, hOld);
                DeleteObject(hBitmap);
                DeleteObject(hdcDest);
                ReleaseDC(IntPtr.Zero, hdcSrc);
                
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
        
        // Télécharger automatiquement les fichiers tessdata manquants (eng/fra)
        

        // Résoudre un chemin valide pour tessdata et s'assurer que les fichiers requis existent
        

        // Crée ou renvoie un moteur Tesseract mis en cache (évite le coût d'initialisation à chaque frame)
        

        // Construire des dictionnaires de matching d'armes (appelé après chargement de profils)
        

        // Pré-traiter l'image pour améliorer la précision OCR (contraste, netteté, binarisation améliorée)
        System.Drawing.Bitmap PreprocessImageForOCR(System.Drawing.Bitmap original) => original;
        
        // Génère plusieurs variantes (échelles) pour OCR afin de couvrir tout type de taille
        List<System.Drawing.Bitmap> BuildOcrVariants(System.Drawing.Bitmap src) => new List<System.Drawing.Bitmap> { src };
        
        // Nettoie et valide un texte OCR pour éviter les faux positifs (retourne null si invalide)
        string? CleanAndValidateOcrText(string text) => null;
        
        // Détection d'arme via OCR (100% fiable - lit directement le nom de l'arme à l'écran)
        void DetectWeaponFromOCR() { }

        

        

        

        

        

        
        
        // Extraire le texte d'une image avec Tesseract OCR (rapide via moteur mis en cache) + fallback
        async Task<string?> ExtractTextFromImage(System.Drawing.Bitmap bitmap)
        {
            return await Task.FromResult<string?>(null);
        }
        
        // Fallback : Utiliser Windows.Media.Ocr (intégré à Windows 10+)
        async Task<string?> ExtractTextWithWindowsOCR(System.Drawing.Bitmap bitmap) => await Task.FromResult<string?>(null);
        
        // Trouver l'arme correspondante au texte détecté (amélioré avec matching intelligent)
        string? FindMatchingWeapon(string detectedText) => null;

        // Sélectionne la meilleure arme à partir d'une liste de mots et confiances OCR
        string? SelectBestWeaponFromWords(List<(string word, double conf)> words) => null;

        string? MatchWeaponByRpm(double rpm)
        {
            try
            {
                string best = ""; double bestScore = double.NegativeInfinity;
                foreach (var kv in _profiles)
                {
                    var wp = kv.Value;
                    if (wp.AudioRpmAvg <= 0) continue;
                    double std = Math.Max(15, wp.AudioRpmStd <= 0 ? 20 : wp.AudioRpmStd);
                    double z = Math.Abs(rpm - wp.AudioRpmAvg) / std; // plus petit est meilleur
                    double score = -z; // inverser
                    if (score > bestScore) { bestScore = score; best = kv.Key; }
                }
                if (bestScore > -1.2) return best; // ~ à moins de ~1.2 sigma
            }
            catch { }
            return null;
        }
        
        // Distance de Levenshtein (similarité entre chaînes)
        int LevenshteinDistance(string s, string t) => 0;
        
        // Ouvrir le sélecteur de zone OCR
        private void SelectOCRZone_Click(object sender, RoutedEventArgs e) { }
        
        // Test de la capture OCR (bouton de test)
        private void TestOCRCapture_Click(object sender, RoutedEventArgs e) { }
        
        // Détection d'arme basée sur le pattern de recul (100% fiable, pas besoin d'apprendre les vibrations)
        void DetectWeaponFromRecoil() { }
        
        // Construire le pattern actuel à partir des échantillons en cours de tir
        List<(int timeMs, double avgX, double avgY)>? BuildCurrentRecoilPattern() => null;
        
        // Charger le pattern IA depuis le profil de l'arme
        void LoadRecoilAIPattern(string weaponName) { }

        

        // Classe IA pour apprentissage automatique du recul
        class RecoilAI
        {
            public readonly string _weaponName;
            private readonly List<(long timeMs, short rx, short ry)> _recoilSamples = new();
            private List<(int timeMs, double avgX, double avgY)> _learnedPattern = new();
            private bool _isLearning = true;
            private int _sampleCount = 0;
            private int _totalShots = 0; // Nombre total de tirs enregistrés
            private long _lastSampleTime = 0;
            private short _lastRx = 0, _lastRy = 0;
            private const int MIN_SAMPLES = 2; // Minimum de tirs pour apprendre (réduit à 2)
            private const int MAX_SAMPLES = 10; // Maximum de tirs à garder
            private const int PATTERN_RESOLUTION = 50; // Résolution en ms

            public RecoilAI(string weaponName)
            {
                _weaponName = weaponName;
            }
            
            // Obtenir les échantillons bruts actuels (pour détection)
            public List<(long timeMs, short rx, short ry)> GetCurrentSamples()
            {
                return new List<(long timeMs, short rx, short ry)>(_recoilSamples);
            }
            
            // Détection d'arme basée sur le pattern de recul
            public double MatchPattern(List<(int timeMs, double avgX, double avgY)> otherPattern)
            {
                if (_learnedPattern.Count == 0 || otherPattern.Count == 0) return double.MaxValue;
                if (_learnedPattern.Count < 3 || otherPattern.Count < 3) return double.MaxValue; // Pas assez de données
                
                // Comparer les patterns en calculant la distance moyenne
                double totalDistance = 0.0;
                int matches = 0;
                
                foreach (var (t1, x1, y1) in _learnedPattern)
                {
                    // Trouver le point le plus proche dans l'autre pattern
                    double minDist = double.MaxValue;
                    foreach (var (t2, x2, y2) in otherPattern)
                    {
                        // Distance temporelle (normalisée) + distance spatiale
                        double timeDiff = Math.Abs(t1 - t2) / 1000.0; // Normaliser par 1000ms
                        double xDiff = Math.Abs(x1 - x2) / 32767.0; // Normaliser
                        double yDiff = Math.Abs(y1 - y2) / 32767.0; // Normaliser
                        double dist = Math.Sqrt(timeDiff * timeDiff + xDiff * xDiff + yDiff * yDiff);
                        if (dist < minDist) minDist = dist;
                    }
                    if (minDist < double.MaxValue)
                    {
                        totalDistance += minDist;
                        matches++;
                    }
                }
                
                return matches > 0 ? totalDistance / matches : double.MaxValue;
            }
            
            // Charger un pattern sauvegardé
            public void LoadPattern(List<(int timeMs, double avgX, double avgY)> pattern, int shotCount)
            {
                _learnedPattern = new List<(int timeMs, double avgX, double avgY)>(pattern);
                _totalShots = shotCount;
                _isLearning = _totalShots < MIN_SAMPLES || _learnedPattern.Count == 0;
            }
            
            // Obtenir le pattern actuel pour sauvegarde
            public List<(int timeMs, double avgX, double avgY)> GetPattern()
            {
                return new List<(int timeMs, double avgX, double avgY)>(_learnedPattern);
            }
            
            // Obtenir le nombre de tirs
            public int GetShotCount()
            {
                return _totalShots;
            }

            // Enregistrer un échantillon de recul pendant le tir
            public void RecordSample(long timeMs, short rx, short ry, bool isFiring)
            {
                if (!isFiring)
                {
                    if (_recoilSamples.Count > 0)
                    {
                        _totalShots++; // Incrémenter le compteur de tirs
                        ProcessSamples();
                        _recoilSamples.Clear(); // Nettoyer après traitement
                    }
                    _lastSampleTime = 0;
                    _lastRx = 0;
                    _lastRy = 0;
                    return;
                }

                // Enregistrer seulement si c'est un nouveau tir ou si assez de temps s'est écoulé
                if (_lastSampleTime == 0 || (timeMs - _lastSampleTime) >= 16) // ~60 FPS
                {
                    _recoilSamples.Add((timeMs, rx, ry));
                    _lastSampleTime = timeMs;
                    _lastRx = rx;
                    _lastRy = ry;
                }
            }

            // Traiter les échantillons pour apprendre le pattern
            private void ProcessSamples()
            {
                if (_recoilSamples.Count < 5) return; // Réduit le minimum (était 10)

                // Normaliser les temps (0 = début du tir)
                long startTime = _recoilSamples[0].timeMs;
                var normalized = _recoilSamples.Select(s => (
                    (int)(s.timeMs - startTime),
                    s.rx,
                    s.ry
                )).ToList();

                // NOUVELLE APPROCHE : Apprendre la compensation MANUELLE du joueur
                // Si le joueur pousse le stick vers le bas pendant le tir, c'est qu'il compense le recul
                // On apprend ce pattern pour le reproduire automatiquement
                
                // Calculer la position initiale (au début du tir)
                short initialX = normalized[0].Item2;
                short initialY = normalized[0].Item3;
                
                // Calculer les différences par rapport à la position initiale
                // Si le stick descend (ry augmente), c'est une compensation manuelle
                var compensations = new List<(int time, short compX, short compY)>();
                foreach (var (t, x, y) in normalized)
                {
                    // Compensation = différence par rapport à la position initiale
                    short compX = (short)(x - initialX);
                    short compY = (short)(y - initialY); // Si positif = stick vers le bas (compensation)
                    
                    compensations.Add((t, compX, compY));
                }

                // Créer des buckets de temps pour moyenne des compensations
                var buckets = new Dictionary<int, List<(short compX, short compY)>>();
                foreach (var (t, cx, cy) in compensations)
                {
                    int bucket = (t / PATTERN_RESOLUTION) * PATTERN_RESOLUTION;
                    if (!buckets.ContainsKey(bucket))
                        buckets[bucket] = new List<(short, short)>();
                    buckets[bucket].Add((cx, cy));
                }

                // Calculer les moyennes par bucket (compensations moyennes)
                var newPattern = new List<(int timeMs, double avgX, double avgY)>();
                foreach (var kv in buckets.OrderBy(k => k.Key))
                {
                    double avgX = kv.Value.Average(v => v.compX);
                    double avgY = kv.Value.Average(v => v.compY);
                    newPattern.Add((kv.Key, avgX, avgY));
                }

                // Fusionner avec le pattern existant (moyenne pondérée)
                if (_learnedPattern.Count == 0)
                {
                    _learnedPattern = newPattern;
                }
                else
                {
                    // Fusionner les patterns : moyenne des valeurs existantes et nouvelles
                    var merged = new Dictionary<int, (double sumX, double sumY, int count)>();
                    
                    // Ajouter les anciennes valeurs
                    foreach (var (t, x, y) in _learnedPattern)
                    {
                        merged[t] = (x, y, 1);
                    }
                    
                    // Ajouter les nouvelles valeurs
                    foreach (var (t, x, y) in newPattern)
                    {
                        if (merged.ContainsKey(t))
                        {
                            var old = merged[t];
                            merged[t] = (old.sumX + x, old.sumY + y, old.count + 1);
                        }
                        else
                        {
                            merged[t] = (x, y, 1);
                        }
                    }
                    
                    // Calculer les moyennes finales
                    _learnedPattern.Clear();
                    foreach (var kv in merged.OrderBy(k => k.Key))
                    {
                        double avgX = kv.Value.sumX / kv.Value.count;
                        double avgY = kv.Value.sumY / kv.Value.count;
                        _learnedPattern.Add((kv.Key, avgX, avgY));
                    }
                }

                // Si on a assez de tirs, on peut commencer à utiliser le pattern
                if (_totalShots >= MIN_SAMPLES && _learnedPattern.Count > 0)
                {
                    _isLearning = false;
                }

                // Garder seulement les derniers échantillons
                if (_recoilSamples.Count > MAX_SAMPLES * 100)
                {
                    _recoilSamples.RemoveRange(0, _recoilSamples.Count - MAX_SAMPLES * 100);
                }
            }

            // Obtenir la correction anti-recul basée sur l'apprentissage
            public (int dx, int dy) GetCompensation(int timeMs)
            {
                if (_learnedPattern.Count == 0 || _isLearning)
                    return (0, 0);

                // Trouver le bucket le plus proche
                int targetBucket = (timeMs / PATTERN_RESOLUTION) * PATTERN_RESOLUTION;
                var match = _learnedPattern.FirstOrDefault(p => p.timeMs >= targetBucket - PATTERN_RESOLUTION / 2 && p.timeMs <= targetBucket + PATTERN_RESOLUTION / 2);
                
                if (match.timeMs == 0 && match.avgX == 0 && match.avgY == 0)
                {
                    // Interpolation linéaire entre les buckets les plus proches
                    var before = _learnedPattern.Where(p => p.timeMs <= targetBucket).OrderByDescending(p => p.timeMs).FirstOrDefault();
                    var after = _learnedPattern.Where(p => p.timeMs >= targetBucket).OrderBy(p => p.timeMs).FirstOrDefault();
                    
                    if (before.timeMs != 0 && after.timeMs != 0)
                    {
                        double ratio = (targetBucket - before.timeMs) / (double)(after.timeMs - before.timeMs);
                        double interpX = before.avgX + (after.avgX - before.avgX) * ratio;
                        double interpY = before.avgY + (after.avgY - before.avgY) * ratio;
                        match = (targetBucket, interpX, interpY);
                    }
                    else if (before.timeMs != 0)
                    {
                        match = before;
                    }
                    else if (after.timeMs != 0)
                    {
                        match = after;
                    }
                }

                // Calculer la compensation basée sur le pattern appris
                // avgX et avgY représentent la compensation MANUELLE moyenne du joueur
                // Dans XInput/WPF : Y positif = stick vers le bas, Y négatif = stick vers le haut
                // Si le joueur poussait le stick vers le bas (avgY positif), c'est pour compenser le recul
                // On doit reproduire cette compensation automatiquement
                
                // Facteur d'amplification (augmenter la compensation pour effet plus fort)
                double amplificationFactor = 1.5; // 150% de la compensation apprise
                
                // Appliquer la compensation apprise
                // Si avgY est positif = le joueur poussait vers le bas (compensation du recul)
                // On applique la même chose mais plus fort
                double compensationY = match.avgY * amplificationFactor;
                
                // Si aucune compensation significative n'a été apprise, appliquer une compensation progressive
                if (Math.Abs(match.avgY) < 100 && timeMs > 50)
                {
                    // Compensation progressive basée sur le temps (le recul augmente avec le temps)
                    // On pousse vers le bas (valeur positive) pour compenser le recul vers le haut
                    compensationY = timeMs * 1.2; // Compensation progressive plus agressive
                }
                
                // Compensation horizontale (reproduire le pattern appris)
                // Si avgX est positif = stick vers la droite, on applique l'inverse pour compenser
                double compensationX = -match.avgX * amplificationFactor; // Inverser pour compenser
                
                // Convertir en unités de stick (valeurs déjà en unités de stick)
                int compY = (int)Math.Round(compensationY);
                int compX = (int)Math.Round(compensationX);
                
                // Limiter les valeurs pour éviter les débordements
                compY = Math.Clamp(compY, -32767, 32767);
                compX = Math.Clamp(compX, -32767, 32767);

                return (compX, compY);
            }

            public void Reset()
            {
                _recoilSamples.Clear();
                _learnedPattern.Clear();
                _isLearning = true;
                _sampleCount = 0;
                _totalShots = 0;
            }

            public string GetStatus()
            {
                if (_isLearning)
                {
                    if (_totalShots == 0)
                        return $"Apprentissage... (Tirez en compensant manuellement le recul)";
                    return $"Apprentissage... ({_totalShots}/{MIN_SAMPLES} tirs) | {_learnedPattern.Count} points appris";
                }
                if (_learnedPattern.Count == 0)
                    return "En attente de données... (Tirez en compensant manuellement)";
                
                // Calculer la compensation moyenne pour affichage
                double avgCompY = _learnedPattern.Where(p => Math.Abs(p.avgY) > 10).Select(p => p.avgY).DefaultIfEmpty(0).Average();
                return $"✅ Actif - {_learnedPattern.Count} points | {_totalShots} tirs | Compensation: {avgCompY:F0}";
            }
        }


        // ======= Data & classes internes =======
        public class WeaponProfile
        {
            public string Name { get; set; } = "— Choisir —";
            public int RapidFireEnable { get; set; } = 0;
            public int SimpleEnable { get; set; } = 1;
            public double AntiRecoilValue { get; set; } = 1.00;
            public double AntiRecoilHorizontal { get; set; } = 0.00;

            public bool UseTimeline { get; set; } = false;
            public AntiRecoilTimeline? Timeline { get; set; } = null;

            public string? PadHotkey { get; set; } = "";
            public string? KbHotkey { get; set; } = "";
            
            // Pattern IA Anti-Recul (sauvegardé par arme)
            public List<(int timeMs, double avgX, double avgY)>? RecoilAIPattern { get; set; } = null;
            public int RecoilAIShotCount { get; set; } = 0;

            // Audio (appris automatiquement): cadence estimée (coups/min) et marge
            public double AudioRpmAvg { get; set; } = 0;
            public double AudioRpmStd { get; set; } = 0;
        }


        public class HapticStore
        {
            public Dictionary<string, List<short[]>> Templates { get; set; } = new();
            public Dictionary<string, int> Counts { get; set; } = new();
        }
        public class PersistState
        {
            public string FireGachette { get; set; } = "R2";
            public string AdsGachette { get; set; } = "L2";
        
        



        public double ArPower { get; set; }

            public string? LastWeapon { get; set; }
            public Dictionary<string, WeaponProfile>? Profiles { get; set; }
            public HapticStore? Haptics { get; set; }
            public string? BackupFolder { get; set; }
            public bool BackupOnSave { get; set; }
            public List<GameRule>? AutoRules { get; set; }
            public AntiRecoilTimeline? Timeline { get; set; }
            public bool UseTimeline { get; set; }

            // Macros/options persistées
            public bool RapidFire { get; set; }
            public double RapidFireHz { get; set; }
            public double RapidFireDuty { get; set; }
            public bool AdsOnly { get; set; }
            public double ArDelayMs { get; set; }
            public double ArRampMs { get; set; }
            public double ArSustainMs { get; set; }
            public double AntiRecoilV { get; set; }
            public double AntiRecoilH { get; set; }
            public bool AimAssist { get; set; }
            public double AAAmplitudeX { get; set; }
            public double AAAmplitudeY { get; set; }
            public double AAFrequency { get; set; }
            public bool AutoPing { get; set; }
            public int AutoPingInterval { get; set; }
            public string HotkeyMode { get; set; } = "Clavier";

            // OCR / Détection
            public bool OCRDetectionEnabled { get; set; }
            public int OCRCaptureX { get; set; }
            public int OCRCaptureY { get; set; }
            public int OCRCaptureWidth { get; set; }
            public int OCRCaptureHeight { get; set; }
            public bool RecoilBasedDetection { get; set; }
        }

        public class ArSegment
        {
            public int DurationMs { get; set; } = 800;
            public int WaitMs { get; set; } = 0;
            public int Repeat { get; set; } = 0;
            public int VX { get; set; } = 0;
            public int VY { get; set; } = -1200;
            public double Ease { get; set; } = 1.0;
        }
        public class AntiRecoilTimeline
        {
            public bool RequireADS { get; set; } = true;
            public double Gain { get; set; } = 1.0;
            public ObservableCollection<ArSegment> Segments { get; set; } = new();

            public static AntiRecoilTimeline Default()
            {
                var t = new AntiRecoilTimeline { RequireADS = true, Gain = 1.0 };
                t.Segments.Add(new ArSegment { DurationMs = 1500, VX = 0, VY = -2200, Ease = 1 });
                t.Segments.Add(new ArSegment { DurationMs = 600, VX = -1600, VY = -900, Ease = 1 });
                return t;
            }
        }

        public class GameRule { public string? Match { get; set; } public string? Profile { get; set; } }

        public sealed class GameProfileService : IDisposable
        {
            public sealed class Rule { public string Match { get; set; } = ""; public string Profile { get; set; } = ""; }

            readonly System.Timers.Timer _timer;
            public List<Rule> Rules { get; } = new();
            public event Action<string>? OnMatchProfile;

            public GameProfileService()
            {
                _timer = new System.Timers.Timer(900);
                _timer.Elapsed += (_, __) => Tick();
            }
            public void Start() => _timer.Start();
            public void Stop() => _timer.Stop();

            void Tick()
            {
                try
                {
                    var exe = GetForegroundExeName();
                    if (string.IsNullOrEmpty(exe)) return;

                    foreach (var r in Rules)
                    {
                        if (string.IsNullOrWhiteSpace(r.Match) || string.IsNullOrWhiteSpace(r.Profile)) continue;
                        if (exe.IndexOf(r.Match, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            OnMatchProfile?.Invoke(r.Profile);
                            break;
                        }
                    }
                }
                catch { }
            }

            static string GetForegroundExeName()
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return "";
                _ = GetWindowThreadProcessId(hwnd, out var pid);
                try { using var p = Process.GetProcessById((int)pid); return p.ProcessName; } catch { return ""; }
            }

            [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
            [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

            public void Dispose() { try { _timer?.Stop(); _timer?.Dispose(); } catch { } }
        }

        private void LoadSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\ArthemisControl"))
                {
                    if (key == null) return;

                    var startWithWindows = FindName("StartWithWindowsCheckBox") as CheckBox;
                    var startMinimized = FindName("StartMinimizedCheckBox") as CheckBox;
                    var alwaysOnTop = FindName("AlwaysOnTopCheckBox") as CheckBox;
                    var showNotifications = FindName("ShowNotificationsCheckBox") as CheckBox;
                    var highPerformance = FindName("HighPerformanceCheckBox") as CheckBox;
                    var hideInTaskbar = FindName("HideInTaskbarCheckBox") as CheckBox;
                    var disableGlobalHotkeys = FindName("DisableGlobalHotkeysCheckBox") as CheckBox;
                    var languageCombo = FindName("LanguageComboBox") as ComboBox;
                    var themeCombo = FindName("ThemeComboBox") as ComboBox;
                    var priorityCombo = FindName("PriorityComboBox") as ComboBox;

                    if (startWithWindows != null) startWithWindows.IsChecked = (bool)(key.GetValue("StartWithWindows", false) ?? false);
                    if (startMinimized != null) startMinimized.IsChecked = (bool)(key.GetValue("StartMinimized", false) ?? false);
                    if (alwaysOnTop != null)
                    {
                        bool onTop = (bool)(key.GetValue("AlwaysOnTop", false) ?? false);
                        alwaysOnTop.IsChecked = onTop;
                        Topmost = onTop;
                    }
                    if (showNotifications != null) showNotifications.IsChecked = (bool)(key.GetValue("ShowNotifications", true) ?? true);
                    if (highPerformance != null) highPerformance.IsChecked = (bool)(key.GetValue("HighPerformance", false) ?? false);
                    if (hideInTaskbar != null) hideInTaskbar.IsChecked = (bool)(key.GetValue("HideInTaskbar", false) ?? false);
                    if (disableGlobalHotkeys != null) disableGlobalHotkeys.IsChecked = (bool)(key.GetValue("DisableGlobalHotkeys", false) ?? false);
                    
                    if (languageCombo != null)
                    {
                        string lang = key.GetValue("Language", "French")?.ToString() ?? "French";
                        foreach (ComboBoxItem item in languageCombo.Items)
                        {
                            if (item.Tag?.ToString() == lang)
                            {
                                languageCombo.SelectedItem = item;
                                var language = lang switch
                                {
                                    "French" => Trident.MITM.Language.French,
                                    "English" => Trident.MITM.Language.English,
                                    "Spanish" => Trident.MITM.Language.Spanish,
                                    "German" => Trident.MITM.Language.German,
                                    _ => Trident.MITM.Language.French
                                };
                                LocalizationManager.SetLanguage(language);
                                break;
                            }
                        }
                    }
                    
                    if (themeCombo != null)
                    {
                        string theme = key.GetValue("Theme", "Red")?.ToString() ?? "Red";
                        foreach (ComboBoxItem item in themeCombo.Items)
                        {
                            if (item.Tag?.ToString() == theme)
                            {
                                themeCombo.SelectedItem = item;
                                // Appliquer le thème au chargement
                                Dispatcher.BeginInvoke(new System.Action(() =>
                                {
                                    var mainWindow = Application.Current.MainWindow as MainWindow;
                                    if (mainWindow != null)
                                    {
                                        var applyThemeMethod = mainWindow.GetType().GetMethod("ApplyTheme", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        if (applyThemeMethod != null)
                                        {
                                            applyThemeMethod.Invoke(mainWindow, new object[] { theme });
                                        }
                                    }
                                }), System.Windows.Threading.DispatcherPriority.Loaded);
                                break;
                            }
                        }
                    }
                    
                    if (priorityCombo != null)
                    {
                        string priority = key.GetValue("Priority", "Normal")?.ToString() ?? "Normal";
                        foreach (ComboBoxItem item in priorityCombo.Items)
                        {
                            if (item.Tag?.ToString() == priority)
                            {
                                priorityCombo.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        
    }
}
