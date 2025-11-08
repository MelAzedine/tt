// MacroSystem.cs - Advanced macro recording and playback system
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Trident.MITM
{
    /// <summary>
    /// Represents a single macro action (button press or release)
    /// </summary>
    public class MacroAction
    {
        public string ButtonName { get; set; } = string.Empty;
        public bool IsPress { get; set; } // true = press, false = release
        public int DelayAfterMs { get; set; } // delay after this action
        
        [JsonIgnore]
        public long TimestampMs { get; set; } // used during recording
    }

    /// <summary>
    /// Represents a complete macro with multiple actions
    /// </summary>
    public class Macro
    {
        public string Name { get; set; } = "New Macro";
        public string Description { get; set; } = string.Empty;
        public List<MacroAction> Actions { get; set; } = new();
        public bool Loop { get; set; } = false;
        public int LoopCount { get; set; } = 1; // 0 = infinite
        public string TriggerButton { get; set; } = string.Empty; // button that triggers this macro
        public bool TriggerOnPress { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        
        public int TotalDurationMs => Actions.Sum(a => a.DelayAfterMs);
        public int ActionCount => Actions.Count;
    }

    /// <summary>
    /// Advanced macro system with recording, playback, and management
    /// </summary>
    public class MacroSystem
    {
        private readonly Dictionary<string, Macro> _macros = new();
        private Macro? _recordingMacro;
        private Stopwatch? _recordingStopwatch;
        private long _lastActionTimestamp;
        private CancellationTokenSource? _playbackCts;
        private readonly object _lock = new();

        public event EventHandler<MacroEventArgs>? MacroRecordingStarted;
        public event EventHandler<MacroEventArgs>? MacroRecordingStopped;
        public event EventHandler<MacroEventArgs>? MacroPlaybackStarted;
        public event EventHandler<MacroEventArgs>? MacroPlaybackStopped;
        public event EventHandler<MacroActionEventArgs>? MacroActionExecuted;

        public bool IsRecording { get; private set; }
        public bool IsPlaying { get; private set; }
        public IReadOnlyDictionary<string, Macro> Macros => _macros;

        /// <summary>
        /// Start recording a new macro
        /// </summary>
        public void StartRecording(string macroName, string description = "")
        {
            lock (_lock)
            {
                if (IsRecording)
                    throw new InvalidOperationException("Already recording a macro");

                _recordingMacro = new Macro
                {
                    Name = macroName,
                    Description = description
                };

                _recordingStopwatch = Stopwatch.StartNew();
                _lastActionTimestamp = 0;
                IsRecording = true;

                MacroRecordingStarted?.Invoke(this, new MacroEventArgs(_recordingMacro));
            }
        }

        /// <summary>
        /// Record a button action during macro recording
        /// </summary>
        public void RecordAction(string buttonName, bool isPress)
        {
            lock (_lock)
            {
                if (!IsRecording || _recordingMacro == null || _recordingStopwatch == null)
                    return;

                long currentTimestamp = _recordingStopwatch.ElapsedMilliseconds;
                int delayAfter = _lastActionTimestamp == 0 ? 0 : (int)(currentTimestamp - _lastActionTimestamp);

                var action = new MacroAction
                {
                    ButtonName = buttonName,
                    IsPress = isPress,
                    DelayAfterMs = delayAfter,
                    TimestampMs = currentTimestamp
                };

                // Update the delay of the previous action
                if (_recordingMacro.Actions.Count > 0)
                {
                    _recordingMacro.Actions[^1].DelayAfterMs = delayAfter;
                }

                _recordingMacro.Actions.Add(action);
                _lastActionTimestamp = currentTimestamp;
            }
        }

        /// <summary>
        /// Stop recording and save the macro
        /// </summary>
        public Macro StopRecording()
        {
            lock (_lock)
            {
                if (!IsRecording || _recordingMacro == null)
                    throw new InvalidOperationException("Not currently recording");

                IsRecording = false;
                _recordingStopwatch?.Stop();

                // Clean up the macro (remove very short delays, etc.)
                OptimizeMacro(_recordingMacro);

                // Save to dictionary
                _macros[_recordingMacro.Name] = _recordingMacro;

                MacroRecordingStopped?.Invoke(this, new MacroEventArgs(_recordingMacro));

                var completedMacro = _recordingMacro;
                _recordingMacro = null;
                _recordingStopwatch = null;

                return completedMacro;
            }
        }

        /// <summary>
        /// Play a macro by name
        /// </summary>
        public async Task PlayMacroAsync(string macroName, Action<string, bool> executeButtonAction, CancellationToken cancellationToken = default)
        {
            if (!_macros.TryGetValue(macroName, out var macro))
                throw new ArgumentException($"Macro '{macroName}' not found");

            await PlayMacroAsync(macro, executeButtonAction, cancellationToken);
        }

        /// <summary>
        /// Play a macro
        /// </summary>
        public async Task PlayMacroAsync(Macro macro, Action<string, bool> executeButtonAction, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (IsPlaying)
                    throw new InvalidOperationException("Already playing a macro");

                IsPlaying = true;
                _playbackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            try
            {
                MacroPlaybackStarted?.Invoke(this, new MacroEventArgs(macro));

                int loopCount = macro.Loop ? (macro.LoopCount == 0 ? int.MaxValue : macro.LoopCount) : 1;

                for (int loop = 0; loop < loopCount && !_playbackCts.Token.IsCancellationRequested; loop++)
                {
                    foreach (var action in macro.Actions)
                    {
                        if (_playbackCts.Token.IsCancellationRequested)
                            break;

                        // Execute the action
                        executeButtonAction(action.ButtonName, action.IsPress);
                        MacroActionExecuted?.Invoke(this, new MacroActionEventArgs(macro, action));

                        // Wait for the specified delay
                        if (action.DelayAfterMs > 0)
                        {
                            await Task.Delay(action.DelayAfterMs, _playbackCts.Token);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            finally
            {
                lock (_lock)
                {
                    IsPlaying = false;
                    _playbackCts?.Dispose();
                    _playbackCts = null;
                }

                MacroPlaybackStopped?.Invoke(this, new MacroEventArgs(macro));
            }
        }

        /// <summary>
        /// Stop playing the current macro
        /// </summary>
        public void StopPlayback()
        {
            _playbackCts?.Cancel();
        }

        /// <summary>
        /// Add or update a macro
        /// </summary>
        public void AddOrUpdateMacro(Macro macro)
        {
            lock (_lock)
            {
                macro.ModifiedDate = DateTime.Now;
                _macros[macro.Name] = macro;
            }
        }

        /// <summary>
        /// Delete a macro
        /// </summary>
        public bool DeleteMacro(string macroName)
        {
            lock (_lock)
            {
                return _macros.Remove(macroName);
            }
        }

        /// <summary>
        /// Get a macro by name
        /// </summary>
        public Macro? GetMacro(string macroName)
        {
            _macros.TryGetValue(macroName, out var macro);
            return macro;
        }

        /// <summary>
        /// Export macros to JSON
        /// </summary>
        public string ExportMacros()
        {
            lock (_lock)
            {
                return JsonSerializer.Serialize(_macros.Values, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
        }

        /// <summary>
        /// Import macros from JSON
        /// </summary>
        public void ImportMacros(string json, bool merge = false)
        {
            lock (_lock)
            {
                var macros = JsonSerializer.Deserialize<List<Macro>>(json);
                if (macros == null)
                    return;

                if (!merge)
                    _macros.Clear();

                foreach (var macro in macros)
                {
                    _macros[macro.Name] = macro;
                }
            }
        }

        /// <summary>
        /// Clear all macros
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _macros.Clear();
            }
        }

        /// <summary>
        /// Optimize macro by removing unnecessary delays and actions
        /// </summary>
        private void OptimizeMacro(Macro macro)
        {
            // Remove actions with very short delays (< 5ms) at the end
            while (macro.Actions.Count > 0 && macro.Actions[^1].DelayAfterMs < 5)
            {
                macro.Actions[^1].DelayAfterMs = 0;
            }

            // Merge consecutive press/release of same button with very short delay
            for (int i = macro.Actions.Count - 2; i >= 0; i--)
            {
                var current = macro.Actions[i];
                var next = macro.Actions[i + 1];

                if (current.ButtonName == next.ButtonName &&
                    current.IsPress && !next.IsPress &&
                    current.DelayAfterMs < 10)
                {
                    // Very quick press-release, keep but set minimum delay
                    current.DelayAfterMs = 5;
                }
            }
        }
    }

    public class MacroEventArgs : EventArgs
    {
        public Macro Macro { get; }
        public MacroEventArgs(Macro macro) => Macro = macro;
    }

    public class MacroActionEventArgs : EventArgs
    {
        public Macro Macro { get; }
        public MacroAction Action { get; }
        public MacroActionEventArgs(Macro macro, MacroAction action)
        {
            Macro = macro;
            Action = action;
        }
    }
}
