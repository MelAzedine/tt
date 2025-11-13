// SessionRecordingSystem.cs — Enregistrement et replay des sessions de jeu
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Trident.MITM
{
    /// <summary>
    /// Système d'enregistrement et de replay des sessions de jeu
    /// Permet d'analyser ses performances, partager ses highlights, et s'améliorer
    /// </summary>
    public class SessionRecordingSystem
    {
        private List<InputFrame> _currentRecording = new();
        private bool _isRecording = false;
        private DateTime _recordingStartTime;
        private string _currentSessionName = "";
        
        public event Action<string>? RecordingStarted;
        public event Action<RecordingSession>? RecordingStopped;
        public event Action<InputFrame>? FrameRecorded;
        
        /// <summary>
        /// Frame d'input enregistré
        /// </summary>
        public class InputFrame
        {
            public long TimestampMs { get; set; }
            public double LeftStickX { get; set; }
            public double LeftStickY { get; set; }
            public double RightStickX { get; set; }
            public double RightStickY { get; set; }
            public double LeftTrigger { get; set; }
            public double RightTrigger { get; set; }
            public Dictionary<string, bool> Buttons { get; set; } = new();
            
            // Métadonnées
            public bool IsAiming { get; set; }
            public bool IsShooting { get; set; }
            public string? CurrentWeapon { get; set; }
            public double AccuracyScore { get; set; }
        }
        
        /// <summary>
        /// Session enregistrée complète
        /// </summary>
        public class RecordingSession
        {
            public string SessionId { get; set; } = Guid.NewGuid().ToString();
            public string Name { get; set; } = "";
            public string GameName { get; set; } = "";
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public TimeSpan Duration => EndTime - StartTime;
            
            public List<InputFrame> Frames { get; set; } = new();
            
            // Statistiques
            public SessionStatistics Statistics { get; set; } = new();
            
            // Métadonnées
            public Dictionary<string, string> Metadata { get; set; } = new();
        }
        
        /// <summary>
        /// Statistiques de session
        /// </summary>
        public class SessionStatistics
        {
            public int TotalFrames { get; set; }
            public int TotalShots { get; set; }
            public double AverageAccuracy { get; set; }
            public double AimingTime { get; set; } // en secondes
            public double MovementDistance { get; set; }
            
            // Inputs
            public Dictionary<string, int> ButtonPresses { get; set; } = new();
            public double AverageRightStickUsage { get; set; }
            public double MaxStickMovement { get; set; }
            
            // Performance
            public double BestAccuracyStreak { get; set; }
            public int LongestComboLength { get; set; }
            public List<string> WeaponsUsed { get; set; } = new();
        }
        
        /// <summary>
        /// Démarrer l'enregistrement
        /// </summary>
        public void StartRecording(string sessionName, string gameName = "")
        {
            if (_isRecording) return;
            
            _currentRecording.Clear();
            _currentSessionName = sessionName;
            _recordingStartTime = DateTime.Now;
            _isRecording = true;
            
            RecordingStarted?.Invoke(sessionName);
        }
        
        /// <summary>
        /// Arrêter l'enregistrement
        /// </summary>
        public RecordingSession StopRecording()
        {
            if (!_isRecording) 
                throw new InvalidOperationException("Aucun enregistrement en cours");
            
            _isRecording = false;
            
            var session = new RecordingSession
            {
                Name = _currentSessionName,
                StartTime = _recordingStartTime,
                EndTime = DateTime.Now,
                Frames = new List<InputFrame>(_currentRecording)
            };
            
            // Calculer les statistiques
            session.Statistics = CalculateStatistics(session);
            
            RecordingStopped?.Invoke(session);
            
            return session;
        }
        
        /// <summary>
        /// Enregistrer une frame
        /// </summary>
        public void RecordFrame(
            double lx, double ly, double rx, double ry,
            double lt, double rt,
            Dictionary<string, bool> buttons,
            bool isAiming = false,
            bool isShooting = false,
            string? currentWeapon = null,
            double accuracy = 0)
        {
            if (!_isRecording) return;
            
            var timestamp = (long)(DateTime.Now - _recordingStartTime).TotalMilliseconds;
            
            var frame = new InputFrame
            {
                TimestampMs = timestamp,
                LeftStickX = lx,
                LeftStickY = ly,
                RightStickX = rx,
                RightStickY = ry,
                LeftTrigger = lt,
                RightTrigger = rt,
                Buttons = new Dictionary<string, bool>(buttons),
                IsAiming = isAiming,
                IsShooting = isShooting,
                CurrentWeapon = currentWeapon,
                AccuracyScore = accuracy
            };
            
            _currentRecording.Add(frame);
            FrameRecorded?.Invoke(frame);
        }
        
        /// <summary>
        /// Calculer les statistiques d'une session
        /// </summary>
        private SessionStatistics CalculateStatistics(RecordingSession session)
        {
            var stats = new SessionStatistics
            {
                TotalFrames = session.Frames.Count
            };
            
            if (session.Frames.Count == 0) return stats;
            
            // Compter les tirs et précision
            var shots = session.Frames.Where(f => f.IsShooting).ToList();
            stats.TotalShots = shots.Count;
            if (shots.Any())
                stats.AverageAccuracy = shots.Average(f => f.AccuracyScore);
            
            // Temps de visée
            var aimingFrames = session.Frames.Count(f => f.IsAiming);
            stats.AimingTime = (aimingFrames * 16.0) / 1000.0; // Assumant 60 FPS
            
            // Boutons pressés
            foreach (var frame in session.Frames)
            {
                foreach (var button in frame.Buttons.Where(b => b.Value))
                {
                    if (!stats.ButtonPresses.ContainsKey(button.Key))
                        stats.ButtonPresses[button.Key] = 0;
                    stats.ButtonPresses[button.Key]++;
                }
            }
            
            // Usage du stick droit (aim)
            var rightStickUsage = session.Frames
                .Select(f => Math.Sqrt(f.RightStickX * f.RightStickX + f.RightStickY * f.RightStickY))
                .Where(m => m > 0.01);
            
            if (rightStickUsage.Any())
            {
                stats.AverageRightStickUsage = rightStickUsage.Average();
                stats.MaxStickMovement = rightStickUsage.Max();
            }
            
            // Distance de mouvement
            double totalDistance = 0;
            for (int i = 1; i < session.Frames.Count; i++)
            {
                var prev = session.Frames[i - 1];
                var curr = session.Frames[i];
                
                double dx = curr.LeftStickX - prev.LeftStickX;
                double dy = curr.LeftStickY - prev.LeftStickY;
                totalDistance += Math.Sqrt(dx * dx + dy * dy);
            }
            stats.MovementDistance = totalDistance;
            
            // Armes utilisées
            stats.WeaponsUsed = session.Frames
                .Where(f => f.CurrentWeapon != null)
                .Select(f => f.CurrentWeapon!)
                .Distinct()
                .ToList();
            
            // Meilleure streak de précision
            double currentStreak = 0;
            double bestStreak = 0;
            foreach (var frame in session.Frames.Where(f => f.IsShooting))
            {
                if (frame.AccuracyScore > 0.7)
                    currentStreak++;
                else
                {
                    bestStreak = Math.Max(bestStreak, currentStreak);
                    currentStreak = 0;
                }
            }
            stats.BestAccuracyStreak = Math.Max(bestStreak, currentStreak);
            
            return stats;
        }
        
        /// <summary>
        /// Sauvegarder une session (compressée)
        /// </summary>
        public async Task SaveSession(RecordingSession session, string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(session, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });
                
                // Compresser avec GZip pour économiser l'espace
                using var fileStream = File.Create(filePath);
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
                using var writer = new StreamWriter(gzipStream);
                await writer.WriteAsync(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur de sauvegarde: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Charger une session
        /// </summary>
        public async Task<RecordingSession> LoadSession(string filePath)
        {
            try
            {
                using var fileStream = File.OpenRead(filePath);
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzipStream);
                var json = await reader.ReadToEndAsync();
                
                var session = JsonSerializer.Deserialize<RecordingSession>(json);
                if (session == null)
                    throw new Exception("Session invalide");
                
                return session;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur de chargement: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Rejouer une session (frame par frame)
        /// </summary>
        public async Task ReplaySession(RecordingSession session, Action<InputFrame> onFrame)
        {
            var startTime = DateTime.Now;
            
            foreach (var frame in session.Frames)
            {
                // Attendre jusqu'au bon timing
                var targetTime = startTime.AddMilliseconds(frame.TimestampMs);
                var delay = targetTime - DateTime.Now;
                
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay);
                
                onFrame(frame);
            }
        }
        
        /// <summary>
        /// Exporter une session vers un format texte lisible
        /// </summary>
        public string ExportToReadableFormat(RecordingSession session)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"SESSION: {session.Name}");
            sb.AppendLine($"ID: {session.SessionId}");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            
            sb.AppendLine("📅 INFORMATIONS");
            sb.AppendLine($"Début: {session.StartTime:g}");
            sb.AppendLine($"Fin: {session.EndTime:g}");
            sb.AppendLine($"Durée: {session.Duration.TotalMinutes:F1} minutes");
            sb.AppendLine();
            
            var stats = session.Statistics;
            sb.AppendLine("📊 STATISTIQUES");
            sb.AppendLine($"Frames totales: {stats.TotalFrames:N0}");
            sb.AppendLine($"Tirs effectués: {stats.TotalShots:N0}");
            sb.AppendLine($"Précision moyenne: {stats.AverageAccuracy:P1}");
            sb.AppendLine($"Temps de visée: {stats.AimingTime:F1}s");
            sb.AppendLine($"Distance parcourue: {stats.MovementDistance:F1} unités");
            sb.AppendLine($"Meilleure streak: {stats.BestAccuracyStreak:F0} tirs");
            sb.AppendLine();
            
            if (stats.WeaponsUsed.Any())
            {
                sb.AppendLine("🔫 ARMES UTILISÉES");
                foreach (var weapon in stats.WeaponsUsed)
                    sb.AppendLine($"  • {weapon}");
                sb.AppendLine();
            }
            
            if (stats.ButtonPresses.Any())
            {
                sb.AppendLine("🎮 BOUTONS LES PLUS UTILISÉS");
                var topButtons = stats.ButtonPresses
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(10);
                
                foreach (var button in topButtons)
                    sb.AppendLine($"  • {button.Key}: {button.Value:N0} fois");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Créer un highlight (extrait) d'une session
        /// </summary>
        public RecordingSession CreateHighlight(RecordingSession session, long startMs, long endMs, string highlightName)
        {
            var frames = session.Frames
                .Where(f => f.TimestampMs >= startMs && f.TimestampMs <= endMs)
                .ToList();
            
            // Ajuster les timestamps pour commencer à 0
            foreach (var frame in frames)
                frame.TimestampMs -= startMs;
            
            var highlight = new RecordingSession
            {
                Name = highlightName,
                GameName = session.GameName,
                StartTime = session.StartTime.AddMilliseconds(startMs),
                EndTime = session.StartTime.AddMilliseconds(endMs),
                Frames = frames
            };
            
            highlight.Statistics = CalculateStatistics(highlight);
            
            return highlight;
        }
        
        /// <summary>
        /// Comparer deux sessions
        /// </summary>
        public SessionComparison CompareSessions(RecordingSession session1, RecordingSession session2)
        {
            return new SessionComparison
            {
                AccuracyDifference = session2.Statistics.AverageAccuracy - session1.Statistics.AverageAccuracy,
                ShotsDifference = session2.Statistics.TotalShots - session1.Statistics.TotalShots,
                AimTimeDifference = session2.Statistics.AimingTime - session1.Statistics.AimingTime,
                MovementDifference = session2.Statistics.MovementDistance - session1.Statistics.MovementDistance
            };
        }
        
        public class SessionComparison
        {
            public double AccuracyDifference { get; set; }
            public int ShotsDifference { get; set; }
            public double AimTimeDifference { get; set; }
            public double MovementDifference { get; set; }
            
            public string GetReport()
            {
                return $@"
📊 COMPARAISON DE SESSIONS
═════════════════════════════

Précision: {(AccuracyDifference > 0 ? "+" : "")}{AccuracyDifference:P1}
Tirs: {(ShotsDifference > 0 ? "+" : "")}{ShotsDifference}
Temps de visée: {(AimTimeDifference > 0 ? "+" : "")}{AimTimeDifference:F1}s
Mouvement: {(MovementDifference > 0 ? "+" : "")}{MovementDifference:F1} unités
";
            }
        }
    }
}
