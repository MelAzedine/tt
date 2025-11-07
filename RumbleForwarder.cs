
using System;
using System.Runtime.CompilerServices;
using HidSharp;

namespace Trident.MITM
{
    public sealed class RumbleForwarder : IDisposable
    {
        private readonly HidDevice _dev;
        private readonly HidStream _out;
        private RumbleForwarder(HidDevice dev, HidStream s) { _dev = dev; _out = s; _out.ReadTimeout = 100; _out.WriteTimeout = 100; }
        public void Dispose() { try { _out?.Close(); _out?.Dispose(); } catch { } }

        public static RumbleForwarder OpenFirstSony(bool preferUsb = true)
        {
            HidDevice best = null; int bestScore = int.MaxValue; int bestOut = 0;
            foreach (var d in DeviceList.Local.GetHidDevices())
            {
                try
                {
                    if (d.VendorID != 0x054C) continue;
                    ushort pid = (ushort)d.ProductID;
                    bool ok = pid == 0x05C4 || pid == 0x09CC || pid == 0x0BA0 || pid == 0x0CE6 || pid == 0x0E5F || pid == 0x0DF2;
                    if (!ok) continue;
                    bool bt = (d.DevicePath ?? "").IndexOf("BTHENUM", StringComparison.OrdinalIgnoreCase) >= 0;
                    int score = preferUsb ? (bt ? 1 : 0) : (bt ? 0 : 1);
                    int outLen = d.GetMaxOutputReportLength();
                    if (best is null || score < bestScore || (score == bestScore && outLen > bestOut)) { best = d; bestScore = score; bestOut = outLen; }
                }
                catch { }
            }
            if (best == null) throw new InvalidOperationException("Aucune manette Sony.");
            if (!best.TryOpen(out HidStream s)) throw new InvalidOperationException("Ouverture HID échouée.");
            return new RumbleForwarder(best, s);
        }

        public void Send(byte small, byte large)
        {
            if (_dev.VendorID != 0x054C) return;
            ushort pid = (ushort)_dev.ProductID;
            bool bt = (_dev.DevicePath ?? "").IndexOf("BTHENUM", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isDS4 = pid == 0x05C4 || pid == 0x09CC || pid == 0x0BA0;
            bool isDS5 = pid == 0x0CE6 || pid == 0x0E5F || pid == 0x0DF2;

            if (isDS4 && !bt)
            {
                Span<byte> r = stackalloc byte[32]; r.Clear(); r[0] = 0x05; r[1] = 0x07; r[4] = small; r[5] = large; TryWrite(r); return;
            }
            if (isDS4 && bt)
            {
                Span<byte> r = stackalloc byte[78]; r.Clear(); r[0] = 0x11; r[1] = 0xC0; r[3] = 0x07; r[6] = small; r[7] = large;
                uint crc = 0xFFFFFFFFu; crc = Crc32Update(crc, 0xA2); for (int i = 0; i < 74; i++) crc = Crc32Update(crc, r[i]); crc = ~crc;
                r[74] = (byte)crc; r[75] = (byte)(crc >> 8); r[76] = (byte)(crc >> 16); r[77] = (byte)(crc >> 24); TryWrite(r); return;
            }
            if (isDS5 && !bt)
            {
                Span<byte> r = stackalloc byte[48]; r.Clear(); r[0] = 0x02; r[1] = 0x07; r[4] = small; r[5] = large; TryWrite(r); return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryWrite(ReadOnlySpan<byte> r) { try { _out.Write(r); } catch { } }

        static readonly uint[] T = Build();
        static uint[] Build() { var t = new uint[256]; for (uint i = 0; i < 256; i++) { uint c = i; for (int j = 0; j < 8; j++) c = ((c & 1) != 0) ? (0xEDB88320U ^ (c >> 1)) : (c >> 1); t[i] = c; } return t; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] static uint Crc32Update(uint crc, int b) => (crc >> 8) ^ T[(crc ^ (byte)b) & 0xFF];
    }
}
