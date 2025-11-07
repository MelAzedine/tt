# ❓ Frequently Asked Questions (FAQ)

Common questions about ARTHEMIS CONTROL

---

## General Questions

### What is ARTHEMIS CONTROL?

ARTHEMIS CONTROL is a professional-grade gamepad enhancement software for Windows that provides features like anti-recoil compensation, aim assist, rapid-fire, and advanced controller customization. It uses the ViGEm virtual controller framework to work seamlessly with games.

### Is this software free?

No, ARTHEMIS CONTROL is commercial software that requires a valid license key. Different license tiers are available for personal, professional, and team use.

### What platforms are supported?

Currently, ARTHEMIS CONTROL supports:
- **Windows 10** (version 1903 or later)
- **Windows 11** (all versions)
- **x64 architecture only**

Mac and Linux are not supported.

---

## Compatibility

### Which controllers are supported?

ARTHEMIS CONTROL works with:
- ✅ Xbox One controllers (wired/wireless)
- ✅ Xbox Series X|S controllers
- ✅ Xbox Elite controllers (1 & 2)
- ✅ PlayStation DualShock 4
- ✅ PlayStation DualSense (PS5)
- ✅ Generic XInput controllers
- ✅ DirectInput controllers (via ViGEm)

### Which games work with this?

ARTHEMIS CONTROL works with any game that accepts XInput or DirectInput controllers. Pre-configured profiles exist for:
- Fortnite
- Call of Duty: Black Ops 6
- Call of Duty: Warzone
- Battlefield 6

You can create custom profiles for any game.

### Does it work with keyboard and mouse?

Yes! ARTHEMIS CONTROL supports hybrid input, allowing you to use controller enhancement features while playing with keyboard and mouse.

---

## Installation & Setup

### How do I install ARTHEMIS CONTROL?

1. Download the installer from the official website
2. Install ViGEm Bus Driver (included)
3. Run the application as Administrator
4. Enter your license key
5. Connect your controller
6. Start gaming!

