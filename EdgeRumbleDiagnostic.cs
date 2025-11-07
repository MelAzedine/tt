
using System;
using System.Linq;
using System.Threading;
using HidSharp;

namespace Trident.MITM
{
    /// <summary>Small standalone helper that sends a DS4/DS5/Edge rumble without touching the app's own I/O.
    /// It opens its own HID OUT pipe and closes it immediately after the test.</summary>
    public static class EdgeRumbleDiagnostic
    {
        static bool IsBluetooth(HidDevice d) => (d.DevicePath ?? "").IndexOf("BTHENUM", StringComparison.OrdinalIgnoreCase) >= 0;

        public static bool Pulse(byte small, byte large, int durationMs = 300)
        {
            try
            {
                var list = DeviceList.Local.GetHidDevices()
                    .Where(d => d.VendorID == 0x054C); // Sony

                foreach (var dev in list)
                {
                    // Prefer USB interfaces with an OUT report channel
                    if (IsBluetooth(dev)) continue;

                    if (!dev.TryOpen(out var s)) continue;

                    try
                    {
                        s.ReadTimeout = Timeout.Infinite;
                        s.WriteTimeout = 120;

                        // DS4?
                        if (dev.ProductID == 0x05C4 || dev.ProductID == 0x09CC || dev.ProductID == 0x0BA0)
                        {
                            var buf = new byte[Math.Max(32, dev.GetMaxOutputReportLength())];
                            buf[0] = 0x05; // report id
                            buf[1] = 0xFF; // enable
                            buf[4] = small;
                            buf[5] = large;
                            s.Write(buf);
                            Thread.Sleep(durationMs);
                            buf[4] = buf[5] = 0; s.Write(buf);
                            return true;
                        }

                        // DualSense / Edge (USB)
                        if (dev.ProductID == 0x0CE6 || dev.ProductID == 0x0E5F || dev.ProductID == 0x0DF2)
                        {
                            int outLen = Math.Max(64, dev.GetMaxOutputReportLength());
                            var r = new byte[outLen];
                            // Common "0x02" simple rumble payload
                            r[0] = 0x02; r[1] = 0x01;
                            r[3] = large; r[4] = small; // left/large, right/small
                            // enable motors (varies a little between firmwares; try both 0x02 and 0x01)
                            r[10] = 0x02;
                            try { s.Write(r); } catch { }

                            // keep alive a bit
                            int stopAt = Environment.TickCount + Math.Max(60, durationMs);
                            while (Environment.TickCount < stopAt)
                            {
                                try { s.Write(r); } catch { }
                                Thread.Sleep(50);
                            }

                            // stop
                            r[3] = r[4] = 0;
                            try { s.Write(r); } catch { }
                            return true;
                        }
                    }
                    finally { try { s.Close(); s.Dispose(); } catch { } }
                }
            }
            catch { }
            return false;
        }
    }
}
