// AdaptiveAimAssist.cs — IA adaptive pour améliorer progressivement votre aim
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace Trident.MITM
{
    /// <summary>
    /// Système d'assistance à la visée adaptatif qui apprend de vos habitudes
    /// et s'améliore avec le temps pour compenser vos tendances personnelles
    /// </summary>
    public class AdaptiveAimAssist
    {
        private readonly Queue<AimDataPoint> _aimHistory = new();
        private const int MAX_HISTORY = 1000;
        
        private AimProfile _currentProfile = new();
        private bool _isLearning = true;
        private int _sessionsCompleted = 0;
        
        public event Action<string>? FeedbackGenerated;
        public event Action<AimProfile>? ProfileUpdated;
        
        /// <summary>
        /// Point de données pour l'analyse de l'aim
        /// </summary>
        public class AimDataPoint
        {
            public DateTime Timestamp { get; set; }
            public double StickX { get; set; }
            public double StickY { get; set; }
            public bool WasAiming { get; set; }
            public bool ShotFired { get; set; }
            public double AccuracyScore { get; set; } // 0-1
        }
        
        /// <summary>
        /// Profil d'aim adaptatif
        /// </summary>
        public class AimProfile
        {
            public string PlayerName { get; set; } = "Player";
            
            // Tendances détectées
            public double HorizontalBias { get; set; } = 0; // Tendance gauche/droite
            public double VerticalBias { get; set; } = 0;   // Tendance haut/bas
            public double OvershootTendency { get; set; } = 0; // Tendance à dépasser
            public double UndershootTendency { get; set; } = 0; // Tendance à sous-viser
            
            // Compensation recommandée
            public double RecommendedHorizontalCompensation { get; set; } = 0;
            public double RecommendedVerticalCompensation { get; set; } = 0;
            public double RecommendedSensitivityMultiplier { get; set; } = 1.0;
            
            // Statistiques
            public double AverageAccuracy { get; set; } = 0;
            public int TotalShots { get; set; } = 0;
            public int SessionsAnalyzed { get; set; } = 0;
            public DateTime LastUpdated { get; set; } = DateTime.Now;
            
            // Patterns avancés
            public Dictionary<string, double> TimeOfDayAccuracy { get; set; } = new();
            public Dictionary<string, double> SessionDurationPerformance { get; set; } = new();
        }
        
        /// <summary>
        /// Enregistrer une donnée d'aim
        /// </summary>
        public void RecordAimData(double stickX, double stickY, bool wasAiming, bool shotFired, double accuracy)
        {
            var dataPoint = new AimDataPoint
            {
                Timestamp = DateTime.Now,
                StickX = stickX,
                StickY = stickY,
                WasAiming = wasAiming,
                ShotFired = shotFired,
                AccuracyScore = accuracy
            };
            
            _aimHistory.Enqueue(dataPoint);
            
            if (_aimHistory.Count > MAX_HISTORY)
                _aimHistory.Dequeue();
            
            // Analyse après chaque 50 tirs
            if (shotFired && _aimHistory.Count(d => d.ShotFired) % 50 == 0)
            {
                AnalyzeAndAdapt();
            }
        }
        
        /// <summary>
        /// Analyser les données et adapter le profil
        /// </summary>
        private void AnalyzeAndAdapt()
        {
            if (_aimHistory.Count < 50) return;
            
            var shots = _aimHistory.Where(d => d.ShotFired).ToList();
            if (shots.Count == 0) return;
            
            // Calculer les tendances
            _currentProfile.HorizontalBias = shots.Average(s => s.StickX);
            _currentProfile.VerticalBias = shots.Average(s => s.StickY);
            _currentProfile.AverageAccuracy = shots.Average(s => s.AccuracyScore);
            _currentProfile.TotalShots = shots.Count;
            
            // Détecter overshoot/undershoot
            var movements = shots.Select(s => Math.Sqrt(s.StickX * s.StickX + s.StickY * s.StickY)).ToList();
            var avgMovement = movements.Average();
            
            if (avgMovement > 0.7) // Mouvements trop grands
                _currentProfile.OvershootTendency = (avgMovement - 0.7) * 2;
            else if (avgMovement < 0.3) // Mouvements trop petits
                _currentProfile.UndershootTendency = (0.3 - avgMovement) * 2;
            
            // Calculer les compensations recommandées
            CalculateRecommendedCompensations();
            
            // Analyser par heure de la journée
            AnalyzeTimeOfDayPatterns();
            
            _currentProfile.SessionsAnalyzed++;
            _currentProfile.LastUpdated = DateTime.Now;
            
            ProfileUpdated?.Invoke(_currentProfile);
            GenerateFeedback();
        }
        
        /// <summary>
        /// Calculer les compensations recommandées
        /// </summary>
        private void CalculateRecommendedCompensations()
        {
            // Compensation horizontale (inverse du bias)
            _currentProfile.RecommendedHorizontalCompensation = -_currentProfile.HorizontalBias * 0.5;
            
            // Compensation verticale (inverse du bias)
            _currentProfile.RecommendedVerticalCompensation = -_currentProfile.VerticalBias * 0.5;
            
            // Ajustement de sensibilité basé sur overshoot/undershoot
            if (_currentProfile.OvershootTendency > 0.1)
            {
                // Réduire la sensibilité si overshoot
                _currentProfile.RecommendedSensitivityMultiplier = 1.0 - (_currentProfile.OvershootTendency * 0.2);
            }
            else if (_currentProfile.UndershootTendency > 0.1)
            {
                // Augmenter la sensibilité si undershoot
                _currentProfile.RecommendedSensitivityMultiplier = 1.0 + (_currentProfile.UndershootTendency * 0.2);
            }
            
            // Limiter les valeurs
            _currentProfile.RecommendedSensitivityMultiplier = Math.Clamp(
                _currentProfile.RecommendedSensitivityMultiplier, 
                0.7, 
                1.3
            );
        }
        
        /// <summary>
        /// Analyser les patterns selon l'heure
        /// </summary>
        private void AnalyzeTimeOfDayPatterns()
        {
            var hourGroups = _aimHistory
                .Where(d => d.ShotFired)
                .GroupBy(d => d.Timestamp.Hour);
            
            foreach (var group in hourGroups)
            {
                var hour = group.Key;
                var accuracy = group.Average(d => d.AccuracyScore);
                var timeRange = $"{hour:D2}:00-{(hour+1):D2}:00";
                _currentProfile.TimeOfDayAccuracy[timeRange] = accuracy;
            }
        }
        
        /// <summary>
        /// Générer un feedback personnalisé
        /// </summary>
        private void GenerateFeedback()
        {
            var feedback = new List<string>();
            
            // Feedback sur la précision
            if (_currentProfile.AverageAccuracy > 0.8)
                feedback.Add("🎯 Excellent! Votre précision est excellente!");
            else if (_currentProfile.AverageAccuracy > 0.6)
                feedback.Add("✅ Bon travail! Votre précision est au-dessus de la moyenne.");
            else if (_currentProfile.AverageAccuracy > 0.4)
                feedback.Add("📊 Précision moyenne. Continuez à vous entraîner!");
            else
                feedback.Add("⚠️ Votre précision peut être améliorée.");
            
            // Feedback sur les tendances
            if (Math.Abs(_currentProfile.HorizontalBias) > 0.1)
            {
                var direction = _currentProfile.HorizontalBias > 0 ? "droite" : "gauche";
                feedback.Add($"↔️ Tendance détectée: vous visez trop à {direction}");
                feedback.Add($"💡 Compensation recommandée: {_currentProfile.RecommendedHorizontalCompensation:F2}");
            }
            
            if (Math.Abs(_currentProfile.VerticalBias) > 0.1)
            {
                var direction = _currentProfile.VerticalBias > 0 ? "haut" : "bas";
                feedback.Add($"↕️ Tendance détectée: vous visez trop en {direction}");
                feedback.Add($"💡 Compensation recommandée: {_currentProfile.RecommendedVerticalCompensation:F2}");
            }
            
            // Feedback sur overshoot/undershoot
            if (_currentProfile.OvershootTendency > 0.1)
            {
                feedback.Add("🎯 Vous dépassez souvent votre cible (overshoot)");
                feedback.Add($"💡 Réduisez votre sensibilité à {_currentProfile.RecommendedSensitivityMultiplier:F2}x");
            }
            else if (_currentProfile.UndershootTendency > 0.1)
            {
                feedback.Add("🎯 Vous n'atteignez pas assez votre cible (undershoot)");
                feedback.Add($"💡 Augmentez votre sensibilité à {_currentProfile.RecommendedSensitivityMultiplier:F2}x");
            }
            
            // Meilleure heure de jeu
            if (_currentProfile.TimeOfDayAccuracy.Any())
            {
                var bestTime = _currentProfile.TimeOfDayAccuracy.OrderByDescending(kvp => kvp.Value).First();
                feedback.Add($"⏰ Meilleure performance: {bestTime.Key} ({bestTime.Value:P0} précision)");
            }
            
            foreach (var msg in feedback)
            {
                FeedbackGenerated?.Invoke(msg);
            }
        }
        
        /// <summary>
        /// Appliquer les compensations au mouvement du stick
        /// </summary>
        public (double x, double y) ApplyAdaptiveCompensation(double x, double y, bool isAiming)
        {
            if (!isAiming || !_isLearning)
                return (x, y);
            
            // Appliquer la compensation de bias
            x += _currentProfile.RecommendedHorizontalCompensation;
            y += _currentProfile.RecommendedVerticalCompensation;
            
            // Appliquer l'ajustement de sensibilité
            x *= _currentProfile.RecommendedSensitivityMultiplier;
            y *= _currentProfile.RecommendedSensitivityMultiplier;
            
            // Limiter les valeurs
            x = Math.Clamp(x, -1.0, 1.0);
            y = Math.Clamp(y, -1.0, 1.0);
            
            return (x, y);
        }
        
        /// <summary>
        /// Obtenir le profil actuel
        /// </summary>
        public AimProfile GetCurrentProfile()
        {
            return _currentProfile;
        }
        
        /// <summary>
        /// Activer/désactiver l'apprentissage
        /// </summary>
        public void SetLearningEnabled(bool enabled)
        {
            _isLearning = enabled;
        }
        
        /// <summary>
        /// Sauvegarder le profil
        /// </summary>
        public void SaveProfile(string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(_currentProfile, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(filePath, json);
            }
            catch { }
        }
        
        /// <summary>
        /// Charger le profil
        /// </summary>
        public void LoadProfile(string filePath)
        {
            if (!File.Exists(filePath)) return;
            
            try
            {
                var json = File.ReadAllText(filePath);
                var profile = JsonSerializer.Deserialize<AimProfile>(json);
                if (profile != null)
                {
                    _currentProfile = profile;
                    ProfileUpdated?.Invoke(_currentProfile);
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Réinitialiser le profil
        /// </summary>
        public void ResetProfile()
        {
            _currentProfile = new AimProfile();
            _aimHistory.Clear();
            ProfileUpdated?.Invoke(_currentProfile);
        }
        
        /// <summary>
        /// Obtenir un rapport détaillé
        /// </summary>
        public string GetDetailedReport()
        {
            return $@"
📊 RAPPORT D'ANALYSE D'AIM ADAPTATIF
═══════════════════════════════════

Joueur: {_currentProfile.PlayerName}
Sessions analysées: {_currentProfile.SessionsAnalyzed}
Total de tirs: {_currentProfile.TotalShots}
Dernière mise à jour: {_currentProfile.LastUpdated:g}

📈 STATISTIQUES DE PRÉCISION
─────────────────────────────
Précision moyenne: {_currentProfile.AverageAccuracy:P1}
Grade: {GetAccuracyGrade(_currentProfile.AverageAccuracy)}

🎯 TENDANCES DÉTECTÉES
─────────────────────────────
Bias horizontal: {_currentProfile.HorizontalBias:F3} {GetDirectionArrow(_currentProfile.HorizontalBias, true)}
Bias vertical: {_currentProfile.VerticalBias:F3} {GetDirectionArrow(_currentProfile.VerticalBias, false)}
Overshoot: {_currentProfile.OvershootTendency:F2}
Undershoot: {_currentProfile.UndershootTendency:F2}

💡 COMPENSATIONS RECOMMANDÉES
─────────────────────────────
Horizontal: {_currentProfile.RecommendedHorizontalCompensation:F3}
Vertical: {_currentProfile.RecommendedVerticalCompensation:F3}
Multiplicateur sensibilité: {_currentProfile.RecommendedSensitivityMultiplier:F2}x

⏰ PERFORMANCE PAR HEURE
─────────────────────────────
{GetTimeOfDayReport()}
";
        }
        
        private string GetAccuracyGrade(double accuracy)
        {
            if (accuracy > 0.9) return "S (Exceptionnel)";
            if (accuracy > 0.8) return "A (Excellent)";
            if (accuracy > 0.7) return "B (Très Bon)";
            if (accuracy > 0.6) return "C (Bon)";
            if (accuracy > 0.5) return "D (Moyen)";
            return "F (À améliorer)";
        }
        
        private string GetDirectionArrow(double value, bool horizontal)
        {
            if (Math.Abs(value) < 0.05) return "✓ (Centré)";
            if (horizontal)
                return value > 0 ? "→ (Droite)" : "← (Gauche)";
            return value > 0 ? "↑ (Haut)" : "↓ (Bas)";
        }
        
        private string GetTimeOfDayReport()
        {
            if (!_currentProfile.TimeOfDayAccuracy.Any())
                return "Pas encore de données";
            
            var sorted = _currentProfile.TimeOfDayAccuracy.OrderByDescending(kvp => kvp.Value);
            return string.Join("\n", sorted.Take(5).Select(kvp => 
                $"{kvp.Key}: {kvp.Value:P0}"));
        }
    }
}
