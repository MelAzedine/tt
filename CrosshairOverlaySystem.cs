// CrosshairOverlaySystem.cs — Réticule personnalisé avec prédiction de trajectoire
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Trident.MITM
{
    /// <summary>
    /// Système de réticule overlay personnalisé avec prédiction de trajectoire
    /// et indicateurs de spread/recoil en temps réel
    /// </summary>
    public class CrosshairOverlaySystem : Window
    {
        private readonly Canvas _canvas;
        private readonly DispatcherTimer _updateTimer;
        
        // Éléments du réticule
        private readonly List<Line> _crosshairLines = new();
        private readonly Ellipse _centerDot;
        private readonly Ellipse _spreadIndicator;
        private readonly Path _trajectoryPath;
        private readonly TextBlock _infoText;
        
        // État
        private double _currentSpread = 0;
        private double _currentRecoil = 0;
        private bool _isAiming = false;
        private bool _isShooting = false;
        private List<Point> _recoilPattern = new();
        
        public CrosshairStyle CurrentStyle { get; set; } = CrosshairStyle.Cross;
        public bool ShowSpreadIndicator { get; set; } = true;
        public bool ShowTrajectoryPrediction { get; set; } = true;
        public bool ShowRecoilPattern { get; set; } = true;
        
        /// <summary>
        /// Styles de réticule disponibles
        /// </summary>
        public enum CrosshairStyle
        {
            Cross,      // Croix classique
            Dot,        // Point central seulement
            Circle,     // Cercle
            TShape,     // T inversé
            Diamond,    // Diamant
            Brackets,   // Crochets
            Custom      // Personnalisé
        }
        
        public CrosshairOverlaySystem()
        {
            // Configuration de la fenêtre
            Title = "Crosshair Overlay";
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            
            // Positionner au centre de l'écran
            Left = 0;
            Top = 0;
            
            // Canvas principal
            _canvas = new Canvas
            {
                Width = Width,
                Height = Height,
                Background = Brushes.Transparent
            };
            Content = _canvas;
            
            // Créer les éléments
            _centerDot = CreateCenterDot();
            _spreadIndicator = CreateSpreadIndicator();
            _trajectoryPath = CreateTrajectoryPath();
            _infoText = CreateInfoText();
            
            _canvas.Children.Add(_spreadIndicator);
            _canvas.Children.Add(_trajectoryPath);
            _canvas.Children.Add(_centerDot);
            _canvas.Children.Add(_infoText);
            
            // Timer pour les mises à jour
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _updateTimer.Tick += UpdateTick;
            _updateTimer.Start();
            
            // Créer le réticule initial
            CreateCrosshair(CrosshairStyle.Cross);
            
            // Permettre les clics à travers la fenêtre
            MakeClickThrough();
        }
        
        /// <summary>
        /// Rendre la fenêtre transparente aux clics
        /// </summary>
        private void MakeClickThrough()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }
        
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        
        /// <summary>
        /// Créer le point central
        /// </summary>
        private Ellipse CreateCenterDot()
        {
            var dot = new Ellipse
            {
                Width = 4,
                Height = 4,
                Fill = Brushes.Cyan,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            
            Canvas.SetLeft(dot, Width / 2 - 2);
            Canvas.SetTop(dot, Height / 2 - 2);
            
            return dot;
        }
        
        /// <summary>
        /// Créer l'indicateur de spread
        /// </summary>
        private Ellipse CreateSpreadIndicator()
        {
            var indicator = new Ellipse
            {
                Width = 40,
                Height = 40,
                Stroke = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0)),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(20, 255, 255, 0))
            };
            
            Canvas.SetLeft(indicator, Width / 2 - 20);
            Canvas.SetTop(indicator, Height / 2 - 20);
            
            return indicator;
        }
        
        /// <summary>
        /// Créer le chemin de trajectoire
        /// </summary>
        private Path CreateTrajectoryPath()
        {
            return new Path
            {
                Stroke = new SolidColorBrush(Color.FromArgb(150, 255, 68, 68)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
        }
        
        /// <summary>
        /// Créer le texte d'info
        /// </summary>
        private TextBlock CreateInfoText()
        {
            var text = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Text = ""
            };
            
            Canvas.SetLeft(text, Width / 2 + 30);
            Canvas.SetTop(text, Height / 2 - 30);
            
            return text;
        }
        
        /// <summary>
        /// Créer le réticule selon le style
        /// </summary>
        public void CreateCrosshair(CrosshairStyle style)
        {
            // Supprimer les anciennes lignes
            foreach (var line in _crosshairLines)
                _canvas.Children.Remove(line);
            _crosshairLines.Clear();
            
            var centerX = Width / 2;
            var centerY = Height / 2;
            var length = 20;
            var gap = 5;
            var thickness = 2.0;
            var color = Brushes.Cyan;
            
            switch (style)
            {
                case CrosshairStyle.Cross:
                    // Haut
                    AddLine(centerX, centerY - gap, centerX, centerY - gap - length, thickness, color);
                    // Bas
                    AddLine(centerX, centerY + gap, centerX, centerY + gap + length, thickness, color);
                    // Gauche
                    AddLine(centerX - gap, centerY, centerX - gap - length, centerY, thickness, color);
                    // Droite
                    AddLine(centerX + gap, centerY, centerX + gap + length, centerY, thickness, color);
                    break;
                
                case CrosshairStyle.Dot:
                    _centerDot.Width = 6;
                    _centerDot.Height = 6;
                    _centerDot.Fill = color;
                    break;
                
                case CrosshairStyle.Circle:
                    var circle = new Ellipse
                    {
                        Width = 30,
                        Height = 30,
                        Stroke = color,
                        StrokeThickness = thickness
                    };
                    Canvas.SetLeft(circle, centerX - 15);
                    Canvas.SetTop(circle, centerY - 15);
                    _canvas.Children.Add(circle);
                    _crosshairLines.Add(new Line()); // Dummy pour tracking
                    break;
                
                case CrosshairStyle.TShape:
                    // Haut
                    AddLine(centerX, centerY - gap, centerX, centerY - gap - length, thickness, color);
                    // Gauche
                    AddLine(centerX - gap, centerY, centerX - gap - length, centerY, thickness, color);
                    // Droite
                    AddLine(centerX + gap, centerY, centerX + gap + length, centerY, thickness, color);
                    break;
                
                case CrosshairStyle.Diamond:
                    // Haut
                    AddLine(centerX, centerY - length, centerX - length/2, centerY, thickness, color);
                    AddLine(centerX, centerY - length, centerX + length/2, centerY, thickness, color);
                    // Bas
                    AddLine(centerX, centerY + length, centerX - length/2, centerY, thickness, color);
                    AddLine(centerX, centerY + length, centerX + length/2, centerY, thickness, color);
                    break;
                
                case CrosshairStyle.Brackets:
                    var bracketLength = 15;
                    var bracketGap = 10;
                    // Haut-Gauche
                    AddLine(centerX - bracketGap, centerY - bracketGap, centerX - bracketGap - bracketLength, centerY - bracketGap, thickness, color);
                    AddLine(centerX - bracketGap, centerY - bracketGap, centerX - bracketGap, centerY - bracketGap - bracketLength, thickness, color);
                    // Haut-Droite
                    AddLine(centerX + bracketGap, centerY - bracketGap, centerX + bracketGap + bracketLength, centerY - bracketGap, thickness, color);
                    AddLine(centerX + bracketGap, centerY - bracketGap, centerX + bracketGap, centerY - bracketGap - bracketLength, thickness, color);
                    // Bas-Gauche
                    AddLine(centerX - bracketGap, centerY + bracketGap, centerX - bracketGap - bracketLength, centerY + bracketGap, thickness, color);
                    AddLine(centerX - bracketGap, centerY + bracketGap, centerX - bracketGap, centerY + bracketGap + bracketLength, thickness, color);
                    // Bas-Droite
                    AddLine(centerX + bracketGap, centerY + bracketGap, centerX + bracketGap + bracketLength, centerY + bracketGap, thickness, color);
                    AddLine(centerX + bracketGap, centerY + bracketGap, centerX + bracketGap, centerY + bracketGap + bracketLength, thickness, color);
                    break;
            }
            
            CurrentStyle = style;
        }
        
        /// <summary>
        /// Ajouter une ligne au réticule
        /// </summary>
        private void AddLine(double x1, double y1, double x2, double y2, double thickness, Brush color)
        {
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = color,
                StrokeThickness = thickness,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round
            };
            
            _canvas.Children.Add(line);
            _crosshairLines.Add(line);
        }
        
        /// <summary>
        /// Mettre à jour le spread (dispersion)
        /// </summary>
        public void UpdateSpread(double spread, bool isMoving, bool isShooting)
        {
            _currentSpread = spread;
            _isShooting = isShooting;
            
            // Augmenter le spread si en mouvement ou en train de tirer
            if (isMoving) _currentSpread *= 1.5;
            if (isShooting) _currentSpread *= 2.0;
            
            // Limiter
            _currentSpread = Math.Clamp(_currentSpread, 10, 200);
        }
        
        /// <summary>
        /// Mettre à jour le pattern de recul
        /// </summary>
        public void UpdateRecoilPattern(List<Point> pattern)
        {
            _recoilPattern = pattern;
        }
        
        /// <summary>
        /// Définir l'état de visée
        /// </summary>
        public void SetAiming(bool isAiming)
        {
            _isAiming = isAiming;
        }
        
        /// <summary>
        /// Tick de mise à jour
        /// </summary>
        private void UpdateTick(object? sender, EventArgs e)
        {
            // Mettre à jour l'indicateur de spread
            if (ShowSpreadIndicator)
            {
                _spreadIndicator.Visibility = Visibility.Visible;
                _spreadIndicator.Width = _currentSpread;
                _spreadIndicator.Height = _currentSpread;
                Canvas.SetLeft(_spreadIndicator, Width / 2 - _currentSpread / 2);
                Canvas.SetTop(_spreadIndicator, Height / 2 - _currentSpread / 2);
                
                // Changer la couleur selon le spread
                var alpha = (byte)Math.Clamp(50 + _currentSpread, 50, 150);
                var color = _isShooting ? Colors.Red : Colors.Yellow;
                _spreadIndicator.Stroke = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            }
            else
            {
                _spreadIndicator.Visibility = Visibility.Collapsed;
            }
            
            // Mettre à jour la trajectoire de recul
            if (ShowRecoilPattern && _recoilPattern.Count > 1)
            {
                _trajectoryPath.Visibility = Visibility.Visible;
                var geometry = new PathGeometry();
                var figure = new PathFigure
                {
                    StartPoint = new Point(Width / 2, Height / 2)
                };
                
                foreach (var point in _recoilPattern)
                {
                    figure.Segments.Add(new LineSegment(
                        new Point(Width / 2 + point.X, Height / 2 - point.Y), 
                        true
                    ));
                }
                
                geometry.Figures.Add(figure);
                _trajectoryPath.Data = geometry;
            }
            else
            {
                _trajectoryPath.Visibility = Visibility.Collapsed;
            }
            
            // Mettre à jour le texte d'info
            if (_isAiming)
            {
                _infoText.Text = $"SPREAD: {_currentSpread:F0}px\n" +
                                $"{(_isShooting ? "FIRING" : "READY")}";
                _infoText.Visibility = Visibility.Visible;
            }
            else
            {
                _infoText.Visibility = Visibility.Collapsed;
            }
            
            // Réduire progressivement le spread
            _currentSpread = Math.Max(10, _currentSpread * 0.98);
        }
        
        /// <summary>
        /// Changer la couleur du réticule
        /// </summary>
        public void SetColor(Color color)
        {
            var brush = new SolidColorBrush(color);
            
            foreach (var line in _crosshairLines)
                line.Stroke = brush;
            
            _centerDot.Fill = brush;
        }
        
        /// <summary>
        /// Obtenir les styles disponibles
        /// </summary>
        public static List<string> GetAvailableStyles()
        {
            return new List<string>
            {
                "Cross - Croix classique",
                "Dot - Point central",
                "Circle - Cercle",
                "TShape - T inversé",
                "Diamond - Diamant",
                "Brackets - Crochets"
            };
        }
    }
}
