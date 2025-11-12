// CharacterGeneratorWindow.xaml.cs
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Trident.MITM
{
    public partial class CharacterGeneratorWindow : Window
    {
        private readonly Random _random = new Random();
        private readonly List<string> _weaponNames = new List<string>();
        
        // Noms de personnages Call of Duty inspirés
        private readonly string[] _firstNames = {
            "Alex", "Mason", "Woods", "Hudson", "Reznov", "Dimitri", "Viktor", 
            "Raul", "Frank", "Joseph", "John", "Simon", "Kyle", "Farah", "Hadir",
            "Gaz", "Soap", "Price", "Ghost", "Roach", "Nikolai", "Yuri", "Kamarov"
        };
        
        private readonly string[] _lastNames = {
            "Price", "MacTavish", "Riley", "Garrick", "Mitchell", "Turner", "Jackson",
            "Miller", "Davis", "Thompson", "Anderson", "Wilson", "Martinez", "Garcia",
            "Rodriguez", "Hernandez", "Lopez", "Gonzalez", "Perez", "Sanchez"
        };
        
        private readonly string[] _callsigns = {
            "Shadow", "Reaper", "Phantom", "Viper", "Titan", "Falcon", "Hawk",
            "Wolf", "Raven", "Hunter", "Nomad", "Warlock", "Spartan", "Bulldog",
            "Demon", "Phoenix", "Cobra", "Tiger", "Dragon", "Scorpion"
        };

        public CharacterGeneratorWindow()
        {
            InitializeComponent();
            Loaded += CharacterGeneratorWindow_Loaded;
        }

        private void CharacterGeneratorWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                EnsureThemeResources();
                LoadWeaponsFromConfig();
                PopulateWeaponDropdowns();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'initialisation : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnsureThemeResources()
        {
            if (!Application.Current.Resources.Contains("B_Bg0"))
            {
                Application.Current.Resources["B_Bg0"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F1116"));
                Application.Current.Resources["B_Bg1"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#151824"));
                Application.Current.Resources["B_Fg"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F2F8FF"));
                Application.Current.Resources["B_Sub"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94C3EA"));
            }
        }

        private void LoadWeaponsFromConfig()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trident.json");
                if (!File.Exists(configPath))
                {
                    // Fallback weapons
                    _weaponNames.AddRange(new[] { 
                        "XM4", "AK-74", "AMES 85", "GPR 91", "Model L", "Goblin Mk2",
                        "Krig CC", "C9", "KSV", "Jackal PDW", "Tanto .22", "PP-919"
                    });
                    return;
                }

                var json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("Profiles", out var profiles))
                {
                    foreach (var prop in profiles.EnumerateObject())
                    {
                        var weaponName = prop.Name;
                        if (weaponName != "— Choisir —" && !string.IsNullOrWhiteSpace(weaponName))
                        {
                            _weaponNames.Add(weaponName);
                        }
                    }
                }
                
                if (_weaponNames.Count == 0)
                {
                    _weaponNames.AddRange(new[] { 
                        "XM4", "AK-74", "AMES 85", "GPR 91", "Model L", "Goblin Mk2"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des armes : {ex.Message}");
                _weaponNames.AddRange(new[] { 
                    "XM4", "AK-74", "AMES 85", "GPR 91", "Model L", "Goblin Mk2"
                });
            }
        }

        private void PopulateWeaponDropdowns()
        {
            // Armes principales (fusils d'assaut, mitraillettes, fusils de précision)
            var primaryWeapons = _weaponNames.Where(w => 
                !w.Contains("GS45") && !w.Contains("Couteau") && !w.Contains("Pioche")).ToList();
            
            foreach (var weapon in primaryWeapons.OrderBy(w => w))
            {
                CmbPrimaryWeapon.Items.Add(new ComboBoxItem { Content = weapon });
            }
            
            // Armes secondaires (pistolets et mêlée)
            var secondaryWeapons = new[] { 
                "Pistolet 9mm", "Pistolet .45", "Revolver .357", "GS45",
                "Couteau de combat", "Hachette tactique", "Tonfa"
            };
            
            foreach (var weapon in secondaryWeapons)
            {
                CmbSecondaryWeapon.Items.Add(new ComboBoxItem { Content = weapon });
            }
            
            if (CmbPrimaryWeapon.Items.Count > 0)
                CmbPrimaryWeapon.SelectedIndex = 0;
            if (CmbSecondaryWeapon.Items.Count > 0)
                CmbSecondaryWeapon.SelectedIndex = 0;
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GenerateRandomCharacter();
                UpdatePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la génération : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateRandomCharacter()
        {
            // Nom
            var firstName = _firstNames[_random.Next(_firstNames.Length)];
            var lastName = _lastNames[_random.Next(_lastNames.Length)];
            var callsign = _callsigns[_random.Next(_callsigns.Length)];
            TxtCharacterName.Text = $"{firstName} \"{callsign}\" {lastName}";
            
            // Classe
            CmbClass.SelectedIndex = _random.Next(CmbClass.Items.Count);
            
            // Rang
            CmbRank.SelectedIndex = _random.Next(CmbRank.Items.Count);
            
            // Armes
            if (CmbPrimaryWeapon.Items.Count > 0)
                CmbPrimaryWeapon.SelectedIndex = _random.Next(CmbPrimaryWeapon.Items.Count);
            if (CmbSecondaryWeapon.Items.Count > 0)
                CmbSecondaryWeapon.SelectedIndex = _random.Next(CmbSecondaryWeapon.Items.Count);
            
            // Équipement
            CmbTactical.SelectedIndex = _random.Next(CmbTactical.Items.Count);
            CmbLethal.SelectedIndex = _random.Next(CmbLethal.Items.Count);
            
            // Perks
            CmbPerk1.SelectedIndex = _random.Next(CmbPerk1.Items.Count);
            CmbPerk2.SelectedIndex = _random.Next(CmbPerk2.Items.Count);
            CmbPerk3.SelectedIndex = _random.Next(CmbPerk3.Items.Count);
            
            // Killstreaks
            CmbKillstreak1.SelectedIndex = _random.Next(CmbKillstreak1.Items.Count);
            CmbKillstreak2.SelectedIndex = _random.Next(CmbKillstreak2.Items.Count);
            CmbKillstreak3.SelectedIndex = _random.Next(CmbKillstreak3.Items.Count);
            
            // Apparence
            CmbCamo.SelectedIndex = _random.Next(CmbCamo.Items.Count);
            CmbEmblem.SelectedIndex = _random.Next(CmbEmblem.Items.Count);
            
            // Statistiques (basées sur la classe)
            GenerateStats();
        }

        private void GenerateStats()
        {
            var classIndex = CmbClass.SelectedIndex;
            
            switch (classIndex)
            {
                case 0: // Assault
                    SetStat(BarSpeed, TxtSpeed, 75 + _random.Next(-10, 11));
                    SetStat(BarHealth, TxtHealth, 85 + _random.Next(-10, 11));
                    SetStat(BarArmor, TxtArmor, 70 + _random.Next(-10, 11));
                    SetStat(BarAccuracy, TxtAccuracy, 70 + _random.Next(-10, 11));
                    break;
                case 1: // Support
                    SetStat(BarSpeed, TxtSpeed, 65 + _random.Next(-10, 11));
                    SetStat(BarHealth, TxtHealth, 90 + _random.Next(-5, 6));
                    SetStat(BarArmor, TxtArmor, 85 + _random.Next(-5, 6));
                    SetStat(BarAccuracy, TxtAccuracy, 65 + _random.Next(-10, 11));
                    break;
                case 2: // Recon
                    SetStat(BarSpeed, TxtSpeed, 85 + _random.Next(-5, 6));
                    SetStat(BarHealth, TxtHealth, 70 + _random.Next(-10, 11));
                    SetStat(BarArmor, TxtArmor, 60 + _random.Next(-10, 11));
                    SetStat(BarAccuracy, TxtAccuracy, 95 + _random.Next(-5, 6));
                    break;
                case 3: // Engineer
                    SetStat(BarSpeed, TxtSpeed, 70 + _random.Next(-10, 11));
                    SetStat(BarHealth, TxtHealth, 75 + _random.Next(-10, 11));
                    SetStat(BarArmor, TxtArmor, 75 + _random.Next(-10, 11));
                    SetStat(BarAccuracy, TxtAccuracy, 75 + _random.Next(-10, 11));
                    break;
                default:
                    SetStat(BarSpeed, TxtSpeed, 70 + _random.Next(-15, 16));
                    SetStat(BarHealth, TxtHealth, 80 + _random.Next(-15, 16));
                    SetStat(BarArmor, TxtArmor, 70 + _random.Next(-15, 16));
                    SetStat(BarAccuracy, TxtAccuracy, 75 + _random.Next(-15, 16));
                    break;
            }
        }

        private void SetStat(System.Windows.Controls.Primitives.RangeBase bar, TextBlock txt, int value)
        {
            value = Math.Clamp(value, 0, 100);
            bar.Value = value;
            txt.Text = value.ToString();
        }

        private void UpdatePreview()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"═══════════════════════════════════════");
            sb.AppendLine($"   {TxtCharacterName.Text}");
            sb.AppendLine($"═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Classe : {GetComboText(CmbClass)}");
            sb.AppendLine($"Rang : {GetComboText(CmbRank)}");
            sb.AppendLine();
            sb.AppendLine($"🔫 ARMEMENT");
            sb.AppendLine($"  Principale : {GetComboText(CmbPrimaryWeapon)}");
            sb.AppendLine($"  Secondaire : {GetComboText(CmbSecondaryWeapon)}");
            sb.AppendLine($"  Tactique : {GetComboText(CmbTactical)}");
            sb.AppendLine($"  Létal : {GetComboText(CmbLethal)}");
            sb.AppendLine();
            sb.AppendLine($"⭐ PERKS");
            sb.AppendLine($"  1. {GetComboText(CmbPerk1)}");
            sb.AppendLine($"  2. {GetComboText(CmbPerk2)}");
            sb.AppendLine($"  3. {GetComboText(CmbPerk3)}");
            sb.AppendLine();
            sb.AppendLine($"🎯 SÉRIES D'ÉLIMINATION");
            sb.AppendLine($"  3 kills : {GetComboText(CmbKillstreak1)}");
            sb.AppendLine($"  5 kills : {GetComboText(CmbKillstreak2)}");
            sb.AppendLine($"  7 kills : {GetComboText(CmbKillstreak3)}");
            sb.AppendLine();
            sb.AppendLine($"📊 STATISTIQUES");
            sb.AppendLine($"  Vitesse : {TxtSpeed.Text}/100");
            sb.AppendLine($"  Santé : {TxtHealth.Text}/100");
            sb.AppendLine($"  Armure : {TxtArmor.Text}/100");
            sb.AppendLine($"  Précision : {TxtAccuracy.Text}/100");
            sb.AppendLine();
            sb.AppendLine($"👤 APPARENCE");
            sb.AppendLine($"  Camouflage : {GetComboText(CmbCamo)}");
            sb.AppendLine($"  Insigne : {GetComboText(CmbEmblem)}");
            
            TxtPreview.Text = sb.ToString();
        }

        private string GetComboText(ComboBox combo)
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Content is string text)
                return text;
            return "N/A";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtCharacterName.Text))
                {
                    MessageBox.Show("Veuillez générer ou entrer un nom de personnage.", 
                        "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var character = CreateCharacterObject();
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var charactersDir = Path.Combine(documentsPath, "Trident_Characters");
                Directory.CreateDirectory(charactersDir);
                
                var fileName = $"{SanitizeFileName(TxtCharacterName.Text)}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(charactersDir, fileName);
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(character, options);
                File.WriteAllText(filePath, json);
                
                MessageBox.Show($"Personnage sauvegardé avec succès !\n\n{filePath}", 
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtCharacterName.Text))
                {
                    MessageBox.Show("Veuillez générer ou entrer un nom de personnage.", 
                        "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var sfd = new SaveFileDialog
                {
                    Filter = "JSON (*.json)|*.json|Texte (*.txt)|*.txt|Tous les fichiers|*.*",
                    FileName = $"{SanitizeFileName(TxtCharacterName.Text)}.json",
                    DefaultExt = "json"
                };
                
                if (sfd.ShowDialog(this) != true) return;
                
                if (sfd.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    // Export en format texte
                    File.WriteAllText(sfd.FileName, TxtPreview.Text);
                }
                else
                {
                    // Export en JSON
                    var character = CreateCharacterObject();
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var json = JsonSerializer.Serialize(character, options);
                    File.WriteAllText(sfd.FileName, json);
                }
                
                MessageBox.Show($"Personnage exporté avec succès !\n\n{sfd.FileName}", 
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'exportation : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Dictionary<string, object> CreateCharacterObject()
        {
            return new Dictionary<string, object>
            {
                { "Name", TxtCharacterName.Text },
                { "Class", GetComboText(CmbClass) },
                { "Rank", GetComboText(CmbRank) },
                { "PrimaryWeapon", GetComboText(CmbPrimaryWeapon) },
                { "SecondaryWeapon", GetComboText(CmbSecondaryWeapon) },
                { "Tactical", GetComboText(CmbTactical) },
                { "Lethal", GetComboText(CmbLethal) },
                { "Perk1", GetComboText(CmbPerk1) },
                { "Perk2", GetComboText(CmbPerk2) },
                { "Perk3", GetComboText(CmbPerk3) },
                { "Killstreak1", GetComboText(CmbKillstreak1) },
                { "Killstreak2", GetComboText(CmbKillstreak2) },
                { "Killstreak3", GetComboText(CmbKillstreak3) },
                { "Camouflage", GetComboText(CmbCamo) },
                { "Emblem", GetComboText(CmbEmblem) },
                { "Stats", new Dictionary<string, int>
                    {
                        { "Speed", (int)BarSpeed.Value },
                        { "Health", (int)BarHealth.Value },
                        { "Armor", (int)BarArmor.Value },
                        { "Accuracy", (int)BarAccuracy.Value }
                    }
                },
                { "CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            };
        }

        private string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Trim().Replace(" ", "_");
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
