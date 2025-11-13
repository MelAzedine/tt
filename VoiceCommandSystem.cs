// VoiceCommandSystem.cs — Contrôle vocal pour changer de profils/armes sans lâcher la manette
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Threading.Tasks;

namespace Trident.MITM
{
    /// <summary>
    /// Système de commandes vocales pour contrôler l'application sans les mains
    /// Parfait pour changer de profil/arme pendant le jeu sans lâcher la manette
    /// </summary>
    public class VoiceCommandSystem : IDisposable
    {
        private SpeechRecognitionEngine? _recognizer;
        private bool _isListening = false;
        private readonly Dictionary<string, Action> _commands = new();
        
        public event Action<string>? CommandRecognized;
        public event Action<string>? CommandExecuted;
        public event Action<double>? ConfidenceReported;
        
        public bool IsListening => _isListening;
        
        /// <summary>
        /// Initialiser le système de reconnaissance vocale
        /// </summary>
        public void Initialize()
        {
            try
            {
                _recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("fr-FR"));
                
                // Créer les grammaires
                var weaponGrammar = CreateWeaponGrammar();
                var profileGrammar = CreateProfileGrammar();
                var featureGrammar = CreateFeatureGrammar();
                var utilityGrammar = CreateUtilityGrammar();
                
                _recognizer.LoadGrammar(weaponGrammar);
                _recognizer.LoadGrammar(profileGrammar);
                _recognizer.LoadGrammar(featureGrammar);
                _recognizer.LoadGrammar(utilityGrammar);
                
                // Événements
                _recognizer.SpeechRecognized += OnSpeechRecognized;
                _recognizer.SpeechRecognitionRejected += OnSpeechRejected;
                
                // Utiliser le micro par défaut
                _recognizer.SetInputToDefaultAudioDevice();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur d'initialisation vocale: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Grammaire pour les armes
        /// </summary>
        private Grammar CreateWeaponGrammar()
        {
            var weapons = new Choices(
                "fusil d'assaut", "AR", "assault rifle",
                "sniper", "fusil de précision",
                "SMG", "mitraillette",
                "shotgun", "fusil à pompe",
                "pistolet", "pistol",
                "DMR", "marksman",
                "LMG", "mitrailleuse"
            );
            
            var prefix = new Choices("arme", "weapon", "équipe", "switch to");
            
            var gb = new GrammarBuilder();
            gb.Append(prefix);
            gb.Append(weapons);
            
            return new Grammar(gb) { Name = "WeaponGrammar" };
        }
        
        /// <summary>
        /// Grammaire pour les profils de jeu
        /// </summary>
        private Grammar CreateProfileGrammar()
        {
            var profiles = new Choices(
                "Fortnite", "fortnite",
                "Call of Duty", "COD", "Warzone",
                "Apex", "Apex Legends",
                "Valorant",
                "Battlefield",
                "par défaut", "default"
            );
            
            var prefix = new Choices("profil", "profile", "mode", "game");
            
            var gb = new GrammarBuilder();
            gb.Append(prefix);
            gb.Append(profiles);
            
            return new Grammar(gb) { Name = "ProfileGrammar" };
        }
        
        /// <summary>
        /// Grammaire pour activer/désactiver les fonctionnalités
        /// </summary>
        private Grammar CreateFeatureGrammar()
        {
            var actions = new Choices("active", "désactive", "enable", "disable");
            
            var features = new Choices(
                "anti-recul", "anti-recoil", "recoil",
                "aim assist", "assistance visée",
                "rapid fire", "tir rapide",
                "auto ping", "ping auto",
                "macro", "macros"
            );
            
            var gb = new GrammarBuilder();
            gb.Append(actions);
            gb.Append(features);
            
            return new Grammar(gb) { Name = "FeatureGrammar" };
        }
        
        /// <summary>
        /// Grammaire pour les commandes utilitaires
        /// </summary>
        private Grammar CreateUtilityGrammar()
        {
            var commands = new Choices(
                "affiche batterie", "show battery",
                "affiche performance", "show performance",
                "affiche overlay", "show overlay",
                "cache overlay", "hide overlay",
                "sauvegarde profil", "save profile",
                "charge profil", "load profile",
                "rapport aim", "aim report",
                "aide", "help"
            );
            
            return new Grammar(new GrammarBuilder(commands)) { Name = "UtilityGrammar" };
        }
        
        /// <summary>
        /// Démarrer l'écoute
        /// </summary>
        public void StartListening()
        {
            if (_recognizer == null)
                throw new InvalidOperationException("Le système vocal n'est pas initialisé");
            
            if (_isListening) return;
            
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            _isListening = true;
        }
        
        /// <summary>
        /// Arrêter l'écoute
        /// </summary>
        public void StopListening()
        {
            if (_recognizer == null || !_isListening) return;
            
            _recognizer.RecognizeAsyncStop();
            _isListening = false;
        }
        
        /// <summary>
        /// Enregistrer une commande personnalisée
        /// </summary>
        public void RegisterCommand(string commandText, Action action)
        {
            _commands[commandText.ToLowerInvariant()] = action;
        }
        
        /// <summary>
        /// Événement de reconnaissance réussie
        /// </summary>
        private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            var confidence = e.Result.Confidence;
            ConfidenceReported?.Invoke(confidence);
            
            // Seuil de confiance minimum
            if (confidence < 0.7) return;
            
            var text = e.Result.Text.ToLowerInvariant();
            CommandRecognized?.Invoke(text);
            
            // Exécuter la commande si elle est enregistrée
            if (_commands.TryGetValue(text, out var action))
            {
                Task.Run(() =>
                {
                    action.Invoke();
                    CommandExecuted?.Invoke(text);
                });
            }
            else
            {
                // Tentative d'exécution basée sur le contenu
                ExecuteBuiltInCommand(text);
            }
        }
        
