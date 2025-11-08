# Advanced Features Documentation 🚀

## Overview

The Advanced Features system transforms Arthemis Control into a professional-grade controller customization platform with **macro recording**, **button remapping**, and **combo detection** capabilities.

---

## 📋 Table of Contents

1. [Macro System](#macro-system)
2. [Button Remapping](#button-remapping)
3. [Combo System](#combo-system)
4. [Integration Guide](#integration-guide)
5. [Configuration Files](#configuration-files)

---

## ⚡ Macro System

### What are Macros?

Macros are recorded sequences of button presses and releases that can be played back automatically. Perfect for:
- Complex fighting game combos
- Repetitive actions in games
- Quick builds in Fortnite
- Automated farming sequences

### Features

- **Recording**: Capture button sequences with precise timing
- **Playback**: Execute recorded macros with one button
- **Loop Control**: Set macros to loop indefinitely or a specific number of times
- **Import/Export**: Share macros with friends or backup your collection
- **Optimization**: Automatic cleanup of unnecessary delays

### How to Use

#### Recording a Macro

1. Open **Advanced Features** window
2. Go to **Macro System** tab
3. Click **START RECORDING**
4. Enter a name for your macro
5. Press the button sequence you want to record
6. Click **STOP RECORDING** when done

#### Playing a Macro

1. Select a macro from the library
2. Click **PLAY MACRO**
3. The macro will execute automatically

#### Advanced Settings

- **Loop Count**: Set to 0 for infinite loop, or specify exact number
- **Enable Looping**: Toggle to enable/disable loop mode
- **Trigger Button**: Assign a button to auto-trigger the macro

### API Reference

```csharp
// Create macro system
var macroSystem = new MacroSystem();

// Start recording
macroSystem.StartRecording("MyMacro", "Description");

// Record actions (usually called from controller input)
macroSystem.RecordAction("A", true);  // Press A
macroSystem.RecordAction("A", false); // Release A

// Stop recording
var macro = macroSystem.StopRecording();

// Play macro
await macroSystem.PlayMacroAsync(macro, ExecuteButtonAction);
```

---

## 🎯 Button Remapping

### What is Button Remapping?

Button remapping allows you to customize your controller layout by mapping any button to any other button. Perfect for:
- Accessibility needs
- Game-specific layouts
- Comfort optimization
- Southpaw configurations

### Features

- **Per-Profile Configuration**: Different mappings for different games
- **Modifier Support**: Use L1/R1 as modifier keys for complex mappings
- **Quick Swap**: Instantly swap two buttons
- **Turbo Mode**: Enable rapid-fire on individual buttons
- **Stick Inversion**: Invert any stick axis independently

### How to Use

#### Creating a Profile

1. Open **Advanced Features** window
2. Go to **Button Remapping** tab
3. Click **NEW PROFILE**
4. Enter profile name (e.g., "Fortnite Layout")

#### Adding Mappings

1. Select a profile
2. Click **ADD MAPPING**
3. Choose source button (what you press)
4. Choose target button (what gets executed)
5. Click **OK**

#### Activating a Profile

1. Select desired profile
2. Click **ACTIVATE**
3. Profile is now active for all button inputs

### Example Configurations

#### Southpaw (Swap Sticks)
- Map left stick → right stick functions
- Map right stick → left stick functions

#### Jump on L1 (FPS Optimization)
- Map L1 → A (Xbox) / X (PlayStation)
- Allows jumping while maintaining aim

#### Turbo Mode
- Select button (e.g., "A")
- Enable turbo: 10 Hz, 50% duty cycle
- Button now auto-repeats when held

### API Reference

```csharp
// Create remapping system
var remapping = new ButtonRemappingSystem();

// Create profile
var profile = remapping.CreateProfile("MyProfile", "My custom layout");

// Add mapping
remapping.AddMapping("A", "B"); // Press A executes B

// Add mapping with modifier
remapping.AddMapping("A", "X", ModifierKeys.L1); // L1+A executes X

// Enable turbo
remapping.SetTurboMode("A", true, 10, 50); // 10 Hz, 50% duty

// Activate profile
remapping.ActivateProfile("MyProfile");
```

---

## ⚔️ Combo System

### What are Combos?

Combos are sequences of button inputs that trigger special actions when detected. Inspired by fighting games, perfect for:
- Special move shortcuts
- Advanced techniques
- Context-sensitive actions
- Quick macros

### Features

- **Input Detection**: Real-time combo detection with timing windows
- **Multiple Actions**: Execute buttons, play macros, or switch profiles
- **Fighting Game Combos**: Built-in support for directional combos
- **Quick Tap Detection**: Double-tap, triple-tap, etc.
- **Statistics Tracking**: See which combos you execute most

### How to Use

#### Creating a Quick Tap Combo

1. Open **Advanced Features** window
2. Go to **Combo System** tab
3. Click **QUICK TAP**
4. Configure:
   - Combo name: "Double Tap A"
   - Button: "A"
   - Taps: 2
   - Max delay: 300ms
5. Click **OK**

#### Creating a Fighting Game Combo

Example: Hadouken (↓→ + A)

```csharp
comboSystem.CreateFightingGameCombo(
    "Hadouken",
    new[] { "DPadDown", "DPadRight" },
    "A",
    "ExecuteButtons",
    "X", "Y" // Action: Press X and Y
);
```

#### Combo Actions

**ExecuteButtons**: Press specified buttons
```csharp
combo.ActionType = "ExecuteButtons";
combo.ActionParameters = new List<string> { "A", "B", "X" };
```

**PlayMacro**: Execute a saved macro
```csharp
combo.ActionType = "PlayMacro";
combo.ActionParameters = new List<string> { "MyMacroName" };
```

**ActivateProfile**: Switch to a different button layout
```csharp
combo.ActionType = "ActivateProfile";
combo.ActionParameters = new List<string> { "AlternateLayout" };
```

### Example Combos

#### Double Tap Dash
```csharp
comboSystem.CreateQuickTapCombo(
    "Dash Forward",
    "DPadRight",
    2,
    300,
    "ExecuteButtons",
    "L3" // Sprint button
);
```

#### Super Jump
```csharp
var combo = new Combo
{
    Name = "Super Jump",
    Inputs = new List<ComboInput>
    {
        new() { ButtonName = "DPadDown", RequirePress = true, MaxDelayMs = 200 },
        new() { ButtonName = "DPadDown", RequirePress = false, MaxDelayMs = 100 },
        new() { ButtonName = "A", RequirePress = true, MaxDelayMs = 200 }
    },
    ActionType = "ExecuteButtons",
    ActionParameters = new List<string> { "A", "A", "A" } // Triple jump
};
```

---

## 🔧 Integration Guide

### Adding to MainWindow

Add this to your `MainWindow.xaml.cs` constructor:

```csharp
public MainWindow()
{
    InitializeComponent();
    InitializeAdvancedFeatures();
    LoadAdvancedFeaturesConfig();
}
```

### Processing Controller Input

Update your controller input handler:

```csharp
void OnButtonStateChanged(string button, bool isPressed)
{
    // Process through advanced features
    ProcessAdvancedFeatures(button, isPressed);
    
    // Continue with normal processing
    // ...
}
```

### Opening Advanced Features Window

Add a button or menu item:

```csharp
private void AdvancedFeaturesButton_Click(object sender, RoutedEventArgs e)
{
    OpenAdvancedFeaturesWindow();
}
```

### Saving on Exit

Add to your window closing handler:

```csharp
protected override void OnClosing(CancelEventArgs e)
{
    SaveAdvancedFeaturesConfig();
    base.OnClosing(e);
}
```

---

## 📁 Configuration Files

### macros.json

Stores all recorded macros.

```json
[
  {
    "Name": "Build Fast",
    "Description": "Quick building macro",
    "Actions": [
      { "ButtonName": "A", "IsPress": true, "DelayAfterMs": 50 },
      { "ButtonName": "A", "IsPress": false, "DelayAfterMs": 100 },
      { "ButtonName": "B", "IsPress": true, "DelayAfterMs": 50 }
    ],
    "Loop": false,
    "LoopCount": 1
  }
]
```

### button_profiles.json

Stores button remapping profiles.

```json
[
  {
    "Name": "FPS Layout",
    "Mappings": [
      {
        "SourceButton": "L1",
        "TargetButton": "A",
        "IsEnabled": true,
        "Modifiers": 0
      }
    ],
    "TurboButtons": {
      "A": {
        "Enabled": true,
        "FrequencyHz": 10,
        "DutyCycle": 50
      }
    }
  }
]
```

### combos.json

Stores combo configurations.

```json
[
  {
    "Name": "Hadouken",
    "Description": "↓→ + A combo",
    "Inputs": [
      { "ButtonName": "DPadDown", "RequirePress": true, "MaxDelayMs": 300 },
      { "ButtonName": "DPadRight", "RequirePress": true, "MaxDelayMs": 300 },
      { "ButtonName": "A", "RequirePress": true, "MaxDelayMs": 200 }
    ],
    "ActionType": "ExecuteButtons",
    "ActionParameters": ["X", "Y"],
    "IsEnabled": true
  }
]
```

---

## 🎮 Best Practices

### Macros

- ✅ Keep macros short (< 10 seconds) for reliability
- ✅ Test macros in practice mode before competitive use
- ✅ Use descriptive names and descriptions
- ✅ Regular backups via export function
- ❌ Don't use in games where macros are prohibited

### Button Remapping

- ✅ Create separate profiles for each game
- ✅ Test new layouts in safe environments
- ✅ Use modifier keys for advanced setups
- ✅ Document your custom layouts
- ❌ Don't remap essential buttons without backup plan

### Combos

- ✅ Set realistic timing windows (200-500ms)
- ✅ Test combos thoroughly
- ✅ Start simple, add complexity gradually
- ✅ Use fighting game notation for documentation
- ❌ Don't make timing windows too strict

---

## 🔥 Pro Tips

1. **Macro Chaining**: Create macros that trigger other macros via combo system
2. **Profile Switching**: Use combos to switch profiles mid-game
3. **Turbo Combos**: Combine turbo mode with remapping for ultimate control
4. **Context Awareness**: Different profiles for building vs. combat
5. **Backup Everything**: Export all configs regularly

---

## 📊 Performance

- **Macro Playback**: < 1ms latency per action
- **Button Remapping**: < 0.5ms processing time
- **Combo Detection**: Real-time, < 1ms per input
- **Memory Usage**: ~2-5 MB for typical configurations
- **File Size**: < 100 KB for average user configs

---

## 🛠️ Troubleshooting

### Macros not playing
- Verify `ExecuteButtonAction` is properly configured
- Check macro has valid actions
- Ensure no other systems are blocking input

### Button remapping not working
- Verify profile is activated
- Check mapping exists in active profile
- Ensure modifier states are correct

### Combos not detecting
- Reduce timing window (MaxDelayMs)
- Verify button names match exactly
- Check combo is enabled
- Reset combo states if stuck

---

## 🚀 Future Enhancements

Planned features for future versions:

- [ ] Visual macro editor with timeline
- [ ] AI-assisted combo creation
- [ ] Cloud sync for profiles
- [ ] Community combo sharing
- [ ] Advanced scripting support
- [ ] Profile auto-detection per game
- [ ] Gesture-based combos
- [ ] Voice command triggers

---

## 📞 Support

For help with advanced features:

1. Check this documentation
2. Review example configurations
3. Join Discord community
4. Submit bug reports on GitHub

---

**Made with ❤️ for the gaming community**

Version: 6.0.4 BANGER UPDATE
