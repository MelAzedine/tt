// AdvancedResponseCurves.cs — Additional stick response curve types
using System;

namespace Trident.MITM
{
    /// <summary>
    /// Advanced response curves for stick input processing
    /// </summary>
    public static class AdvancedResponseCurves
    {
        /// <summary>
        /// Response curve types
        /// </summary>
        public enum CurveType
        {
            Linear,
            Exponential,
            Logarithmic,
            PowerCurve,
            SCurve,
            Aggressive,
            Smooth,
            Custom
        }
        
        /// <summary>
        /// Apply response curve to normalized input (-1 to 1)
        /// </summary>
        public static double ApplyCurve(double input, CurveType curveType, double intensity = 1.0)
        {
            if (Math.abs(input) < 0.001) return 0;
            
            double sign = Math.Sign(input);
            double absInput = Math.Abs(input);
            
            double output = curveType switch
            {
                CurveType.Linear => absInput,
                CurveType.Exponential => ApplyExponential(absInput, intensity),
                CurveType.Logarithmic => ApplyLogarithmic(absInput, intensity),
                CurveType.PowerCurve => ApplyPowerCurve(absInput, intensity),
                CurveType.SCurve => ApplySCurve(absInput, intensity),
                CurveType.Aggressive => ApplyAggressive(absInput, intensity),
                CurveType.Smooth => ApplySmooth(absInput, intensity),
                _ => absInput
            };
            
            return sign * Math.Clamp(output, 0.0, 1.0);
        }
        
        /// <summary>
        /// Exponential curve - slow at start, fast at end
        /// Good for precision aiming
        /// </summary>
        private static double ApplyExponential(double input, double intensity)
        {
            // Formula: x^(1 + intensity)
            double exponent = 1.0 + intensity;
            return Math.Pow(input, exponent);
        }
        
        /// <summary>
        /// Logarithmic curve - fast at start, slow at end
        /// Good for quick flicks
        /// </summary>
        private static double ApplyLogarithmic(double input, double intensity)
        {
            // Formula: log(1 + x * intensity) / log(1 + intensity)
            if (intensity < 0.1) intensity = 0.1;
            return Math.Log(1 + input * intensity) / Math.Log(1 + intensity);
        }
        
        /// <summary>
        /// Power curve - customizable exponential
        /// </summary>
        private static double ApplyPowerCurve(double input, double power)
        {
            if (power < 0.1) power = 0.1;
            if (power > 5.0) power = 5.0;
            return Math.Pow(input, power);
        }
        
        /// <summary>
        /// S-Curve - slow at extremes, fast in middle
        /// Good for smooth acceleration
        /// </summary>
        private static double ApplySCurve(double input, double intensity)
        {
            // Sigmoid function: 1 / (1 + e^(-k*(x-0.5)))
            double k = 5.0 * intensity; // Steepness
            double shifted = input - 0.5;
            double sigmoid = 1.0 / (1.0 + Math.Exp(-k * shifted));
            // Normalize to 0-1
            return (sigmoid - 0.5) * 2.0;
        }
        
        /// <summary>
        /// Aggressive curve - emphasizes small movements
        /// Good for competitive play
        /// </summary>
        private static double ApplyAggressive(double input, double intensity)
        {
            // Combination of exponential and linear
            double blend = 0.3 + (intensity * 0.4); // 0.3 to 0.7
            double exp = Math.Pow(input, 1.5);
            return input * blend + exp * (1 - blend);
        }
        
        /// <summary>
        /// Smooth curve - reduces small movements
        /// Good for stability
        /// </summary>
        private static double ApplySmooth(double input, double intensity)
        {
            // Gentle S-curve with deadzone-like effect
            double threshold = 0.1 * intensity;
            if (input < threshold)
                return 0;
            
            double adjusted = (input - threshold) / (1.0 - threshold);
            return Math.Pow(adjusted, 0.8);
        }
        
        /// <summary>
        /// Apply curve to 2D stick input
        /// </summary>
        public static (double x, double y) ApplyCurve2D(double x, double y, CurveType curveType, double intensity = 1.0)
        {
            // Calculate magnitude and angle
            double magnitude = Math.Sqrt(x * x + y * y);
            
            if (magnitude < 0.001)
                return (0, 0);
            
            // Apply curve to magnitude
            double newMagnitude = ApplyCurve(magnitude, curveType, intensity);
            
            // Preserve direction
            double scale = newMagnitude / magnitude;
            return (x * scale, y * scale);
        }
        
        /// <summary>
        /// Custom curve using control points (Catmull-Rom spline)
        /// </summary>
        public static double ApplyCustomCurve(double input, double[] controlPoints)
        {
            if (controlPoints == null || controlPoints.Length < 2)
                return input;
            
            double absInput = Math.Abs(input);
            int segments = controlPoints.Length - 1;
            double segmentSize = 1.0 / segments;
            
            // Find which segment we're in
            int segment = Math.Min((int)(absInput / segmentSize), segments - 1);
            double t = (absInput - segment * segmentSize) / segmentSize;
            
            // Linear interpolation between control points
            double p0 = controlPoints[segment];
            double p1 = controlPoints[segment + 1];
            double output = p0 + (p1 - p0) * t;
            
            return Math.Sign(input) * output;
        }
        
        /// <summary>
        /// Get curve description
        /// </summary>
        public static string GetCurveDescription(CurveType curveType)
        {
            return curveType switch
            {
                CurveType.Linear => "1:1 response, no modification",
                CurveType.Exponential => "Slow start, fast end - precision aiming",
                CurveType.Logarithmic => "Fast start, slow end - quick flicks",
                CurveType.PowerCurve => "Customizable power curve",
                CurveType.SCurve => "Smooth acceleration curve",
                CurveType.Aggressive => "Enhanced small movements - competitive",
                CurveType.Smooth => "Reduced small movements - stability",
                CurveType.Custom => "User-defined control points",
                _ => "Unknown curve"
            };
        }
        
        /// <summary>
        /// Get recommended intensity range
        /// </summary>
        public static (double min, double max, double recommended) GetIntensityRange(CurveType curveType)
        {
            return curveType switch
            {
                CurveType.Exponential => (0.5, 3.0, 1.5),
                CurveType.Logarithmic => (0.5, 5.0, 2.0),
                CurveType.PowerCurve => (0.5, 3.0, 1.2),
                CurveType.SCurve => (0.5, 2.0, 1.0),
                CurveType.Aggressive => (0.0, 1.0, 0.7),
                CurveType.Smooth => (0.0, 1.0, 0.5),
                _ => (0.0, 1.0, 1.0)
            };
        }
    }
}
