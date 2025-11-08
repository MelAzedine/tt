// ButtonRemappingSystem.cs - Advanced button remapping and customization
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trident.MITM
{
    /// <summary>
    /// Represents a button mapping configuration
    /// </summary>
    public class ButtonMapping
    {
        public string SourceButton { get; set; } = string.Empty;
        public string TargetButton { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Modifier keys for advanced button combinations
    /// </summary>
    [Flags]
    public enum ModifierKeys
    {
        None = 0,
        Shift = 1,
        Ctrl = 2,
        Alt = 4,
        L3 = 8,  // Left stick press
        R3 = 16, // Right stick press
        L1 = 32,
        R1 = 64
    }

    /// <summary>
    /// Represents a complete button mapping profile
    /// </summary>
    public class ButtonMappingProfile
    {
        public string Name { get; set; } = "Default Profile";
        public string Description { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public List<ButtonMapping> Mappings { get; set; } = new();
        public Dictionary<string, string> QuickSwap { get; set; } = new(); // button -> button quick swaps
        public bool InvertLeftStickX { get; set; } = false;
        public bool InvertLeftStickY { get; set; } = false;
        public bool InvertRightStickX { get; set; } = false;
        public bool InvertRightStickY { get; set; } = false;
        public Dictionary<string, TurboSettings> TurboButtons { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Turbo settings for individual buttons
    /// </summary>
    public class TurboSettings
    {
        public bool Enabled { get; set; } = false;
        public int FrequencyHz { get; set; } = 10; // presses per second
        public int DutyCycle { get; set; } = 50; // percentage of time button is pressed
    }

    /// <summary>
    /// Advanced button remapping and customization system
    /// </summary>
    public class ButtonRemappingSystem
    {
        private readonly Dictionary<string, ButtonMappingProfile> _profiles = new();
        private ButtonMappingProfile? _activeProfile;
        private readonly Dictionary<string, bool> _modifierStates = new();
        private readonly object _lock = new();

        public event EventHandler<ProfileEventArgs>? ProfileActivated;
        public event EventHandler<ProfileEventArgs>? ProfileChanged;
        public event EventHandler<ButtonMappingEventArgs>? ButtonRemapped;

        public IReadOnlyDictionary<string, ButtonMappingProfile> Profiles => _profiles;
        public ButtonMappingProfile? ActiveProfile => _activeProfile;

        // Standard button names for Xbox controller
        public static readonly string[] StandardButtons = new[]
        {
            "A", "B", "X", "Y",
            "DPadUp", "DPadDown", "DPadLeft", "DPadRight",
            "L1", "R1", "L2", "R2",
            "L3", "R3",
            "Start", "Select", "Guide"
        };

        public ButtonRemappingSystem()
        {
            // Create default profile
            var defaultProfile = new ButtonMappingProfile
            {
                Name = "Default",
                Description = "Default button mapping (no remapping)"
            };

            _profiles["Default"] = defaultProfile;
            _activeProfile = defaultProfile;

            InitializeModifierStates();
        }

        /// <summary>
        /// Create a new button mapping profile
        /// </summary>
        public ButtonMappingProfile CreateProfile(string name, string description = "", string gameName = "")
        {
            lock (_lock)
            {
                var profile = new ButtonMappingProfile
                {
                    Name = name,
                    Description = description,
                    GameName = gameName
                };

                _profiles[name] = profile;
                return profile;
            }
        }

        /// <summary>
        /// Activate a profile by name
        /// </summary>
        public bool ActivateProfile(string profileName)
        {
            lock (_lock)
            {
                if (!_profiles.TryGetValue(profileName, out var profile))
                    return false;

                _activeProfile = profile;
                ProfileActivated?.Invoke(this, new ProfileEventArgs(profile));
                return true;
            }
        }

        /// <summary>
        /// Add a button mapping to the active profile
        /// </summary>
        public void AddMapping(string sourceButton, string targetButton, ModifierKeys modifiers = ModifierKeys.None, string description = "")
        {
            lock (_lock)
            {
                if (_activeProfile == null)
                    throw new InvalidOperationException("No active profile");

                // Remove existing mapping for this source button with same modifiers
                _activeProfile.Mappings.RemoveAll(m =>
                    m.SourceButton == sourceButton && m.Modifiers == modifiers);

                var mapping = new ButtonMapping
                {
                    SourceButton = sourceButton,
                    TargetButton = targetButton,
                    Modifiers = modifiers,
                    Description = description,
                    IsEnabled = true
                };

                _activeProfile.Mappings.Add(mapping);
                _activeProfile.ModifiedDate = DateTime.Now;

                ButtonRemapped?.Invoke(this, new ButtonMappingEventArgs(mapping));
            }
        }

        /// <summary>
        /// Remove a button mapping
        /// </summary>
        public bool RemoveMapping(string sourceButton, ModifierKeys modifiers = ModifierKeys.None)
        {
            lock (_lock)
            {
                if (_activeProfile == null)
                    return false;

                int removed = _activeProfile.Mappings.RemoveAll(m =>
                    m.SourceButton == sourceButton && m.Modifiers == modifiers);

                if (removed > 0)
                    _activeProfile.ModifiedDate = DateTime.Now;

                return removed > 0;
            }
        }

        /// <summary>
        /// Get the mapped button for a source button, considering current modifier states
        /// </summary>
        public string GetMappedButton(string sourceButton)
        {
            lock (_lock)
            {
                if (_activeProfile == null)
                    return sourceButton;

                // Get current active modifiers
                var activeModifiers = GetActiveModifiers();

                // Find exact match with current modifiers
                var exactMatch = _activeProfile.Mappings.FirstOrDefault(m =>
                    m.IsEnabled &&
                    m.SourceButton == sourceButton &&
                    m.Modifiers == activeModifiers);

                if (exactMatch != null)
                    return exactMatch.TargetButton;

                // Find match without modifiers
                var noModifierMatch = _activeProfile.Mappings.FirstOrDefault(m =>
                    m.IsEnabled &&
                    m.SourceButton == sourceButton &&
                    m.Modifiers == ModifierKeys.None);

                if (noModifierMatch != null)
                    return noModifierMatch.TargetButton;

                // Check quick swap
                if (_activeProfile.QuickSwap.TryGetValue(sourceButton, out var swapped))
                    return swapped;

                return sourceButton;
            }
        }

        /// <summary>
        /// Update modifier state (e.g., when L1 is pressed/released)
        /// </summary>
        public void UpdateModifierState(string button, bool isPressed)
        {
            lock (_lock)
            {
                _modifierStates[button] = isPressed;
            }
        }

        /// <summary>
        /// Set turbo mode for a button
        /// </summary>
        public void SetTurboMode(string button, bool enabled, int frequencyHz = 10, int dutyCycle = 50)
        {
            lock (_lock)
            {
                if (_activeProfile == null)
                    return;

                if (!_activeProfile.TurboButtons.ContainsKey(button))
                {
                    _activeProfile.TurboButtons[button] = new TurboSettings();
                }

                var turbo = _activeProfile.TurboButtons[button];
                turbo.Enabled = enabled;
                turbo.FrequencyHz = Math.Clamp(frequencyHz, 1, 30);
                turbo.DutyCycle = Math.Clamp(dutyCycle, 10, 90);

                _activeProfile.ModifiedDate = DateTime.Now;
            }
        }

        /// <summary>
        /// Get turbo settings for a button
        /// </summary>
        public TurboSettings? GetTurboSettings(string button)
        {
            lock (_lock)
            {
                if (_activeProfile == null)
                    return null;

                _activeProfile.TurboButtons.TryGetValue(button, out var turbo);
                return turbo;
            }
        }

        /// <summary>
        /// Add a quick swap between two buttons
        /// </summary>
        public void AddQuickSwap(string button1, string button2)
        {
            lock (_lock)
            {
                if (_activeProfile == null)
                    return;

                _activeProfile.QuickSwap[button1] = button2;
                _activeProfile.QuickSwap[button2] = button1;
                _activeProfile.ModifiedDate = DateTime.Now;
            }
        }

        /// <summary>
        /// Remove a quick swap
        /// </summary>
        public void RemoveQuickSwap(string button)
        {
            lock (_lock)
            {
                if (_activeProfile == null)
                    return;

                if (_activeProfile.QuickSwap.TryGetValue(button, out var swapped))
                {
                    _activeProfile.QuickSwap.Remove(button);
                    _activeProfile.QuickSwap.Remove(swapped);
                    _activeProfile.ModifiedDate = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Delete a profile
        /// </summary>
        public bool DeleteProfile(string profileName)
        {
            lock (_lock)
            {
                if (profileName == "Default")
                    return false; // Cannot delete default profile

                if (_activeProfile?.Name == profileName)
                {
                    ActivateProfile("Default"); // Switch to default if deleting active
                }

                return _profiles.Remove(profileName);
            }
        }

        /// <summary>
        /// Export a profile to JSON
        /// </summary>
        public string ExportProfile(string profileName)
        {
            lock (_lock)
            {
                if (!_profiles.TryGetValue(profileName, out var profile))
                    throw new ArgumentException($"Profile '{profileName}' not found");

                return JsonSerializer.Serialize(profile, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
        }

        /// <summary>
        /// Import a profile from JSON
        /// </summary>
        public void ImportProfile(string json)
        {
            lock (_lock)
            {
                var profile = JsonSerializer.Deserialize<ButtonMappingProfile>(json);
                if (profile != null)
                {
                    _profiles[profile.Name] = profile;
                }
            }
        }

        /// <summary>
        /// Export all profiles to JSON
        /// </summary>
        public string ExportAllProfiles()
        {
            lock (_lock)
            {
                return JsonSerializer.Serialize(_profiles.Values, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
        }

        /// <summary>
        /// Import all profiles from JSON
        /// </summary>
        public void ImportAllProfiles(string json, bool merge = false)
        {
            lock (_lock)
            {
                var profiles = JsonSerializer.Deserialize<List<ButtonMappingProfile>>(json);
                if (profiles == null)
                    return;

                if (!merge)
                {
                    var defaultProfile = _profiles.GetValueOrDefault("Default");
                    _profiles.Clear();
                    if (defaultProfile != null)
                        _profiles["Default"] = defaultProfile;
                }

                foreach (var profile in profiles)
                {
                    if (profile.Name != "Default") // Don't overwrite default
                    {
                        _profiles[profile.Name] = profile;
                    }
                }
            }
        }

        private void InitializeModifierStates()
        {
            foreach (var button in StandardButtons)
            {
                _modifierStates[button] = false;
            }
        }

        private ModifierKeys GetActiveModifiers()
        {
            var modifiers = ModifierKeys.None;

            if (_modifierStates.GetValueOrDefault("L1"))
                modifiers |= ModifierKeys.L1;
            if (_modifierStates.GetValueOrDefault("R1"))
                modifiers |= ModifierKeys.R1;
            if (_modifierStates.GetValueOrDefault("L3"))
                modifiers |= ModifierKeys.L3;
            if (_modifierStates.GetValueOrDefault("R3"))
                modifiers |= ModifierKeys.R3;

            return modifiers;
        }
    }

    public class ProfileEventArgs : EventArgs
    {
        public ButtonMappingProfile Profile { get; }
        public ProfileEventArgs(ButtonMappingProfile profile) => Profile = profile;
    }

    public class ButtonMappingEventArgs : EventArgs
    {
        public ButtonMapping Mapping { get; }
        public ButtonMappingEventArgs(ButtonMapping mapping) => Mapping = mapping;
    }
}
