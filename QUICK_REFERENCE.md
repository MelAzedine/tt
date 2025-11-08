# 🎮 ARTHEMIS CONTROL v6.0.4 - Quick Reference Card

## 🚀 New Advanced Features

### ⚡ MACRO SYSTEM
**Record and playback button sequences**

| Action | How To |
|--------|--------|
| 📹 **Record Macro** | Advanced → Macro System → START RECORDING |
| ▶️ **Play Macro** | Select macro → PLAY MACRO |
| 🔁 **Loop Macro** | Edit macro → Enable Looping → Set count |
| 💾 **Export Macros** | Macro System → EXPORT |
| 📥 **Import Macros** | Macro System → IMPORT |

**Example Use Cases:**
- Quick builds in Fortnite
- Complex fighting game combos
- Automated farming sequences
- Repetitive actions

---

### 🎯 BUTTON REMAPPING
**Customize your controller layout**

| Feature | Description |
|---------|-------------|
| 🔀 **Swap Buttons** | Map any button to any other button |
| 🎮 **Per-Game Profiles** | Different layouts for each game |
| 🔧 **Modifier Keys** | Use L1/R1 to create button combos |
| ⚡ **Turbo Mode** | Add rapid-fire to any button |
| 🔄 **Quick Swap** | Instantly swap two buttons |

**Popular Configurations:**
- **FPS Optimized**: L1 → A (Jump without losing aim)
- **Southpaw**: Swap left and right stick functions
- **Racing**: X → R2 (Accelerate with triggers)

---

### ⚔️ COMBO SYSTEM
**Fighting game style input detection**

| Combo Type | Example | Action |
|------------|---------|--------|
| **Quick Tap** | Double-tap A | Execute special move |
| **Directional** | ↓→ + A | Hadouken |
| **Sequential** | A → B → X | Combo attack |
| **Modifier** | L1 + A | Context action |

**Creating a Quick Tap Combo:**
1. Advanced → Combo System
2. Click **QUICK TAP**
3. Choose button and tap count
4. Set max delay (300ms recommended)
5. Click **OK**

---

## 🎨 INTERFACE GUIDE

### Navigation Bar
```
Home | Profiles | Controller | ⚡ ADVANCED | KB/Mouse | Settings | Optimization | License
                                    ↑
                              Click here for
                           Advanced Features!
```

### Advanced Features Window Tabs
1. **⚡ MACRO SYSTEM** - Record and manage macros
2. **🎯 BUTTON REMAPPING** - Customize button layouts
3. **⚔️ COMBO SYSTEM** - Set up input combos
4. **📊 STATISTICS** - View usage analytics

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **F8** | Toggle all features ON/OFF |
| **Ctrl+M** | Open Macro System |
| **Ctrl+B** | Open Button Remapping |
| **Ctrl+C** | Open Combo System |

---

## 💾 Configuration Files

All configurations are automatically saved to:

| File | Contents |
|------|----------|
| `macros.json` | All recorded macros |
| `button_profiles.json` | Button remapping profiles |
| `combos.json` | Combo configurations |
| `trident.json` | Main app settings |

**Backup Location**: Same folder as `Trident.MITM.App.exe`

---

## 🔥 Pro Tips

### Macros
✅ **DO:**
- Start simple (2-3 actions)
- Test in practice mode first
- Use descriptive names
- Export regularly for backup

❌ **DON'T:**
- Record macros longer than 10 seconds
- Use in games where prohibited
- Forget to set loop count

### Button Remapping
✅ **DO:**
- Create profiles per game
- Use modifier keys for advanced setups
- Test new layouts in safe environments
- Document your custom layouts

❌ **DON'T:**
- Remap essential buttons without backup
- Use too many modifiers (confusing)
- Forget which profile is active

### Combos
✅ **DO:**
- Set realistic timing windows (200-500ms)
- Test thoroughly before use
- Start with quick-taps
- Use fighting game notation

❌ **DON'T:**
- Make timing windows too strict
- Create conflicting combos
- Forget to enable combos

---

## 🎯 Quick Start Examples

### Example 1: Fortnite Build Macro
```
1. Open Advanced Features → Macro System
2. Click "START RECORDING"
3. Name: "Quick Ramp"
4. Press: Q → G → G → Q (walls and ramps)
5. Click "STOP RECORDING"
6. Assign to a button via combo system
```

