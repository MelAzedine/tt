// AdvancedFeaturesIntegration.cs - Integration layer for advanced features into MainWindow
using System;
using System.Collections.Generic;
using System.Windows;

namespace Trident.MITM
{
    /// <summary>
    /// Integration helper for advanced features in the main application
    /// </summary>
    public partial class MainWindow
    {
        // Advanced feature systems
        private MacroSystem? _macroSystem;
        private ButtonRemappingSystem? _remappingSystem;
        private ComboSystem? _comboSystem;
        private AdvancedFeaturesWindow? _advancedFeaturesWindow;

        /// <summary>
        /// Initialize advanced feature systems
        /// </summary>
        private void InitializeAdvancedFeatures()
        {
            try
            {
                // Initialize macro system
                _macroSystem = new MacroSystem();
                _macroSystem.MacroRecordingStarted += OnMacroRecordingStarted;
                _macroSystem.MacroRecordingStopped += OnMacroRecordingStopped;
                _macroSystem.MacroPlaybackStarted += OnMacroPlaybackStarted;
                _macroSystem.MacroPlaybackStopped += OnMacroPlaybackStopped;
                _macroSystem.MacroActionExecuted += OnMacroActionExecuted;

                // Initialize button remapping system
                _remappingSystem = new ButtonRemappingSystem();
                _remappingSystem.ProfileActivated += OnProfileActivated;
                _remappingSystem.ButtonRemapped += OnButtonRemapped;

                // Initialize combo system
                _comboSystem = new ComboSystem();
                _comboSystem.ComboDetected += OnComboDetected;
                _comboSystem.ComboProgress += OnComboProgress;

                // Create some default combos as examples
                CreateDefaultCombos();

                Log("Advanced features initialized successfully");
            }
            catch (Exception ex)
            {
                Log($"Error initializing advanced features: {ex.Message}");
            }
        }

        /// <summary>
        /// Open the advanced features window
        /// </summary>
        private void OpenAdvancedFeaturesWindow()
        {
            if (_macroSystem == null || _remappingSystem == null || _comboSystem == null)
            {
                InitializeAdvancedFeatures();
            }

            if (_advancedFeaturesWindow == null || !_advancedFeaturesWindow.IsVisible)
            {
                _advancedFeaturesWindow = new AdvancedFeaturesWindow(
                    _macroSystem!,
                    _remappingSystem!,
                    _comboSystem!,
                    ExecuteButtonAction
                );
                _advancedFeaturesWindow.Show();
            }
            else
            {
                _advancedFeaturesWindow.Activate();
            }
        }

        /// <summary>
        /// Execute a button action (for macro playback)
        /// </summary>
        private void ExecuteButtonAction(string buttonName, bool isPress)
        {
            try
            {
                // This method would interface with the virtual controller
                // For now, just log the action
                Log($"Execute: {buttonName} {(isPress ? "Press" : "Release")}");
                
                // TODO: Implement actual button execution via ViGEm
                // Example: _virt?.SetButtonState(Xbox360Button.A, isPress);
            }
            catch (Exception ex)
            {
                Log($"Error executing button action: {ex.Message}");
            }
        }

        /// <summary>
        /// Process button input through all advanced systems
        /// </summary>
        private void ProcessAdvancedFeatures(string buttonName, bool isPress)
        {
            try
            {
                // Process through combo system
                if (_comboSystem != null)
                {
                    var detectedCombos = _comboSystem.ProcessInput(buttonName, isPress);
                    foreach (var combo in detectedCombos)
                    {
                        HandleComboAction(combo);
                    }
                }

                // Process through button remapping
                if (_remappingSystem != null && _remappingSystem.ActiveProfile != null)
                {
                    // Update modifier states for buttons that can be modifiers
                    _remappingSystem.UpdateModifierState(buttonName, isPress);
                    
                    // Get mapped button
                    var mappedButton = _remappingSystem.GetMappedButton(buttonName);
                    if (mappedButton != buttonName)
                    {
                        Log($"Button remapped: {buttonName} → {mappedButton}");
                        buttonName = mappedButton;
                    }

                    // Check for turbo mode
                    var turboSettings = _remappingSystem.GetTurboSettings(buttonName);
                    if (turboSettings != null && turboSettings.Enabled && isPress)
                    {
                        StartTurboMode(buttonName, turboSettings);
                    }
                }

                // Record macro if recording
                if (_macroSystem != null && _macroSystem.IsRecording)
                {
                    _macroSystem.RecordAction(buttonName, isPress);
                }
            }
            catch (Exception ex)
            {
                Log($"Error processing advanced features: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle combo action execution
        /// </summary>
        private void HandleComboAction(Combo combo)
        {
            try
            {
                switch (combo.ActionType)
                {
                    case "ExecuteButtons":
                        foreach (var param in combo.ActionParameters)
                        {
                            ExecuteButtonAction(param, true);
                            System.Threading.Thread.Sleep(50);
                            ExecuteButtonAction(param, false);
                        }
                        break;

                    case "PlayMacro":
                        if (combo.ActionParameters.Count > 0 && _macroSystem != null)
                        {
                            _ = _macroSystem.PlayMacroAsync(combo.ActionParameters[0], ExecuteButtonAction);
                        }
                        break;

                    case "ActivateProfile":
                        if (combo.ActionParameters.Count > 0 && _remappingSystem != null)
                        {
                            _remappingSystem.ActivateProfile(combo.ActionParameters[0]);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"Error handling combo action: {ex.Message}");
            }
        }

        /// <summary>
        /// Start turbo mode for a button
        /// </summary>
        private void StartTurboMode(string buttonName, TurboSettings settings)
        {
            // This would start a rapid-fire loop for the button
            // Implementation depends on the application's existing rapid-fire system
            Log($"Turbo mode activated for {buttonName}: {settings.FrequencyHz} Hz");
        }

        /// <summary>
        /// Create default example combos
        /// </summary>
        private void CreateDefaultCombos()
        {
            if (_comboSystem == null) return;

            try
            {
                // Double-tap A combo
                _comboSystem.CreateQuickTapCombo(
                    "Double Tap A",
                    "A",
                    2,
                    300,
                    "ExecuteButtons",
                    "B" // Execute B button when double-tap A detected
                );

                // Fighting game style combo: ↓→ + A (Hadouken)
                _comboSystem.CreateFightingGameCombo(
                    "Hadouken",
                    new[] { "DPadDown", "DPadRight" },
                    "A",
                    "ExecuteButtons",
                    "X", "Y" // Execute X and Y buttons
                );
            }
            catch (Exception ex)
            {
                Log($"Error creating default combos: {ex.Message}");
            }
        }

        #region Event Handlers for Advanced Features

        private void OnMacroRecordingStarted(object? sender, MacroEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Log($"Macro recording started: {e.Macro.Name}");
            });
        }

        private void OnMacroRecordingStopped(object? sender, MacroEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Log($"Macro recording stopped: {e.Macro.Name} ({e.Macro.ActionCount} actions)");
            });
        }

