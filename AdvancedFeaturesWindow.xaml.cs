// AdvancedFeaturesWindow.xaml.cs - Code-behind for advanced features management
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Trident.MITM
{
    public partial class AdvancedFeaturesWindow : Window
    {
        private MacroSystem _macroSystem;
        private ButtonRemappingSystem _remappingSystem;
        private ComboSystem _comboSystem;
        private Action<string, bool>? _executeButtonAction;

        public AdvancedFeaturesWindow(
            MacroSystem macroSystem, 
            ButtonRemappingSystem remappingSystem, 
            ComboSystem comboSystem,
            Action<string, bool>? executeButtonAction = null)
        {
            InitializeComponent();
            
            _macroSystem = macroSystem;
            _remappingSystem = remappingSystem;
            _comboSystem = comboSystem;
            _executeButtonAction = executeButtonAction;

            LoadMacros();
            LoadProfiles();
            LoadCombos();
            UpdateStatistics();

            // Subscribe to events
            _macroSystem.MacroRecordingStarted += OnMacroRecordingStarted;
            _macroSystem.MacroRecordingStopped += OnMacroRecordingStopped;
            _comboSystem.ComboDetected += OnComboDetected;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #region Macro Management

        private void LoadMacros()
        {
            MacroListBox.Items.Clear();
            foreach (var macro in _macroSystem.Macros.Values)
            {
                MacroListBox.Items.Add($"⚡ {macro.Name} ({macro.ActionCount} actions)");
            }
        }

        private void MacroListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MacroListBox.SelectedIndex < 0) return;

            var macroName = MacroListBox.SelectedItem.ToString()?.Split('(')[0].Replace("⚡ ", "").Trim();
            if (macroName == null) return;

            var macro = _macroSystem.GetMacro(macroName);
            if (macro != null)
            {
                MacroNameTextBox.Text = macro.Name;
                MacroDescTextBox.Text = macro.Description;
                MacroLoopCheckBox.IsChecked = macro.Loop;
                MacroLoopCountTextBox.Text = macro.LoopCount.ToString();
                MacroActionsGrid.ItemsSource = macro.Actions;
            }
        }

        private void NewMacro_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("New Macro", "Enter macro name:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
            {
                var macro = new Macro
                {
                    Name = dialog.ResponseText,
                    Description = "New macro"
                };
                
                _macroSystem.AddOrUpdateMacro(macro);
                LoadMacros();
                UpdateStatistics();
                
                MessageBox.Show($"Macro '{macro.Name}' created!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RecordMacro_Click(object sender, RoutedEventArgs e)
        {
            if (_macroSystem.IsRecording)
            {
                var macro = _macroSystem.StopRecording();
                RecordMacroButton.Content = "🎬 START RECORDING";
                LoadMacros();
                MessageBox.Show($"Recording stopped! Macro '{macro.Name}' saved with {macro.ActionCount} actions.", 
                    "Recording Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var dialog = new InputDialog("Record Macro", "Enter macro name:");
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
                {
                    _macroSystem.StartRecording(dialog.ResponseText);
                    RecordMacroButton.Content = "⏹ STOP RECORDING";
                    MessageBox.Show("Recording started! Press controller buttons to record your macro.", 
                        "Recording", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private async void PlayMacro_Click(object sender, RoutedEventArgs e)
        {
            if (MacroListBox.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a macro to play.", "No Selection", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var macroName = MacroListBox.SelectedItem.ToString()?.Split('(')[0].Replace("⚡ ", "").Trim();
            if (macroName == null) return;

            try
            {
                if (_executeButtonAction != null)
                {
                    await _macroSystem.PlayMacroAsync(macroName, _executeButtonAction);
                    MessageBox.Show($"Macro '{macroName}' playback complete!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Button execution not configured.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing macro: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteMacro_Click(object sender, RoutedEventArgs e)
        {
            if (MacroListBox.SelectedIndex < 0) return;

            var macroName = MacroListBox.SelectedItem.ToString()?.Split('(')[0].Replace("⚡ ", "").Trim();
            if (macroName == null) return;

            var result = MessageBox.Show($"Delete macro '{macroName}'?", "Confirm Delete", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _macroSystem.DeleteMacro(macroName);
                LoadMacros();
                UpdateStatistics();
            }
        }

        private void SaveMacro_Click(object sender, RoutedEventArgs e)
        {
            if (MacroListBox.SelectedIndex < 0) return;

            var macroName = MacroListBox.SelectedItem.ToString()?.Split('(')[0].Replace("⚡ ", "").Trim();
            if (macroName == null) return;

            var macro = _macroSystem.GetMacro(macroName);
            if (macro != null)
            {
                macro.Name = MacroNameTextBox.Text;
                macro.Description = MacroDescTextBox.Text;
                macro.Loop = MacroLoopCheckBox.IsChecked ?? false;
                if (int.TryParse(MacroLoopCountTextBox.Text, out int loopCount))
                {
                    macro.LoopCount = loopCount;
                }

                _macroSystem.AddOrUpdateMacro(macro);
                LoadMacros();
                
                MessageBox.Show("Macro saved successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportMacro_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = "macros_export.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = _macroSystem.ExportMacros();
                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show("Macros exported successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportMacro_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    _macroSystem.ImportMacros(json, merge: true);
                    LoadMacros();
                    UpdateStatistics();
                    MessageBox.Show("Macros imported successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnMacroRecordingStarted(object? sender, MacroEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                RecordMacroButton.Content = "⏹ STOP RECORDING";
            });
        }

        private void OnMacroRecordingStopped(object? sender, MacroEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                RecordMacroButton.Content = "🎬 START RECORDING";
                LoadMacros();
            });
        }

        #endregion

        #region Button Remapping

        private void LoadProfiles()
        {
            ProfileListBox.Items.Clear();
            foreach (var profile in _remappingSystem.Profiles.Values)
            {
                var indicator = profile == _remappingSystem.ActiveProfile ? "✓ " : "   ";
                ProfileListBox.Items.Add($"{indicator}🎯 {profile.Name}");
            }
        }

        private void ProfileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileListBox.SelectedIndex < 0) return;

            var profileName = ProfileListBox.SelectedItem.ToString()?.Replace("✓ ", "").Replace("   ", "").Replace("🎯 ", "").Trim();
            if (profileName == null) return;

            LoadButtonMappings(profileName);
        }

        private void LoadButtonMappings(string profileName)
        {
            ButtonMappingPanel.Children.Clear();

            if (!_remappingSystem.Profiles.TryGetValue(profileName, out var profile))
                return;

            foreach (var mapping in profile.Mappings)
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                panel.Children.Add(new TextBlock
                {
                    Text = $"{mapping.SourceButton} → {mapping.TargetButton}",
                    Width = 200,
                    Foreground = System.Windows.Media.Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                });

                var deleteBtn = new Button
                {
                    Content = "🗑️",
                    Width = 30,
                    Height = 25,
                    Margin = new Thickness(10, 0, 0, 0),
                    Tag = mapping
                };
                deleteBtn.Click += DeleteMapping_Click;
                panel.Children.Add(deleteBtn);

                ButtonMappingPanel.Children.Add(panel);
            }
        }

        private void NewProfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("New Profile", "Enter profile name:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
            {
                _remappingSystem.CreateProfile(dialog.ResponseText);
                LoadProfiles();
                UpdateStatistics();
                MessageBox.Show($"Profile '{dialog.ResponseText}' created!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ActivateProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileListBox.SelectedIndex < 0) return;

            var profileName = ProfileListBox.SelectedItem.ToString()?.Replace("✓ ", "").Replace("   ", "").Replace("🎯 ", "").Trim();
            if (profileName == null) return;

            if (_remappingSystem.ActivateProfile(profileName))
            {
                LoadProfiles();
                MessageBox.Show($"Profile '{profileName}' activated!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileListBox.SelectedIndex < 0) return;

            var profileName = ProfileListBox.SelectedItem.ToString()?.Replace("✓ ", "").Replace("   ", "").Replace("🎯 ", "").Trim();
            if (profileName == null || profileName == "Default") return;

            var result = MessageBox.Show($"Delete profile '{profileName}'?", "Confirm Delete", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _remappingSystem.DeleteProfile(profileName);
                LoadProfiles();
                UpdateStatistics();
            }
        }

        private void AddMapping_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ButtonMappingDialog();
            if (dialog.ShowDialog() == true)
            {
                _remappingSystem.AddMapping(dialog.SourceButton, dialog.TargetButton);
                
                var profileName = ProfileListBox.SelectedItem?.ToString()?.Replace("✓ ", "").Replace("   ", "").Replace("🎯 ", "").Trim();
                if (profileName != null)
                {
                    LoadButtonMappings(profileName);
                }
            }
        }

        private void DeleteMapping_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ButtonMapping mapping)
            {
                _remappingSystem.RemoveMapping(mapping.SourceButton, mapping.Modifiers);
                
                var profileName = ProfileListBox.SelectedItem?.ToString()?.Replace("✓ ", "").Replace("   ", "").Replace("🎯 ", "").Trim();
                if (profileName != null)
                {
                    LoadButtonMappings(profileName);
                }
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Profile saved successfully!", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileListBox.SelectedIndex < 0) return;

            var profileName = ProfileListBox.SelectedItem.ToString()?.Replace("✓ ", "").Replace("   ", "").Replace("🎯 ", "").Trim();
            if (profileName == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"{profileName}_profile.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = _remappingSystem.ExportProfile(profileName);
                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show("Profile exported successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Combo System

        private void LoadCombos()
        {
            ComboListBox.Items.Clear();
            foreach (var combo in _comboSystem.Combos.Values)
            {
                var status = combo.IsEnabled ? "✓" : "✗";
                ComboListBox.Items.Add($"{status} ⚔️ {combo.Name} ({combo.Inputs.Count} inputs)");
            }
        }

        private void ComboListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboListBox.SelectedIndex < 0) return;

            var comboName = ComboListBox.SelectedItem.ToString()?.Split('⚔')[1].Split('(')[0].Trim();
            if (comboName == null) return;

            var combo = _comboSystem.GetCombo(comboName);
            if (combo != null)
            {
                ComboStatusText.Text = $"{combo.Name}: {combo.Description} (Success: {combo.SuccessCount}x)";
                
                var inputsWithStep = combo.Inputs.Select((input, index) => new
                {
                    Step = index + 1,
                    input.ButtonName,
                    input.RequirePress,
                    input.MaxDelayMs
                }).ToList();
                
                ComboInputsGrid.ItemsSource = inputsWithStep;
            }
        }

        private void NewCombo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("New Combo", "Enter combo name:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
            {
                var combo = new Combo
                {
                    Name = dialog.ResponseText,
                    Description = "New combo"
                };
                
                _comboSystem.AddCombo(combo);
                LoadCombos();
                UpdateStatistics();
                
                MessageBox.Show($"Combo '{combo.Name}' created!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void QuickTapCombo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new QuickTapDialog();
            if (dialog.ShowDialog() == true)
            {
                _comboSystem.CreateQuickTapCombo(
                    dialog.ComboName,
                    dialog.Button,
                    dialog.TapCount,
                    dialog.MaxDelay
                );
                
                LoadCombos();
                UpdateStatistics();
                
                MessageBox.Show($"Quick tap combo created: {dialog.TapCount}x {dialog.Button}", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteCombo_Click(object sender, RoutedEventArgs e)
        {
            if (ComboListBox.SelectedIndex < 0) return;

            var comboName = ComboListBox.SelectedItem.ToString()?.Split('⚔')[1].Split('(')[0].Trim();
            if (comboName == null) return;

            var result = MessageBox.Show($"Delete combo '{comboName}'?", "Confirm Delete", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _comboSystem.RemoveCombo(comboName);
                LoadCombos();
                UpdateStatistics();
            }
        }

        private void SaveCombo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Combo saved successfully!", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportCombos_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = "combos_export.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = _comboSystem.ExportCombos();
                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show("Combos exported successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnComboDetected(object? sender, ComboEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Combo detected: {e.Combo.Name}", "Combo!", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        #endregion

        #region Statistics

        private void UpdateStatistics()
        {
            MacroCountText.Text = $"{_macroSystem.Macros.Count} Total";
            ProfileCountText.Text = $"{_remappingSystem.Profiles.Count} Total";
            ComboCountText.Text = $"{_comboSystem.Combos.Count} Total";

            StatsListBox.Items.Clear();
            
            // Add top macros by usage
            var topMacros = _macroSystem.Macros.Values
                .OrderByDescending(m => m.ActionCount)
                .Take(5);
            
            foreach (var macro in topMacros)
            {
                StatsListBox.Items.Add($"⚡ {macro.Name} - {macro.ActionCount} actions");
            }

            // Add top combos by success count
            var topCombos = _comboSystem.Combos.Values
                .OrderByDescending(c => c.SuccessCount)
                .Take(5);
            
            foreach (var combo in topCombos)
            {
                StatsListBox.Items.Add($"⚔️ {combo.Name} - {combo.SuccessCount} successes");
            }
        }

        #endregion
    }

    #region Helper Dialogs

    public class InputDialog : Window
    {
        public string ResponseText { get; private set; } = string.Empty;
        private TextBox _textBox;

        public InputDialog(string title, string prompt)
        {
            Title = title;
            Width = 400;
            Height = 180;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(13, 17, 23));
            
            var stack = new StackPanel { Margin = new Thickness(20) };
            
            stack.Children.Add(new TextBlock
            {
                Text = prompt,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            });

            _textBox = new TextBox
            {
                Padding = new Thickness(8),
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stack.Children.Add(_textBox);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okBtn = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            okBtn.Click += (s, e) => { ResponseText = _textBox.Text; DialogResult = true; };
            btnPanel.Children.Add(okBtn);

            var cancelBtn = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 30
            };
            cancelBtn.Click += (s, e) => { DialogResult = false; };
            btnPanel.Children.Add(cancelBtn);

            stack.Children.Add(btnPanel);
            Content = stack;
        }
    }

    public class ButtonMappingDialog : Window
    {
        public string SourceButton { get; private set; } = string.Empty;
        public string TargetButton { get; private set; } = string.Empty;

        public ButtonMappingDialog()
        {
            Title = "Add Button Mapping";
            Width = 400;
            Height = 220;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(13, 17, 23));
            
            var stack = new StackPanel { Margin = new Thickness(20) };
            
            stack.Children.Add(new TextBlock
            {
                Text = "Source Button:",
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 5, 0, 5)
            });

            var sourceCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
            foreach (var btn in ButtonRemappingSystem.StandardButtons)
                sourceCombo.Items.Add(btn);
            sourceCombo.SelectedIndex = 0;
            stack.Children.Add(sourceCombo);

            stack.Children.Add(new TextBlock
            {
                Text = "Target Button:",
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 5, 0, 5)
            });

            var targetCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 20) };
            foreach (var btn in ButtonRemappingSystem.StandardButtons)
                targetCombo.Items.Add(btn);
            targetCombo.SelectedIndex = 0;
            stack.Children.Add(targetCombo);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okBtn = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0) };
            okBtn.Click += (s, e) => 
            { 
                SourceButton = sourceCombo.SelectedItem?.ToString() ?? "";
                TargetButton = targetCombo.SelectedItem?.ToString() ?? "";
                DialogResult = true; 
            };
            btnPanel.Children.Add(okBtn);

            var cancelBtn = new Button { Content = "Cancel", Width = 80, Height = 30 };
            cancelBtn.Click += (s, e) => { DialogResult = false; };
            btnPanel.Children.Add(cancelBtn);

            stack.Children.Add(btnPanel);
            Content = stack;
        }
    }

    public class QuickTapDialog : Window
    {
        public string ComboName { get; private set; } = string.Empty;
        public string Button { get; private set; } = string.Empty;
        public int TapCount { get; private set; } = 2;
        public int MaxDelay { get; private set; } = 300;

        public QuickTapDialog()
        {
            Title = "Create Quick Tap Combo";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(13, 17, 23));
            
            var stack = new StackPanel { Margin = new Thickness(20) };
            
            stack.Children.Add(new TextBlock { Text = "Combo Name:", Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 5, 0, 5) });
            var nameBox = new TextBox { Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(5) };
            stack.Children.Add(nameBox);

            stack.Children.Add(new TextBlock { Text = "Button:", Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 5, 0, 5) });
            var buttonCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
            foreach (var btn in ButtonRemappingSystem.StandardButtons)
                buttonCombo.Items.Add(btn);
            buttonCombo.SelectedIndex = 0;
            stack.Children.Add(buttonCombo);

            stack.Children.Add(new TextBlock { Text = "Number of Taps:", Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 5, 0, 5) });
            var tapsBox = new TextBox { Text = "2", Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(5) };
            stack.Children.Add(tapsBox);

            stack.Children.Add(new TextBlock { Text = "Max Delay (ms):", Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 5, 0, 5) });
            var delayBox = new TextBox { Text = "300", Margin = new Thickness(0, 0, 0, 20), Padding = new Thickness(5) };
            stack.Children.Add(delayBox);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var okBtn = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0) };
            okBtn.Click += (s, e) => 
            { 
                ComboName = nameBox.Text;
                Button = buttonCombo.SelectedItem?.ToString() ?? "";
                int.TryParse(tapsBox.Text, out int taps);
                int.TryParse(delayBox.Text, out int delay);
                TapCount = Math.Max(2, taps);
                MaxDelay = Math.Max(100, delay);
                DialogResult = true; 
            };
            btnPanel.Children.Add(okBtn);

            var cancelBtn = new Button { Content = "Cancel", Width = 80, Height = 30 };
            cancelBtn.Click += (s, e) => { DialogResult = false; };
            btnPanel.Children.Add(cancelBtn);

            stack.Children.Add(btnPanel);
            Content = stack;
        }
    }

    #endregion
}
