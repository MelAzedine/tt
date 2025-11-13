// AntiCheatDetectionAI.cs — IA pour détecter les patterns suspects et s'auto-réguler
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trident.MITM
{
    /// <summary>
    /// IA qui analyse les inputs pour détecter les patterns suspects
    /// et ajuster automatiquement pour rester indétectable par les anti-cheats
    /// </summary>
    public class AntiCheatDetectionAI
    {
        private readonly Queue<InputEvent> _inputHistory = new();
        private const int HISTORY_SIZE = 1000;
        
        private double _currentHumanlikeScore = 1.0; // 0-1, 1 = 100% humain
        private readonly Random _random = new();
        
        public event Action<string>? SuspiciousPatternDetected;
        public event Action<double>? HumanlikeScoreChanged;
        
        /// <summary>
        /// Événement d'input
        /// </summary>
        public class InputEvent
        {
            public DateTime Timestamp { get; set; }
            public string Type { get; set; } = ""; // "Button", "Stick", "Trigger"
            public string Name { get; set; } = "";
            public double Value { get; set; }
            public long TimeSinceLast { get; set; } // ms
        }
        
        /// <summary>
        /// Enregistrer un événement d'input
        /// </summary>
        public void RecordInput(string type, string name, double value)
        {
            var now = DateTime.Now;
            var timeSinceLast = _inputHistory.Any() 
                ? (long)(now - _inputHistory.Last().Timestamp).TotalMilliseconds 
                : 0;
            
            var inputEvent = new InputEvent
            {
                Timestamp = now,
                Type = type,
                Name = name,
                Value = value,
                TimeSinceLast = timeSinceLast
            };
            
            _inputHistory.Enqueue(inputEvent);
            
            if (_inputHistory.Count > HISTORY_SIZE)
                _inputHistory.Dequeue();
            
            // Analyser périodiquement
            if (_inputHistory.Count % 100 == 0)
            {
                AnalyzeSuspiciousPatterns();
            }
        }
        
        /// <summary>
        /// Analyser les patterns suspects
        /// </summary>
        private void AnalyzeSuspiciousPatterns()
        {
            var score = 1.0;
            var issues = new List<string>();
            
            // 1. Vérifier les timings trop parfaits (macro detection)
            var buttonPresses = _inputHistory
                .Where(e => e.Type == "Button" && e.Value > 0)
                .ToList();
            
            if (buttonPresses.Count > 20)
            {
                var timings = buttonPresses.Select(b => b.TimeSinceLast).ToList();
                var avgTiming = timings.Average();
                var stdDev = CalculateStdDev(timings);
                
                // Si écart-type très faible = timings trop réguliers = suspect
                if (stdDev < 5 && avgTiming > 0)
                {
                    score -= 0.3;
                    issues.Add("⚠️ Timings trop réguliers détectés (possible macro)");
                }
            }
            
            // 2. Vérifier les mouvements de stick trop linéaires
            var stickInputs = _inputHistory
                .Where(e => e.Type == "Stick")
                .ToList();
            
            if (stickInputs.Count > 50)
            {
                var values = stickInputs.Select(s => s.Value).ToList();
                var changes = new List<double>();
                
                for (int i = 1; i < values.Count; i++)
                {
                    changes.Add(Math.Abs(values[i] - values[i - 1]));
                }
                
                var avgChange = changes.Average();
                var changeStdDev = CalculateStdDev(changes);
                
                // Mouvements trop uniformes = suspect
                if (changeStdDev < 0.01 && avgChange > 0.05)
                {
                    score -= 0.2;
                    issues.Add("⚠️ Mouvements de stick trop linéaires");
                }
            }
            
            // 3. Vérifier les réactions surhumaines
            var reactionTimes = new List<long>();
            for (int i = 1; i < _inputHistory.Count; i++)
            {
                var events = _inputHistory.ToList();
                if (events[i].Type == "Button" && events[i - 1].Type == "Stick")
                {
                    reactionTimes.Add(events[i].TimeSinceLast);
                }
            }
            
            if (reactionTimes.Any())
            {
                var avgReaction = reactionTimes.Average();
                
                // Réaction < 150ms de manière constante = surhumain
                if (avgReaction < 150)
                {
                    score -= 0.25;
                    issues.Add("⚠️ Temps de réaction surhumains détectés");
                }
            }
            
            // 4. Vérifier l'absence de micro-mouvements (humains tremblent toujours un peu)
            var microMovements = stickInputs
                .Where(s => Math.Abs(s.Value) < 0.05 && Math.Abs(s.Value) > 0)
                .Count();
            
            double microMovementRatio = stickInputs.Any() 
                ? (double)microMovements / stickInputs.Count 
                : 0;
            
            // Humains ont toujours des micro-mouvements
            if (microMovementRatio < 0.05)
            {
                score -= 0.15;
                issues.Add("⚠️ Absence de micro-mouvements naturels");
            }
            
            // Mettre à jour le score
            _currentHumanlikeScore = Math.Max(0, score);
            HumanlikeScoreChanged?.Invoke(_currentHumanlikeScore);
            
            // Alerter si score faible
            if (_currentHumanlikeScore < 0.7 && issues.Any())
            {
                var alert = string.Join("\n", issues);
                SuspiciousPatternDetected?.Invoke(alert);
            }
        }
        
        /// <summary>
        /// Ajouter du bruit humain aux inputs pour paraître plus naturel
        /// </summary>
        public double AddHumanNoise(double value, double intensity = 0.02)
        {
            if (_currentHumanlikeScore > 0.8)
                return value; // Déjà naturel
            
            // Ajouter un petit bruit aléatoire
            var noise = (_random.NextDouble() - 0.5) * 2 * intensity;
            var result = value + noise;
            
            return Math.Clamp(result, -1.0, 1.0);
        }
        
        /// <summary>
        /// Ajouter une micro-variation au timing pour paraître humain
        /// </summary>
        public int AddTimingVariation(int baseDelayMs, double variation = 0.1)
        {
            if (_currentHumanlikeScore > 0.8)
                return baseDelayMs;
            
            // Variation de ±10% par défaut
            var variationAmount = baseDelayMs * variation;
            var randomVariation = (_random.NextDouble() - 0.5) * 2 * variationAmount;
            
            return Math.Max(1, (int)(baseDelayMs + randomVariation));
        }
        
        /// <summary>
        /// Simuler des micro-mouvements de stick humains
        /// </summary>
        public (double x, double y) AddMicroMovements(double x, double y)
        {
            if (_currentHumanlikeScore > 0.8)
                return (x, y);
            
            // Très petits mouvements aléatoires (tremblement humain)
            var microX = (_random.NextDouble() - 0.5) * 0.01;
            var microY = (_random.NextDouble() - 0.5) * 0.01;
            
            return (
                Math.Clamp(x + microX, -1.0, 1.0),
                Math.Clamp(y + microY, -1.0, 1.0)
            );
        }
        
        /// <summary>
        /// Obtenir le score actuel de "naturalité"
        /// </summary>
        public double GetHumanlikeScore()
        {
            return _currentHumanlikeScore;
        }
        
        /// <summary>
        /// Obtenir un rapport d'analyse
        /// </summary>
        public string GetAnalysisReport()
        {
            var grade = _currentHumanlikeScore switch
            {
                >= 0.9 => "A+ (Très naturel)",
                >= 0.8 => "A (Naturel)",
                >= 0.7 => "B (Acceptable)",
                >= 0.6 => "C (Suspect)",
                >= 0.5 => "D (Très suspect)",
                _ => "F (Détectable)"
            };
            
            var colorEmoji = _currentHumanlikeScore switch
            {
                >= 0.8 => "🟢",
                >= 0.6 => "🟡",
                _ => "🔴"
            };
            
            return $@"
═══════════════════════════════════════
IA ANTI-DÉTECTION
═══════════════════════════════════════

{colorEmoji} Score de naturalité: {_currentHumanlikeScore:P0}
Grade: {grade}

📊 Inputs analysés: {_inputHistory.Count}

💡 RECOMMANDATIONS:
{GetRecommendations()}
";
        }
        
        private string GetRecommendations()
        {
            if (_currentHumanlikeScore >= 0.9)
                return "  ✅ Vos inputs sont très naturels!\n  Aucune action nécessaire.";
            
            if (_currentHumanlikeScore >= 0.7)
                return "  ⚠️ Inputs acceptables mais améliorables\n  Activez les micro-mouvements automatiques.";
            
            return @"  🔴 Inputs suspects détectés!
  1. Activez l'ajout de bruit humain
  2. Variez vos timings
  3. Évitez les macros trop répétitives
  4. Ajoutez des micro-mouvements";
        }
        
        /// <summary>
        /// Calculer l'écart-type
        /// </summary>
        private double CalculateStdDev(List<long> values)
        {
            if (!values.Any()) return 0;
            
            double avg = values.Average();
            double sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
            
            return Math.Sqrt(sumOfSquares / values.Count);
        }
        
        private double CalculateStdDev(List<double> values)
        {
            if (!values.Any()) return 0;
            
            double avg = values.Average();
            double sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
            
            return Math.Sqrt(sumOfSquares / values.Count);
        }
        
        /// <summary>
        /// Réinitialiser l'analyse
        /// </summary>
        public void Reset()
        {
            _inputHistory.Clear();
            _currentHumanlikeScore = 1.0;
        }
    }
}
