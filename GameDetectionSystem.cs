// GameDetectionSystem.cs — Automatic game detection and profile switching
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Trident.MITM
{
    /// <summary>
    /// Automatically detects running games and switches controller profiles
    /// </summary>
    public class GameDetectionSystem
    {
        private readonly Dictionary<string, string> _gameProfiles = new();
        private CancellationTokenSource? _detectionCts;
        private string? _lastDetectedGame;
        private const int DETECTION_INTERVAL_MS = 2000; // Check every 2 seconds
        
        public event Action<string, string>? GameDetected; // Game name, Profile name
        public event Action? GameClosed;
        
        public bool IsEnabled { get; set; } = false;
        
        /// <summary>
        /// Add a game process to monitor
        /// </summary>
        public void AddGameProfile(string processName, string profileName)
        {
            _gameProfiles[processName.ToLowerInvariant()] = profileName;
        }
        
        /// <summary>
        /// Remove a game from monitoring
        /// </summary>
        public void RemoveGameProfile(string processName)
        {
            _gameProfiles.Remove(processName.ToLowerInvariant());
        }
        
        /// <summary>
        /// Start monitoring for games
        /// </summary>
        public void StartDetection()
        {
            if (_detectionCts != null) return;
            
            IsEnabled = true;
            _detectionCts = new CancellationTokenSource();
            Task.Run(() => DetectionLoop(_detectionCts.Token));
        }
        
        /// <summary>
        /// Stop monitoring for games
        /// </summary>
        public void StopDetection()
        {
            IsEnabled = false;
            _detectionCts?.Cancel();
            _detectionCts = null;
            _lastDetectedGame = null;
        }
        
        private async Task DetectionLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    CheckForGames();
                    await Task.Delay(DETECTION_INTERVAL_MS, ct);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Silently continue on errors
                }
            }
        }
        
        private void CheckForGames()
        {
            try
            {
                var processes = Process.GetProcesses();
                string? detectedGame = null;
                string? detectedProfile = null;
                
                // Check if any monitored game is running
                foreach (var proc in processes)
                {
                    try
                    {
                        var processName = proc.ProcessName.ToLowerInvariant();
                        if (_gameProfiles.TryGetValue(processName, out var profile))
                        {
                            detectedGame = proc.ProcessName;
                            detectedProfile = profile;
                            break;
                        }
                    }
                    catch
                    {
                        // Skip processes we can't access
                    }
                }
                
                // Handle game state changes
                if (detectedGame != null && detectedGame != _lastDetectedGame)
                {
                    // New game detected
                    _lastDetectedGame = detectedGame;
                    GameDetected?.Invoke(detectedGame, detectedProfile!);
                }
                else if (detectedGame == null && _lastDetectedGame != null)
                {
                    // Game closed
                    _lastDetectedGame = null;
                    GameClosed?.Invoke();
                }
            }
            catch
            {
                // Silently continue on errors
            }
        }
        
        /// <summary>
        /// Load game profiles from JSON file
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return;
            
            try
            {
                var json = File.ReadAllText(filePath);
                var profiles = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                
                if (profiles != null)
                {
                    _gameProfiles.Clear();
                    foreach (var kvp in profiles)
                    {
                        _gameProfiles[kvp.Key.ToLowerInvariant()] = kvp.Value;
                    }
                }
            }
            catch
            {
                // Silently fail if file is corrupted
            }
        }
        
        /// <summary>
        /// Save game profiles to JSON file
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(_gameProfiles, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Silently fail on save errors
            }
        }
        
        /// <summary>
        /// Get all configured game profiles
        /// </summary>
        public Dictionary<string, string> GetGameProfiles()
        {
            return new Dictionary<string, string>(_gameProfiles);
        }
        
        /// <summary>
        /// Initialize with common game profiles
        /// </summary>
        public void InitializeDefaultProfiles()
        {
            AddGameProfile("FortniteClient-Win64-Shipping", "Fortnite");
            AddGameProfile("cod", "Call of Duty: Black Ops 6");
            AddGameProfile("modernwarfare", "Call of Duty: Warzone");
            AddGameProfile("bf2042", "Battlefield 6");
            AddGameProfile("RainbowSix", "Rainbow Six Siege");
            AddGameProfile("VALORANT-Win64-Shipping", "Valorant");
            AddGameProfile("ApexLegends", "Apex Legends");
            AddGameProfile("r5apex", "Apex Legends");
            AddGameProfile("destiny2", "Destiny 2");
            AddGameProfile("overwatch", "Overwatch 2");
        }
    }
}
