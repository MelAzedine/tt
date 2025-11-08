# 🎯 Quick Reference - New Features (v6.0.5)

## At a Glance

### 🎮 Profile Auto-Switching
**What**: Automatically detects running games and switches controller profiles
**Use**: Never manually switch profiles again
**Setup**: Enable in Settings → Auto-Switch

```csharp
_gameDetection.StartDetection(); // That's it!
```

---

### 🔋 Battery Monitoring
**What**: Shows wireless controller battery level in real-time
**Use**: Never get caught with dead battery
**Update**: Every 30 seconds

```csharp
_batteryMonitor.StartMonitoring(0);
// Status: 🟢 Battery: 75%
```

---

### 📺 Input Display Overlay
**What**: Real-time button press visualization
**Use**: Perfect for streaming, tutorials, debugging
**Toggle**: Click "Show Input Display" button

```csharp
_inputOverlay.Show();
_inputOverlay.UpdateButton("A", true);
_inputOverlay.UpdateStick("RightStick", 0.5, 0.3);
```

---

### 📈 Advanced Response Curves
**What**: 7 curve types for stick response
**Use**: Fine-tune stick feel per game
**Types**: Exponential, Logarithmic, S-Curve, Aggressive, Smooth, Power, Custom

```csharp
// Precision aiming
var output = AdvancedResponseCurves.ApplyCurve(
    input, 
    CurveType.Exponential, 
    intensity: 1.5
);
```

**Recommended by Game Type:**
- Tactical Shooters → Exponential (1.5)
- Arena Shooters → Logarithmic (2.0)
- Battle Royale → S-Curve (1.0)
- Competitive → Aggressive (0.7)

---

### 📊 Performance Monitor
**What**: Tracks input lag, polling rate, system performance
**Use**: Ensure optimal controller response
**Metrics**: Polling rate (Hz), Latency (ms), Processing time (ms)

```csharp
_perfMonitor.BeginInputProcessing();
// ... process input ...
_perfMonitor.EndInputProcessing();

// View metrics
var metrics = _perfMonitor.GetCurrentMetrics();
// Polling: 1000 Hz, Latency: 1.2 ms ⚡ EXCELLENT
```

**Performance Grades:**
- ⚡ EXCELLENT: < 2ms, > 800 Hz
- ✅ GOOD: < 5ms, > 500 Hz
- ⚠️ FAIR: < 10ms, > 250 Hz
- ❌ POOR: Higher values

---

### 🎚️ Sensitivity Profiles
**What**: Per-game sensitivity with context awareness
**Use**: Perfect sensitivity feel for each game
**Features**: ADS multiplier, context modes, acceleration

```csharp
// Create profile
var profile = _sensSystem.CreateProfile("Fortnite Build", "Fortnite");
profile.RightStickSensitivity = 1.4;
profile.ADSMultiplier = 0.6;
profile.ContextSensitivity["Building"] = 1.8;
profile.ContextSensitivity["Combat"] = 1.2;

// Activate
_sensSystem.ActivateProfile("Fortnite Build");

// Apply to input
var (x, y) = _sensSystem.ApplySensitivity(
    stickX, stickY, 
    isRightStick: true, 
    isADS: false
);
```

---

## Quick Setup (5 Minutes)

### 1. Initialize Features
```csharp
// In MainWindow constructor
_gameDetection = new GameDetectionSystem();
_gameDetection.InitializeDefaultProfiles();

_batteryMonitor = new BatteryMonitor();
_perfMonitor = new PerformanceMonitor();
_sensSystem = new SensitivityProfileSystem();
_sensSystem.InitializeDefaultProfiles();
```

### 2. Start Monitoring
```csharp
_gameDetection.StartDetection();
_batteryMonitor.StartMonitoring(0);
```

### 3. Integrate Input Processing
```csharp
private void ProcessInput()
{
    _perfMonitor?.BeginInputProcessing();
    
    // Apply sensitivity
    var (x, y) = _sensSystem.ApplySensitivity(stickX, stickY, true);
    
    // Apply curve
    var (finalX, finalY) = AdvancedResponseCurves.ApplyCurve2D(
        x, y, CurveType.Exponential, 1.5
    );
    
    // Update overlay
    _inputOverlay?.UpdateStick("RightStick", finalX, finalY);
    
    _perfMonitor?.EndInputProcessing();
}
```

### 4. Save on Exit
```csharp
protected override void OnClosing(CancelEventArgs e)
{
    _gameDetection?.SaveToFile("game_profiles.json");
    _sensSystem?.SaveToFile("sensitivity_profiles.json");
    _gameDetection?.StopDetection();
    _batteryMonitor?.StopMonitoring();
}
```

---

## Default Game Profiles

Pre-configured game detection:
- ✅ Fortnite
- ✅ Call of Duty (Black Ops 6, Warzone)
- ✅ Battlefield
- ✅ Rainbow Six Siege
- ✅ Valorant
- ✅ Apex Legends
- ✅ Destiny 2
- ✅ Overwatch 2

---

## Default Sensitivity Presets

