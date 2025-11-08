using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Microsoft.Win32;

namespace Trident.MITM
{
    public partial class MainWindow
    {
        // ============ Modes Aim Assist ============
        public enum AimAssistMode
        {
            Sinusoidal,
            Circular,
            Random,
            Linear,
            Spiral,
            Figure8
        }

        private AimAssistMode _currentAimAssistMode = AimAssistMode.Sinusoidal;
        private Random _random = new Random();

        public AimAssistMode CurrentAimAssistMode
        {
            get => _currentAimAssistMode;
            set { _currentAimAssistMode = value; N(nameof(CurrentAimAssistMode)); }
        }

        // ============ Émulation Clavier/Souris ============
        private bool _keyboardMouseEmulationEnabled = false;
        private Thread? _keyboardMouseThread;
        private CancellationTokenSource? _keyboardMouseCts;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);
        
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr hWnd);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);
        
        [DllImport("psapi.dll", CharSet = CharSet.Auto)]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, System.Text.StringBuilder lpFilename, int nSize);
        
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
        
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;
        
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private IntPtr _mouseHook = IntPtr.Zero;
        private LowLevelMouseProc _mouseProc;
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private POINT _lastMousePos;
        private bool _mousePosInitialized = false;
        private double _mouseSensitivityX = 1.0;
        private double _mouseSensitivityY = 1.0;
        // Ces valeurs seront récupérées depuis MainWindow via Dispatcher
        // Les propriétés sont définies dans MainWindow.xaml.cs
        private bool _mouseCentered = false; // Indique si la souris a été recentrée
        
        // Variables pour EMA et normalisation (comme dans le code Python)
        private double _prevEmaDeltaX = 0.0;
        private double _prevEmaDeltaY = 0.0;
        private double _prevNormDeltaX = 0.0;
        private double _prevNormDeltaY = 0.0;
        
        // Centre de l'écran (calculé une fois)
        private int _screenCenterX = 0;
        private int _screenCenterY = 0;
        private bool _screenCenterInitialized = false;
        
        // Accumulateurs pour les mouvements de souris (capturés par le hook)
        private int _mouseDeltaX = 0;
        private int _mouseDeltaY = 0;
        private readonly object _mouseLock = new object();

        public bool KeyboardMouseEmulationEnabled
        {
            get => _keyboardMouseEmulationEnabled;
            set
            {
                _keyboardMouseEmulationEnabled = value;
                if (value)
                {
                    // S'assurer que la manette virtuelle est créée avant de démarrer
                    Dispatcher.Invoke(() =>
                    {
                        EnsureVirtualConnected();
                    });
                    StartKeyboardMouseEmulation();
                }
                else
                {
                    StopKeyboardMouseEmulation();
                }
                N(nameof(KeyboardMouseEmulationEnabled));
            }
        }
        
        // Propriété publique pour vérifier si la manette virtuelle est connectée
        public bool IsVirtualControllerConnected => _x360 != null || _ds4 != null;

        private void StartKeyboardMouseEmulation()
        {
            StopKeyboardMouseEmulation();
            
            // Installer le hook de souris (doit être fait sur le thread UI)
            Dispatcher.Invoke(() =>
            {
                _mouseProc = MouseHookProc;
                IntPtr hMod = IntPtr.Zero;
                try
                {
                    using (var process = System.Diagnostics.Process.GetCurrentProcess())
                    using (var module = process.MainModule)
                    {
                        hMod = GetModuleHandle(module?.ModuleName);
                    }
                }
                catch { }
                
                if (hMod == IntPtr.Zero)
                {
                    hMod = Marshal.GetHINSTANCE(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0]);
                }
                
                _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
                
                if (_mouseHook == IntPtr.Zero)
                {
                    Log("Erreur: Impossible d'installer le hook de souris");
                }
                else
                {
                    Log("Hook de souris installé avec succès");
                }
            });
            
            _keyboardMouseCts = new CancellationTokenSource();
            _keyboardMouseThread = new Thread(() => KeyboardMouseLoop(_keyboardMouseCts.Token))
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            _keyboardMouseThread.Start();
            Log("Émulation clavier/souris activée");
        }

        private void StopKeyboardMouseEmulation()
        {
            _keyboardMouseCts?.Cancel();
            _keyboardMouseThread?.Join(500);
            _keyboardMouseCts = null;
            _keyboardMouseThread = null;
            
            // Désinstaller le hook de souris
            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
            
            // Réinitialiser les accumulateurs
            lock (_mouseLock)
            {
                _mouseDeltaX = 0;
                _mouseDeltaY = 0;
                _mousePosInitialized = false;
            }
            
            Log("Émulation clavier/souris désactivée");
        }
        
        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEMOVE)
            {
                try
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    
                    lock (_mouseLock)
                    {
                        if (_mousePosInitialized)
                        {
                            // Calculer les deltas
                            int dx = hookStruct.pt.X - _lastMousePos.X;
                            int dy = hookStruct.pt.Y - _lastMousePos.Y;
                            
                            // Accumuler tous les mouvements (même petits)
                            _mouseDeltaX += dx;
                            _mouseDeltaY += dy;
                            _lastMousePos = hookStruct.pt;
                        }
                        else
                        {
                            _lastMousePos = hookStruct.pt;
                            _mousePosInitialized = true;
                        }
                    }
                }
                catch { }
            }
            
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private void KeyboardMouseLoop(CancellationToken ct)
        {
            // Initialiser le centre de l'écran
            if (!_screenCenterInitialized)
            {
                _screenCenterX = GetSystemMetrics(SM_CXSCREEN) / 2;
                _screenCenterY = GetSystemMetrics(SM_CYSCREEN) / 2;
                _screenCenterInitialized = true;
            }
            
            // Attendre que la manette virtuelle soit créée
            int retries = 0;
            while (retries < 50 && !ct.IsCancellationRequested)
            {
                bool isConnected = false;
                Dispatcher.Invoke(() =>
                {
                    if (!IsVirtualControllerConnected)
                    {
                        EnsureVirtualConnected();
                    }
                    isConnected = IsVirtualControllerConnected;
                });
                if (isConnected) break;
                Thread.Sleep(100);
                retries++;
            }
            
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Vérifier si la manette virtuelle est connectée
                    bool isConnected = false;
                    Dispatcher.Invoke(() => { isConnected = IsVirtualControllerConnected; });
                    if (!isConnected)
                    {
                        // Essayer de recréer la manette virtuelle
                        Dispatcher.Invoke(() => { EnsureVirtualConnected(); });
                        Thread.Sleep(50);
                        continue;
                    }

                    // Lecture clavier
                    short w = GetAsyncKeyState(0x57); // W
                    short a = GetAsyncKeyState(0x41); // A
                    short s = GetAsyncKeyState(0x53); // S
                    short d = GetAsyncKeyState(0x44); // D
                    short space = GetAsyncKeyState(0x20); // Espace
                    short shift = GetAsyncKeyState(0x10); // Shift
                    short ctrl = GetAsyncKeyState(0x11); // Ctrl
                    short tab = GetAsyncKeyState(0x09); // Tab
                    short lmb = GetAsyncKeyState(0x01); // Clic gauche
                    short rmb = GetAsyncKeyState(0x02); // Clic droit

                    // Stick gauche (WASD)
                    short lx = 0, ly = 0;
                    if ((w & 0x8000) != 0) ly = 32767; // Avant
                    if ((s & 0x8000) != 0) ly = -32767; // Arrière
                    if ((a & 0x8000) != 0) lx = -32767; // Gauche
                    if ((d & 0x8000) != 0) lx = 32767; // Droite

                    // Stick droit (Souris) - Utiliser les deltas accumulés du hook
                    int deltaX = 0, deltaY = 0;
                    lock (_mouseLock)
                    {
                        deltaX = _mouseDeltaX;
                        deltaY = _mouseDeltaY;
                        _mouseDeltaX = 0;
                        _mouseDeltaY = 0;
                    }
                    
                    // Récupérer les paramètres configurables depuis MainWindow
                    double sensitivityBase = 0.1;
                    double smoothingFactor = 0.15;
                    double emaAlpha = 0.16;
                    double amplification = 10.0;
                    
                    Dispatcher.Invoke(() =>
                    {
                        sensitivityBase = MouseSensitivityBase;
                        smoothingFactor = MouseSmoothingFactor;
                        emaAlpha = MouseEmaAlpha;
                        amplification = MouseAmplification;
                    });
                    
                    // Convertir les deltas de pixels en valeurs normalisées
                    double normalized_delta_x = (deltaX * sensitivityBase) / 100.0; // Normaliser par 100 pixels
                    double normalized_delta_y = (deltaY * sensitivityBase) / 100.0;
                    
                    // Smoothing configurable
                    normalized_delta_x = _prevNormDeltaX + smoothingFactor * (normalized_delta_x - _prevNormDeltaX);
                    normalized_delta_y = _prevNormDeltaY + smoothingFactor * (normalized_delta_y - _prevNormDeltaY);
                    
                    // EMA (Exponential Moving Average) - configurable
                    double ema_delta_x = emaAlpha * normalized_delta_x + (1 - emaAlpha) * _prevEmaDeltaX;
                    double ema_delta_y = emaAlpha * normalized_delta_y + (1 - emaAlpha) * _prevEmaDeltaY;
                    
                    _prevEmaDeltaX = ema_delta_x;
                    _prevEmaDeltaY = ema_delta_y;
                    _prevNormDeltaX = normalized_delta_x;
                    _prevNormDeltaY = normalized_delta_y;
                    
                    // Convertir en valeurs de stick (-1.0 à 1.0) avec facteur d'amplification configurable
                    double right_stick_x = Math.Clamp(ema_delta_x * amplification, -1.0, 1.0);
                    double right_stick_y = Math.Clamp(ema_delta_y * amplification, -1.0, 1.0);
                    
                    // Recentrage automatique en jeu uniquement
                    bool isInGame = IsInGame();
                    if (isInGame)
                    {
                        GetCursorPos(out POINT mousePos);
                        int distance_from_center = (int)Math.Sqrt((mousePos.X - _screenCenterX) * (mousePos.X - _screenCenterX) + 
                                                                  (mousePos.Y - _screenCenterY) * (mousePos.Y - _screenCenterY));
                        
                        // Recentrer seulement si pas de mouvement récent ET distance > 1200 pixels
                        if (Math.Abs(deltaX) + Math.Abs(deltaY) < 5 && distance_from_center > 1200)
                        {
                            SetCursorPos(_screenCenterX, _screenCenterY);
                        }
                    }
                    
                    // Appliquer la courbe Bézier pour clavier/souris si définie
                    double bezierP1 = 0.5, bezierP2 = 0.5;
                    Dispatcher.Invoke(() => 
                    { 
                        bezierP1 = KbMouseBezierP1;
                        bezierP2 = KbMouseBezierP2;
                    });
                    // Appliquer la courbe Bézier si elle est configurée (différente des valeurs par défaut)
                    if (Math.Abs(bezierP1 - 0.5) > 0.01 || Math.Abs(bezierP2 - 0.5) > 0.01)
                    {
                        var kbMouseBezier = new BezierCurve(bezierP1, bezierP2);
                        right_stick_x = kbMouseBezier.ApplyToStick(right_stick_x);
                        right_stick_y = kbMouseBezier.ApplyToStick(right_stick_y);
                    }
                    
                    // S'assurer que les valeurs minimales sont suffisantes pour être détectées
                    if (Math.Abs(right_stick_x) < 0.01 && Math.Abs(right_stick_y) < 0.01)
                    {
                        right_stick_x = 0.0;
                        right_stick_y = 0.0;
                    }
                    
                    short rx = (short)(right_stick_x * 32767.0);
                    short ry = (short)(-right_stick_y * 32767.0); // Inversé pour Y

                    // Boutons
                    bool buttonA = (space & 0x8000) != 0;
                    bool buttonLB = (shift & 0x8000) != 0;
                    bool buttonLS = (ctrl & 0x8000) != 0;
                    bool buttonBack = (tab & 0x8000) != 0;
                    byte rt = (byte)((lmb & 0x8000) != 0 ? 255 : 0);
                    byte lt = (byte)((rmb & 0x8000) != 0 ? 255 : 0);

                    // Envoi vers manette virtuelle via méthode publique
                    Dispatcher.Invoke(() =>
                    {
                        SendKeyboardMouseInput(lx, ly, rx, ry, buttonA, buttonLB, buttonLS, buttonBack, lt, rt);
                    });
                }
                catch { }
                Thread.Sleep(1); // Délai minimal pour réduire la latence au maximum
            }
        }

        // ============ Détection si on est dans un jeu (vérification stricte) ============
        private bool IsInGame()
        {
            try
            {
                IntPtr foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                    return false; // Par défaut, ne pas recentrer si on ne peut pas détecter

                // Vérifier si la fenêtre est en plein écran (les jeux le sont généralement)
                RECT windowRect;
                if (!GetWindowRect(foregroundWindow, out windowRect))
                    return false;
                
                int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                int screenHeight = GetSystemMetrics(SM_CYSCREEN);
                
                // Calculer la taille de la fenêtre
                int windowWidth = windowRect.Right - windowRect.Left;
                int windowHeight = windowRect.Bottom - windowRect.Top;
                
                // Vérifier si la fenêtre est en plein écran (tolérance de 10 pixels pour les bordures)
                bool isFullscreen = (windowWidth >= screenWidth - 20 && windowHeight >= screenHeight - 20) || 
                                    IsZoomed(foregroundWindow);
                
                // Si la fenêtre n'est pas en plein écran, ce n'est probablement pas un jeu
                if (!isFullscreen)
                    return false;

                // Obtenir le nom du processus de la fenêtre active
                uint processId;
                GetWindowThreadProcessId(foregroundWindow, out processId);
                
                IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);
                if (hProcess == IntPtr.Zero)
                    return false; // Par défaut, ne pas recentrer

                try
                {
                    System.Text.StringBuilder processName = new System.Text.StringBuilder(260);
                    if (GetModuleFileNameEx(hProcess, IntPtr.Zero, processName, processName.Capacity) > 0)
                    {
                        string exePath = processName.ToString().ToLower();
                        string exeName = System.IO.Path.GetFileName(exePath);
                        
                        // Liste noire étendue : applications où on NE VEUT JAMAIS de recentrage
                        string[] blacklist = {
                            "explorer.exe",           // Bureau Windows
                            "chrome.exe",             // Chrome
                            "msedge.exe",             // Edge
                            "firefox.exe",            // Firefox
                            "opera.exe",              // Opera
                            "brave.exe",              // Brave
                            "vivaldi.exe",            // Vivaldi
                            "notepad.exe",            // Notepad
                            "notepad++.exe",          // Notepad++
                            "code.exe",               // VS Code
                            "devenv.exe",             // Visual Studio
                            "winword.exe",            // Word
                            "excel.exe",              // Excel
                            "powerpnt.exe",           // PowerPoint
                            "outlook.exe",            // Outlook
                            "discord.exe",            // Discord
                            "steam.exe",              // Steam (launcher, pas le jeu)
                            "epicgameslauncher.exe",  // Epic Games Launcher
                            "origin.exe",             // Origin
                            "uplay.exe",             // Uplay
                            "battle.net.exe",        // Battle.net
                            "spotify.exe",            // Spotify
                            "vlc.exe",                // VLC
                            "wmplayer.exe",           // Windows Media Player
                            "calc.exe",               // Calculatrice
                            "mspaint.exe",            // Paint
                            "cmd.exe",                // CMD
                            "powershell.exe",          // PowerShell
                            "wt.exe",                 // Windows Terminal
                            "taskmgr.exe",            // Gestionnaire des tâches
                            "regedit.exe",            // Éditeur de registre
                            "control.exe",            // Panneau de configuration
                            "msconfig.exe",           // Configuration système
                            "obs64.exe",              // OBS Studio
                            "obs32.exe",              // OBS Studio 32-bit
                            "streamlabs obs.exe",     // Streamlabs OBS
                            "xsplit.core.exe",       // XSplit
                            "xsplit.exe",             // XSplit
                            "nvidia share.exe",       // NVIDIA ShadowPlay
                            "radeon software.exe",    // AMD Software
                            "fraps.exe",              // FRAPS
                            "bandicam.exe",           // Bandicam
                            "dxtory.exe",            // Dxtory
                            "dxdiag.exe",             // DirectX Diagnostic Tool
                            "trident.mitm.app.exe",   // Notre application
                            "arthemis control.exe"    // Notre application
                        };
                        
                        // Vérifier si l'application est dans la liste noire
                        foreach (string blacklisted in blacklist)
                        {
                            if (exeName == blacklisted)
                                return false; // Ne pas recentrer
                        }
                        
                        // Vérifier aussi le titre de la fenêtre
                        System.Text.StringBuilder windowTitle = new System.Text.StringBuilder(256);
                        GetWindowText(foregroundWindow, windowTitle, windowTitle.Capacity);
                        string title = windowTitle.ToString().ToLower();
                        
                        // Si le titre contient des mots-clés d'applications non-jeux, ne pas recentrer
                        if (title.Contains("chrome") || title.Contains("firefox") || title.Contains("edge") || 
                            title.Contains("explorer") || title.Contains("game controller") || 
                            title.Contains("joy.cpl") || title.Contains("controller settings") ||
                            title.Contains("notepad") || title.Contains("visual studio") || 
                            title.Contains("discord") || title.Contains("steam") || title.Contains("spotify"))
                        {
                            return false; // Ne pas recentrer
                        }
                        
                        // Si la fenêtre est en plein écran ET n'est pas dans la liste noire, c'est probablement un jeu
                        return true;
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }
            catch { }
            
            // Par défaut, ne pas recentrer si on ne peut pas détecter (être prudent)
            return false;
        }

        // ============ Calcul Aim Assist selon le mode ============
        private (int ax, int ay) CalculateAimAssist(double time)
        {
            double amplitudeX = AAAmplitudeX;
            double amplitudeY = AAAmplitudeY;
            double frequency = AAFrequency;

            return _currentAimAssistMode switch
            {
                AimAssistMode.Sinusoidal => (
                    (int)(amplitudeX * Math.Sin(2 * Math.PI * frequency * time)),
                    (int)(amplitudeY * Math.Cos(2 * Math.PI * frequency * time))
                ),
                AimAssistMode.Circular => (
                    (int)(amplitudeX * Math.Sin(2 * Math.PI * frequency * time)),
                    (int)(amplitudeY * Math.Sin(2 * Math.PI * frequency * time))
                ),
                AimAssistMode.Random => (
                    (int)(amplitudeX * (_random.NextDouble() * 2 - 1)),
                    (int)(amplitudeY * (_random.NextDouble() * 2 - 1))
                ),
                AimAssistMode.Linear => (
                    (int)(amplitudeX * Math.Sin(2 * Math.PI * frequency * time)),
                    (int)(amplitudeY * (2 * Math.PI * frequency * time % 1.0))
                ),
                AimAssistMode.Spiral => (
                    (int)(amplitudeX * Math.Sin(2 * Math.PI * frequency * time) * (time % 2.0)),
                    (int)(amplitudeY * Math.Cos(2 * Math.PI * frequency * time) * (time % 2.0))
                ),
                AimAssistMode.Figure8 => (
                    (int)(amplitudeX * Math.Sin(2 * Math.PI * frequency * time)),
                    (int)(amplitudeY * Math.Sin(4 * Math.PI * frequency * time))
                ),
                _ => (0, 0)
            };
        }

        // ============ Gestion des onglets avec animations ============
        private void ShowTab(string tabName)
        {
            try
            {
                var homeTab = FindName("HomeTab") as Grid;
                var profilesTab = FindName("ProfilesTab") as Grid;
                var controllerTab = FindName("ControllerTab") as Grid;
                var keyboardMouseTab = FindName("KeyboardMouseTab") as Grid;
                var settingsTab = FindName("SettingsTab") as Grid;
                var optimizationTab = FindName("OptimizationTab") as Grid;
                var licenseTab = FindName("LicenseTab") as Grid;
                var antiRecoilConfigTab = FindName("AntiRecoilConfigTab") as Grid;
                var aimAssistConfigTab = FindName("AimAssistConfigTab") as Grid;
                var rapidFireConfigTab = FindName("RapidFireConfigTab") as Grid;
                var autoPingConfigTab = FindName("AutoPingConfigTab") as Grid;
                var deadzonesConfigTab = FindName("DeadzonesConfigTab") as Grid;
                var fortniteProfileTab = FindName("FortniteProfileTab") as Grid;
                var blackOps6ProfileTab = FindName("BlackOps6ProfileTab") as Grid;
                var warzoneProfileTab = FindName("WarzoneProfileTab") as Grid;
                var battlefield6ProfileTab = FindName("Battlefield6ProfileTab") as Grid;

                // Masquer tous les onglets
                if (homeTab != null) homeTab.Visibility = Visibility.Collapsed;
                if (profilesTab != null) profilesTab.Visibility = Visibility.Collapsed;
                if (controllerTab != null) controllerTab.Visibility = Visibility.Collapsed;
                if (keyboardMouseTab != null) keyboardMouseTab.Visibility = Visibility.Collapsed;
                if (settingsTab != null) settingsTab.Visibility = Visibility.Collapsed;
                if (optimizationTab != null) optimizationTab.Visibility = Visibility.Collapsed;
                if (licenseTab != null) licenseTab.Visibility = Visibility.Collapsed;
                if (antiRecoilConfigTab != null) antiRecoilConfigTab.Visibility = Visibility.Collapsed;
                if (aimAssistConfigTab != null) aimAssistConfigTab.Visibility = Visibility.Collapsed;
                if (rapidFireConfigTab != null) rapidFireConfigTab.Visibility = Visibility.Collapsed;
                if (autoPingConfigTab != null) autoPingConfigTab.Visibility = Visibility.Collapsed;
                if (deadzonesConfigTab != null) deadzonesConfigTab.Visibility = Visibility.Collapsed;
                if (fortniteProfileTab != null) fortniteProfileTab.Visibility = Visibility.Collapsed;
                if (blackOps6ProfileTab != null) blackOps6ProfileTab.Visibility = Visibility.Collapsed;
                if (warzoneProfileTab != null) warzoneProfileTab.Visibility = Visibility.Collapsed;
                if (battlefield6ProfileTab != null) battlefield6ProfileTab.Visibility = Visibility.Collapsed;

                // Afficher l'onglet sélectionné avec animation
                Grid? selectedTab = tabName switch
                {
                    "Home" => homeTab,
                    "Profiles" => profilesTab,
                    "Controller" => controllerTab,
                    "KeyboardMouse" => keyboardMouseTab,
                    "Settings" => settingsTab,
                    "Optimization" => optimizationTab,
                    "License" => licenseTab,
                    "AntiRecoilConfig" => antiRecoilConfigTab,
                    "AimAssistConfig" => aimAssistConfigTab,
                    "RapidFireConfig" => rapidFireConfigTab,
                    "AutoPingConfig" => autoPingConfigTab,
                    "DeadzonesConfig" => deadzonesConfigTab,
                    "FortniteProfile" => fortniteProfileTab,
                    "BlackOps6Profile" => blackOps6ProfileTab,
                    "WarzoneProfile" => warzoneProfileTab,
                    "Battlefield6Profile" => battlefield6ProfileTab,
                    _ => homeTab
                };

                if (selectedTab != null)
                {
                    selectedTab.Visibility = Visibility.Visible;
                    
                    // Animation seulement si l'onglet n'est pas déjà visible
                    if (selectedTab.Opacity < 1)
                    {
                        selectedTab.Opacity = 0;
                        selectedTab.RenderTransform = new System.Windows.Media.TranslateTransform(0, 20);
                        
                        var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
                        var slideAnim = new System.Windows.Media.Animation.DoubleAnimation(20, 0, TimeSpan.FromSeconds(0.3));
                        slideAnim.EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
                        
                        selectedTab.BeginAnimation(UIElement.OpacityProperty, anim);
                        selectedTab.RenderTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideAnim);
                    }
                }

                // Mettre à jour les styles des boutons d'onglets
                UpdateTabButtonStyles(tabName);
            }
            catch (Exception ex)
            {
                Log($"Erreur ShowTab: {ex.Message}");
            }
        }

        private void UpdateTabButtonStyles(string activeTab)
        {
            try
            {
                // Mettre à jour tous les boutons d'onglets
                var buttons = new[] 
                { 
                    ("HomeTabBtn", "Home"),
                    ("ProfilesTabBtn", "Profiles"),
                    ("ControllerTabBtn", "Controller"),
                    ("KeyboardMouseTabBtn", "KeyboardMouse"),
                    ("SettingsTabBtn", "Settings"),
                    ("OptimizationTabBtn", "Optimization"),
                    ("LicenseTabBtn", "License")
                };
                
                foreach (var (btnName, tabName) in buttons)
                {
                    if (FindName(btnName) is Button btn)
                    {
                        if (tabName == activeTab)
                        {
                            btn.Tag = "Active";
                            // Appliquer le style actif si disponible
                            if (Resources["ActiveTabButton"] is Style activeStyle)
                            {
                                btn.Style = activeStyle;
                            }
                        }
                        else
                        {
                            btn.Tag = tabName;
                            // Appliquer le style normal
                            if (Resources["TabButton"] is Style normalStyle)
                            {
                                btn.Style = normalStyle;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Erreur UpdateTabButtonStyles: {ex.Message}");
            }
        }

        // ============ Gestionnaires d'événements UI ============
        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                ShowTab(tag);
            }
        }
        
        private void ShowTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tabName)
            {
                ShowTab(tabName);
                
                // Mettre à jour les styles des boutons
                UpdateTabButtonStyles(tabName);
            }
        }
        
        // Navigation depuis les cards
        private void OpenAntiRecoil_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("AntiRecoilConfig");
        }
        
        private void OpenAimAssist_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("AimAssistConfig");
        }
        
        private void OpenRapidFire_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("RapidFireConfig");
        }
        
        
        private void OpenAutoPing_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("AutoPingConfig");
        }
        
        private void OpenDeadzones_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("DeadzonesConfig");
        }
        
        private void BackToHome_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("Home");
        }
        
        private void BackToProfiles_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("Profiles");
        }
        
        // Open Advanced Features Window
        private void OpenAdvancedFeatures_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenAdvancedFeaturesWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Advanced Features: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Appliquer les profils de jeu
        private void ApplyFortniteProfile_Click(object sender, RoutedEventArgs e)
        {
            AntiRecoilV = 1.5;
            AntiRecoilH = 0.2;
            CurrentAimAssistMode = AimAssistMode.Sinusoidal;
            AAAmplitudeX = 2000;
            AAAmplitudeY = 2000;
            AAFrequency = 8.0;
            RapidFireHz = 12.0;
            RapidFireDuty = 55.0;
        }
        
        private void ApplyBlackOps6Profile_Click(object sender, RoutedEventArgs e)
        {
            AntiRecoilV = 1.8;
            AntiRecoilH = 0.3;
            CurrentAimAssistMode = AimAssistMode.Circular;
            AAAmplitudeX = 2500;
            AAAmplitudeY = 2500;
            AAFrequency = 10.0;
            RapidFireHz = 15.0;
            RapidFireDuty = 60.0;
        }
        
        private void ApplyWarzoneProfile_Click(object sender, RoutedEventArgs e)
        {
            AntiRecoilV = 2.0;
            AntiRecoilH = 0.4;
            CurrentAimAssistMode = AimAssistMode.Spiral;
            AAAmplitudeX = 3000;
            AAAmplitudeY = 3000;
            AAFrequency = 12.0;
            RapidFireHz = 14.0;
            RapidFireDuty = 58.0;
        }
        
        private void ApplyBattlefield6Profile_Click(object sender, RoutedEventArgs e)
        {
            AntiRecoilV = 1.6;
            AntiRecoilH = 0.25;
            CurrentAimAssistMode = AimAssistMode.Linear;
            AAAmplitudeX = 2200;
            AAAmplitudeY = 2200;
            AAFrequency = 9.0;
            RapidFireHz = 11.0;
            RapidFireDuty = 52.0;
        }
        
        // Navigation vers les profils de jeu
        private void OpenFortniteProfile_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("FortniteProfile");
        }
        
        private void OpenBlackOps6Profile_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("BlackOps6Profile");
        }
        
        private void OpenWarzoneProfile_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("WarzoneProfile");
        }
        
        private void OpenBattlefield6Profile_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("Battlefield6Profile");
        }
        
        // Handlers pour les présélections Rapid-Fire
        private void LoadPistolPreset_Click(object sender, RoutedEventArgs e)
        {
            RapidFireHz = 10.0;
            RapidFireDuty = 50.0;
        }
        
        private void LoadSMGPreset_Click(object sender, RoutedEventArgs e)
        {
            RapidFireHz = 15.0;
            RapidFireDuty = 60.0;
        }
        
        private void LoadARPreset_Click(object sender, RoutedEventArgs e)
        {
            RapidFireHz = 12.0;
            RapidFireDuty = 55.0;
        }
        
        private void LoadSniperPreset_Click(object sender, RoutedEventArgs e)
        {
            RapidFireHz = 5.0;
            RapidFireDuty = 40.0;
        }
        
        private void LoadTimelineExample_Click(object sender, RoutedEventArgs e)
        {
            TimelineJson = "[{\"time\": 0, \"v\": 0.5, \"h\": 0.0}, {\"time\": 500, \"v\": 1.2, \"h\": 0.1}, {\"time\": 1000, \"v\": 1.8, \"h\": 0.2}, {\"time\": 1500, \"v\": 2.0, \"h\": 0.15}]";
        }
        
        // Handlers pour Auto-Ping presets
        private void LoadAutoPingFast_Click(object sender, RoutedEventArgs e)
        {
            AutoPingInterval = 1200;
        }
        
        private void LoadAutoPingMedium_Click(object sender, RoutedEventArgs e)
        {
            AutoPingInterval = 2000;
        }
        
        private void LoadAutoPingSlow_Click(object sender, RoutedEventArgs e)
        {
            AutoPingInterval = 4000;
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private void ResetDeadzones_Click(object sender, RoutedEventArgs e)
        {
            LDead = 0.0;
            LADZ = 0.0;
            LMax = 1.0;
            LGamma = 1.0;
            RDead = 0.0;
            RADZ = 0.0;
            RMax = 1.0;
            RGamma = 1.0;
        }
        
        private bool _statusEnabled = false;
        public bool StatusEnabled 
        { 
            get => _statusEnabled; 
            set 
            { 
                _statusEnabled = value; 
                UpdateStatusButton();
            } 
        }

        private void StatusButton_Click(object sender, RoutedEventArgs e)
        {
            StatusEnabled = !StatusEnabled;
            
            if (StatusEnabled)
            {
                // Créer la manette virtuelle quand on active le status
                // MainWindow.Extensions.cs est une classe partielle, donc on peut appeler EnsureVirtualConnected directement
                // Mais il faut d'abord s'assurer que _virtualAuto est activé
                var virtualAutoField = GetType().GetField("_virtualAuto", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (virtualAutoField != null)
                {
                    virtualAutoField.SetValue(this, true);
                }
                EnsureVirtualConnected();
                LastMessage = LocalizationManager.GetString("StatusON");
            }
            else
            {
                // Déconnecter la manette virtuelle quand on désactive
                DisconnectVirtual("déconnectée manuellement");
                LastMessage = LocalizationManager.GetString("StatusOFF");
            }
        }

        private void UpdateStatusButton()
        {
            Dispatcher.Invoke(() =>
            {
                var statusButton = FindName("StatusButton") as Button;
                if (statusButton != null)
                {
                    statusButton.Background = StatusEnabled ? 
                        new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3FB950")) : 
                        new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF3333"));
                    
                    var stackPanel = statusButton.Content as StackPanel;
                    if (stackPanel != null && stackPanel.Children.Count > 1)
                    {
                        var textBlock = stackPanel.Children[1] as TextBlock;
                        if (textBlock != null)
                        {
                            textBlock.Text = StatusEnabled ? LocalizationManager.GetString("StatusON") : LocalizationManager.GetString("StatusOFF");
                        }
                    }
                }
            });
        }
        
        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            // Configure hotkey
            LastMessage = "Configuration du raccourci clavier...";
        }

        private System.Windows.FrameworkElement? _pressedButton = null;

        private void ControllerCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var canvas = sender as System.Windows.Controls.Canvas;
            if (canvas == null) return;
            
            var pos = e.GetPosition(canvas);
            var hitElement = canvas.InputHitTest(pos) as System.Windows.FrameworkElement;
            
            if (hitElement != null)
            {
                string elementName = hitElement.Name;
                if (!string.IsNullOrEmpty(elementName))
                {
                    _pressedButton = hitElement;
                    // Appliquer un effet visuel
                    if (hitElement is System.Windows.Shapes.Ellipse ellipse)
                    {
                        ellipse.Opacity = 1.0;
                        ellipse.StrokeThickness = 4;
                    }
                    else if (hitElement is System.Windows.Shapes.Rectangle rect)
                    {
                        rect.Opacity = 1.0;
                        rect.StrokeThickness = 4;
                    }
                    
                    // Envoyer la commande à la manette virtuelle X360
                    SendButtonToVirtualX360(elementName, true);
                    LastMessage = $"Bouton {elementName} pressé";
                }
            }
        }

        private void ControllerCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Hover effects peuvent être ajoutés ici
        }

        private void ControllerCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_pressedButton != null)
            {
                string elementName = _pressedButton.Name;
                
                // Restaurer l'effet visuel
                if (_pressedButton is System.Windows.FrameworkElement element)
                {
                    var glowEffect = element.Effect as System.Windows.Media.Effects.DropShadowEffect;
                    if (glowEffect != null)
                    {
                        glowEffect.BlurRadius = 0;
                        glowEffect.Opacity = 0;
                    }
                }
                
                // Relâcher le bouton sur la manette virtuelle
                SendButtonToVirtualX360(elementName, false);
                _pressedButton = null;
            }
        }

        private void ControllerCanvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Relâcher tous les boutons si la souris quitte le canvas
            if (_pressedButton != null)
            {
                string elementName = _pressedButton.Name;
                SendButtonToVirtualX360(elementName, false);
                
                if (_pressedButton is System.Windows.FrameworkElement element)
                {
                    var glowEffect = element.Effect as System.Windows.Media.Effects.DropShadowEffect;
                    if (glowEffect != null)
                    {
                        glowEffect.BlurRadius = 0;
                        glowEffect.Opacity = 0;
                    }
                }
                
                _pressedButton = null;
            }
        }

        private void ControllerButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender != _pressedButton)
            {
                // Activer l'effet glow au survol
                if (sender is System.Windows.FrameworkElement element)
                {
                    var glowEffect = element.Effect as System.Windows.Media.Effects.DropShadowEffect;
                    if (glowEffect != null)
                    {
                        glowEffect.BlurRadius = 20;
                        glowEffect.Opacity = 0.8;
                    }
                }
            }
        }

        private void ControllerButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender != _pressedButton)
            {
                // Désactiver l'effet glow
                if (sender is System.Windows.FrameworkElement element)
                {
                    var glowEffect = element.Effect as System.Windows.Media.Effects.DropShadowEffect;
                    if (glowEffect != null)
                    {
                        glowEffect.BlurRadius = 0;
                        glowEffect.Opacity = 0;
                    }
                }
            }
        }

        private void SendButtonToVirtualX360(string elementName, bool pressed)
        {
            try
            {
                // Utiliser la réflexion pour accéder à _x360 depuis MainWindow
                var x360Field = typeof(MainWindow).GetField("_x360", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var x360 = x360Field?.GetValue(this) as Nefarius.ViGEm.Client.Targets.IXbox360Controller;
                
                if (x360 == null)
                {
                    // Essayer de connecter la manette virtuelle
                    EnsureVirtualConnected();
                    x360 = x360Field?.GetValue(this) as Nefarius.ViGEm.Client.Targets.IXbox360Controller;
                }
                
                if (x360 == null) return;
                
                // Mapping des éléments UI vers les boutons Xbox 360
                switch (elementName)
                {
                    case "ButtonA":
                        x360.SetButtonState(Xbox360Button.A, pressed);
                        break;
                    case "ButtonB":
                        x360.SetButtonState(Xbox360Button.B, pressed);
                        break;
                    case "ButtonX":
                        x360.SetButtonState(Xbox360Button.X, pressed);
                        break;
                    case "ButtonY":
                        x360.SetButtonState(Xbox360Button.Y, pressed);
                        break;
                    case "TriggerLeft":
                    case "TriggerRight":
                        // Les gâchettes sont gérées via SetSliderValue dans ControllerCanvas_MouseMove
                        break;
                    case "BumperLeft":
                        x360.SetButtonState(Xbox360Button.LeftShoulder, pressed);
                        break;
                    case "BumperRight":
                        x360.SetButtonState(Xbox360Button.RightShoulder, pressed);
                        break;
                    case "LeftStick":
                    case "RightStick":
                        // Les sticks sont gérés via SetAxisValue dans ControllerCanvas_MouseMove
                        break;
                }
                
                x360.SubmitReport();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur SendButtonToVirtualX360: {ex.Message}");
            }
        }

        private void AimAssistKbMouseToggle_Checked(object sender, RoutedEventArgs e)
        {
            var statusText = FindName("AimAssistKbMouseStatusText") as System.Windows.Controls.TextBlock;
            if (statusText != null)
            {
                statusText.Text = "ON";
                statusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 68, 68)); // AccentRed
            }
            // Activer l'émulation clavier/souris quand AIM ASSIST est activé
            KeyboardMouseEmulationEnabled = true;
            LastMessage = "Aim Assist Clavier/Souris activé - Émulation activée";
        }

        private void AimAssistKbMouseToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            var statusText = FindName("AimAssistKbMouseStatusText") as System.Windows.Controls.TextBlock;
            if (statusText != null)
            {
                statusText.Text = "OFF";
                statusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 188, 192)); // FgSecondary
            }
            // Désactiver l'émulation clavier/souris quand AIM ASSIST est désactivé
            KeyboardMouseEmulationEnabled = false;
            LastMessage = "Aim Assist Clavier/Souris désactivé - Émulation désactivée";
        }

        // Mise à jour en temps réel de la manette interactive
        public void UpdateInteractiveController(short lx, short ly, short rx, short ry, byte lt, byte rt, byte b0, byte b1)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Récupérer les éléments du canvas
                    var leftStick = FindName("LeftStick") as System.Windows.Controls.Canvas;
                    var rightStick = FindName("RightStick") as System.Windows.Controls.Canvas;
                    var buttonA = FindName("ButtonA") as System.Windows.Shapes.Ellipse;
                    var buttonB = FindName("ButtonB") as System.Windows.Shapes.Ellipse;
                    var buttonX = FindName("ButtonX") as System.Windows.Shapes.Ellipse;
                    var buttonY = FindName("ButtonY") as System.Windows.Shapes.Ellipse;
                    var triggerLeft = FindName("TriggerLeft") as System.Windows.Shapes.Rectangle;
                    var triggerRight = FindName("TriggerRight") as System.Windows.Shapes.Rectangle;

                    // Mettre à jour la position du stick gauche
                    // Zone de base : Canvas.Left="100" Canvas.Top="154" Width="90" Height="90"
                    // Indicateur : Canvas.Left="35" Canvas.Top="35" Width="20" Height="20" (relatif au parent Canvas)
                    if (leftStick != null)
                    {
                        var indicator = leftStick.FindName("LeftStickIndicator") as System.Windows.Shapes.Ellipse;
                        if (indicator != null)
                        {
                            double maxRange = 35; // Distance maximale depuis le centre (rayon de la zone - rayon de l'indicateur = 45 - 10)
                            
                            // Convertir les valeurs -32767..32767 en -1..1
                            double normalizedX = lx / 32767.0;
                            double normalizedY = ly / 32767.0;
                            
                            // Appliquer la position (l'indicateur fait 20x20, position initiale 35,35 pour centrer)
                            double newX = 35 + (normalizedX * maxRange); // Position relative au parent Canvas
                            double newY = 35 - (normalizedY * maxRange); // Inverser Y et position relative
                            
                            System.Windows.Controls.Canvas.SetLeft(indicator, newX);
                            System.Windows.Controls.Canvas.SetTop(indicator, newY);
                            
                            // L'indicateur reste toujours lumineux
                            indicator.Opacity = 1.0;
                        }
                    }

                    // Mettre à jour la position du stick droit
                    // Zone de base : Canvas.Left="273" Canvas.Top="223" Width="90" Height="90"
                    // Indicateur : Canvas.Left="35" Canvas.Top="35" Width="20" Height="20" (relatif au parent Canvas)
                    if (rightStick != null)
                    {
                        var indicator = rightStick.FindName("RightStickIndicator") as System.Windows.Shapes.Ellipse;
                        if (indicator != null)
                        {
                            double maxRange = 35; // Distance maximale depuis le centre
                            
                            // Convertir les valeurs -32767..32767 en -1..1
                            double normalizedX = rx / 32767.0;
                            double normalizedY = ry / 32767.0;
                            
                            // Appliquer la position
                            double newX = 35 + (normalizedX * maxRange); // Position relative au parent Canvas
                            double newY = 35 - (normalizedY * maxRange); // Inverser Y et position relative
                            
                            System.Windows.Controls.Canvas.SetLeft(indicator, newX);
                            System.Windows.Controls.Canvas.SetTop(indicator, newY);
                            
                            // L'indicateur reste toujours lumineux
                            indicator.Opacity = 1.0;
                        }
                    }

                    // Mettre à jour l'état des boutons A, B, X, Y
                    // b0: bits 0-3 = hat, bit 4 = X, bit 5 = A, bit 6 = B, bit 7 = Y
                    bool buttonAPressed = (b0 & 0x20) != 0;
                    bool buttonBPressed = (b0 & 0x40) != 0;
                    bool buttonXPressed = (b0 & 0x10) != 0;
                    bool buttonYPressed = (b0 & 0x80) != 0;

                    // Mettre à jour l'effet glow des boutons selon leur état
                    if (buttonA != null)
                    {
                        var glow = buttonA.Effect as System.Windows.Media.Effects.DropShadowEffect;
                        if (glow != null)
                        {
                            if (buttonAPressed)
                            {
                                glow.BlurRadius = 25;
                                glow.Opacity = 1.0;
                            }
                            else
                            {
                                // Garder le glow du survol s'il existe, sinon 0
                                if (glow.BlurRadius == 0) glow.Opacity = 0;
                            }
                        }
                    }
                    if (buttonB != null)
                    {
                        var glow = buttonB.Effect as System.Windows.Media.Effects.DropShadowEffect;
                        if (glow != null)
                        {
                            if (buttonBPressed)
                            {
                                glow.BlurRadius = 25;
                                glow.Opacity = 1.0;
                            }
                            else
                            {
                                if (glow.BlurRadius == 0) glow.Opacity = 0;
                            }
                        }
                    }
                    if (buttonX != null)
                    {
                        var glow = buttonX.Effect as System.Windows.Media.Effects.DropShadowEffect;
                        if (glow != null)
                        {
                            if (buttonXPressed)
                            {
                                glow.BlurRadius = 25;
                                glow.Opacity = 1.0;
                            }
                            else
                            {
                                if (glow.BlurRadius == 0) glow.Opacity = 0;
                            }
                        }
                    }
                    if (buttonY != null)
                    {
                        var glow = buttonY.Effect as System.Windows.Media.Effects.DropShadowEffect;
                        if (glow != null)
                        {
                            if (buttonYPressed)
                            {
                                glow.BlurRadius = 25;
                                glow.Opacity = 1.0;
                            }
                            else
                            {
                                if (glow.BlurRadius == 0) glow.Opacity = 0;
                            }
                        }
                    }

                    // Mettre à jour l'effet glow des gâchettes
                    // b1: bit 0 = LB, bit 1 = RB, bit 4 = Back, bit 5 = Start, bit 6 = LS, bit 7 = RS
                    // Les gâchettes LT/RT sont analogiques (0-255)
                    if (triggerLeft != null)
                    {
                        var glow = triggerLeft.Effect as System.Windows.Media.Effects.DropShadowEffect;
                        if (glow != null)
                        {
                            if (lt > 0)
                            {
                                glow.BlurRadius = 20;
                                glow.Opacity = lt / 255.0; // Opacité proportionnelle à la pression
                            }
                            else
                            {
                                if (glow.BlurRadius == 0) glow.Opacity = 0;
                            }
                        }
                    }
                    if (triggerRight != null)
                    {
                        var glow = triggerRight.Effect as System.Windows.Media.Effects.DropShadowEffect;
                        if (glow != null)
                        {
                            if (rt > 0)
                            {
                                glow.BlurRadius = 20;
                                glow.Opacity = rt / 255.0; // Opacité proportionnelle à la pression
                            }
                            else
                            {
                                if (glow.BlurRadius == 0) glow.Opacity = 0;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // Ignorer les erreurs de mise à jour UI
                System.Diagnostics.Debug.WriteLine($"Erreur UpdateInteractiveController: {ex.Message}");
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
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

                    // Sauvegarder dans le registre
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\ArthemisControl"))
                    {
                        if (startWithWindows != null) key.SetValue("StartWithWindows", startWithWindows.IsChecked ?? false);
                        if (startMinimized != null) key.SetValue("StartMinimized", startMinimized.IsChecked ?? false);
                        if (alwaysOnTop != null)
                        {
                            bool onTop = alwaysOnTop.IsChecked ?? false;
                            key.SetValue("AlwaysOnTop", onTop);
                            Topmost = onTop;
                        }
                        if (showNotifications != null) key.SetValue("ShowNotifications", showNotifications.IsChecked ?? true);
                        if (highPerformance != null) key.SetValue("HighPerformance", highPerformance.IsChecked ?? false);
                        if (hideInTaskbar != null) key.SetValue("HideInTaskbar", hideInTaskbar.IsChecked ?? false);
                        if (disableGlobalHotkeys != null) key.SetValue("DisableGlobalHotkeys", disableGlobalHotkeys.IsChecked ?? false);
                        
                        if (languageCombo?.SelectedItem is ComboBoxItem langItem && langItem.Tag is string lang)
                        {
                            key.SetValue("Language", lang);
                            var language = lang switch
                            {
                                "French" => Trident.MITM.Language.French,
                                "English" => Trident.MITM.Language.English,
                                "Spanish" => Trident.MITM.Language.Spanish,
                                "German" => Trident.MITM.Language.German,
                                _ => Trident.MITM.Language.French
                            };
                            LocalizationManager.SetLanguage(language);
                        }
                        
                        if (themeCombo?.SelectedItem is ComboBoxItem themeItem && themeItem.Tag is string theme)
                        {
                            key.SetValue("Theme", theme);
                            ApplyTheme(theme);
                        }
                        
                        if (priorityCombo?.SelectedItem is ComboBoxItem priorityItem && priorityItem.Tag is string priority)
                        {
                            key.SetValue("Priority", priority);
                            // TODO: Implémenter le changement de priorité
                        }
                    }

                    LastMessage = "Paramètres enregistrés";
                }
                catch (Exception ex)
                {
                    LastMessage = $"Erreur: {ex.Message}";
                }
            });
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
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

                if (startWithWindows != null) startWithWindows.IsChecked = false;
                if (startMinimized != null) startMinimized.IsChecked = false;
                if (alwaysOnTop != null)
                {
                    alwaysOnTop.IsChecked = false;
                    Topmost = false;
                }
                if (showNotifications != null) showNotifications.IsChecked = true;
                if (highPerformance != null) highPerformance.IsChecked = false;
                if (hideInTaskbar != null) hideInTaskbar.IsChecked = false;
                if (disableGlobalHotkeys != null) disableGlobalHotkeys.IsChecked = false;
                
                if (languageCombo != null)
                {
                    foreach (ComboBoxItem item in languageCombo.Items)
                    {
                        if (item.Tag?.ToString() == "French")
                        {
                            languageCombo.SelectedItem = item;
                            LocalizationManager.SetLanguage(Trident.MITM.Language.French);
                            break;
                        }
                    }
                }
                
                if (themeCombo != null)
                {
                    foreach (ComboBoxItem item in themeCombo.Items)
                    {
                        if (item.Tag?.ToString() == "Red")
                        {
                            themeCombo.SelectedItem = item;
                            break;
                        }
                    }
                }
                
                if (priorityCombo != null)
                {
                    foreach (ComboBoxItem item in priorityCombo.Items)
                    {
                        if (item.Tag?.ToString() == "Normal")
                        {
                            priorityCombo.SelectedItem = item;
                            break;
                        }
                    }
                }

                LastMessage = "Paramètres réinitialisés";
            });
        }
        
        private void ToggleCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // Empêcher la propagation du clic pour que le bouton parent ne soit pas déclenché
            e.Handled = true;
        }

        private void ApplyOptimizations_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // TODO: Implémenter l'application des optimisations
                    // Cette méthode devrait modifier les paramètres système via le registre Windows
                    // et les API Windows pour optimiser le PC
                    LastMessage = "Optimisations appliquées (fonctionnalité à implémenter)";
                }
                catch (Exception ex)
                {
                    LastMessage = $"Erreur: {ex.Message}";
                }
            });
        }

        private void ResetOptimizations_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // Réinitialiser tous les checkboxes et comboboxes d'optimisation
                    var checkboxes = new[] { 
                        "OptimizeNetworkLatencyCheckBox", "DisableNagleCheckBox", "HighNetworkPriorityCheckBox",
                        "OptimizeDNSCheckBox", "DisableP2PUpdateCheckBox", "DisableNetworkServicesCheckBox",
                        "DisableVSyncCheckBox", "EnableGameModeCheckBox", "DisableFullscreenOptCheckBox",
                        "DisableMouseAccelCheckBox", "DisableRawInputBufferCheckBox", "HighInputThreadPriorityCheckBox",
                        "DisableGPUSchedulingCheckBox", "HighPerformanceGPUCheckBox", "DisableGPUPowerSavingCheckBox",
                        "DisableWindowsDVRCheckBox", "DisableHardwareAccelCheckBox", "OptimizeShadersCheckBox",
                        "DisableCPUPowerThrottlingCheckBox", "DisableCPUParkingCheckBox", "HighPerformanceCPUCheckBox",
                        "DisableCPUCStatesCheckBox", "DisableTurboBoostCheckBox", "OptimizeInterruptsCheckBox",
                        "DisableWindowsDefenderCheckBox", "DisableSuperfetchCheckBox", "DisableWindowsSearchCheckBox",
                        "DisableTelemetryCheckBox", "DisableWindowsUpdateCheckBox", "DisableUnnecessaryServicesCheckBox",
                        "OptimizeMemoryCheckBox", "DisableVisualEffectsCheckBox"
                    };
                    
                    foreach (var name in checkboxes)
                    {
                        var checkbox = FindName(name) as CheckBox;
                        if (checkbox != null) checkbox.IsChecked = false;
                    }
                    
                    var combos = new Dictionary<string, string>
                    {
                        { "NetworkQoSComboBox", "Disabled" },
                        { "NetworkBufferComboBox", "Auto" },
                        { "TimerResolutionComboBox", "0.5" },
                        { "PollingRateComboBox", "1000" },
                        { "GPUPrefetchComboBox", "1" },
                        { "GPUTearingComboBox", "Disabled" },
                        { "CPUAffinityComboBox", "All" },
                        { "CPUPriorityComboBox", "Normal" },
                        { "PowerModeComboBox", "High" },
                        { "PagefileComboBox", "System" }
                    };
                    
                    foreach (var kvp in combos)
                    {
                        var combo = FindName(kvp.Key) as ComboBox;
                        if (combo != null)
                        {
                            foreach (ComboBoxItem item in combo.Items)
                            {
                                if (item.Tag?.ToString() == kvp.Value)
                                {
                                    combo.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                    
                    LastMessage = "Optimisations réinitialisées";
                }
                catch (Exception ex)
                {
                    LastMessage = $"Erreur: {ex.Message}";
                }
            });
        }

        private void ApplyTheme(string themeName)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // Utiliser les ressources de la fenêtre principale ET de l'application
                    var windowResources = Resources;
                    var appResources = Application.Current.Resources;
                    
                    System.Windows.Media.Color accentColor, accentBright, accentDark, successColor, errorColor;
                    
                    switch (themeName)
                    {
                        case "Red":
                            accentColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4444");
                            accentBright = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF6666");
                            accentDark = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CC0000");
                            successColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4444");
                            errorColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF3333");
                            break;
                            
                        case "Blue":
                            accentColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4488FF");
                            accentBright = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6699FF");
                            accentDark = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0066CC");
                            successColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4488FF");
                            errorColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3366FF");
                            break;
                            
                        case "Purple":
                            accentColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#AA44FF");
                            accentBright = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CC66FF");
                            accentDark = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#8800CC");
                            successColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#AA44FF");
                            errorColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9944FF");
                            break;
                            
                        default:
                            accentColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4444");
                            accentBright = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF6666");
                            accentDark = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CC0000");
                            successColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4444");
                            errorColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF3333");
                            break;
                    }
                    
                    // Mettre à jour les ressources de la fenêtre
                    if (windowResources["AccentRed"] is SolidColorBrush accentRed) accentRed.Color = accentColor;
                    if (windowResources["AccentRedBright"] is SolidColorBrush accentRedBright) accentRedBright.Color = accentBright;
                    if (windowResources["AccentRedDark"] is SolidColorBrush accentRedDark) accentRedDark.Color = accentDark;
                    if (windowResources["AccentCyan"] is SolidColorBrush accentCyan) accentCyan.Color = accentBright;
                    if (windowResources["Success"] is SolidColorBrush success) success.Color = successColor;
                    if (windowResources["Error"] is SolidColorBrush error) error.Color = errorColor;
                    if (windowResources["RedGlow"] is DropShadowEffect redGlow) redGlow.Color = accentColor;
                    
                    // Mettre à jour les ressources de l'application (si elles existent)
                    if (appResources["AccentRed"] is SolidColorBrush appAccentRed) appAccentRed.Color = accentColor;
                    if (appResources["AccentRedBright"] is SolidColorBrush appAccentRedBright) appAccentRedBright.Color = accentBright;
                    if (appResources["AccentRedDark"] is SolidColorBrush appAccentRedDark) appAccentRedDark.Color = accentDark;
                    if (appResources["AccentCyan"] is SolidColorBrush appAccentCyan) appAccentCyan.Color = accentBright;
                    if (appResources["Success"] is SolidColorBrush appSuccess) appSuccess.Color = successColor;
                    if (appResources["Error"] is SolidColorBrush appError) appError.Color = errorColor;
                    if (appResources["RedGlow"] is DropShadowEffect appRedGlow) appRedGlow.Color = accentColor;
                    
                    // Forcer la mise à jour de l'interface
                    InvalidateVisual();
                    UpdateLayout();
                    
                    // Mettre à jour tous les éléments visuels qui utilisent ces ressources
                    foreach (System.Windows.DependencyObject child in LogicalTreeHelper.GetChildren(this))
                    {
                        if (child is System.Windows.FrameworkElement fe)
                        {
                            fe.InvalidateVisual();
                        }
                    }
                    
                    LastMessage = $"Thème {themeName} appliqué";
                }
                catch (Exception ex)
                {
                    LastMessage = $"Erreur thème: {ex.Message}";
                }
            });
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is string lang)
            {
                var language = lang switch
                {
                    "French" => Trident.MITM.Language.French,
                    "English" => Trident.MITM.Language.English,
                    "Spanish" => Trident.MITM.Language.Spanish,
                    "German" => Trident.MITM.Language.German,
                    _ => Trident.MITM.Language.French
                };
                LocalizationManager.SetLanguage(language);
            }
        }

        private void ShowLoginForm_Click(object sender, RoutedEventArgs e)
        {
            var loginForm = FindName("LoginForm") as Border;
            var registerForm = FindName("RegisterForm") as Border;
            var loginTabButton = FindName("LoginTabButton") as Button;
            var registerTabButton = FindName("RegisterTabButton") as Button;

            if (loginForm != null) loginForm.Visibility = Visibility.Visible;
            if (registerForm != null) registerForm.Visibility = Visibility.Collapsed;
            
            if (loginTabButton != null)
            {
                loginTabButton.Style = (Style)FindResource("ModernButton");
                loginTabButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4444"));
                loginTabButton.Foreground = System.Windows.Media.Brushes.White;
            }
            
            if (registerTabButton != null)
            {
                registerTabButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E"));
                registerTabButton.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC"));
            }
        }

        private void ShowRegisterForm_Click(object sender, RoutedEventArgs e)
        {
            var loginForm = FindName("LoginForm") as Border;
            var registerForm = FindName("RegisterForm") as Border;
            var loginTabButton = FindName("LoginTabButton") as Button;
            var registerTabButton = FindName("RegisterTabButton") as Button;

            if (loginForm != null) loginForm.Visibility = Visibility.Collapsed;
            if (registerForm != null) registerForm.Visibility = Visibility.Visible;
            
            if (registerTabButton != null)
            {
                registerTabButton.Style = (Style)FindResource("ModernButton");
                registerTabButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4444"));
                registerTabButton.Foreground = System.Windows.Media.Brushes.White;
            }
            
            if (loginTabButton != null)
            {
                loginTabButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E"));
                loginTabButton.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC"));
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var usernameBox = FindName("LoginUsernameTextBox") as TextBox;
            var passwordBox = FindName("LoginPasswordBox") as PasswordBox;

            if (usernameBox == null || passwordBox == null) return;

            string username = usernameBox.Text;
            string password = passwordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                LastMessage = "Veuillez remplir tous les champs";
                return;
            }

            // TODO: Implémenter l'appel à l'API KeyAuth pour le login
            // Pour l'instant, on simule juste
            LastMessage = $"Connexion en cours pour {username}...";
            
            // Exemple d'appel API (à adapter selon votre implémentation KeyAuth)
            // var result = await KeyAuthApi.Login(username, password);
            // if (result.Success) { ... }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var usernameBox = FindName("RegisterUsernameTextBox") as TextBox;
            var passwordBox = FindName("RegisterPasswordBox") as PasswordBox;
            var licenseKeyBox = FindName("RegisterLicenseKeyTextBox") as TextBox;

            if (usernameBox == null || passwordBox == null || licenseKeyBox == null) return;

            string username = usernameBox.Text;
            string password = passwordBox.Password;
            string licenseKey = licenseKeyBox.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(licenseKey))
            {
                LastMessage = "Veuillez remplir tous les champs";
                return;
            }

            // TODO: Implémenter l'appel à l'API KeyAuth pour le register
            // Pour l'instant, on simule juste
            LastMessage = $"Inscription en cours pour {username} avec la clé {licenseKey}...";
            
            // Exemple d'appel API (à adapter selon votre implémentation KeyAuth)
            // var result = await KeyAuthApi.Register(username, password, licenseKey);
            // if (result.Success) { ... }
        }

        private void AimAssistModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is string mode)
            {
                CurrentAimAssistMode = mode switch
                {
                    "Sinusoidal" => AimAssistMode.Sinusoidal,
                    "Circular" => AimAssistMode.Circular,
                    "Random" => AimAssistMode.Random,
                    "Linear" => AimAssistMode.Linear,
                    "Spiral" => AimAssistMode.Spiral,
                    "Figure8" => AimAssistMode.Figure8,
                    _ => AimAssistMode.Sinusoidal
                };
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
        
        private void MouseSensitivityXSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                _mouseSensitivityX = Math.Clamp(slider.Value, 0.1, 20.0);
            }
        }
        
        private void MouseSensitivityYSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                _mouseSensitivityY = Math.Clamp(slider.Value, 0.1, 20.0);
            }
        }

        // ============ Propriétés de licence ============
        private string _licenseKey = "";
        private DateTime _expirationDate = DateTime.MinValue;
        private string _licenseStatus = "Unknown";

        public string LicenseKey
        {
            get => _licenseKey;
            set { _licenseKey = value; N(nameof(LicenseKey)); }
        }

        public string ExpirationDate
        {
            get => _expirationDate != DateTime.MinValue ? _expirationDate.ToString("dd/MM/yyyy HH:mm") : "N/A";
        }

        public string DaysRemaining
        {
            get
            {
                if (_expirationDate == DateTime.MinValue) return "N/A";
                var days = (_expirationDate - DateTime.Now).Days;
                return days > 0 ? $"{days} jours" : "Expirée";
            }
        }

        public string LicenseStatus
        {
            get => _licenseStatus;
            set { _licenseStatus = value; N(nameof(LicenseStatus)); N(nameof(DaysRemaining)); }
        }

        public void UpdateLicenseInfo(string key, DateTime expiration)
        {
            LicenseKey = key;
            _expirationDate = expiration;
            LicenseStatus = expiration > DateTime.Now ? "Active" : "Expired";
            N(nameof(ExpirationDate));
            N(nameof(DaysRemaining));
        }
    }
}

