// BatteryMonitor.cs — Controller battery level monitoring
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.XInput;

namespace Trident.MITM
{
    /// <summary>
    /// Monitors battery level for wireless controllers
    /// </summary>
    public class BatteryMonitor
    {
        private CancellationTokenSource? _monitorCts;
        private const int CHECK_INTERVAL_MS = 30000; // Check every 30 seconds
        
        public event Action<BatteryInfo>? BatteryLevelChanged;
        
        public BatteryInfo? CurrentBatteryInfo { get; private set; }
        public bool IsMonitoring { get; private set; }
        
        /// <summary>
        /// Battery information
        /// </summary>
        public class BatteryInfo
        {
            public BatteryLevel Level { get; set; }
            public BatteryType Type { get; set; }
            public int PercentageEstimate { get; set; }
            public bool IsCharging { get; set; }
            public DateTime LastUpdate { get; set; }
        }
        
        /// <summary>
        /// Battery level enumeration
        /// </summary>
        public enum BatteryLevel
        {
            Empty = 0,
            Low = 1,
            Medium = 2,
            Full = 3
        }
        
        /// <summary>
        /// Battery type enumeration
        /// </summary>
        public enum BatteryType
        {
            Disconnected = 0,
            Wired = 1,
            Alkaline = 2,
            NiMH = 3,
            Unknown = 255
        }
        
        /// <summary>
        /// Start monitoring battery level
        /// </summary>
        public void StartMonitoring(int controllerIndex = 0)
        {
            if (IsMonitoring) return;
            
            IsMonitoring = true;
            _monitorCts = new CancellationTokenSource();
            Task.Run(() => MonitorLoop(controllerIndex, _monitorCts.Token));
        }
        
        /// <summary>
        /// Stop monitoring battery level
        /// </summary>
        public void StopMonitoring()
        {
            IsMonitoring = false;
            _monitorCts?.Cancel();
            _monitorCts = null;
        }
        
        /// <summary>
        /// Get current battery level immediately
        /// </summary>
        public BatteryInfo? GetBatteryLevel(int controllerIndex = 0)
        {
            try
            {
                var controller = new Controller((UserIndex)controllerIndex);
                
                if (!controller.IsConnected)
                    return null;
                
                var batteryInfo = controller.GetBatteryInformation(BatteryDeviceType.Gamepad);
                
                var info = new BatteryInfo
                {
                    Level = ConvertBatteryLevel(batteryInfo.BatteryLevel),
                    Type = ConvertBatteryType(batteryInfo.BatteryType),
                    PercentageEstimate = CalculatePercentage(batteryInfo.BatteryLevel),
                    IsCharging = false, // XInput doesn't provide charging status
                    LastUpdate = DateTime.Now
                };
                
                return info;
            }
            catch
            {
                return null;
            }
        }
        
        private async Task MonitorLoop(int controllerIndex, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var batteryInfo = GetBatteryLevel(controllerIndex);
                    
                    if (batteryInfo != null)
                    {
                        // Check if battery level changed significantly
                        if (CurrentBatteryInfo == null || 
                            CurrentBatteryInfo.Level != batteryInfo.Level ||
                            CurrentBatteryInfo.Type != batteryInfo.Type)
                        {
                            CurrentBatteryInfo = batteryInfo;
                            BatteryLevelChanged?.Invoke(batteryInfo);
                        }
                    }
                    
                    await Task.Delay(CHECK_INTERVAL_MS, ct);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch
                {
                    // Silently continue on errors
                }
            }
        }
        
        private static BatteryLevel ConvertBatteryLevel(SharpDX.XInput.BatteryLevel level)
        {
            return level switch
            {
                SharpDX.XInput.BatteryLevel.Empty => BatteryLevel.Empty,
                SharpDX.XInput.BatteryLevel.Low => BatteryLevel.Low,
                SharpDX.XInput.BatteryLevel.Medium => BatteryLevel.Medium,
                SharpDX.XInput.BatteryLevel.Full => BatteryLevel.Full,
                _ => BatteryLevel.Medium
            };
        }
        
        private static BatteryType ConvertBatteryType(SharpDX.XInput.BatteryType type)
        {
            return type switch
            {
                SharpDX.XInput.BatteryType.Disconnected => BatteryType.Disconnected,
                SharpDX.XInput.BatteryType.Wired => BatteryType.Wired,
                SharpDX.XInput.BatteryType.Alkaline => BatteryType.Alkaline,
                SharpDX.XInput.BatteryType.Nimh => BatteryType.NiMH,
                _ => BatteryType.Unknown
            };
        }
        
        private static int CalculatePercentage(SharpDX.XInput.BatteryLevel level)
        {
            return level switch
            {
                SharpDX.XInput.BatteryLevel.Empty => 0,
                SharpDX.XInput.BatteryLevel.Low => 25,
                SharpDX.XInput.BatteryLevel.Medium => 60,
                SharpDX.XInput.BatteryLevel.Full => 100,
                _ => 50
            };
        }
        
        /// <summary>
        /// Get battery level icon emoji based on level
        /// </summary>
        public static string GetBatteryIcon(BatteryLevel level)
        {
            return level switch
            {
                BatteryLevel.Empty => "🔴",
                BatteryLevel.Low => "🟡",
                BatteryLevel.Medium => "🟢",
                BatteryLevel.Full => "🟢",
                _ => "⚪"
            };
        }
        
        /// <summary>
        /// Get battery level text description
        /// </summary>
        public static string GetBatteryDescription(BatteryInfo info)
        {
            if (info.Type == BatteryType.Wired)
                return "Wired Connection";
            
            if (info.Type == BatteryType.Disconnected)
                return "Disconnected";
            
            var icon = GetBatteryIcon(info.Level);
            return $"{icon} Battery: {info.PercentageEstimate}%";
        }
    }
}
