// SmartAutoAimAI.cs — IA d'aim assist intelligent qui s'adapte au type d'ennemi et situation
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trident.MITM
{
    /// <summary>
    /// IA d'auto-aim intelligent qui ajuste la force d'assistance selon:
    /// - Distance de la cible
    /// - Vitesse de déplacement de la cible
    /// - Type d'arme utilisée
    /// - Situation de jeu (combat proche, sniper, etc.)
    /// </summary>
    public class SmartAutoAimAI
    {
        private readonly Dictionary<string, AimProfile> _weaponProfiles = new();
        private TargetInfo? _currentTarget;
        private double _baseAssistStrength = 0.3;
        
        public event Action<string>? TargetAcquired;
        public event Action<AimAdjustment>? AimAdjusted;
        
        /// <summary>
        /// Information sur la cible
        /// </summary>
        public class TargetInfo
        {
            public double Distance { get; set; } // En mètres/unités
            public double Speed { get; set; } // Vitesse de déplacement
            public double Direction { get; set; } // Direction de mouvement (degrés)
            public string TargetType { get; set; } = "Player"; // Player, Vehicle, Boss, etc.
            public bool IsMoving => Speed > 0.5;
        }
        
        /// <summary>
        /// Profil d'aim pour une arme
        /// </summary>
        public class AimProfile
        {
            public string WeaponName { get; set; } = "";
            public double OptimalRange { get; set; } // Distance optimale
            public double MaxRange { get; set; } // Portée max
            public double ProjectileSpeed { get; set; } // Vitesse du projectile
            public bool IsHitscan { get; set; } // Instantané ou projectile
            public double RecommendedAssistStrength { get; set; } = 0.3;
        }
        
        /// <summary>
        /// Ajustement d'aim calculé
        /// </summary>
        public class AimAdjustment
        {
            public double AdjustmentX { get; set; }
            public double AdjustmentY { get; set; }
            public double AssistStrength { get; set; }
            public string Reason { get; set; } = "";
            public bool IsLeading { get; set; } // Vise devant la cible (projectiles)
        }
        
        /// <summary>
        /// Initialiser avec des profils d'armes par défaut
        /// </summary>
        public void InitializeDefaultProfiles()
        {
            // Fusils d'assaut
            _weaponProfiles["AR"] = new AimProfile
            {
                WeaponName = "Assault Rifle",
                OptimalRange = 25,
                MaxRange = 50,
                ProjectileSpeed = 300,
                IsHitscan = false,
                RecommendedAssistStrength = 0.35
            };
            
            // Sniper
            _weaponProfiles["Sniper"] = new AimProfile
            {
                WeaponName = "Sniper Rifle",
                OptimalRange = 80,
                MaxRange = 200,
                ProjectileSpeed = 800,
                IsHitscan = true,
                RecommendedAssistStrength = 0.25 // Moins d'aide pour le skill
            };
            
            // SMG
            _weaponProfiles["SMG"] = new AimProfile
            {
                WeaponName = "SMG",
                OptimalRange = 15,
                MaxRange = 30,
                ProjectileSpeed = 250,
                IsHitscan = false,
                RecommendedAssistStrength = 0.45 // Plus d'aide en combat proche
            };
            
            // Shotgun
            _weaponProfiles["Shotgun"] = new AimProfile
            {
                WeaponName = "Shotgun",
                OptimalRange = 8,
                MaxRange = 15,
                ProjectileSpeed = 200,
                IsHitscan = false,
                RecommendedAssistStrength = 0.5 // Forte aide en très proche
            };
        }
        
        /// <summary>
        /// Définir la cible actuelle
        /// </summary>
        public void SetTarget(double distance, double speed, double direction, string targetType = "Player")
        {
            _currentTarget = new TargetInfo
            {
                Distance = distance,
                Speed = speed,
                Direction = direction,
                TargetType = targetType
            };
            
            TargetAcquired?.Invoke($"Cible acquise: {targetType} à {distance:F1}m, vitesse {speed:F1}");
        }
        
        /// <summary>
        /// Calculer l'ajustement d'aim intelligent
        /// </summary>
        public AimAdjustment CalculateSmartAim(double currentAimX, double currentAimY, string weaponType)
        {
            if (_currentTarget == null)
                return new AimAdjustment { Reason = "Pas de cible" };
            
            if (!_weaponProfiles.TryGetValue(weaponType, out var weaponProfile))
                weaponProfile = _weaponProfiles.Values.First(); // Default
            
            var adjustment = new AimAdjustment();
            
            // 1. Ajuster la force selon la distance
            var distanceFactor = CalculateDistanceFactor(_currentTarget.Distance, weaponProfile);
            var strengthAdjustment = weaponProfile.RecommendedAssistStrength * distanceFactor;
            
            // 2. Ajuster selon la vitesse de la cible
            var speedFactor = CalculateSpeedFactor(_currentTarget.Speed);
            strengthAdjustment *= speedFactor;
            
            // 3. Calculer le lead (viser devant) si projectile lent
            if (!weaponProfile.IsHitscan && _currentTarget.IsMoving)
            {
                var lead = CalculateLeadAdjustment(_currentTarget, weaponProfile);
                adjustment.AdjustmentX = lead.x * strengthAdjustment;
                adjustment.AdjustmentY = lead.y * strengthAdjustment;
                adjustment.IsLeading = true;
                adjustment.Reason = $"Lead calculé pour cible en mouvement ({_currentTarget.Speed:F1} m/s)";
            }
            else
            {
                // Aim direct vers le centre
                adjustment.AdjustmentX = -currentAimX * strengthAdjustment;
                adjustment.AdjustmentY = -currentAimY * strengthAdjustment;
                adjustment.Reason = $"Aim direct (distance: {_currentTarget.Distance:F1}m)";
            }
            
            // 4. Boost si cible spéciale (boss, etc.)
            if (_currentTarget.TargetType == "Boss")
            {
                strengthAdjustment *= 0.7; // Moins d'aide sur boss (skill requis)
                adjustment.Reason += " - Boss détecté";
            }
            else if (_currentTarget.TargetType == "Vehicle")
            {
                strengthAdjustment *= 1.3; // Plus d'aide sur véhicules
                adjustment.Reason += " - Véhicule détecté";
            }
            
            adjustment.AssistStrength = Math.Clamp(strengthAdjustment, 0, 1);
            
            AimAdjusted?.Invoke(adjustment);
            return adjustment;
        }
        
        /// <summary>
        /// Calculer le facteur de distance
        /// </summary>
        private double CalculateDistanceFactor(double distance, AimProfile weapon)
        {
            // Force maximale à distance optimale
            if (distance <= weapon.OptimalRange)
            {
                return 1.0;
            }
            
            // Diminue linéairement jusqu'à la portée max
            if (distance <= weapon.MaxRange)
            {
                return 1.0 - ((distance - weapon.OptimalRange) / (weapon.MaxRange - weapon.OptimalRange)) * 0.5;
            }
            
            // Hors de portée = aide minimale
            return 0.2;
        }
        
        /// <summary>
        /// Calculer le facteur de vitesse
        /// </summary>
        private double CalculateSpeedFactor(double speed)
        {
            // Plus la cible va vite, plus on aide
            if (speed < 2) return 1.0; // Cible lente/immobile
            if (speed < 5) return 1.2; // Cible rapide
            if (speed < 10) return 1.4; // Très rapide
            return 1.6; // Extrêmement rapide (véhicule)
        }
        
        /// <summary>
        /// Calculer le lead (viser devant la cible)
        /// </summary>
        private (double x, double y) CalculateLeadAdjustment(TargetInfo target, AimProfile weapon)
        {
            // Temps que met le projectile pour atteindre la cible
            var timeToImpact = target.Distance / weapon.ProjectileSpeed;
            
            // Distance que la cible va parcourir pendant ce temps
            var leadDistance = target.Speed * timeToImpact;
            
            // Convertir en coordonnées X/Y basées sur la direction
            var directionRad = target.Direction * Math.PI / 180.0;
            var leadX = Math.Cos(directionRad) * leadDistance * 0.01; // Normaliser
            var leadY = Math.Sin(directionRad) * leadDistance * 0.01;
            
            return (leadX, leadY);
        }
        
        /// <summary>
        /// Prédire la position future de la cible
        /// </summary>
        public (double x, double y) PredictTargetPosition(double currentX, double currentY, double predictionTimeMs)
        {
            if (_currentTarget == null || !_currentTarget.IsMoving)
                return (currentX, currentY);
            
            var predictionTimeSec = predictionTimeMs / 1000.0;
            var travelDistance = _currentTarget.Speed * predictionTimeSec;
            
            var directionRad = _currentTarget.Direction * Math.PI / 180.0;
            var predictedX = currentX + Math.Cos(directionRad) * travelDistance;
            var predictedY = currentY + Math.Sin(directionRad) * travelDistance;
            
            return (predictedX, predictedY);
        }
        
        /// <summary>
        /// Appliquer l'ajustement au stick
        /// </summary>
        public (double x, double y) ApplySmartAim(double stickX, double stickY, string weaponType, bool isAiming)
        {
            if (!isAiming || _currentTarget == null)
                return (stickX, stickY);
            
            var adjustment = CalculateSmartAim(stickX, stickY, weaponType);
            
            // Appliquer l'ajustement graduellement (pas brutalement)
            var adjustedX = stickX + adjustment.AdjustmentX * 0.6;
            var adjustedY = stickY + adjustment.AdjustmentY * 0.6;
            
            return (
                Math.Clamp(adjustedX, -1.0, 1.0),
                Math.Clamp(adjustedY, -1.0, 1.0)
            );
        }
        
        /// <summary>
        /// Définir la force de base de l'assistance
        /// </summary>
        public void SetBaseAssistStrength(double strength)
        {
            _baseAssistStrength = Math.Clamp(strength, 0, 1);
        }
        
        /// <summary>
        /// Obtenir un rapport de configuration
        /// </summary>
        public string GetConfigurationReport(string weaponType)
        {
            if (!_weaponProfiles.TryGetValue(weaponType, out var profile))
                return "Arme inconnue";
            
            var targetInfo = _currentTarget != null 
                ? $@"
CIBLE ACTUELLE
  Distance: {_currentTarget.Distance:F1}m
  Vitesse: {_currentTarget.Speed:F1} m/s
  Type: {_currentTarget.TargetType}
  En mouvement: {(_currentTarget.IsMoving ? "Oui" : "Non")}"
                : "\nAucune cible verrouillée";
            
            return $@"
═══════════════════════════════════════
IA SMART AUTO-AIM
═══════════════════════════════════════

ARME: {profile.WeaponName}
  Portée optimale: {profile.OptimalRange}m
  Portée max: {profile.MaxRange}m
  Type: {(profile.IsHitscan ? "Hitscan" : "Projectile")}
  Vitesse projectile: {profile.ProjectileSpeed} m/s
  Force d'aide: {profile.RecommendedAssistStrength:P0}
{targetInfo}
";
        }
    }
}
