using System.Windows;

namespace Trident.MITM
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();
        }

        public void SetStatus(string weapon, bool rapidFire, bool running, string vigem, string pad)
        {
            TxtLine1.Text = $"{(running ? "ON" : "OFF")} • {weapon}";
            TxtLine2.Text = $"RF:{(rapidFire ? 1 : 0)} • ViGEm:{vigem} • Pad:{pad}";
        }
    }
}
