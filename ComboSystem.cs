// ComboSystem.cs - Advanced combo detection and execution
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace Trident.MITM
{
    /// <summary>
    /// Represents a single input in a combo sequence
    /// </summary>
    public class ComboInput
    {
        public string ButtonName { get; set; } = string.Empty;
        public bool RequirePress { get; set; } = true; // true = press, false = release
        public int MaxDelayMs { get; set; } = 500; // max time to next input
    }

    /// <summary>
    /// Represents a complete combo with its trigger action
    /// </summary>
    public class Combo
    {
        public string Name { get; set; } = "New Combo";
        public string Description { get; set; } = string.Empty;
        public List<ComboInput> Inputs { get; set; } = new();
        public string ActionType { get; set; } = "ExecuteButtons"; // ExecuteButtons, PlayMacro, ActivateProfile
        public List<string> ActionParameters { get; set; } = new(); // depends on ActionType
        public bool IsEnabled { get; set; } = true;
        public int SuccessCount { get; set; } = 0; // statistics
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Advanced combo detection system for fighting game style inputs
    /// </summary>
    public class ComboSystem
    {
        private readonly Dictionary<string, Combo> _combos = new();
        private readonly Dictionary<string, ComboState> _comboStates = new();
        private readonly object _lock = new();

        public event EventHandler<ComboEventArgs>? ComboDetected;
        public event EventHandler<ComboProgressEventArgs>? ComboProgress;

        public IReadOnlyDictionary<string, Combo> Combos => _combos;

        private class ComboState
        {
            public int CurrentStep { get; set; }
            public Stopwatch Timer { get; set; } = new();
            public bool IsActive { get; set; }
        }

        /// <summary>
        /// Add a new combo
        /// </summary>
        public void AddCombo(Combo combo)
        {
            lock (_lock)
            {
                _combos[combo.Name] = combo;
                _comboStates[combo.Name] = new ComboState();
            }
        }

        /// <summary>
        /// Remove a combo
        /// </summary>
        public bool RemoveCombo(string comboName)
        {
            lock (_lock)
            {
                _comboStates.Remove(comboName);
                return _combos.Remove(comboName);
            }
        }

        /// <summary>
        /// Process a button input and check for combo matches
        /// </summary>
        public List<Combo> ProcessInput(string buttonName, bool isPress)
        {
            var detectedCombos = new List<Combo>();

            lock (_lock)
            {
                foreach (var kvp in _combos)
                {
                    var combo = kvp.Value;
                    if (!combo.IsEnabled)
                        continue;

                    var state = _comboStates[combo.Name];

                    // Check if this input matches the current expected step
                    if (state.CurrentStep < combo.Inputs.Count)
                    {
                        var expectedInput = combo.Inputs[state.CurrentStep];

                        // Check if input matches
                        if (expectedInput.ButtonName == buttonName &&
                            expectedInput.RequirePress == isPress)
                        {
                            // Check timing
                            if (state.CurrentStep == 0 || state.Timer.ElapsedMilliseconds <= expectedInput.MaxDelayMs)
                            {
                                state.CurrentStep++;
                                state.Timer.Restart();

                                // Notify progress
                                ComboProgress?.Invoke(this, new ComboProgressEventArgs(
                                    combo, state.CurrentStep, combo.Inputs.Count));

                                // Check if combo is complete
                                if (state.CurrentStep >= combo.Inputs.Count)
                                {
                                    // Combo detected!
                                    combo.SuccessCount++;
                                    detectedCombos.Add(combo);
                                    ComboDetected?.Invoke(this, new ComboEventArgs(combo));

                                    // Reset state
                                    state.CurrentStep = 0;
                                    state.Timer.Reset();
                                }
                            }
                            else
                            {
                                // Timeout - reset
                                ResetComboState(state);
                            }
                        }
                        else if (state.CurrentStep > 0)
                        {
                            // Wrong input - reset if we were in progress
                            ResetComboState(state);
                        }
                    }
                }
            }

            return detectedCombos;
        }

        /// <summary>
        /// Reset all combo states
        /// </summary>
        public void ResetAllStates()
        {
            lock (_lock)
            {
                foreach (var state in _comboStates.Values)
                {
                    ResetComboState(state);
                }
            }
        }

        /// <summary>
        /// Get combo by name
        /// </summary>
        public Combo? GetCombo(string comboName)
        {
            _combos.TryGetValue(comboName, out var combo);
            return combo;
        }

        /// <summary>
        /// Enable or disable a combo
        /// </summary>
        public void SetComboEnabled(string comboName, bool enabled)
        {
            lock (_lock)
            {
                if (_combos.TryGetValue(comboName, out var combo))
                {
                    combo.IsEnabled = enabled;
                }
            }
        }

        /// <summary>
        /// Create a common fighting game combo (e.g., Hadouken: ↓↘→ + Punch)
        /// </summary>
        public Combo CreateFightingGameCombo(string name, string[] directions, string finishButton, string actionType = "ExecuteButtons", params string[] actionParams)
        {
            var combo = new Combo
            {
                Name = name,
                ActionType = actionType,
                ActionParameters = actionParams.ToList()
            };

            // Add directional inputs
            foreach (var direction in directions)
            {
                combo.Inputs.Add(new ComboInput
                {
                    ButtonName = direction,
                    RequirePress = true,
                    MaxDelayMs = 300
                });
            }

            // Add finish button
            combo.Inputs.Add(new ComboInput
            {
                ButtonName = finishButton,
                RequirePress = true,
                MaxDelayMs = 200
            });

            AddCombo(combo);
            return combo;
        }

        /// <summary>
        /// Create a quick tap combo (e.g., double-tap A)
        /// </summary>
        public Combo CreateQuickTapCombo(string name, string button, int taps, int maxDelayMs = 300, string actionType = "ExecuteButtons", params string[] actionParams)
        {
            var combo = new Combo
            {
                Name = name,
                ActionType = actionType,
                ActionParameters = actionParams.ToList()
            };

            for (int i = 0; i < taps; i++)
            {
                combo.Inputs.Add(new ComboInput
                {
                    ButtonName = button,
                    RequirePress = true,
                    MaxDelayMs = maxDelayMs
                });
            }

            AddCombo(combo);
            return combo;
        }

        /// <summary>
        /// Export combos to JSON
        /// </summary>
        public string ExportCombos()
        {
            lock (_lock)
            {
                return JsonSerializer.Serialize(_combos.Values, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
        }

        /// <summary>
        /// Import combos from JSON
        /// </summary>
        public void ImportCombos(string json, bool merge = false)
        {
            lock (_lock)
            {
                var combos = JsonSerializer.Deserialize<List<Combo>>(json);
                if (combos == null)
                    return;

                if (!merge)
                {
                    _combos.Clear();
                    _comboStates.Clear();
                }

                foreach (var combo in combos)
                {
                    _combos[combo.Name] = combo;
                    _comboStates[combo.Name] = new ComboState();
                }
            }
        }

        private void ResetComboState(ComboState state)
        {
            state.CurrentStep = 0;
            state.Timer.Reset();
            state.IsActive = false;
        }
    }

    public class ComboEventArgs : EventArgs
    {
        public Combo Combo { get; }
        public ComboEventArgs(Combo combo) => Combo = combo;
    }

    public class ComboProgressEventArgs : EventArgs
    {
        public Combo Combo { get; }
        public int CurrentStep { get; }
        public int TotalSteps { get; }
        public double Progress => (double)CurrentStep / TotalSteps;

        public ComboProgressEventArgs(Combo combo, int currentStep, int totalSteps)
        {
            Combo = combo;
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
        }
    }
}
