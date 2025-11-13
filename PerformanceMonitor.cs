// PerformanceMonitor.cs — Track input lag, polling rate, and performance metrics
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Trident.MITM
{
    /// <summary>
    /// Monitors controller input performance and system metrics
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly Queue<long> _inputTimestamps = new();
        private readonly Queue<double> _processingTimes = new();
        private readonly Stopwatch _stopwatch = new();
        private const int SAMPLE_SIZE = 100;
        
        private long _totalInputs = 0;
        private long _lastInputTime = 0;
        private double _lastProcessingTime = 0;
        
        public event Action<PerformanceMetrics>? MetricsUpdated;
        
        /// <summary>
        /// Performance metrics data
        /// </summary>
        public class PerformanceMetrics
        {
            public double AveragePollingRate { get; set; }      // Hz
            public double CurrentPollingRate { get; set; }      // Hz
            public double AverageInputLatency { get; set; }     // ms
            public double MinInputLatency { get; set; }         // ms
            public double MaxInputLatency { get; set; }         // ms
            public double AverageProcessingTime { get; set; }   // ms
            public double MaxProcessingTime { get; set; }       // ms
            public long TotalInputsProcessed { get; set; }
            public double DroppedInputsPercent { get; set; }
            public int QueueSize { get; set; }
            public DateTime LastUpdate { get; set; }
        }
        
        public PerformanceMonitor()
        {
            _stopwatch.Start();
        }
        
        /// <summary>
        /// Record start of input processing
        /// </summary>
        public void BeginInputProcessing()
        {
            _lastInputTime = _stopwatch.ElapsedTicks;
        }
        
        /// <summary>
        /// Record end of input processing
        /// </summary>
        public void EndInputProcessing()
        {
            long endTime = _stopwatch.ElapsedTicks;
            double processingTimeMs = (endTime - _lastInputTime) * 1000.0 / Stopwatch.Frequency;
            
            _lastProcessingTime = processingTimeMs;
            _totalInputs++;
            
            // Add to queues
            _inputTimestamps.Enqueue(endTime);
            _processingTimes.Enqueue(processingTimeMs);
            
            // Maintain queue size
            if (_inputTimestamps.Count > SAMPLE_SIZE)
            {
                _inputTimestamps.Dequeue();
                _processingTimes.Dequeue();
            }
            
            // Update metrics every 10 inputs
            if (_totalInputs % 10 == 0)
            {
                UpdateMetrics();
            }
        }
        
        /// <summary>
        /// Record an input event (for polling rate calculation)
        /// </summary>
        public void RecordInput()
        {
            long timestamp = _stopwatch.ElapsedTicks;
            _inputTimestamps.Enqueue(timestamp);
            _totalInputs++;
            
            if (_inputTimestamps.Count > SAMPLE_SIZE)
            {
                _inputTimestamps.Dequeue();
            }
        }
        
        private void UpdateMetrics()
        {
            if (_inputTimestamps.Count < 2)
                return;
            
            var timestamps = _inputTimestamps.ToArray();
            var processingTimes = _processingTimes.ToArray();
            
            // Calculate polling rate
            double avgPollingRate = 0;
            double currentPollingRate = 0;
            
            if (timestamps.Length >= 2)
            {
                // Average polling rate over all samples
                long totalTime = timestamps[timestamps.Length - 1] - timestamps[0];
                double totalSeconds = totalTime / (double)Stopwatch.Frequency;
                avgPollingRate = timestamps.Length / totalSeconds;
                
                // Current polling rate (last 2 samples)
                long lastInterval = timestamps[timestamps.Length - 1] - timestamps[timestamps.Length - 2];
                double lastSeconds = lastInterval / (double)Stopwatch.Frequency;
                currentPollingRate = 1.0 / lastSeconds;
            }
            
            // Calculate latency metrics
            var latencies = new List<double>();
            for (int i = 1; i < timestamps.Length; i++)
            {
                double intervalMs = (timestamps[i] - timestamps[i - 1]) * 1000.0 / Stopwatch.Frequency;
                latencies.Add(intervalMs);
            }
            
            var metrics = new PerformanceMetrics
            {
                AveragePollingRate = avgPollingRate,
                CurrentPollingRate = currentPollingRate,
                AverageInputLatency = latencies.Any() ? latencies.Average() : 0,
                MinInputLatency = latencies.Any() ? latencies.Min() : 0,
                MaxInputLatency = latencies.Any() ? latencies.Max() : 0,
                AverageProcessingTime = processingTimes.Any() ? processingTimes.Average() : 0,
                MaxProcessingTime = processingTimes.Any() ? processingTimes.Max() : 0,
                TotalInputsProcessed = _totalInputs,
                DroppedInputsPercent = 0, // TODO: Implement dropped input detection
                QueueSize = _inputTimestamps.Count,
                LastUpdate = DateTime.Now
            };
            
            MetricsUpdated?.Invoke(metrics);
        }
        
        /// <summary>
        /// Get current metrics immediately
        /// </summary>
        public PerformanceMetrics GetCurrentMetrics()
        {
            UpdateMetrics();
            
            var timestamps = _inputTimestamps.ToArray();
            var processingTimes = _processingTimes.ToArray();
            
            double avgPollingRate = 0;
            double currentPollingRate = 0;
            
            if (timestamps.Length >= 2)
            {
                long totalTime = timestamps[timestamps.Length - 1] - timestamps[0];
                double totalSeconds = totalTime / (double)Stopwatch.Frequency;
                avgPollingRate = timestamps.Length / totalSeconds;
                
                long lastInterval = timestamps[timestamps.Length - 1] - timestamps[timestamps.Length - 2];
                double lastSeconds = lastInterval / (double)Stopwatch.Frequency;
                currentPollingRate = 1.0 / lastSeconds;
            }
            
            var latencies = new List<double>();
            for (int i = 1; i < timestamps.Length; i++)
            {
                double intervalMs = (timestamps[i] - timestamps[i - 1]) * 1000.0 / Stopwatch.Frequency;
                latencies.Add(intervalMs);
            }
            
            return new PerformanceMetrics
            {
                AveragePollingRate = avgPollingRate,
                CurrentPollingRate = currentPollingRate,
                AverageInputLatency = latencies.Any() ? latencies.Average() : 0,
                MinInputLatency = latencies.Any() ? latencies.Min() : 0,
                MaxInputLatency = latencies.Any() ? latencies.Max() : 0,
                AverageProcessingTime = processingTimes.Any() ? processingTimes.Average() : 0,
                MaxProcessingTime = processingTimes.Any() ? processingTimes.Max() : 0,
                TotalInputsProcessed = _totalInputs,
                DroppedInputsPercent = 0,
                QueueSize = _inputTimestamps.Count,
                LastUpdate = DateTime.Now
            };
        }
        
        /// <summary>
        /// Reset all metrics
        /// </summary>
        public void Reset()
        {
            _inputTimestamps.Clear();
            _processingTimes.Clear();
            _totalInputs = 0;
            _lastInputTime = 0;
            _lastProcessingTime = 0;
        }
        
        /// <summary>
        /// Get performance grade based on metrics
        /// </summary>
        public static string GetPerformanceGrade(PerformanceMetrics metrics)
        {
            if (metrics.AverageInputLatency < 2.0 && metrics.AveragePollingRate > 800)
                return "⚡ EXCELLENT";
            if (metrics.AverageInputLatency < 5.0 && metrics.AveragePollingRate > 500)
                return "✅ GOOD";
            if (metrics.AverageInputLatency < 10.0 && metrics.AveragePollingRate > 250)
                return "⚠️ FAIR";
            return "❌ POOR";
        }
        
        /// <summary>
        /// Get formatted metrics report
        /// </summary>
        public static string GetMetricsReport(PerformanceMetrics metrics)
        {
            return $@"Performance Metrics Report
================================
Polling Rate: {metrics.AveragePollingRate:F1} Hz (Current: {metrics.CurrentPollingRate:F1} Hz)
Input Latency: {metrics.AverageInputLatency:F2} ms (Min: {metrics.MinInputLatency:F2} / Max: {metrics.MaxInputLatency:F2})
Processing Time: {metrics.AverageProcessingTime:F3} ms (Max: {metrics.MaxProcessingTime:F3})
Total Inputs: {metrics.TotalInputsProcessed:N0}
Grade: {GetPerformanceGrade(metrics)}
Last Update: {metrics.LastUpdate:HH:mm:ss}";
        }
    }
}
