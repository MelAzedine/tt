# 🚀 NEW FEATURES DOCUMENTATION

## Overview

This document describes the **NEW** useful features added to Arthemis Control that enhance the user experience and provide professional-grade functionality not previously available in the application.

---

## 📋 Table of Contents

1. [Profile Auto-Switching](#profile-auto-switching)
2. [Controller Battery Monitoring](#controller-battery-monitoring)
3. [Input Display Overlay](#input-display-overlay)
4. [Advanced Response Curves](#advanced-response-curves)
5. [Performance Monitor](#performance-monitor)
6. [Sensitivity Profile System](#sensitivity-profile-system)

---

## 🎮 Profile Auto-Switching

### What is it?

Automatically detects which game is running and switches to the appropriate controller profile. No more manual profile switching!

### Features

- **Automatic Game Detection**: Continuously monitors running processes
- **Instant Profile Switching**: Seamlessly switches profiles when games launch
- **Custom Mappings**: Map any game executable to any profile
- **Default Presets**: Pre-configured for popular games
- **Low Overhead**: Only checks every 2 seconds, minimal CPU usage

### How to Use

#### Setting up Auto-Switch

```csharp
var gameDetection = new GameDetectionSystem();

// Add game-to-profile mappings
gameDetection.AddGameProfile("FortniteClient-Win64-Shipping", "Fortnite Build Mode");
gameDetection.AddGameProfile("cod", "Call of Duty: Black Ops 6");
gameDetection.AddGameProfile("r5apex", "Apex Legends");

// Start monitoring
gameDetection.StartDetection();
```

#### Handling Auto-Switch Events

```csharp
gameDetection.GameDetected += (gameName, profileName) =>
{
    Log($"Detected {gameName}, switching to profile: {profileName}");
    LoadProfile(profileName);
};

gameDetection.GameClosed += () =>
{
    Log("Game closed, reverting to default profile");
    LoadProfile("Default");
};
```

### Configuration

Save and load mappings from JSON:

```csharp
// Save current mappings
gameDetection.SaveToFile("game_profiles.json");

// Load mappings
gameDetection.LoadFromFile("game_profiles.json");
```

### Default Game Mappings

The system includes presets for:
- Fortnite
- Call of Duty (Black Ops 6, Warzone)
- Battlefield
- Rainbow Six Siege
- Valorant
- Apex Legends
- Destiny 2
- Overwatch 2

---

## 🔋 Controller Battery Monitoring

### What is it?

Displays real-time battery level for wireless controllers, so you never get caught with a dead controller mid-game!

### Features

- **Real-time Monitoring**: Updates every 30 seconds
- **Battery Percentage**: Shows estimated battery level
- **Battery Type Detection**: Identifies wired, alkaline, or rechargeable batteries
- **Low Battery Warnings**: Visual indicators for low battery
- **Multiple Controller Support**: Monitor any connected controller

### How to Use

#### Start Monitoring

```csharp
var batteryMonitor = new BatteryMonitor();

// Subscribe to battery updates
batteryMonitor.BatteryLevelChanged += (info) =>
{
    string status = BatteryMonitor.GetBatteryDescription(info);
    UpdateBatteryUI(status);
};

// Start monitoring controller 0
batteryMonitor.StartMonitoring(controllerIndex: 0);
```

#### Check Battery Immediately

```csharp
var batteryInfo = batteryMonitor.GetBatteryLevel(0);
if (batteryInfo != null)
{
    Console.WriteLine($"Battery: {batteryInfo.PercentageEstimate}%");
    Console.WriteLine($"Level: {batteryInfo.Level}");
    Console.WriteLine($"Type: {batteryInfo.Type}");
}
```

### Battery Levels

- 🟢 **Full** (100%): Battery is fully charged
- 🟢 **Medium** (60%): Battery is good
- 🟡 **Low** (25%): Consider charging soon
- 🔴 **Empty** (0%): Battery critically low

### UI Display

```csharp
// Get formatted battery description
string display = BatteryMonitor.GetBatteryDescription(batteryInfo);
// Output: "🟢 Battery: 75%"

// Get just the icon
string icon = BatteryMonitor.GetBatteryIcon(batteryInfo.Level);
// Output: "🟢"
```

---

## 📺 Input Display Overlay

### What is it?

Real-time visualization of controller inputs, perfect for streaming, recording gameplay, or creating tutorials!

### Features

- **Real-time Display**: Shows all button presses instantly
- **Analog Stick Visualization**: See exact stick positions
- **Trigger Pressure**: Visual trigger depth indication
- **Transparent Overlay**: Stays on top, doesn't interfere with games
- **Customizable Position**: Place anywhere on screen
- **Color-coded Buttons**: Easy identification of each input
- **Always on Top**: Stays visible over all windows

### How to Use

#### Show the Overlay

```csharp
var inputOverlay = new InputDisplayOverlay();
inputOverlay.Show();
```

#### Update Button States

```csharp
// Button pressed
inputOverlay.UpdateButton("A", true);

// Button released
inputOverlay.UpdateButton("A", false);

// Update stick position (normalized -1 to 1)
inputOverlay.UpdateStick("LeftStick", x: 0.5, y: 0.3);
inputOverlay.UpdateStick("RightStick", x: -0.2, y: 0.8);

// Update trigger values (0 to 1)
inputOverlay.UpdateTrigger("LT", 0.5);  // Half-pressed
inputOverlay.UpdateTrigger("RT", 1.0);  // Fully pressed
```

#### Reset All Inputs

```csharp
inputOverlay.ResetAllInputs();
```

### Button Names

- Face buttons: `A`, `B`, `X`, `Y`
- Shoulders: `LB`, `RB`, `LT`, `RT`
- D-Pad: `Up`, `Down`, `Left`, `Right`
- Special: `Start`, `Back`, `LS`, `RS`
- Sticks: `LeftStick`, `RightStick`

### Use Cases

- **Streaming**: Show viewers your inputs
- **Tutorials**: Demonstrate button combinations
- **Debugging**: Verify controller is working correctly
- **Speedrunning**: Prove input authenticity
- **Competitive Play**: Review your own inputs

---

## 📈 Advanced Response Curves

### What is it?

Additional stick response curve types beyond linear and Bezier, giving you fine-tuned control over how your sticks respond to input.

### Curve Types

#### 1. **Exponential** 🎯
- Slow at start, fast at end
- Perfect for precision aiming
- Recommended intensity: 1.5

```csharp
double output = AdvancedResponseCurves.ApplyCurve(input, CurveType.Exponential, 1.5);
```

#### 2. **Logarithmic** ⚡
- Fast at start, slow at end
- Great for quick flicks and turns
- Recommended intensity: 2.0

```csharp
double output = AdvancedResponseCurves.ApplyCurve(input, CurveType.Logarithmic, 2.0);
```

#### 3. **S-Curve** 🌊
- Slow at extremes, fast in middle
- Smooth acceleration
- Recommended intensity: 1.0

```csharp
double output = AdvancedResponseCurves.ApplyCurve(input, CurveType.SCurve, 1.0);
```

#### 4. **Aggressive** ⚔️
- Emphasizes small movements
- Best for competitive play
- Recommended intensity: 0.7

```csharp
double output = AdvancedResponseCurves.ApplyCurve(input, CurveType.Aggressive, 0.7);
```

#### 5. **Smooth** 🛡️
- Reduces small movements
- Increases stability
- Recommended intensity: 0.5

```csharp
double output = AdvancedResponseCurves.ApplyCurve(input, CurveType.Smooth, 0.5);
```

#### 6. **Power Curve** 💪
- Customizable exponential
- Full control over response
- Intensity range: 0.5 - 3.0

```csharp
double output = AdvancedResponseCurves.ApplyCurve(input, CurveType.PowerCurve, 1.2);
```

#### 7. **Custom** ✨
- Define your own control points
- Maximum flexibility

```csharp
double[] controlPoints = { 0.0, 0.2, 0.5, 0.8, 1.0 };
double output = AdvancedResponseCurves.ApplyCustomCurve(input, controlPoints);
```

### Using Curves in 2D

Apply curves while preserving direction:

```csharp
var (newX, newY) = AdvancedResponseCurves.ApplyCurve2D(
    x, y, 
    CurveType.Exponential, 
    intensity: 1.5
);
```

### Curve Recommendations by Game Type

| Game Type | Recommended Curve | Intensity |
|-----------|------------------|-----------|
| Tactical Shooter | Exponential | 1.5 |
| Arena Shooter | Logarithmic | 2.0 |
| Battle Royale | S-Curve | 1.0 |
| Competitive FPS | Aggressive | 0.7 |
| Single Player | Smooth | 0.5 |
| Racing | Power Curve | 1.2 |

---

## 📊 Performance Monitor

### What is it?

Tracks input lag, polling rate, and system performance to ensure optimal controller response.

### Metrics Tracked

- **Polling Rate**: How often controller is read (Hz)
- **Input Latency**: Time between inputs (ms)
- **Processing Time**: Time to process each input (ms)
- **Total Inputs**: Lifetime input count
- **Performance Grade**: Overall system rating

### How to Use

#### Start Monitoring

```csharp
var perfMonitor = new PerformanceMonitor();

// Subscribe to metric updates
perfMonitor.MetricsUpdated += (metrics) =>
{
    UpdatePerformanceUI(metrics);
};
```

#### Record Input Processing

```csharp
// Start timing
perfMonitor.BeginInputProcessing();

// Process controller input
ProcessControllerInput();

// End timing
perfMonitor.EndInputProcessing();
```

#### Get Metrics Report

```csharp
var metrics = perfMonitor.GetCurrentMetrics();
string report = PerformanceMonitor.GetMetricsReport(metrics);
Console.WriteLine(report);
```

Output:
```
Performance Metrics Report
================================
Polling Rate: 1000.2 Hz (Current: 1001.5 Hz)
Input Latency: 1.02 ms (Min: 0.98 / Max: 1.15)
Processing Time: 0.234 ms (Max: 0.987)
Total Inputs: 125,432
Grade: ⚡ EXCELLENT
Last Update: 14:32:15
```

### Performance Grades

- ⚡ **EXCELLENT**: < 2ms latency, > 800 Hz polling
- ✅ **GOOD**: < 5ms latency, > 500 Hz polling
- ⚠️ **FAIR**: < 10ms latency, > 250 Hz polling
- ❌ **POOR**: Higher latency or lower polling rate

### Optimization Tips

If you see poor performance:

1. **Close background applications**
2. **Run as Administrator**
3. **Use wired connection** (Bluetooth adds latency)
4. **Disable Windows Game Bar**
5. **Apply system optimizations** (in Tools menu)
6. **Check USB port** (use USB 3.0+)

---

## 🎚️ Sensitivity Profile System

### What is it?

Per-game sensitivity profiles with context-aware adjustments (building, driving, etc.) and easy switching.

### Features

- **Per-Game Profiles**: Different sensitivity for each game
- **Context-Aware**: Different settings for building, driving, etc.
- **ADS Multiplier**: Automatic sensitivity reduction when aiming
- **Axis Inversion**: Individual X/Y inversion
- **Acceleration**: Optional stick acceleration
- **Response Curves**: Per-stick curve selection
- **Quick Switching**: Change profiles instantly
- **Profile Cloning**: Duplicate and modify existing profiles

### How to Use

#### Create a Profile

```csharp
var sensSystem = new SensitivityProfileSystem();

var profile = sensSystem.CreateProfile("Fortnite Build", "Fortnite");
profile.RightStickSensitivity = 1.4;
profile.ADSMultiplier = 0.6;
profile.RightStickCurveType = "Exponential";
profile.RightStickCurveIntensity = 1.5;

// Context-specific sensitivity
profile.ContextSensitivity["Building"] = 1.8;
profile.ContextSensitivity["Combat"] = 1.2;
```

#### Activate a Profile

```csharp
sensSystem.ActivateProfile("Fortnite Build");
```

#### Apply Sensitivity to Input

```csharp
var (adjustedX, adjustedY) = sensSystem.ApplySensitivity(
    x, y,
    isRightStick: true,
    isADS: false
);
```

#### Clone a Profile

```csharp
var newProfile = sensSystem.CloneProfile(
    "Fortnite Build", 
    "Fortnite Build - Variant"
);
```

### Profile Settings

```csharp
public class SensitivityProfile
{
    // Basic settings
    public double LeftStickSensitivity { get; set; }     // 0.1 - 3.0
    public double RightStickSensitivity { get; set; }    // 0.1 - 3.0
    public double ADSMultiplier { get; set; }            // 0.3 - 1.0
    
    // Dead zones
    public double LeftStickDeadZone { get; set; }        // 0.0 - 0.3
    public double RightStickDeadZone { get; set; }       // 0.0 - 0.3
    
    // Response curves
    public string LeftStickCurveType { get; set; }       // Linear, Exponential, etc.
    public string RightStickCurveType { get; set; }
    public double LeftStickCurveIntensity { get; set; }  // 0.5 - 3.0
    public double RightStickCurveIntensity { get; set; }
    
    // Advanced
    public bool InvertYAxis { get; set; }
    public bool InvertXAxis { get; set; }
    public double Acceleration { get; set; }             // 0.0 - 1.0
    public double MaxTurnSpeed { get; set; }             // 0.5 - 2.0
    
    // Context-specific
    public Dictionary<string, double> ContextSensitivity { get; set; }
}
```

### Default Profiles

The system includes presets for:

1. **Fortnite - Build Mode**: High sensitivity with context switching
2. **CoD/Warzone - Tactical**: Precision for tactical shooters
3. **Apex Legends - Balanced**: Balanced for fast-paced combat
4. **Overwatch - High Sens**: High sensitivity with acceleration

### Save/Load Profiles

```csharp
// Save all profiles
sensSystem.SaveToFile("sensitivity_profiles.json");

// Load profiles
sensSystem.LoadFromFile("sensitivity_profiles.json");
```

---

## 🔧 Integration Examples

### Complete Feature Integration

```csharp
public class EnhancedController
{
    private GameDetectionSystem gameDetection;
    private BatteryMonitor batteryMonitor;
    private InputDisplayOverlay inputOverlay;
    private PerformanceMonitor perfMonitor;
    private SensitivityProfileSystem sensSystem;
    
    public void Initialize()
    {
        // Setup game detection
        gameDetection = new GameDetectionSystem();
        gameDetection.InitializeDefaultProfiles();
        gameDetection.GameDetected += OnGameDetected;
        gameDetection.StartDetection();
        
        // Setup battery monitoring
        batteryMonitor = new BatteryMonitor();
        batteryMonitor.BatteryLevelChanged += OnBatteryChanged;
        batteryMonitor.StartMonitoring(0);
        
        // Setup input overlay
        inputOverlay = new InputDisplayOverlay();
        
        // Setup performance monitor
        perfMonitor = new PerformanceMonitor();
        perfMonitor.MetricsUpdated += OnMetricsUpdated;
        
        // Setup sensitivity system
        sensSystem = new SensitivityProfileSystem();
        sensSystem.InitializeDefaultProfiles();
        sensSystem.ProfileActivated += OnProfileActivated;
    }
    
    private void OnGameDetected(string gameName, string profileName)
    {
        Log($"Game detected: {gameName}");
        sensSystem.ActivateProfile(profileName);
    }
    
    private void OnBatteryChanged(BatteryMonitor.BatteryInfo info)
    {
        string status = BatteryMonitor.GetBatteryDescription(info);
        UpdateBatteryStatusUI(status);
        
        if (info.PercentageEstimate < 20)
        {
            ShowLowBatteryWarning();
        }
    }
    
    private void ProcessInput(double x, double y, bool isRightStick)
    {
        perfMonitor.BeginInputProcessing();
        
        // Apply sensitivity
        var (adjX, adjY) = sensSystem.ApplySensitivity(x, y, isRightStick);
        
        // Apply response curve
        var (finalX, finalY) = AdvancedResponseCurves.ApplyCurve2D(
            adjX, adjY,
            AdvancedResponseCurves.CurveType.Exponential,
            1.5
        );
        
        // Update overlay
        inputOverlay.UpdateStick(
            isRightStick ? "RightStick" : "LeftStick",
            finalX, finalY
        );
        
        // Process the input
        SendToGame(finalX, finalY);
        
        perfMonitor.EndInputProcessing();
    }
}
```

---

## 📝 Best Practices

### Performance

- ✅ Enable performance monitor to track metrics
- ✅ Use wired connection for best performance
- ✅ Keep overlay on secondary monitor if possible
- ✅ Monitor battery regularly for wireless controllers

### Profiles

- ✅ Create separate sensitivity profiles per game
- ✅ Use auto-switching to avoid manual changes
- ✅ Clone and modify presets instead of starting from scratch
- ✅ Export profiles before making major changes

### Streaming

- ✅ Position input overlay where it won't block gameplay
- ✅ Use transparent background for cleaner look
- ✅ Test overlay visibility before streaming

### Battery Life

- ✅ Keep spare batteries or charging cable nearby
- ✅ Watch for low battery warnings
- ✅ Consider wired mode for long sessions
- ✅ Monitor battery usage patterns

---

## 🚀 Future Enhancements

Planned features for future versions:

- [ ] Cloud sync for sensitivity profiles
- [ ] Community profile sharing
- [ ] Machine learning-based auto-tuning
- [ ] Advanced input replay system
- [ ] Multi-controller battery dashboard
- [ ] Per-weapon sensitivity in shooters
- [ ] Automatic curve optimization
- [ ] Telemetry and analytics dashboard

---

## 🆘 Troubleshooting

### Game Detection Not Working

- Verify game process name matches exactly
- Run as Administrator for process access
- Check that detection is enabled
- Restart detection service

### Battery Monitoring Shows "Disconnected"

- Controller must support XInput battery reporting
- Some wired-only controllers don't report battery
- Try reconnecting the controller
- Update controller firmware

### Input Overlay Not Showing

- Check if overlay window is minimized
- Verify Topmost property is set
- Try repositioning the window
- Restart the application

### Poor Performance Metrics

- Close background applications
- Use wired connection
- Apply system optimizations
- Check USB port quality
- Disable Windows Game Bar

---

## 📞 Support

For help with new features:

1. Check this documentation
2. Review example code above
3. Test with default profiles first
4. Report issues on GitHub

---

**Made with ❤️ for the gaming community**

Version: 6.0.5 - NEW FEATURES UPDATE
