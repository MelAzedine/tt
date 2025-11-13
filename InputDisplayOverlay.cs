// InputDisplayOverlay.cs — Real-time button press visualization for streaming
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Trident.MITM
{
    /// <summary>
    /// Displays real-time controller inputs as an overlay for streaming/recording
    /// </summary>
    public class InputDisplayOverlay : Window
    {
        private readonly Dictionary<string, Border> _buttonElements = new();
        private readonly Grid _mainGrid;
        private const double BUTTON_SIZE = 50;
        private const double STICK_SIZE = 100;
        
        public InputDisplayOverlay()
        {
            Title = "Input Display";
            Width = 600;
            Height = 350;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Color.FromArgb(180, 20, 20, 30));
            Topmost = true;
            ShowInTaskbar = false;
            
            _mainGrid = new Grid();
            Content = _mainGrid;
            
            InitializeLayout();
        }
        
        private void InitializeLayout()
        {
            // Face buttons (A, B, X, Y)
            CreateButton("A", 450, 200, Colors.LimeGreen);
            CreateButton("B", 500, 150, Colors.Red);
            CreateButton("X", 400, 150, Colors.Blue);
            CreateButton("Y", 450, 100, Colors.Yellow);
            
            // Shoulder buttons
            CreateButton("LB", 100, 20, Colors.Gray);
            CreateButton("RB", 450, 20, Colors.Gray);
            CreateButton("LT", 50, 20, Colors.LightGray);
            CreateButton("RT", 500, 20, Colors.LightGray);
            
            // D-Pad
            CreateButton("Up", 100, 100, Colors.DarkGray);
            CreateButton("Down", 100, 200, Colors.DarkGray);
            CreateButton("Left", 50, 150, Colors.DarkGray);
            CreateButton("Right", 150, 150, Colors.DarkGray);
            
            // Special buttons
            CreateButton("Start", 320, 150, Colors.White);
            CreateButton("Back", 250, 150, Colors.White);
            CreateButton("LS", 150, 280, Colors.DarkSlateGray);
            CreateButton("RS", 400, 280, Colors.DarkSlateGray);
            
            // Stick indicators
            CreateStickIndicator("LeftStick", 150, 280);
            CreateStickIndicator("RightStick", 400, 280);
        }
        
        private void CreateButton(string name, double left, double top, Color color)
        {
            var border = new Border
            {
                Width = BUTTON_SIZE,
                Height = BUTTON_SIZE,
                CornerRadius = new CornerRadius(25),
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(3),
                Background = new SolidColorBrush(Color.FromArgb(50, color.R, color.G, color.B)),
                Margin = new Thickness(left, top, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            
            var text = new TextBlock
            {
                Text = name,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            border.Child = text;
            _mainGrid.Children.Add(border);
            _buttonElements[name] = border;
        }
        
        private void CreateStickIndicator(string name, double left, double top)
        {
            var container = new Canvas
            {
                Width = STICK_SIZE,
                Height = STICK_SIZE,
                Margin = new Thickness(left - 25, top - 25, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            
            // Outer circle (range)
            var outerCircle = new Ellipse
            {
                Width = STICK_SIZE,
                Height = STICK_SIZE,
                Stroke = Brushes.Gray,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(30, 128, 128, 128))
            };
            container.Children.Add(outerCircle);
            
            // Inner indicator (position)
            var indicator = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = Brushes.Cyan,
                Name = name + "_Indicator"
            };
            Canvas.SetLeft(indicator, STICK_SIZE / 2 - 10);
            Canvas.SetTop(indicator, STICK_SIZE / 2 - 10);
            container.Children.Add(indicator);
            
            _mainGrid.Children.Add(container);
            RegisterName(name + "_Indicator", indicator);
        }
        
        /// <summary>
        /// Update button press state
        /// </summary>
        public void UpdateButton(string buttonName, bool isPressed)
        {
            if (_buttonElements.TryGetValue(buttonName, out var border))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (isPressed)
                    {
                        // Animate press
                        var colorAnimation = new ColorAnimation
                        {
                            To = ((SolidColorBrush)border.BorderBrush).Color,
                            Duration = TimeSpan.FromMilliseconds(50)
                        };
                        
                        var bgBrush = border.Background as SolidColorBrush;
                        if (bgBrush != null)
                        {
                            var baseColor = ((SolidColorBrush)border.BorderBrush).Color;
                            border.Background = new SolidColorBrush(Color.FromArgb(200, baseColor.R, baseColor.G, baseColor.B));
                        }
                    }
                    else
                    {
                        // Animate release
                        var baseColor = ((SolidColorBrush)border.BorderBrush).Color;
                        border.Background = new SolidColorBrush(Color.FromArgb(50, baseColor.R, baseColor.G, baseColor.B));
                    }
                });
            }
        }
        
        /// <summary>
        /// Update stick position
        /// </summary>
        public void UpdateStick(string stickName, double x, double y)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var indicator = FindName(stickName + "_Indicator") as Ellipse;
                    if (indicator != null)
                    {
                        // Convert normalized coordinates (-1 to 1) to pixel position
                        double centerX = STICK_SIZE / 2 - 10;
                        double centerY = STICK_SIZE / 2 - 10;
                        double maxRadius = STICK_SIZE / 2 - 15;
                        
                        double offsetX = x * maxRadius;
                        double offsetY = -y * maxRadius; // Invert Y for screen coordinates
                        
                        Canvas.SetLeft(indicator, centerX + offsetX);
                        Canvas.SetTop(indicator, centerY + offsetY);
                    }
                }
                catch
                {
                    // Silently ignore errors
                }
            });
        }
        
        /// <summary>
        /// Update trigger value (0-1)
        /// </summary>
        public void UpdateTrigger(string triggerName, double value)
        {
            if (_buttonElements.TryGetValue(triggerName, out var border))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var baseColor = ((SolidColorBrush)border.BorderBrush).Color;
                    byte alpha = (byte)(50 + value * 150); // 50-200 alpha based on trigger value
                    border.Background = new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
                });
            }
        }
        
        /// <summary>
        /// Reset all inputs to default state
        /// </summary>
        public void ResetAllInputs()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var kvp in _buttonElements)
                {
                    var baseColor = ((SolidColorBrush)kvp.Value.BorderBrush).Color;
                    kvp.Value.Background = new SolidColorBrush(Color.FromArgb(50, baseColor.R, baseColor.G, baseColor.B));
                }
                
                UpdateStick("LeftStick", 0, 0);
                UpdateStick("RightStick", 0, 0);
            });
        }
    }
}
