using System;

namespace Trident.MITM
{
    public class BezierCurve
    {
        public double P1 { get; set; } = 0.5; // Point de contrôle 1 (0.0 à 1.0)
        public double P2 { get; set; } = 0.5; // Point de contrôle 2 (0.0 à 1.0)

        public BezierCurve() { }

        public BezierCurve(double p1, double p2)
        {
            P1 = Math.Clamp(p1, 0.0, 1.0);
            P2 = Math.Clamp(p2, 0.0, 1.0);
        }

        /// <summary>
        /// Applique la courbe de Bézier à une valeur normalisée (0.0 à 1.0)
        /// </summary>
        public double Apply(double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            
            // Courbe de Bézier cubique avec points de contrôle P1 et P2
            // P0 = (0, 0), P1 = (P1, P1), P2 = (P2, P2), P3 = (1, 1)
            double u = 1.0 - t;
            double tt = t * t;
            double uu = u * u;
            double uuu = uu * u;
            double ttt = tt * t;

            // Formule de Bézier cubique
            double result = uuu * 0.0 + 3 * uu * t * P1 + 3 * u * tt * P2 + ttt * 1.0;
            
            return Math.Clamp(result, 0.0, 1.0);
        }

        /// <summary>
        /// Applique la courbe à une valeur de stick (-1.0 à 1.0)
        /// </summary>
        public double ApplyToStick(double value)
        {
            double absValue = Math.Abs(value);
            double sign = value >= 0 ? 1.0 : -1.0;
            
            if (absValue < 1e-6)
                return 0.0;
            
            double normalized = absValue; // Déjà normalisé entre 0 et 1
            double curved = Apply(normalized);
            
            return curved * sign;
        }
    }
}