        private void OnMacroPlaybackStarted(object? sender, MacroEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Log($"Playing macro: {e.Macro.Name}");
            });
        }

        private void OnMacroPlaybackStopped(object? sender, MacroEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Log($"Macro playback complete: {e.Macro.Name}");
            });
        }

        private void OnMacroActionExecuted(object? sender, MacroActionEventArgs e)
        {
            // Optionally log each action
            // Log($"Macro action: {e.Action.ButtonName} {(e.Action.IsPress ? "Press" : "Release")}");
        }

        private void OnProfileActivated(object? sender, ProfileEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Log($"Profile activated: {e.Profile.Name}");
            });
        }

        private void OnButtonRemapped(object? sender, ButtonMappingEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Log($"Button mapped: {e.Mapping.SourceButton} → {e.Mapping.TargetButton}");
            });
        }

        private void OnComboDetected(object? sender, ComboEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Log($"🎯 COMBO DETECTED: {e.Combo.Name}!");
            });
        }

        private void OnComboProgress(object? sender, ComboProgressEventArgs e)
        {
            // Optionally show combo progress
            // Log($"Combo progress: {e.Combo.Name} ({e.CurrentStep}/{e.TotalSteps})");
        }

        #endregion

        /// <summary>
        /// Save advanced features configuration
        /// </summary>
        private void SaveAdvancedFeaturesConfig()
        {
            try
            {
                if (_macroSystem != null)
                {
                    var macrosJson = _macroSystem.ExportMacros();
                    System.IO.File.WriteAllText("macros.json", macrosJson);
                }

                if (_remappingSystem != null)
                {
                    var profilesJson = _remappingSystem.ExportAllProfiles();
                    System.IO.File.WriteAllText("button_profiles.json", profilesJson);
                }

                if (_comboSystem != null)
                {
                    var combosJson = _comboSystem.ExportCombos();
                    System.IO.File.WriteAllText("combos.json", combosJson);
                }

                Log("Advanced features configuration saved");
            }
            catch (Exception ex)
            {
                Log($"Error saving advanced features config: {ex.Message}");
            }
        }

        /// <summary>
        /// Load advanced features configuration
        /// </summary>
        private void LoadAdvancedFeaturesConfig()
        {
            try
            {
                if (_macroSystem != null && System.IO.File.Exists("macros.json"))
                {
                    var macrosJson = System.IO.File.ReadAllText("macros.json");
                    _macroSystem.ImportMacros(macrosJson, merge: false);
                    Log($"Loaded {_macroSystem.Macros.Count} macros");
                }

                if (_remappingSystem != null && System.IO.File.Exists("button_profiles.json"))
                {
                    var profilesJson = System.IO.File.ReadAllText("button_profiles.json");
                    _remappingSystem.ImportAllProfiles(profilesJson, merge: false);
                    Log($"Loaded {_remappingSystem.Profiles.Count} button profiles");
                }

                if (_comboSystem != null && System.IO.File.Exists("combos.json"))
                {
                    var combosJson = System.IO.File.ReadAllText("combos.json");
                    _comboSystem.ImportCombos(combosJson, merge: false);
                    Log($"Loaded {_comboSystem.Combos.Count} combos");
                }
            }
            catch (Exception ex)
            {
                Log($"Error loading advanced features config: {ex.Message}");
            }
        }
    }
}