Pre-configured sensitivity profiles:
- **Fortnite - Build Mode**: High sens with context switching
- **CoD/Warzone - Tactical**: Precision for tactical shooters
- **Apex Legends - Balanced**: Balanced fast-paced combat
- **Overwatch - High Sens**: High sensitivity with acceleration

---

## Configuration Files

Auto-created JSON files:
- `game_profiles.json` - Game detection mappings
- `sensitivity_profiles.json` - Sensitivity configurations

Location: Application directory

---

## Keyboard Shortcuts (Add to Your App)

Suggested shortcuts:
- `Ctrl+G` - Toggle game detection
- `Ctrl+B` - Show battery status
- `Ctrl+I` - Toggle input overlay
- `Ctrl+P` - Show performance report
- `Ctrl+S` - Switch sensitivity profile

---

## Common Usage Patterns

### For Streamers
```csharp
// Show input display
_inputOverlay = new InputDisplayOverlay();
_inputOverlay.Show();

// Position it
_inputOverlay.Left = 1600; // Right monitor
_inputOverlay.Top = 50;
```

### For Competitive Players
```csharp
// Use aggressive curve
var (x, y) = AdvancedResponseCurves.ApplyCurve2D(
    stickX, stickY,
    CurveType.Aggressive,
    0.7
);

// Monitor performance
_perfMonitor.MetricsUpdated += (metrics) => {
    if (metrics.AverageInputLatency > 5.0)
        ShowWarning("High input lag detected!");
};
```

### For Content Creators
```csharp
// Enable input display
_inputOverlay.Show();

// Monitor performance for viewers
var metrics = _perfMonitor.GetCurrentMetrics();
var report = PerformanceMonitor.GetMetricsReport(metrics);
ShowOnOverlay(report);
```

---

## Pro Tips

### Optimization
1. Use **Exponential** curve for precision (tactical shooters)
2. Use **Logarithmic** for quick reactions (arena shooters)
3. Enable **Performance Monitor** to verify < 2ms latency
4. Set **ADS Multiplier** to 0.5-0.7 for precision aiming
5. Use **Context Sensitivity** for Fortnite building vs combat

### Battery Life
1. Monitor shows real battery level, not estimates
2. < 20% shows warning - charge soon
3. Wired mode = unlimited battery
4. Keep spare batteries for wireless play

### Streaming Setup
1. Position overlay on secondary monitor
2. Or place in corner of game screen
3. Resize overlay if needed (edit InputDisplayOverlay.cs)
4. Use for tutorial content - viewers see exact inputs

### Auto-Switching
1. Add custom games: `_gameDetection.AddGameProfile("process", "profile")`
2. Test with: Launch game → Check profile switches
3. Saves automatically to `game_profiles.json`
4. Edit JSON manually for bulk changes

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Game not detected | Run as Administrator |
| Battery shows "--" | Controller doesn't support XInput battery |
| Overlay not visible | Check if minimized, recreate window |
| Poor performance | Close background apps, use wired |
| Profile not switching | Check process name matches exactly |

---

## API Quick Reference

### GameDetectionSystem
```csharp
.StartDetection()                              // Start monitoring
.StopDetection()                               // Stop monitoring
.AddGameProfile(process, profile)              // Add mapping
.SaveToFile(path)                              // Save config
.LoadFromFile(path)                            // Load config
```

### BatteryMonitor
```csharp
.StartMonitoring(index)                        // Start monitoring
.StopMonitoring()                              // Stop monitoring
.GetBatteryLevel(index)                        // Get immediate status
.GetBatteryDescription(info)                   // Format description
```

### InputDisplayOverlay
```csharp
.Show()                                        // Show overlay
.Hide()                                        // Hide overlay
.UpdateButton(name, pressed)                   // Update button
.UpdateStick(name, x, y)                       // Update stick
.UpdateTrigger(name, value)                    // Update trigger
.ResetAllInputs()                              // Reset display
```

### AdvancedResponseCurves
```csharp
.ApplyCurve(input, type, intensity)            // 1D curve
.ApplyCurve2D(x, y, type, intensity)          // 2D curve
.ApplyCustomCurve(input, controlPoints)        // Custom curve
.GetCurveDescription(type)                     // Get info
```

### PerformanceMonitor
```csharp
.BeginInputProcessing()                        // Start timing
.EndInputProcessing()                          // End timing
.GetCurrentMetrics()                           // Get metrics
.GetMetricsReport(metrics)                     // Format report
.Reset()                                       // Clear data
```

### SensitivityProfileSystem
```csharp
.CreateProfile(name, game)                     // Create new
.ActivateProfile(name)                         // Activate
.GetActiveProfile()                            // Get active
.ApplySensitivity(x, y, isRight, isADS)       // Apply to input
.CloneProfile(source, newName)                 // Clone
.SaveToFile(path)                              // Save
.LoadFromFile(path)                            // Load
```

---

## Resources

- 📖 [Complete Feature Guide](NEW_FEATURES.md)
- 🔧 [Integration Guide](INTEGRATION_GUIDE.md)
- 📚 [Advanced Features](ADVANCED_FEATURES.md)
- ❓ [FAQ](FAQ.md)

---

**Version 6.0.5 - Intelligent Features Update**

*Made with ❤️ for the gaming community*
