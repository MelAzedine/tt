# 🤝 Contributing to ARTHEMIS CONTROL

Thank you for your interest in contributing to ARTHEMIS CONTROL! This document provides guidelines for contributing to the project.

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [How Can I Contribute?](#how-can-i-contribute)
3. [Development Setup](#development-setup)
4. [Coding Standards](#coding-standards)
5. [Pull Request Process](#pull-request-process)
6. [Testing Guidelines](#testing-guidelines)
7. [Documentation](#documentation)

---

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inspiring community for all. Please:

- ✅ Be respectful and inclusive
- ✅ Welcome newcomers and help them learn
- ✅ Accept constructive criticism gracefully
- ✅ Focus on what's best for the community
- ❌ Do not harass, discriminate, or be disrespectful
- ❌ Do not share exploits or cheats publicly
- ❌ Do not violate game ToS or anti-cheat policies

---

## How Can I Contribute?

### Reporting Bugs

Before creating a bug report, please:

1. **Check existing issues** to avoid duplicates
2. **Update to latest version** to ensure bug still exists
3. **Gather information**:
   - Windows version
   - Application version
   - Steps to reproduce
   - Expected vs actual behavior
   - Screenshots/logs if applicable

**Bug Report Template**:
```markdown
**Describe the bug**
A clear description of the bug.

**To Reproduce**
Steps to reproduce the behavior:
1. Go to '...'
2. Click on '...'
3. See error

**Expected behavior**
What you expected to happen.

**Screenshots**
Add screenshots if applicable.

**Environment:**
 - OS: [e.g. Windows 11]
 - Version: [e.g. 6.0.3]
 - Controller: [e.g. Xbox Elite 2]

**Additional context**
Any other relevant information.
```

### Suggesting Features

Feature requests are welcome! Please:

1. **Check existing feature requests** first
2. **Explain the use case** clearly
3. **Describe the solution** you envision
4. **Consider alternatives** you've thought about

**Feature Request Template**:
```markdown
**Problem Statement**
What problem does this solve?

**Proposed Solution**
Describe your proposed solution.

**Alternatives Considered**
What alternatives have you considered?

**Additional Context**
Screenshots, mockups, or examples.
```

### Contributing Code

Areas where contributions are welcome:

- 🐛 **Bug Fixes**: Fix reported issues
- ✨ **Features**: Implement new features
- 🎨 **UI/UX**: Improve interface and user experience
- 📝 **Documentation**: Improve docs and guides
- 🧪 **Tests**: Add or improve test coverage
- 🔧 **Optimization**: Performance improvements
- 🌍 **Localization**: Add new language support

---

## Development Setup

### Prerequisites

- **Visual Studio 2022** (Community or higher)
- **.NET 8.0 SDK**
- **ViGEm Bus Driver**
- **Git**

### Getting Started

1. **Fork the repository**
   ```bash
   # On GitHub, click the Fork button
   ```

2. **Clone your fork**
   ```bash
   git clone https://github.com/YOUR_USERNAME/tt.git
   cd tt
   ```

3. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/MelAzedine/tt.git
   ```

4. **Create a branch**
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/bug-description
   ```

5. **Open in Visual Studio**
   ```
   Open Trident.MITM.App.sln
   ```

6. **Restore packages**
   ```
   Right-click solution → Restore NuGet Packages
   ```

7. **Build the project**
   ```
   Press F6 or Build → Build Solution
   ```

### Project Structure

```
tt/
├── Assets/              # Images and resources
├── *.xaml               # UI files
├── *.xaml.cs            # UI code-behind
├── *.cs                 # Core logic files
├── trident.json         # Configuration file
├── Trident.MITM.App.csproj
└── Trident.MITM.App.sln
```

---

## Coding Standards

### C# Style Guide

Follow these conventions:

**Naming**:
- `PascalCase` for classes, methods, properties
- `camelCase` for local variables, parameters
- `_camelCase` for private fields
- `UPPER_CASE` for constants

**Example**:
```csharp
public class WeaponProfile
{
    private double _recoilValue;
    private const int MAX_RECOIL = 100;
    
    public string WeaponName { get; set; }
    
    public void ApplyRecoil(double value)
    {
        _recoilValue = Math.Min(value, MAX_RECOIL);
    }
}
```

**Code Style**:
- Use 4 spaces for indentation (no tabs)
- Place opening braces on new line
- Use `var` only when type is obvious
- Always use braces for control statements
- Add XML documentation for public members

**Example**:
```csharp
/// <summary>
/// Applies anti-recoil compensation to stick input
/// </summary>
/// <param name="input">Raw stick input value</param>
/// <returns>Compensated stick value</returns>
public double ApplyAntiRecoil(double input)
{
    if (input < 0)
    {
        return input * _compensationFactor;
    }
    return input;
}
```

### XAML Style Guide

- Use consistent indentation (4 spaces)
- Group related properties
- Use meaningful names for x:Name
- Comment complex bindings

**Example**:
```xaml
<Button x:Name="ApplyButton"
        Content="Apply Settings"
        Style="{StaticResource ModernButton}"
        Click="ApplyButton_Click"
        Margin="10,5"
        HorizontalAlignment="Right"/>
```

### Git Commit Messages

Follow conventional commits:

**Format**:
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting)
- `refactor`: Code refactoring
- `test`: Adding tests
- `chore`: Maintenance tasks

**Examples**:
```
feat(anti-recoil): add timeline-based recoil patterns

Implement timeline editor for custom recoil compensation.
Users can now draw recoil patterns and save them per weapon.

Closes #123
```

```
fix(ui): correct toggle switch animation glitch

Fixed issue where toggle switch would jump when rapidly clicked.
Added debounce logic to prevent animation conflicts.

Fixes #456
```

---

## Pull Request Process

### Before Submitting

1. **Update from upstream**
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Test your changes thoroughly**
   - Build succeeds without errors/warnings
   - Features work as expected
   - No regressions introduced
   - All tests pass (if applicable)

3. **Update documentation**
   - Update README if needed
   - Add XML comments to new public members
   - Update CHANGELOG

4. **Commit your changes**
   ```bash
   git add .
   git commit -m "feat: your feature description"
   ```

5. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

### Submitting PR

1. **Create Pull Request on GitHub**
   - Use a clear, descriptive title
   - Reference related issues (#123)
   - Describe what changed and why
   - Include screenshots for UI changes

2. **PR Template**:
   ```markdown
   ## Description
   Brief description of changes
   
   ## Type of Change
   - [ ] Bug fix
   - [ ] New feature
   - [ ] Breaking change
   - [ ] Documentation update
   
   ## Testing Done
   Describe testing performed
   
   ## Screenshots (if applicable)
   Add screenshots
   
   ## Checklist
   - [ ] Code builds without errors
   - [ ] Tested on Windows 10/11
   - [ ] Added/updated documentation
   - [ ] No compiler warnings
   - [ ] Follows coding standards
   ```

### Review Process

1. **Automated checks** will run (build, tests)
2. **Maintainer review** - may request changes
3. **Address feedback** - push updates to same branch
4. **Approval & merge** - maintainer will merge PR

---

## Testing Guidelines

### Manual Testing

Always test:

1. **Build Process**
   - Clean build succeeds
   - No warnings in Error List
   - Application starts without errors

2. **Core Features**
   - Controller detection
   - Feature toggles (anti-recoil, aim assist, etc.)
   - Profile switching
   - Hotkey functionality

3. **UI/UX**
   - All tabs navigate properly
   - Buttons and controls responsive
   - No visual glitches
   - Animations smooth

4. **Edge Cases**
   - Controller disconnect/reconnect
   - Rapid feature toggling
   - Invalid configuration values
   - Missing files/permissions

### Automated Testing

(If/when implemented)

```csharp
[TestMethod]
public void TestAntiRecoilCompensation()
{
    var profile = new WeaponProfile();
    profile.AntiRecoilValue = 1.5;
    
    double input = -100.0;
    double result = profile.ApplyRecoil(input);
    
    Assert.AreEqual(-150.0, result, 0.01);
}
```

---

## Documentation

### Code Documentation

Use XML documentation for public APIs:

```csharp
/// <summary>
/// Manages weapon-specific recoil profiles
/// </summary>
public class WeaponProfileManager
{
    /// <summary>
    /// Gets or sets the active weapon profile
    /// </summary>
    public WeaponProfile ActiveProfile { get; set; }
    
    /// <summary>
    /// Loads a profile from file
    /// </summary>
    /// <param name="filePath">Path to profile file</param>
    /// <returns>Loaded weapon profile</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when profile file doesn't exist
    /// </exception>
    public WeaponProfile LoadProfile(string filePath)
    {
        // Implementation
    }
}
```

### User Documentation

When adding features:

1. Update README.md with feature description
2. Add section to USER_GUIDE.md with:
   - Feature explanation
   - Configuration steps
   - Screenshots/examples
   - Tips and best practices

---

## Community

### Getting Help

- 💬 **Discord**: Join our community server
- 📧 **Email**: dev@arthemiscontrol.com
- 🐛 **Issues**: GitHub Issues for bugs
- 💡 **Discussions**: GitHub Discussions for questions

### Recognition

Contributors will be:
- Added to CONTRIBUTORS.md
- Mentioned in release notes
- Credited in application About section (for significant contributions)

---

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.

---

## Questions?

Don't hesitate to ask! Open an issue with the "question" label or reach out on Discord.

**Thank you for making ARTHEMIS CONTROL better! 🎮**

---

<div align="center">

[⬆ Back to Top](#-contributing-to-arthemis-control)

</div>