        /// <summary>
        /// Événement de reconnaissance échouée
        /// </summary>
        private void OnSpeechRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
        {
            ConfidenceReported?.Invoke(0);
        }
        
        /// <summary>
        /// Exécuter les commandes intégrées
        /// </summary>
        private void ExecuteBuiltInCommand(string text)
        {
            // Armes
            if (text.Contains("assault") || text.Contains("fusil d'assaut") || text.Contains("ar"))
                CommandExecuted?.Invoke("switch_weapon:AR");
            else if (text.Contains("sniper") || text.Contains("précision"))
                CommandExecuted?.Invoke("switch_weapon:Sniper");
            else if (text.Contains("smg") || text.Contains("mitraillette"))
                CommandExecuted?.Invoke("switch_weapon:SMG");
            else if (text.Contains("shotgun") || text.Contains("pompe"))
                CommandExecuted?.Invoke("switch_weapon:Shotgun");
            
            // Profils
            else if (text.Contains("fortnite"))
                CommandExecuted?.Invoke("switch_profile:Fortnite");
            else if (text.Contains("cod") || text.Contains("call of duty") || text.Contains("warzone"))
                CommandExecuted?.Invoke("switch_profile:COD");
            else if (text.Contains("apex"))
                CommandExecuted?.Invoke("switch_profile:Apex");
            else if (text.Contains("valorant"))
                CommandExecuted?.Invoke("switch_profile:Valorant");
            
            // Fonctionnalités
            else if (text.Contains("active") || text.Contains("enable"))
            {
                if (text.Contains("recul") || text.Contains("recoil"))
                    CommandExecuted?.Invoke("enable:anti-recoil");
                else if (text.Contains("aim"))
                    CommandExecuted?.Invoke("enable:aim-assist");
                else if (text.Contains("rapid") || text.Contains("rapide"))
                    CommandExecuted?.Invoke("enable:rapid-fire");
                else if (text.Contains("ping"))
                    CommandExecuted?.Invoke("enable:auto-ping");
            }
            else if (text.Contains("désactive") || text.Contains("disable"))
            {
                if (text.Contains("recul") || text.Contains("recoil"))
                    CommandExecuted?.Invoke("disable:anti-recoil");
                else if (text.Contains("aim"))
                    CommandExecuted?.Invoke("disable:aim-assist");
                else if (text.Contains("rapid") || text.Contains("rapide"))
                    CommandExecuted?.Invoke("disable:rapid-fire");
                else if (text.Contains("ping"))
                    CommandExecuted?.Invoke("disable:auto-ping");
            }
            
            // Utilitaires
            else if (text.Contains("batterie") || text.Contains("battery"))
                CommandExecuted?.Invoke("show:battery");
            else if (text.Contains("performance"))
                CommandExecuted?.Invoke("show:performance");
            else if (text.Contains("rapport") && text.Contains("aim"))
                CommandExecuted?.Invoke("show:aim-report");
            else if (text.Contains("aide") || text.Contains("help"))
                CommandExecuted?.Invoke("show:help");
        }
        
        /// <summary>
        /// Obtenir la liste des commandes disponibles
        /// </summary>
        public List<string> GetAvailableCommands()
        {
            return new List<string>
            {
                // Armes
                "Arme fusil d'assaut", "Arme sniper", "Arme SMG", "Arme shotgun",
                
                // Profils
                "Profil Fortnite", "Profil Call of Duty", "Profil Apex", "Profil Valorant",
                
                // Fonctionnalités
                "Active anti-recul", "Désactive anti-recul",
                "Active aim assist", "Désactive aim assist",
                "Active rapid fire", "Désactive rapid fire",
                "Active auto ping", "Désactive auto ping",
                
                // Utilitaires
                "Affiche batterie", "Affiche performance", "Rapport aim", "Aide"
            };
        }
        
        /// <summary>
        /// Obtenir l'aide vocale
        /// </summary>
        public string GetVoiceHelp()
        {
            return @"
🎤 COMMANDES VOCALES DISPONIBLES
═══════════════════════════════════

🔫 ARMES
  • ""Arme fusil d'assaut"" / ""Arme AR""
  • ""Arme sniper"" / ""Arme fusil de précision""
  • ""Arme SMG"" / ""Arme mitraillette""
  • ""Arme shotgun"" / ""Arme fusil à pompe""

🎮 PROFILS
  • ""Profil Fortnite""
  • ""Profil Call of Duty"" / ""Profil COD""
  • ""Profil Apex"" / ""Profil Apex Legends""
  • ""Profil Valorant""
  • ""Profil Battlefield""

⚙️ FONCTIONNALITÉS
  • ""Active anti-recul"" / ""Désactive anti-recul""
  • ""Active aim assist"" / ""Désactive aim assist""
  • ""Active rapid fire"" / ""Désactive rapid fire""
  • ""Active auto ping"" / ""Désactive auto ping""

🛠️ UTILITAIRES
  • ""Affiche batterie"" - Voir le niveau de batterie
  • ""Affiche performance"" - Voir les performances
  • ""Rapport aim"" - Voir l'analyse d'aim
  • ""Aide"" - Afficher cette aide

💡 CONSEILS
  • Parlez clairement et pas trop vite
  • Utilisez un micro de qualité
  • Évitez le bruit de fond
  • Confiance minimum: 70%
";
        }
        
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            StopListening();
            _recognizer?.Dispose();
        }
    }
}