### Example 2: FPS Jump Shot
```
1. Open Advanced Features → Button Remapping
2. New Profile: "Warzone Pro"
3. Add Mapping: L1 → A (jump)
4. Add Mapping: L1+R1 → B (crouch)
5. Activate Profile
```

### Example 3: Double-Tap Sprint
```
1. Open Advanced Features → Combo System
2. Click "QUICK TAP"
3. Name: "Sprint"
4. Button: "LS" (Left Stick)
5. Taps: 2
6. Max Delay: 300ms
7. Action: ExecuteButtons → L3
```

---

## 📊 Feature Comparison

| Feature | Basic | Advanced | Pro |
|---------|-------|----------|-----|
| Anti-Recoil | ✅ | ✅ | ✅ |
| Aim Assist | ✅ | ✅ | ✅ |
| Rapid Fire | ✅ | ✅ | ✅ |
| **Macros** | ❌ | ✅ | ✅ |
| **Button Remap** | ❌ | ✅ | ✅ |
| **Combos** | ❌ | ❌ | ✅ |
| **Statistics** | ❌ | ❌ | ✅ |
| **Import/Export** | ❌ | ✅ | ✅ |

**You have: PRO Edition! 🔥**

---

## 🆘 Troubleshooting

### Macros Not Playing
**Solution:**
1. Check macro has valid actions
2. Verify `ExecuteButtonAction` is configured
3. Ensure no other systems blocking input

### Button Remapping Not Working
**Solution:**
1. Verify profile is activated (green checkmark)
2. Check mapping exists in active profile
3. Ensure modifier states are correct

### Combos Not Detecting
**Solution:**
1. Increase timing window (MaxDelayMs)
2. Verify button names match exactly
3. Check combo is enabled (✓)
4. Reset combo states

### Window Won't Open
**Solution:**
1. Check logs for errors
2. Restart application
3. Delete config files (backup first!)

---

## 📞 Support & Resources

- **Documentation**: [ADVANCED_FEATURES.md](ADVANCED_FEATURES.md)
- **User Guide**: [USER_GUIDE.md](USER_GUIDE.md)
- **Visual Guide**: [VISUAL_ENHANCEMENTS.md](VISUAL_ENHANCEMENTS.md)
- **FAQ**: [FAQ.md](FAQ.md)
- **Discord**: Join community for help
- **GitHub Issues**: Report bugs

---

## 🎮 Controller Button Names Reference

### Xbox Controller
```
Buttons: A, B, X, Y
Triggers: L1, R1, L2, R2
D-Pad: DPadUp, DPadDown, DPadLeft, DPadRight
Sticks: L3, R3
Special: Start, Select, Guide
```

### PlayStation Controller
```
Buttons: Cross (A), Circle (B), Square (X), Triangle (Y)
Triggers: L1, R1, L2, R2
D-Pad: DPadUp, DPadDown, DPadLeft, DPadRight
Sticks: L3, R3
Special: Start (Options), Select (Share), Guide (PS)
```

---

## 📈 Statistics Explained

| Metric | Meaning |
|--------|---------|
| **Total Macros** | Number of saved macros |
| **Total Profiles** | Button remapping profiles |
| **Total Combos** | Configured combos |
| **Success Count** | Times combo triggered |
| **Action Count** | Steps in macro |
| **Duration** | Total macro playback time |

---

## 🌟 What's New in v6.0.4 BANGER

### Professional Features
✨ **Macro System** - Record unlimited button sequences
✨ **Button Remapping** - Complete controller customization
✨ **Combo Detection** - Fighting game inputs
✨ **Statistics Dashboard** - Track everything
✨ **Import/Export** - Share configurations

### Quality of Life
✨ **Auto-save** - Never lose your work
✨ **Backup System** - Automatic config backups
✨ **Error Handling** - Better error messages
✨ **Performance** - Optimized for speed
✨ **Documentation** - Complete guides

---

**Made with ❤️ for the gaming community**

**Version**: 6.0.4 BANGER UPDATE 🔥
**Date**: 2025
**Platform**: Windows 10/11
**Framework**: .NET 8.0

---

> **"From good to BANGER - Your controller, your rules!"** 

🎮 **Happy Gaming!** 🎮