Detailed instructions in [README.md](README.md#-installation)

### Why do I need to run as Administrator?

Administrator privileges are required to:
- Access low-level controller drivers
- Create virtual controllers via ViGEm
- Apply system optimizations
- Inject input modifications

The software will not function properly without admin rights.

### What is ViGEm Bus Driver?

ViGEm (Virtual Gamepad Emulation Framework) is a free, open-source driver that allows applications to create virtual Xbox 360 and DualShock 4 controllers. It's required for ARTHEMIS CONTROL to function.

---

## Features

### How does anti-recoil work?

Anti-recoil compensates for weapon recoil by automatically adjusting your aim in the opposite direction of the recoil pattern. You can:
- Set vertical and horizontal compensation values
- Create custom patterns per weapon
- Use timeline-based compensation
- Enable ADS-only mode for precision

### What is aim assist?

Aim assist adds subtle movement to your aim to help with target tracking. Three modes available:
- **Sinusoidal**: Smooth wave pattern
- **Circular**: Consistent circular motion
- **Spiral**: Expanding/contracting spiral

All parameters (amplitude, frequency) are customizable.

### Is rapid-fire detectable?

Rapid-fire converts semi-automatic weapons to full-auto by rapidly pressing the fire button. While the software operates at the driver level, extremely high frequencies (>20Hz) may be noticeable. Use responsibly and within game rules.

### Can I use multiple features at once?

Yes! You can combine:
- Anti-recoil + Aim Assist
- Anti-recoil + Rapid-Fire
- All features simultaneously

Each feature can be toggled independently.

---

## Configuration

### How do I create a custom profile?

1. Configure all features to your preferences
2. Go to Settings → Profile Management
3. Click "Save as New Profile"
4. Name your profile
5. Profile saved for future use

You can also import/export profiles as JSON files.

### What is dead zone configuration?

Dead zones define how your controller sticks respond:
- **Dead Zone**: Ignores small movements (fixes drift)
- **Anti-Dead Zone**: Makes stick more responsive
- **Max Zone**: Limits maximum stick value

Adjust these in the Controller tab while viewing real-time stick response.

### How do I set up weapon detection?

Two methods available:

**OCR Detection** (Optical Character Recognition):
1. Enable in Settings → Weapon Detection
2. Configure capture area on screen
3. Software reads weapon name from HUD
4. Auto-switches to weapon profile

**Recoil-Based Detection**:
1. Enable in Settings → Weapon Detection
2. Software analyzes recoil pattern
3. Matches to known weapon signatures
4. Auto-switches profile

---

## Troubleshooting

### Controller not detected?

Try these steps:
1. ✅ Check USB connection / Bluetooth pairing
2. ✅ Install/reinstall ViGEm Bus Driver
3. ✅ Run application as Administrator
4. ✅ Try different USB port
5. ✅ Restart application
6. ✅ Check Windows Device Manager for errors

### Features not working?

Verify:
1. ✅ Feature toggle is ON (green indicator)
2. ✅ Controller is connected
3. ✅ Running as Administrator
4. ✅ No conflicting software (other gamepad utilities)
5. ✅ ViGEm driver installed correctly

### High input lag?

Reduce latency by:
1. ✅ Use wired connection instead of Bluetooth
2. ✅ Apply system optimizations (Optimization tab)
3. ✅ Close background applications
4. ✅ Disable Windows Game Bar
5. ✅ Set polling rate to 1000 Hz

### Profile not loading?

Check:
1. ✅ trident.json file exists
2. ✅ JSON syntax is valid
3. ✅ Profile name matches exactly
4. ✅ No special characters in profile name
5. ✅ File permissions allow reading

### Hotkeys not responding?

Ensure:
1. ✅ Running as Administrator
2. ✅ No key conflicts with other software
3. ✅ Hotkey properly assigned in Settings
4. ✅ Game not blocking system-wide hotkeys
5. ✅ Try different key combinations

---

## Performance

### Will this affect my game's FPS?

No. ARTHEMIS CONTROL operates at the driver level and has minimal CPU/GPU impact (<1% on modern systems). The system optimization features may actually improve FPS.

### How much RAM does it use?

Typical memory usage is 50-100 MB, depending on active features. Very lightweight compared to modern games.

### Does it work on low-end PCs?

Yes! Minimum requirements:
- Windows 10 (1903+)
- 4 GB RAM
- Any x64 CPU
- 100 MB free storage

The software is highly optimized and works on budget systems.

---

## Legal & Ethics

### Is this software legal?

Yes, ARTHEMIS CONTROL is legal software. However:
- ⚠️ You are responsible for compliance with game ToS
- ⚠️ Check anti-cheat policies before use
- ⚠️ Use may violate competitive gaming rules
- ⚠️ Account bans are your responsibility

### Will I get banned for using this?

**It depends on the game**. ARTHEMIS CONTROL:
- Operates at driver level (harder to detect)
- Does not modify game files
- Does not inject into game processes
- Uses legitimate Windows APIs

However:
- Some games prohibit ANY third-party tools
- Competitive modes may have stricter rules
- Anti-cheat systems vary by game

**Always check game ToS before use!**

### Can I use this in tournaments?

**Most likely NO**. Professional and tournament play typically prohibit:
- Third-party input modifications
- Aim assistance
- Recoil compensation
- Rapid-fire

Check specific tournament rules. When in doubt, don't use it.

### Is this considered cheating?

This is subjective and game-dependent:

**Arguments for legitimate use**:
- Accessibility tool for players with disabilities
- Controller customization is personal preference
- Equivalent to expensive hardware features
- Many "pro" controllers have similar features

**Arguments against**:
- Provides advantage over stock controller users
- May violate fair play principles
- Some features (aim assist) cross ethical lines
- Not all players can afford it

**Our position**: Use responsibly, ethically, and within game rules.

---

## Licensing

### What license types are available?

- **Personal** ($49/year): Individual use, standard support
- **Professional** ($99/year): Priority support, early access
- **Team** ($299/year): Multi-user, custom integrations
- **Lifetime** ($299): One-time purchase, lifetime updates

### Can I transfer my license?

No. Licenses are non-transferable and tied to your account. Contact support for special circumstances.

### What happens when my license expires?

- Software stops functioning
- Cannot use any features
- Must renew to continue use
- Profiles and settings preserved for renewal

### Is there a free trial?

Yes! 7-day free trial available:
1. Download from official website
2. Select "Free Trial" during activation
3. Full feature access for 7 days
4. No credit card required

### Can I get a refund?

Yes, within 30 days of purchase if:
- Software doesn't work as described
- Technical issues cannot be resolved
- Unsatisfied with features

No refunds for:
- Violations of terms of service
- Game bans (your responsibility)
- Changed mind after 30 days

---

## Updates & Support

### How do I update the software?

Updates are automatic:
1. Application checks for updates on startup
2. Notification shown if update available
3. Click "Update Now" to download
4. Application restarts with new version

Or manually: Settings → About → Check for Updates

### How often are updates released?

- **Major updates** (x.0.0): Quarterly
- **Minor updates** (x.x.0): Monthly
- **Patches** (x.x.x): As needed for bugs

All updates free during active license period.

### How do I get support?

Multiple support channels:
- 📧 **Email**: support@arthemiscontrol.com (24-48hr response)
- 💬 **Discord**: Community and quick help
- 🐛 **GitHub**: Bug reports and feature requests
- 📚 **Documentation**: README, User Guide, FAQ
- 🎥 **YouTube**: Video tutorials

Priority support for Professional/Team licenses.

### Where can I report bugs?

1. **GitHub Issues**: Best for technical bugs
2. **Discord**: Quick community troubleshooting
3. **Email**: Detailed bug reports with logs

Please include:
- Windows version
- Application version
- Steps to reproduce
- Error messages/logs
- Screenshots if applicable

---

## Community

### Is there a Discord server?

Yes! Join our community:
- Get help from other users
- Share profiles and configurations
- Provide feedback and suggestions
- Chat with developers
- Participate in beta testing

Link: https://discord.gg/arthemis

### Can I contribute to development?

Absolutely! See [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Code contributions
- Feature suggestions
- Bug reports
- Documentation improvements
- Community support

### Are there video tutorials?

Yes! Official YouTube channel has:
- Getting started guide
- Feature walkthroughs
- Profile creation tutorials
- Optimization guides
- Tips and tricks

Subscribe for new content: https://youtube.com/@arthemiscontrol

---

## Advanced

### Can I create custom Bezier curves?

Yes! Advanced stick response customization:
1. Go to Controller → Stick Settings
2. Enable "Custom Curve"
3. Adjust control points
4. Test in real-time
5. Save curve to profile

### How does timeline-based recoil work?

Timeline editor allows frame-perfect recoil compensation:
1. Open "Draw Your Recoil" tool
2. Draw weapon's recoil pattern
3. Timeline auto-generated from drawing
4. Fine-tune segments (duration, velocity, easing)
5. Apply to weapon profile

### Can I script macros?

Currently, macro support is limited to:
- Hotkey combinations
- Profile switching
- Feature toggling

Full scripting support planned for version 7.0.

### Is there an API for integration?

Not currently. API and plugin system planned for:
- Custom extensions
- Third-party integrations
- Community tools

Planned for version 7.0 (Q4 2024).

---

## Comparison

### How does this compare to hardware solutions?

| Feature | ARTHEMIS CONTROL | Scuf/Elite Controllers | XIM Apex | Cronus Zen |
|---------|------------------|------------------------|----------|------------|
| Price | $49-299/year | $150-300 (one-time) | $125 | $100 |
| Anti-Recoil | ✅ Advanced | ❌ No | ✅ Basic | ✅ Advanced |
| Aim Assist | ✅ Multiple modes | ❌ No | ✅ Basic | ✅ Basic |
| Rapid-Fire | ✅ Full control | ❌ No | ✅ Yes | ✅ Yes |
| Customization | ✅ Unlimited | ✅ Limited | ✅ Good | ✅ Good |
| Updates | ✅ Regular | ❌ No | ✅ Occasional | ✅ Regular |
| Setup | ⚡ Easy | ⚡ Easy | ⚠️ Complex | ⚠️ Complex |
| Portability | ❌ PC only | ✅ Any platform | ✅ Console | ✅ Console |

### Advantages of ARTHEMIS CONTROL:
- More advanced features
- Regular updates
- Better customization
- Lower initial cost
- Easy to update/configure

### Advantages of hardware:
- Works on consoles
- More portable
- One-time cost
- Harder to detect
- Official hardware warranty

---

## Didn't Find Your Answer?

Still have questions?

1. 📖 Check [User Guide](USER_GUIDE.md) for detailed instructions
2. 📋 Read [README](README.md) for feature overview
3. 💬 Ask on [Discord](https://discord.gg/arthemis)
4. 📧 Email [support@arthemiscontrol.com](mailto:support@arthemiscontrol.com)
5. 🐛 Open [GitHub Issue](https://github.com/MelAzedine/tt/issues)

---

<div align="center">

**Happy Gaming! 🎮**

*Questions? Suggestions? Let us know!*

[⬆ Back to Top](#-frequently-asked-questions-faq)

</div>
