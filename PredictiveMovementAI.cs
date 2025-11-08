// PredictiveMovementAI.cs — IA prédictive de mouvement basée sur les patterns de jeu
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace Trident.MITM
{
    /// <summary>
    /// IA qui apprend vos patterns de mouvement et prédit vos prochaines actions
    /// pour pré-optimiser les paramètres (sensibilité, courbes, etc.)
    /// </summary>
    public class PredictiveMovementAI
    {
        private readonly Queue<MovementPattern> _recentPatterns = new();
        private readonly Dictionary<string, List<MovementSequence>> _learnedSequences = new();
        private const int PATTERN_HISTORY_SIZE = 500;
        private const int MIN_SEQUENCE_LENGTH = 3;
        
        public event Action<string>? PatternPredicted;
        public event Action<MovementOptimization>? OptimizationSuggested;
        
        /// <summary>
        /// Pattern de mouvement enregistré
        /// </summary>
        public class MovementPattern
        {
            public DateTime Timestamp { get; set; }
            public string Action { get; set; } = ""; // "Sprint", "Crouch", "Jump", "Slide", etc.
            public double StickIntensity { get; set; } // 0-1
            public double Direction { get; set; } // 0-360 degrees
            public string Context { get; set; } = ""; // "Combat", "Building", "Exploring"
        }
        
        /// <summary>
        /// Séquence de mouvements apprise
        /// </summary>
        public class MovementSequence
        {
            public List<string> Actions { get; set; } = new();
            public int Frequency { get; set; } = 0;
            public double AverageSuccessRate { get; set; } = 0;
            public string Context { get; set; } = "";
        }
        
        /// <summary>
        /// Optimisation suggérée par l'IA
        /// </summary>
        public class MovementOptimization
        {
            public string Type { get; set; } = ""; // "Sensitivity", "Curve", "DeadZone"
            public double RecommendedValue { get; set; }
            public string Reason { get; set; } = "";
            public double ConfidenceScore { get; set; } // 0-1
        }
        
        /// <summary>
        /// Enregistrer un pattern de mouvement
        /// </summary>
        public void RecordMovementPattern(string action, double stickIntensity, double direction, string context)
        {
            var pattern = new MovementPattern
            {
                Timestamp = DateTime.Now,
                Action = action,
                StickIntensity = stickIntensity,
                Direction = direction,
                Context = context
            };
            
            _recentPatterns.Enqueue(pattern);
            
            if (_recentPatterns.Count > PATTERN_HISTORY_SIZE)
                _recentPatterns.Dequeue();
            
            // Analyser les séquences après chaque 50 patterns
            if (_recentPatterns.Count % 50 == 0)
            {
                AnalyzeSequences();
            }
        }
        
        /// <summary>
        /// Analyser et apprendre les séquences de mouvements
        /// </summary>
        private void AnalyzeSequences()
        {
            var patterns = _recentPatterns.ToList();
            if (patterns.Count < MIN_SEQUENCE_LENGTH) return;
            
            // Chercher des séquences répétées
            for (int length = MIN_SEQUENCE_LENGTH; length <= 6; length++)
            {
                for (int i = 0; i <= patterns.Count - length; i++)
                {
                    var sequence = patterns.Skip(i).Take(length).Select(p => p.Action).ToList();
                    var context = patterns[i].Context;
                    var key = string.Join("->", sequence);
                    
                    if (!_learnedSequences.ContainsKey(context))
                        _learnedSequences[context] = new();
                    
                    var existing = _learnedSequences[context].FirstOrDefault(s => 
                        s.Actions.SequenceEqual(sequence));
                    
                    if (existing != null)
                    {
                        existing.Frequency++;
                    }
                    else
                    {
                        _learnedSequences[context].Add(new MovementSequence
                        {
                            Actions = sequence,
                            Frequency = 1,
                            Context = context
                        });
                    }
                }
            }
            
            // Générer des optimisations basées sur les patterns
            GenerateOptimizations();
        }
        
        /// <summary>
        /// Prédire la prochaine action basée sur l'historique récent
        /// </summary>
        public string? PredictNextAction(string context)
        {
            if (!_learnedSequences.ContainsKey(context))
                return null;
            
            var recent = _recentPatterns.TakeLast(5).Select(p => p.Action).ToList();
            if (recent.Count < 2) return null;
            
            // Chercher une séquence qui commence par les actions récentes
            var matchingSequences = _learnedSequences[context]
                .Where(s => s.Actions.Count > recent.Count && 
                           s.Actions.Take(recent.Count).SequenceEqual(recent))
                .OrderByDescending(s => s.Frequency)
                .ToList();
            
            if (matchingSequences.Any())
            {
                var predicted = matchingSequences.First();
                var nextAction = predicted.Actions[recent.Count];
                
                PatternPredicted?.Invoke($"Prochaine action prédite: {nextAction} (confiance: {predicted.Frequency})");
                return nextAction;
            }
            
            return null;
        }
        
        /// <summary>
        /// Générer des optimisations basées sur les patterns détectés
        /// </summary>
        private void GenerateOptimizations()
        {
            var allPatterns = _recentPatterns.ToList();
            if (allPatterns.Count < 50) return;
            
            // Analyser l'intensité moyenne des sticks par contexte
            var contextGroups = allPatterns.GroupBy(p => p.Context);
            
            foreach (var group in contextGroups)
            {
                var avgIntensity = group.Average(p => p.StickIntensity);
                
                // Si intensité faible dans un contexte spécifique, suggérer sensibilité plus élevée
                if (avgIntensity < 0.4 && group.Count() > 20)
                {
                    var optimization = new MovementOptimization
                    {
                        Type = "Sensitivity",
                        RecommendedValue = 1.0 + (0.4 - avgIntensity),
                        Reason = $"Mouvements lents détectés en {group.Key}. Augmentez la sensibilité.",
                        ConfidenceScore = Math.Min(1.0, group.Count() / 50.0)
                    };
                    
                    OptimizationSuggested?.Invoke(optimization);
                }
                // Si intensité élevée, suggérer sensibilité plus basse
                else if (avgIntensity > 0.8 && group.Count() > 20)
                {
                    var optimization = new MovementOptimization
                    {
                        Type = "Sensitivity",
                        RecommendedValue = 1.0 - (avgIntensity - 0.8) * 0.5,
                        Reason = $"Mouvements brusques détectés en {group.Key}. Réduisez la sensibilité.",
                        ConfidenceScore = Math.Min(1.0, group.Count() / 50.0)
                    };
                    
                    OptimizationSuggested?.Invoke(optimization);
                }
            }
            
            // Détecter les actions répétitives rapides (macro potential)
            DetectMacroPotential();
        }
        
        /// <summary>
        /// Détecter les séquences qui pourraient être macro-isées
        /// </summary>
        private void DetectMacroPotential()
        {
            foreach (var context in _learnedSequences.Keys)
            {
                var frequentSequences = _learnedSequences[context]
                    .Where(s => s.Frequency > 10 && s.Actions.Count >= 3)
                    .OrderByDescending(s => s.Frequency)
                    .Take(3);
                
                foreach (var sequence in frequentSequences)
                {
                    var optimization = new MovementOptimization
                    {
                        Type = "MacroSuggestion",
                        RecommendedValue = sequence.Frequency,
                        Reason = $"Séquence répétée {sequence.Frequency} fois: {string.Join(" → ", sequence.Actions)}. Créez une macro!",
                        ConfidenceScore = Math.Min(1.0, sequence.Frequency / 20.0)
                    };
                    
                    OptimizationSuggested?.Invoke(optimization);
                }
            }
        }
        
        /// <summary>
        /// Obtenir les séquences les plus fréquentes
        /// </summary>
        public List<MovementSequence> GetTopSequences(string context, int count = 10)
        {
            if (!_learnedSequences.ContainsKey(context))
                return new List<MovementSequence>();
            
            return _learnedSequences[context]
                .OrderByDescending(s => s.Frequency)
                .Take(count)
                .ToList();
        }
        
        /// <summary>
        /// Obtenir un rapport d'analyse
        /// </summary>
        public string GetAnalysisReport()
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("═══════════════════════════════════════");
            report.AppendLine("RAPPORT IA PRÉDICTIVE DE MOUVEMENT");
            report.AppendLine("═══════════════════════════════════════\n");
            
            report.AppendLine($"📊 Patterns enregistrés: {_recentPatterns.Count}");
            report.AppendLine($"🎯 Contextes appris: {_learnedSequences.Keys.Count}\n");
            
            foreach (var context in _learnedSequences.Keys)
            {
                report.AppendLine($"📁 CONTEXTE: {context}");
                report.AppendLine("─────────────────────────────────────");
                
                var topSequences = GetTopSequences(context, 5);
                foreach (var seq in topSequences)
                {
                    report.AppendLine($"  • {string.Join(" → ", seq.Actions)}");
                    report.AppendLine($"    Fréquence: {seq.Frequency} fois\n");
                }
            }
            
            return report.ToString();
        }
        
        /// <summary>
        /// Sauvegarder les patterns appris
        /// </summary>
        public void SaveLearning(string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(_learnedSequences, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(filePath, json);
            }
            catch { }
        }
        
        /// <summary>
        /// Charger les patterns appris
        /// </summary>
        public void LoadLearning(string filePath)
        {
            if (!File.Exists(filePath)) return;
            
            try
            {
                var json = File.ReadAllText(filePath);
                var sequences = JsonSerializer.Deserialize<Dictionary<string, List<MovementSequence>>>(json);
                
                if (sequences != null)
                {
                    _learnedSequences.Clear();
                    foreach (var kvp in sequences)
                    {
                        _learnedSequences[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch { }
        }
    }
}
