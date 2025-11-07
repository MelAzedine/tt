# 📚 ARTHEMIS CONTROL - User Guide

Complete guide to mastering ARTHEMIS CONTROL

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Interface Overview](#2-interface-overview)
3. [Features In-Depth](#3-features-in-depth)
4. [Game Profiles](#4-game-profiles)
5. [Advanced Configuration](#5-advanced-configuration)
6. [Optimization](#6-optimization)
7. [Troubleshooting](#7-troubleshooting)
8. [Tips & Tricks](#8-tips--tricks)

---

## 1. Getting Started

### 1.1 First Launch

1. **Run as Administrator**
   - Right-click `Trident.MITM.App.exe`
   - Select "Run as administrator"
   - This ensures full driver access

2. **License Activation**
   - Enter your license key
   - The key format is: XXXX-XXXX-XXXX-XXXX
   - Click "Validate"
   - Your license information will be displayed

3. **Controller Connection**
   - Connect your controller via USB or Bluetooth
   - Wait for the green "Connected" indicator
   - Check the Controller tab for connection status

### 1.2 Interface Navigation

- **Home**: Feature cards and quick toggles
- **Profiles**: Game-specific configurations
- **Controller**: Interactive controller mapping
- **Keyboard/Mouse**: KB/M integration settings
- **Settings**: Application preferences
- **Optimization**: System performance tools
- **License**: License information and status

---

## 2. Interface Overview

### 2.1 Header Section

**Left**: Logo and application name  
**Center**: Navigation tabs  
**Right**: Version information (PRO 6.0.3)

### 2.2 Main Content Area

Scrollable content with feature cards or configuration panels

### 2.3 Footer Section

**Left**: Status indicator (ON/OFF button)  
**Center**: Active hotkey display (F8)  
**Right**: Profile name and Save button

### 2.4 Status Indicators

- 🟢 **Green**: Feature active
- 🔴 **Red**: Feature inactive
- ✅ **Connected**: Device ready
- ⚠️ **Warning**: Configuration needed

---

## 3. Features In-Depth

### 3.1 Anti-Recoil System

**Purpose**: Compensate for weapon recoil automatically

#### Basic Configuration
1. Click the "No Recoil" card on Home tab
2. Adjust vertical compensation (0.0 - 5.0)
3. Adjust horizontal compensation (-2.0 to +2.0)
4. Enable "ADS Only" for precision mode

#### Advanced Settings
- **Delay**: Time before compensation starts (ms)
- **Ramp**: Gradual increase of compensation
- **Sustain**: Duration of full compensation

#### Per-Weapon Profiles
1. Go to Settings → Weapon Profiles
2. Select weapon from dropdown
3. Set custom values
4. Save profile
5. Auto-activation on weapon detection

#### Timeline-Based Recoil
1. Click "Draw Your Recoil" button
2. Draw the recoil pattern on canvas
3. Pattern auto-generated from your drawing
4. Fine-tune with timeline editor
5. Apply to current weapon

**Tips**:
- Start with low values (1.0-1.5)
- Increase gradually while testing
- Different weapons need different values
- Use training mode to calibrate

### 3.2 Aim Assist

**Purpose**: Enhance aiming precision and tracking

#### Modes Available

**Sinusoidal Mode**
- Smooth, wave-like movement
- Best for: Tracking moving targets
- Parameters: Amplitude X/Y, Frequency

**Circular Mode**
- Consistent circular motion
- Best for: Close-range combat
- Parameters: Radius, Speed

**Spiral Mode**
- Expanding/contracting spiral
- Best for: Long-range tracking
- Parameters: Amplitude X/Y, Frequency, Growth

#### Configuration Steps
1. Click "Aim Assist" card
2. Select mode from dropdown
3. Adjust Amplitude X (horizontal strength)
4. Adjust Amplitude Y (vertical strength)
5. Set Frequency (movement speed in Hz)
6. Test in-game and fine-tune

**Recommended Settings**:
- **Close Range**: Circular, 1500/1500, 10 Hz
- **Medium Range**: Sinusoidal, 2000/2000, 8 Hz
- **Long Range**: Spiral, 3000/3000, 6 Hz

### 3.3 Rapid Fire

**Purpose**: Convert semi-automatic to full-auto

#### Setup
1. Click "Rapid Fire" card
2. Set frequency (1-30 Hz)
   - Low (1-8 Hz): Slow firing weapons
   - Medium (9-15 Hz): Standard pistols
   - High (16-30 Hz): Fast pistols
3. Set duty cycle (20-80%)
   - Lower %: Longer pauses between shots
   - Higher %: Faster sustained fire
4. Toggle ON

**Optimal Settings by Weapon Type**:
- **Pistols**: 12-15 Hz, 55-60%
- **Tactical Rifles**: 8-12 Hz, 50-55%
- **DMRs**: 6-10 Hz, 45-50%

### 3.4 Auto Ping

**Purpose**: Automatically mark enemies

#### Configuration
1. Click "Auto Ping" card
2. Set interval (500-5000 ms)
   - Fast (500-1000ms): Aggressive marking
   - Medium (1500-2500ms): Balanced
   - Slow (3000-5000ms): Occasional marking
3. Toggle ON

**Best Practices**:
- Use 2000ms for most games
- Faster intervals may look suspicious
- Combine with manual pings

### 3.5 Dead Zones

**Purpose**: Eliminate stick drift and fine-tune sensitivity

#### Types of Dead Zones

**Dead Zone**
- Ignores small stick movements
- Fixes stick drift
- Range: 0.00 - 0.30

**Anti-Dead Zone**
- Makes stick more responsive
- Reduces initial resistance
- Range: 0.00 - 0.20

**Max Zone**
- Limits maximum stick value
- Creates buffer at edge
- Range: 0.70 - 1.00

#### Configuration
1. Go to Controller tab
2. Click on left or right stick
3. Adjust sliders while testing stick in real-time
4. Save configuration

**Recommended Values**:
- **Standard**: DZ=0.10, ADZ=0.05, MZ=0.95
- **Competitive**: DZ=0.05, ADZ=0.10, MZ=0.98
- **Drifting Stick**: DZ=0.15-0.20, ADZ=0.00, MZ=0.95

---

## 4. Game Profiles

### 4.1 Using Pre-Made Profiles

1. Navigate to "Profiles" tab
2. Select your game (Fortnite, Black Ops 6, Warzone, Battlefield 6)
3. Review profile settings
4. Click "Apply Profile"
5. Profile automatically loaded

### 4.2 Creating Custom Profiles

1. Configure all features to your liking
2. Go to Settings → Profile Management
3. Click "Save as New Profile"
4. Name your profile
5. Profile saved for future use

### 4.3 Importing/Exporting Profiles

**Export**:
1. Settings → Profile Management
2. Select profile
3. Click "Export"
4. Choose save location
5. Share .json file

**Import**:
1. Settings → Profile Management
2. Click "Import"
3. Select .json file
4. Profile added to list

---

## 5. Advanced Configuration

### 5.1 Bezier Curves

**Purpose**: Custom stick response curves

#### Creating a Curve
1. Go to Controller → Stick Settings
2. Enable "Custom Curve"
3. Adjust control points on curve editor
4. Test response in real-time
5. Save curve

**Curve Types**:
- **Linear**: Direct 1:1 response
- **Exponential**: Slow start, fast end
- **S-Curve**: Smooth acceleration
- **Custom**: Full control via Bezier

### 5.2 Hotkey System

**Global Hotkeys**:
- **Toggle All**: F8 (customizable)
- **Next Profile**: F9
- **Previous Profile**: F7

**Per-Profile Hotkeys**:
1. Settings → Hotkeys
2. Select profile
3. Set keyboard key
4. Set gamepad button combo
5. Save

### 5.3 Timeline Editor

**Purpose**: Frame-perfect recoil compensation

#### Using the Editor
1. Open "Draw Your Recoil" tool
2. Draw pattern with mouse
3. Timeline auto-generated
4. Fine-tune segments:
   - Duration: Length of movement
   - VX/VY: Velocity X/Y
   - Ease: Smoothing function
5. Test and iterate

### 5.4 Weapon Auto-Detection

**Setup**:
1. Settings → Weapon Detection
2. Enable "OCR Detection" or "Recoil-Based Detection"
3. Configure capture area (for OCR)
4. Test detection accuracy
5. Assign profiles to detected weapons

---

## 6. Optimization

### 6.1 Quick Optimization

1. Go to Optimization tab
2. Click "Apply Recommended Settings"
3. Restart computer
4. Enjoy lower latency

### 6.2 Custom Optimization

**Network**:
- Enable "Optimize Network Latency"
- Disable Nagle Algorithm
- Set QoS to "High"
- Optimize DNS

**Input Lag**:
- Disable VSync
- Enable Game Mode
- Set Timer Resolution to 0.5ms
- Set Polling Rate to 1000 Hz

**GPU**:
- Enable High Performance Mode
- Disable Windows DVR
- Optimize Shaders
- Set GPU Prefetch to 1 frame

**CPU**:
- Disable Power Throttling
- Disable CPU Parking
- Set CPU Priority to "High"
- Optimize Interrupts

**System**:
- Disable Windows Defender (while gaming)
- Disable Superfetch
- Disable Telemetry
- Optimize Memory

### 6.3 Reverting Optimizations

1. Optimization tab
2. Click "Reset Settings"
3. All optimizations reverted
4. System returned to default

---

## 7. Troubleshooting

### 7.1 Common Issues

**Controller Not Detected**
- ✅ Check USB connection
- ✅ Install ViGEm Bus Driver
- ✅ Restart application as Administrator
- ✅ Try different USB port

**Features Not Working**
- ✅ Verify feature is toggled ON
- ✅ Check Status indicator (green = active)
- ✅ Verify controller is connected
- ✅ Check hotkey assignments

**High Input Lag**
- ✅ Use wired connection
- ✅ Apply optimization preset
- ✅ Close background applications
- ✅ Disable Game Bar

**Profile Not Loading**
- ✅ Check trident.json syntax
- ✅ Verify profile name
- ✅ Re-import profile
- ✅ Create new profile

**Hotkeys Not Responding**
- ✅ Check for key conflicts
- ✅ Run as Administrator
- ✅ Verify hotkey settings
- ✅ Test different keys

### 7.2 Advanced Troubleshooting

**Enable Debug Logging**:
1. Settings → Advanced
2. Enable "Debug Mode"
3. Reproduce issue
4. Check log file in app folder

**Reset to Defaults**:
1. Close application
2. Delete trident.json
3. Restart application
4. Reconfigure from scratch

**Driver Issues**:
1. Uninstall ViGEm Bus
2. Restart computer
3. Reinstall ViGEm Bus as Administrator
4. Restart computer again
5. Test application

---

## 8. Tips & Tricks

### 8.1 Performance Tips

1. **Always run as Administrator** for full functionality
2. **Use wired controllers** for lowest latency
3. **Apply system optimizations** for best performance
4. **Close background apps** to free resources
5. **Disable Xbox Game Bar** to reduce interference

### 8.2 Configuration Tips

1. **Start with profiles** before creating custom settings
2. **Test in training mode** before competitive matches
3. **Save frequently** to avoid losing configurations
4. **Backup profiles** before major changes
5. **Use descriptive names** for custom profiles

### 8.3 Gameplay Tips

1. **Gradual adjustment**: Don't max out settings immediately
2. **Muscle memory**: Give yourself time to adapt
3. **Situational profiles**: Create profiles for different scenarios
4. **Hotkey mastery**: Learn to switch profiles quickly
5. **Regular calibration**: Re-test settings periodically

### 8.4 Advanced Tips

1. **Layer features**: Combine anti-recoil + aim assist
2. **Per-weapon optimization**: Fine-tune each weapon separately
3. **Timeline mastery**: Use timeline editor for perfect recoil
4. **Curve experimentation**: Try different Bezier curves
5. **Macro creation**: Combine features with hotkeys

### 8.5 Competitive Tips

1. **Subtlety is key**: Don't make assistance obvious
2. **Know the rules**: Check game ToS and anti-cheat policies
3. **Practice with and without**: Maintain raw skills
4. **Adapt to updates**: Game patches may require re-tuning
5. **Ethics matter**: Use responsibly

---

## 9. Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| F8 | Toggle All Features |
| F9 | Next Profile |
| F7 | Previous Profile |
| Ctrl+S | Save Configuration |
| Ctrl+O | Open Settings |
| Ctrl+P | Open Profiles |
| Alt+F4 | Exit Application |
| F1 | Open Help |

---

## 10. Glossary

**ADS**: Aim Down Sights - Looking through weapon sight  
**Dead Zone**: Area where stick input is ignored  
**Anti-Dead Zone**: Compensation for initial stick resistance  
**Max Zone**: Maximum stick value limit  
**Bezier Curve**: Mathematical curve for smooth stick response  
**Timeline**: Sequence of movements over time  
**OCR**: Optical Character Recognition for weapon detection  
**ViGEm**: Virtual Gamepad Emulation Framework  
**QoS**: Quality of Service - Network traffic prioritization  
**Hz**: Hertz - Frequency measurement (cycles per second)

---

## 11. Support Resources

- 📖 [README](README.md) - Quick overview
- 💬 Discord - Community support
- 📧 Email - contact@arthemiscontrol.com
- 🐛 GitHub Issues - Bug reports
- 📺 YouTube - Video tutorials
- 📱 Twitter - Updates and tips

---

<div align="center">

**Happy Gaming! 🎮**

*Master the game, dominate the competition*

</div>
