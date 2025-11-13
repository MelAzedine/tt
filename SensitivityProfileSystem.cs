// SensitivityProfileSystem.cs — Per-game sensitivity profiles with easy switching
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Trident.MITM
{
    /// <summary>
    /// Manage sensitivity profiles for different games
    /// </summary>
    public class SensitivityProfileSystem
    {
        private readonly Dictionary<string, SensitivityProfile> _profiles = new();
        private SensitivityProfile? _activeProfile;
        
        public event Action<SensitivityProfile>? ProfileActivated;
        public event Action<string, SensitivityProfile>? ProfileCreated;
        public event Action<string>? ProfileDeleted;
        
        /// <summary>
        /// Sensitivity profile configuration
        /// </summary>
        public class SensitivityProfile
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string GameName { get; set; } = "";
            
            // Stick settings
            public double LeftStickSensitivity { get; set; } = 1.0;
            public double RightStickSensitivity { get; set; } = 1.0;
            public double LeftStickDeadZone { get; set; } = 0.1;
            public double RightStickDeadZone { get; set; } = 0.1;
            
            // Aim settings
            public double AimSensitivityX { get; set; } = 1.0;
            public double AimSensitivityY { get; set; } = 1.0;
            public double ADSMultiplier { get; set; } = 0.7; // Sensitivity when aiming down sights
            
            // Response curves
            public string LeftStickCurveType { get; set; } = "Linear";
            public string RightStickCurveType { get; set; } = "Linear";
            public double LeftStickCurveIntensity { get; set; } = 1.0;
            public double RightStickCurveIntensity { get; set; } = 1.0;
            
            // Advanced settings
            public bool InvertYAxis { get; set; } = false;
            public bool InvertXAxis { get; set; } = false;
            public double Acceleration { get; set; } = 0.0; // 0 = no acceleration, 1 = full
            public double MaxTurnSpeed { get; set; } = 1.0; // Multiplier
            
            // Per-context sensitivity (building, driving, etc.)
            public Dictionary<string, double> ContextSensitivity { get; set; } = new();
            
            public DateTime CreatedDate { get; set; } = DateTime.Now;
            public DateTime LastModified { get; set; } = DateTime.Now;
        }
        
        /// <summary>
        /// Create a new sensitivity profile
        /// </summary>
        public SensitivityProfile CreateProfile(string name, string gameName = "")
        {
            var profile = new SensitivityProfile
            {
                Name = name,
                GameName = gameName,
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now
            };
            
            _profiles[name] = profile;
            ProfileCreated?.Invoke(name, profile);
            
            return profile;
        }
        
        /// <summary>
        /// Delete a profile
        /// </summary>
        public bool DeleteProfile(string name)
        {
            if (_profiles.Remove(name))
            {
                ProfileDeleted?.Invoke(name);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Activate a profile
        /// </summary>
        public bool ActivateProfile(string name)
        {
            if (_profiles.TryGetValue(name, out var profile))
            {
                _activeProfile = profile;
                ProfileActivated?.Invoke(profile);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get active profile
        /// </summary>
        public SensitivityProfile? GetActiveProfile()
        {
            return _activeProfile;
        }
        
        /// <summary>
        /// Get profile by name
        /// </summary>
        public SensitivityProfile? GetProfile(string name)
        {
            return _profiles.TryGetValue(name, out var profile) ? profile : null;
        }
        
        /// <summary>
        /// Get all profiles
        /// </summary>
        public List<SensitivityProfile> GetAllProfiles()
        {
            return _profiles.Values.ToList();
        }
        
        /// <summary>
        /// Update an existing profile
        /// </summary>
        public void UpdateProfile(string name, SensitivityProfile profile)
        {
            profile.LastModified = DateTime.Now;
            _profiles[name] = profile;
        }
        
        /// <summary>
        /// Clone a profile
        /// </summary>
        public SensitivityProfile CloneProfile(string sourceName, string newName)
        {
            if (!_profiles.TryGetValue(sourceName, out var source))
                throw new ArgumentException($"Profile '{sourceName}' not found");
            
            var json = JsonSerializer.Serialize(source);
            var clone = JsonSerializer.Deserialize<SensitivityProfile>(json);
            
            if (clone != null)
            {
                clone.Name = newName;
                clone.CreatedDate = DateTime.Now;
                clone.LastModified = DateTime.Now;
                _profiles[newName] = clone;
                ProfileCreated?.Invoke(newName, clone);
                return clone;
            }
            
            throw new Exception("Failed to clone profile");
        }
        
        /// <summary>
        /// Apply sensitivity adjustments to stick input
        /// </summary>
        public (double x, double y) ApplySensitivity(double x, double y, bool isRightStick, bool isADS = false)
        {
            if (_activeProfile == null)
                return (x, y);
            
            double sensitivity = isRightStick 
                ? _activeProfile.RightStickSensitivity 
                : _activeProfile.LeftStickSensitivity;
            
            // Apply ADS multiplier if aiming
            if (isADS && isRightStick)
            {
                sensitivity *= _activeProfile.ADSMultiplier;
            }
            
            // Apply inversion
            if (_activeProfile.InvertXAxis)
                x = -x;
            if (_activeProfile.InvertYAxis)
                y = -y;
            
            // Apply sensitivity
            x *= sensitivity;
            y *= sensitivity;
            
            // Apply acceleration (increases sensitivity with magnitude)
            if (_activeProfile.Acceleration > 0)
            {
                double magnitude = Math.Sqrt(x * x + y * y);
                double accelMultiplier = 1.0 + (_activeProfile.Acceleration * magnitude);
                x *= accelMultiplier;
                y *= accelMultiplier;
            }
            
            // Clamp to valid range
            x = Math.Clamp(x, -1.0, 1.0);
            y = Math.Clamp(y, -1.0, 1.0);
            
            return (x, y);
        }
        
        /// <summary>
        /// Load profiles from JSON file
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;
            
            try
            {
                var json = File.ReadAllText(filePath);
                var profiles = JsonSerializer.Deserialize<List<SensitivityProfile>>(json);
                
                if (profiles != null)
                {
                    _profiles.Clear();
                    foreach (var profile in profiles)
                    {
                        _profiles[profile.Name] = profile;
                    }
                }
            }
            catch
            {
                // Silently fail on load error
            }
        }
        
        /// <summary>
        /// Save profiles to JSON file
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                var profiles = _profiles.Values.ToList();
                var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Silently fail on save error
            }
        }
        
        /// <summary>
        /// Initialize with common game presets
        /// </summary>
        public void InitializeDefaultProfiles()
        {
            // High sensitivity for Fortnite building
            var fortnite = CreateProfile("Fortnite - Build Mode", "Fortnite");
            fortnite.RightStickSensitivity = 1.4;
            fortnite.ADSMultiplier = 0.6;
            fortnite.RightStickCurveType = "Exponential";
            fortnite.ContextSensitivity["Building"] = 1.6;
            fortnite.ContextSensitivity["Combat"] = 1.2;
            
            // Precision for tactical shooters
            var tactical = CreateProfile("CoD/Warzone - Tactical", "Call of Duty");
            tactical.RightStickSensitivity = 0.9;
            tactical.ADSMultiplier = 0.5;
            tactical.RightStickCurveType = "Exponential";
            tactical.RightStickCurveIntensity = 1.5;
            
            // Balanced for Apex
            var apex = CreateProfile("Apex Legends - Balanced", "Apex Legends");
            apex.RightStickSensitivity = 1.1;
            apex.ADSMultiplier = 0.65;
            apex.RightStickCurveType = "SCurve";
            
            // High sensitivity for Overwatch
            var overwatch = CreateProfile("Overwatch - High Sens", "Overwatch");
            overwatch.RightStickSensitivity = 1.5;
            overwatch.ADSMultiplier = 0.8;
            overwatch.Acceleration = 0.3;
        }
    }
}
