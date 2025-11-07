using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Trident.MITM
{
	public partial class RecoilDrawWindow : Window
	{
		private readonly Polyline _stroke = new Polyline { Stroke = new SolidColorBrush(Color.FromRgb(255, 91, 91)), StrokeThickness = 2.0, SnapsToDevicePixels = true };
		private readonly List<Point> _points = new List<Point>();
		private bool _drawing = false;

		public int ResultDurationMs { get; private set; } = 1000;
		public int ResultAmplitudeMax { get; private set; } = 3500;
		public MainWindow.AntiRecoilTimeline? ResultTimeline { get; private set; }

		public RecoilDrawWindow()
		{
			InitializeComponent();
		}

		private void DrawCanvas_Loaded(object sender, RoutedEventArgs e)
		{
			DrawCanvas.Children.Clear();
			DrawCanvas.Children.Add(_stroke);
			DrawCenterLine();
			DrawCanvas.Cursor = Cursors.Cross;
		}

		private void DrawCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			DrawCenterLine();
		}

		private void DrawCenterLine()
		{
			// center horizontal reference
			var centerY = DrawCanvas.ActualHeight / 2.0;
			var line = new Line
			{
				X1 = 0,
				Y1 = centerY,
				X2 = DrawCanvas.ActualWidth,
				Y2 = centerY,
				Stroke = new SolidColorBrush(Color.FromRgb(44, 44, 51)),
				StrokeThickness = 1
			};
			// keep stroke as last child
			DrawCanvas.Children.Clear();
			DrawCanvas.Children.Add(line);
			DrawCanvas.Children.Add(_stroke);
			RedrawStroke();
		}

		private void DrawCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DrawCanvas.Focus();
			DrawCanvas.CaptureMouse();
			_points.Clear();
			_stroke.Points.Clear();
			_drawing = true;
			AddPoint(e.GetPosition(DrawCanvas));
			e.Handled = true;
		}

		private void DrawCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (!_drawing) return;
			AddPoint(e.GetPosition(DrawCanvas));
			e.Handled = true;
		}

		private void DrawCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (!_drawing) return;
			_drawing = false;
			DrawCanvas.ReleaseMouseCapture();
			e.Handled = true;
		}

		private void AddPoint(Point p)
		{
			// clamp inside
			var x = Math.Max(0, Math.Min(DrawCanvas.ActualWidth, p.X));
			var y = Math.Max(0, Math.Min(DrawCanvas.ActualHeight, p.Y));
			var q = new Point(x, y);
			if (_points.Count == 0 || Distance(_points[^1], q) >= 0.5)
			{
				_points.Add(q);
				_stroke.Points.Add(q);
			}
		}

		private static double Distance(in Point a, in Point b)
		{
			double dx = a.X - b.X, dy = a.Y - b.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}

		private void RedrawStroke()
		{
			_stroke.Points.Clear();
			foreach (var p in _points) _stroke.Points.Add(p);
		}

		private void Clear_Click(object sender, RoutedEventArgs e)
		{
			_points.Clear();
			_stroke.Points.Clear();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			ResultTimeline = null;
			DialogResult = false;
			Close();
		}

		private void Apply_Click(object sender, RoutedEventArgs e)
		{
			ResultDurationMs = (int)Math.Round(DurationSlider.Value);
			ResultAmplitudeMax = (int)Math.Round(AmpSlider.Value);

			if (_points.Count < 3)
			{
				MessageBox.Show(this, "Trace au moins une ligne de recul.", "Dessine ton recul", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			// Normalize et rééchantillonnage sur la durée choisie
			int buckets = Math.Max(6, ResultDurationMs / 50); // ~50 ms
			var times = Enumerable.Range(0, buckets + 1).Select(i => (double)i / buckets).ToArray(); // 0..1
			double width = Math.Max(1.0, DrawCanvas.ActualWidth);
			double height = Math.Max(1.0, DrawCanvas.ActualHeight);
			double centerY = height / 2.0;

			// Interpolation linéaire du tracé le long de X
			var pts = _points.OrderBy(p => p.X).ToList();
			if (pts.Count < 2)
			{
				MessageBox.Show(this, "Trace une ligne plus longue (min 2 points).", "Dessine ton recul", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			int idx = 0;
			double[] yVals = new double[times.Length];
			double ema = 0.0;
			bool emaInit = false;
			for (int i = 0; i < times.Length; i++)
			{
				double targetX = times[i] * width;
				// Avancer l'index pour encadrer targetX
				while (idx + 1 < pts.Count && targetX > pts[idx + 1].X) idx++;
				double yPix;
				if (targetX <= pts[0].X) yPix = pts[0].Y;
				else if (targetX >= pts[^1].X) yPix = pts[^1].Y;
				else
				{
					Point a = pts[idx], b = pts[idx + 1];
					double t = (targetX - a.X) / Math.Max(1e-6, (b.X - a.X));
					yPix = a.Y + t * (b.Y - a.Y);
				}
				// yNorm: +1 = haut (au-dessus du centre), -1 = bas
				double yNorm = (centerY - yPix) / (height / 2.0);
				// Anti-recul = INVERSE du dessin (on pousse à l'opposé du kick)
				double antiTarget = -(yNorm * ResultAmplitudeMax);
				// Lissage léger (EMA) pour éviter des pics
				if (!emaInit) { ema = antiTarget; emaInit = true; }
				else { ema = 0.7 * ema + 0.3 * antiTarget; }
				yVals[i] = ema;
			}

			// Convert cumulative samples to segments of deltas
			var tl = new MainWindow.AntiRecoilTimeline
			{
				RequireADS = true,
				Gain = 1.0,
				Segments = new System.Collections.ObjectModel.ObservableCollection<MainWindow.ArSegment>()
			};

			int bucketMs = Math.Max(20, ResultDurationMs / Math.Max(1, buckets));
			double prev = 0.0;
			for (int i = 0; i < yVals.Length; i++)
			{
				double target = yVals[i];
				double delta = target - prev;
				prev = target;
				// VY: positive raises up (will reduce outRY); for anti-recoil (push down), drawn below center (yNorm<0) -> target negative -> delta negative
				tl.Segments.Add(new MainWindow.ArSegment
				{
					DurationMs = bucketMs,
					WaitMs = 0,
					Repeat = 0,
					VX = 0,
					VY = -(int)Math.Round(delta),
					Ease = 1.0
				});
			}

			ResultTimeline = tl;
			DialogResult = true;
			Close();
		}
	}
}


