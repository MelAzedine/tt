// LicenseWindow.xaml.cs — init & login KeyAuth v1.3 (sans secret)
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Security.Cryptography;

namespace Trident.MITM
{
    public partial class LicenseWindow : Window
    {
        // REMPLIS ICI
        private const string AppName = "ArthWindows";
        private const string OwnerId = "NEDJeXShHq";
        private const string Version = "1.0";  // la même que celle de ton app
        private const string ApiBase = "https://keyauth.win/api/1.3/";

        private static readonly HttpClient http = new HttpClient();
        private string? _sessionId;
        public string? ValidatedKey { get; private set; }
        public DateTime? ExpirationDate { get; private set; }
        private bool _loginPasswordVisible = false;
        private bool _registerPasswordVisible = false;

        public LicenseWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                LoadSavedCredentials();
                LoginUsernameTextBox.Focus();
                await InitKeyAuthAsync();
            };
        }

        // ---------------------- SAVE/LOAD CREDENTIALS ----------------------
        private void SaveCredentials(string username, string password)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\ArthemisControl\Credentials"))
                {
                    if (key != null)
                    {
                        key.SetValue("Username", username);
                        // Chiffrer le mot de passe (simple base64 pour l'instant, à améliorer avec un vrai chiffrement)
                        var passwordBytes = Encoding.UTF8.GetBytes(password);
                        var encryptedPassword = Convert.ToBase64String(passwordBytes);
                        key.SetValue("Password", encryptedPassword);
                        key.SetValue("RememberCredentials", true);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignorer les erreurs de sauvegarde
                System.Diagnostics.Debug.WriteLine($"Erreur sauvegarde credentials: {ex.Message}");
            }
        }

        private void LoadSavedCredentials()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\ArthemisControl\Credentials"))
                {
                    if (key != null)
                    {
                        var remember = key.GetValue("RememberCredentials");
                        if (remember != null && (bool)remember)
                        {
                            var username = key.GetValue("Username") as string;
                            var encryptedPassword = key.GetValue("Password") as string;

                            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(encryptedPassword))
                            {
                                LoginUsernameTextBox.Text = username;
                                // Déchiffrer le mot de passe
                                try
                                {
                                    var passwordBytes = Convert.FromBase64String(encryptedPassword);
                                    var password = Encoding.UTF8.GetString(passwordBytes);
                                    LoginPasswordBox.Password = password;
                                    LoginPasswordTextBox.Text = password;
                                    // Mettre le focus sur le bouton de connexion pour faciliter la connexion rapide
                                    Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        BtnLogin.Focus();
                                        // Permettre la connexion avec Entrée
                                        BtnLogin.IsDefault = true;
                                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                                }
                                catch
                                {
                                    // Ignorer les erreurs de déchiffrement
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignorer les erreurs de chargement
                System.Diagnostics.Debug.WriteLine($"Erreur chargement credentials: {ex.Message}");
            }
        }

        // ---------------------- PASSWORD TOGGLE ----------------------
        private void LoginPasswordToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _loginPasswordVisible = !_loginPasswordVisible;
            if (_loginPasswordVisible)
            {
                LoginPasswordTextBox.Text = LoginPasswordBox.Password;
                LoginPasswordBox.Visibility = Visibility.Collapsed;
                LoginPasswordTextBox.Visibility = Visibility.Visible;
                LoginPasswordTextBox.Focus();
                LoginPasswordToggleButton.Content = "🙈";
            }
            else
            {
                LoginPasswordBox.Password = LoginPasswordTextBox.Text;
                LoginPasswordTextBox.Visibility = Visibility.Collapsed;
                LoginPasswordBox.Visibility = Visibility.Visible;
                LoginPasswordBox.Focus();
                LoginPasswordToggleButton.Content = "👁";
            }
        }

        private void RegisterPasswordToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _registerPasswordVisible = !_registerPasswordVisible;
            if (_registerPasswordVisible)
            {
                RegisterPasswordTextBox.Text = RegisterPasswordBox.Password;
                RegisterPasswordBox.Visibility = Visibility.Collapsed;
                RegisterPasswordTextBox.Visibility = Visibility.Visible;
                RegisterPasswordTextBox.Focus();
                RegisterPasswordToggleButton.Content = "🙈";
            }
            else
            {
                RegisterPasswordBox.Password = RegisterPasswordTextBox.Text;
                RegisterPasswordTextBox.Visibility = Visibility.Collapsed;
                RegisterPasswordBox.Visibility = Visibility.Visible;
                RegisterPasswordBox.Focus();
                RegisterPasswordToggleButton.Content = "👁";
            }
        }

        private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Toujours synchroniser les deux champs pour éviter les problèmes
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null && !string.IsNullOrEmpty(passwordBox.Password))
            {
                LoginPasswordTextBox.Text = passwordBox.Password;
            }
        }

        private void LoginPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Toujours synchroniser les deux champs pour éviter les problèmes
            var textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                LoginPasswordBox.Password = textBox.Text;
            }
        }

        private void RegisterPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Toujours synchroniser les deux champs pour éviter les problèmes
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null && !string.IsNullOrEmpty(passwordBox.Password))
            {
                RegisterPasswordTextBox.Text = passwordBox.Password;
            }
        }

        private void RegisterPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Toujours synchroniser les deux champs pour éviter les problèmes
            var textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                RegisterPasswordBox.Password = textBox.Text;
            }
        }

        // ---------------------- KEY HANDLERS ----------------------
        private void LoginUsernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && BtnLogin.IsEnabled)
            {
                LoginPasswordBox.Focus();
                e.Handled = true;
            }
        }

        private void LoginPasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && BtnLogin.IsEnabled)
            {
                LoginButton_Click(BtnLogin, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void LoginPasswordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && BtnLogin.IsEnabled)
            {
                LoginButton_Click(BtnLogin, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        // ---------------------- INIT ----------------------
        private async Task InitKeyAuthAsync()
        {
            try
            {
                StatusText.Text = LocalizationManager.GetString("Initializing");
                // hash exe optionnel -> vide si tu n'utilises pas l'App Hash dans KeyAuth
                var url = $"{ApiBase}?type=init&ver={Uri.EscapeDataString(Version)}" +
                          $"&name={Uri.EscapeDataString(AppName)}" +
                          $"&ownerid={Uri.EscapeDataString(OwnerId)}" +
                          $"&hash=";

                using var resp = await http.GetAsync(url).ConfigureAwait(true);
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(true);
                
                // KeyAuth peut retourner des réponses texte simples comme "KeyAuth_Invalid"
                if (string.IsNullOrWhiteSpace(json))
                {
                    StatusText.Text = "Init KeyAuth échouée : Réponse vide";
                    BtnLogin.IsEnabled = false;
                    BtnRegister.IsEnabled = false;
                    return;
                }

                // Si la réponse n'est pas du JSON, c'est probablement une erreur KeyAuth
                if (!json.TrimStart().StartsWith("{"))
                {
                    var errorMsg = json.Trim();
                    if (errorMsg == "KeyAuth_Invalid")
                    {
                        StatusText.Text = "Init KeyAuth échouée : Configuration KeyAuth invalide. Vérifiez AppName, OwnerId et Version.";
                    }
                    else
                    {
                        StatusText.Text = $"Init KeyAuth échouée : {errorMsg}";
                    }
                    BtnLogin.IsEnabled = false;
                    BtnRegister.IsEnabled = false;
                    return;
                }

                KeyAuthReply? data = null;
                try
                {
                    data = JsonSerializer.Deserialize<KeyAuthReply>(json);
                }
                catch (JsonException jsonEx)
                {
                    StatusText.Text = $"Init KeyAuth échouée : Erreur parsing JSON - {jsonEx.Message}";
                    BtnLogin.IsEnabled = false;
                    BtnRegister.IsEnabled = false;
                    return;
                }

                if (data == null)
                {
                    StatusText.Text = "Init KeyAuth échouée : Réponse nulle";
                    BtnLogin.IsEnabled = false;
                    BtnRegister.IsEnabled = false;
                    return;
                }

                if (data.success && !string.IsNullOrWhiteSpace(data.sessionid))
                {
                    _sessionId = data.sessionid;
                    StatusText.Text = LocalizationManager.GetString("Waiting");
                    BtnLogin.IsEnabled = true;
                    BtnRegister.IsEnabled = true;
                }
                else
                {
                    StatusText.Text = "Init KeyAuth échouée : " + (data.message ?? "Réponse invalide");
                    BtnLogin.IsEnabled = false;
                    BtnRegister.IsEnabled = false;
                }
            }
            catch (HttpRequestException httpEx)
            {
                StatusText.Text = $"Init KeyAuth échouée : Erreur réseau - {httpEx.Message}";
                BtnLogin.IsEnabled = false;
                BtnRegister.IsEnabled = false;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Init KeyAuth échouée : " + ex.Message;
                BtnLogin.IsEnabled = false;
                BtnRegister.IsEnabled = false;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoginUsernameTextBox.Focus();
            }
            catch { /* si les noms diffèrent, on évite un crash */ }

            await InitKeyAuthAsync();
        }

        // ---------------------- TOGGLE FORMS ----------------------
        private void ShowLoginForm_Click(object sender, RoutedEventArgs e)
        {
            LoginForm.Visibility = Visibility.Visible;
            RegisterForm.Visibility = Visibility.Collapsed;
            LoginTabButton.Style = (Style)FindResource("PremiumButton");
            RegisterTabButton.Style = (Style)FindResource("SecondaryButton");
            // Afficher le StatusText
            StatusText.Visibility = Visibility.Visible;
        }

        private void ShowRegisterForm_Click(object sender, RoutedEventArgs e)
        {
            LoginForm.Visibility = Visibility.Collapsed;
            RegisterForm.Visibility = Visibility.Visible;
            LoginTabButton.Style = (Style)FindResource("SecondaryButton");
            RegisterTabButton.Style = (Style)FindResource("PremiumButton");
            // Afficher le StatusText
            StatusText.Visibility = Visibility.Visible;
            // Mettre le focus sur le champ Username d'abord, puis LicenseKey
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RegisterUsernameTextBox.Focus();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void RegisterLicenseKeyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var placeholder = FindName("RegisterLicenseKeyPlaceholder") as TextBlock;
            if (placeholder != null)
            {
                placeholder.Visibility = Visibility.Collapsed;
            }
        }

        private void RegisterLicenseKeyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            var placeholder = FindName("RegisterLicenseKeyPlaceholder") as TextBlock;
            if (placeholder != null && textBox != null)
            {
                placeholder.Visibility = string.IsNullOrWhiteSpace(textBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void RegisterLicenseKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var placeholder = FindName("RegisterLicenseKeyPlaceholder") as TextBlock;
            if (placeholder != null && textBox != null)
            {
                placeholder.Visibility = string.IsNullOrWhiteSpace(textBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // ---------------------- LOGIN ----------------------
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_sessionId))
                {
                    StatusText.Text = "Session absente. Ré-initialisation…";
                    await InitKeyAuthAsync();
                    if (string.IsNullOrWhiteSpace(_sessionId)) return;
                }

                var username = (LoginUsernameTextBox.Text ?? "").Trim();
                // Récupérer le mot de passe depuis les deux sources possibles
                var passwordFromBox = LoginPasswordBox.Password ?? "";
                var passwordFromTextBox = LoginPasswordTextBox.Text ?? "";
                // Prendre celui qui n'est pas vide, ou celui du TextBox si les deux ont une valeur
                var password = !string.IsNullOrWhiteSpace(passwordFromTextBox) ? passwordFromTextBox.Trim() : passwordFromBox.Trim();
                // S'assurer que les deux sont synchronisés
                if (!string.IsNullOrWhiteSpace(password))
                {
                    LoginPasswordBox.Password = password;
                    LoginPasswordTextBox.Text = password;
                }

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    StatusText.Text = "Veuillez remplir tous les champs";
                    return;
                }

                BtnLogin.IsEnabled = false;
                StatusText.Visibility = Visibility.Visible;
                StatusText.Text = LocalizationManager.GetString("Validating");

                // HWID simple et stable (minimum 20 caractères requis par KeyAuth)
                var machineName = Environment.MachineName ?? "UNKNOWN";
                var userName = Environment.UserName ?? "USER";
                var processorId = Environment.ProcessorCount.ToString();
                var osVersion = Environment.OSVersion.VersionString ?? "";
                // Créer un HWID d'au moins 20 caractères en combinant plusieurs infos système
                var hwidBase = $"{machineName}-{userName}-{processorId}-{osVersion}".Replace(" ", "");
                // Limiter à 50 caractères maximum si trop long
                if (hwidBase.Length > 50)
                {
                    hwidBase = hwidBase.Substring(0, 50);
                }
                // S'assurer que le HWID fait au moins 20 caractères
                while (hwidBase.Length < 20)
                {
                    var guidPart = Guid.NewGuid().ToString("N");
                    var needed = 20 - hwidBase.Length;
                    hwidBase += "-" + guidPart.Substring(0, Math.Min(needed, guidPart.Length));
                }
                var hwid = Uri.EscapeDataString(hwidBase);

                // KeyAuth utilise "pass" et non "password" pour le paramètre mot de passe
                var url = $"{ApiBase}?type=login" +
                          $"&username={Uri.EscapeDataString(username)}" +
                          $"&pass={Uri.EscapeDataString(password)}" +
                          $"&sessionid={Uri.EscapeDataString(_sessionId!)}" +
                          $"&name={Uri.EscapeDataString(AppName)}" +
                          $"&ownerid={Uri.EscapeDataString(OwnerId)}" +
                          $"&hwid={hwid}";

                using var resp = await http.GetAsync(url).ConfigureAwait(true);
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(true);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    StatusText.Text = "Erreur : Réponse vide de l'API";
                    BtnLogin.IsEnabled = true;
                    return;
                }

                if (!json.TrimStart().StartsWith("{"))
                {
                    var errorMsg = json.Trim();
                    StatusText.Text = $"Erreur : {errorMsg}";
                    BtnLogin.IsEnabled = true;
                    return;
                }

                KeyAuthReply? data = null;
                try
                {
                    data = JsonSerializer.Deserialize<KeyAuthReply>(json);
                }
                catch (JsonException jsonEx)
                {
                    StatusText.Text = $"Erreur parsing JSON : {jsonEx.Message}";
                    BtnLogin.IsEnabled = true;
                    return;
                }

                if (data == null)
                {
                    StatusText.Text = "Erreur : Réponse nulle de l'API";
                    BtnLogin.IsEnabled = true;
                    return;
                }

                if (data.success)
                {
                    ValidatedKey = username; // Utiliser le username comme clé validée
                    ExpirationDate = DateTime.Now.AddDays(30); // Par défaut, à adapter selon votre API
                    
                    // Sauvegarder les identifiants
                    SaveCredentials(username, password);
                    
                    StatusText.Text = LocalizationManager.GetString("LicenseValidated");
                    DialogResult = true;
                    Close();
                }
                else
                {
                    StatusText.Text = "Erreur : " + (data.message ?? "Identifiants invalides");
                    BtnLogin.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Erreur : " + ex.Message;
                BtnLogin.IsEnabled = true;
            }
        }

        // ---------------------- REGISTER ----------------------
        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_sessionId))
                {
                    StatusText.Text = "Session absente. Ré-initialisation…";
                    await InitKeyAuthAsync();
                    if (string.IsNullOrWhiteSpace(_sessionId)) return;
                }

                var username = (RegisterUsernameTextBox.Text ?? "").Trim();
                
                // FORCER la synchronisation des deux champs AVANT de récupérer le mot de passe
                // Si le TextBox est visible, prendre sa valeur et mettre à jour le PasswordBox
                if (RegisterPasswordTextBox.Visibility == Visibility.Visible && !string.IsNullOrWhiteSpace(RegisterPasswordTextBox.Text))
                {
                    RegisterPasswordBox.Password = RegisterPasswordTextBox.Text;
                }
                // Si le PasswordBox est visible, prendre sa valeur et mettre à jour le TextBox
                else if (RegisterPasswordBox.Visibility == Visibility.Visible && !string.IsNullOrWhiteSpace(RegisterPasswordBox.Password))
                {
                    RegisterPasswordTextBox.Text = RegisterPasswordBox.Password;
                }
                
                // Récupérer le mot de passe depuis le PasswordBox (source de vérité)
                var password = RegisterPasswordBox.Password ?? "";
                
                // Si le PasswordBox est vide mais le TextBox a une valeur, l'utiliser
                if (string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(RegisterPasswordTextBox.Text))
                {
                    password = RegisterPasswordTextBox.Text;
                    RegisterPasswordBox.Password = password; // Synchroniser
                }
                
                password = password.Trim();
                
                var licenseKey = (RegisterLicenseKeyTextBox.Text ?? "").Trim();

                // Debug: afficher ce qui a été récupéré
                System.Diagnostics.Debug.WriteLine($"Register - Username: '{username}', Password: '{(password.Length > 0 ? "***" + password.Length + " chars" : "VIDE")}', LicenseKey: '{licenseKey}'");
                System.Diagnostics.Debug.WriteLine($"Register - PasswordBox.Password length: {RegisterPasswordBox.Password?.Length ?? 0}, TextBox.Text length: {RegisterPasswordTextBox.Text?.Length ?? 0}");

                if (string.IsNullOrWhiteSpace(username))
                {
                    StatusText.Text = "Erreur : Le nom d'utilisateur est requis";
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(password))
                {
                    StatusText.Text = "Erreur : Le mot de passe est requis. Veuillez entrer un mot de passe.";
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(licenseKey))
                {
                    StatusText.Text = "Erreur : La clé de licence est requise";
                    return;
                }

                BtnRegister.IsEnabled = false;
                StatusText.Visibility = Visibility.Visible;
                StatusText.Text = LocalizationManager.GetString("Validating");

                // HWID simple et stable (minimum 20 caractères requis par KeyAuth)
                var machineName = Environment.MachineName ?? "UNKNOWN";
                var userName = Environment.UserName ?? "USER";
                var processorId = Environment.ProcessorCount.ToString();
                var osVersion = Environment.OSVersion.VersionString ?? "";
                // Créer un HWID d'au moins 20 caractères en combinant plusieurs infos système
                var hwidBase = $"{machineName}-{userName}-{processorId}-{osVersion}".Replace(" ", "");
                // Limiter à 50 caractères maximum si trop long
                if (hwidBase.Length > 50)
                {
                    hwidBase = hwidBase.Substring(0, 50);
                }
                // S'assurer que le HWID fait au moins 20 caractères
                while (hwidBase.Length < 20)
                {
                    var guidPart = Guid.NewGuid().ToString("N");
                    var needed = 20 - hwidBase.Length;
                    hwidBase += "-" + guidPart.Substring(0, Math.Min(needed, guidPart.Length));
                }
                var hwid = Uri.EscapeDataString(hwidBase);

                // Double vérification que le mot de passe n'est pas vide avant d'envoyer
                if (string.IsNullOrWhiteSpace(password))
                {
                    StatusText.Text = "Erreur : Le mot de passe est vide. Veuillez entrer un mot de passe.";
                    BtnRegister.IsEnabled = true;
                    return;
                }

                // Encoder correctement tous les paramètres
                var encodedUsername = Uri.EscapeDataString(username);
                var encodedPassword = Uri.EscapeDataString(password);
                var encodedKey = Uri.EscapeDataString(licenseKey);
                var encodedSessionId = Uri.EscapeDataString(_sessionId!);
                var encodedAppName = Uri.EscapeDataString(AppName);
                var encodedOwnerId = Uri.EscapeDataString(OwnerId);

                // KeyAuth utilise "pass" et non "password" pour le paramètre mot de passe
                var url = $"{ApiBase}?type=register" +
                          $"&username={encodedUsername}" +
                          $"&pass={encodedPassword}" +
                          $"&key={encodedKey}" +
                          $"&sessionid={encodedSessionId}" +
                          $"&name={encodedAppName}" +
                          $"&ownerid={encodedOwnerId}" +
                          $"&hwid={hwid}";

                // Debug: vérifier l'URL (sans le mot de passe complet pour sécurité)
                System.Diagnostics.Debug.WriteLine($"Register URL - Username: {encodedUsername}, Password length: {password.Length}, Key: {encodedKey}");
                System.Diagnostics.Debug.WriteLine($"Register URL (masked): {url.Replace(encodedPassword, "***")}");

                using var resp = await http.GetAsync(url).ConfigureAwait(true);
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(true);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    StatusText.Text = "Erreur : Réponse vide de l'API";
                    BtnRegister.IsEnabled = true;
                    return;
                }

                if (!json.TrimStart().StartsWith("{"))
                {
                    var errorMsg = json.Trim();
                    StatusText.Text = $"Erreur : {errorMsg}";
                    BtnRegister.IsEnabled = true;
                    return;
                }

                KeyAuthReply? data = null;
                try
                {
                    data = JsonSerializer.Deserialize<KeyAuthReply>(json);
                }
                catch (JsonException jsonEx)
                {
                    StatusText.Text = $"Erreur parsing JSON : {jsonEx.Message}";
                    BtnRegister.IsEnabled = true;
                    return;
                }

                if (data == null)
                {
                    StatusText.Text = "Erreur : Réponse nulle de l'API";
                    BtnRegister.IsEnabled = true;
                    return;
                }

                if (data.success)
                {
                    ValidatedKey = username;
                    ExpirationDate = DateTime.Now.AddDays(30); // Par défaut, à adapter selon votre API
                    
                    // Sauvegarder les identifiants
                    SaveCredentials(username, password);
                    
                    StatusText.Text = "Inscription réussie !";
                    DialogResult = true;
                    Close();
                }
                else
                {
                    StatusText.Text = "Erreur : " + (data.message ?? "Échec de l'inscription");
                    BtnRegister.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Erreur : " + ex.Message;
                BtnRegister.IsEnabled = true;
            }
        }

        private void QuitBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // valide seulement si ShowDialog()
            Application.Current.Shutdown();
        }


        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is string lang)
            {
                var language = lang switch
                {
                    "French" => Trident.MITM.Language.French,
                    "English" => Trident.MITM.Language.English,
                    "Spanish" => Trident.MITM.Language.Spanish,
                    "German" => Trident.MITM.Language.German,
                    _ => Trident.MITM.Language.French
                };
                LocalizationManager.SetLanguage(language);
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            StatusText.Text = LocalizationManager.GetString("Waiting");
            BtnLogin.Content = LocalizationManager.GetString("LoginButton");
            BtnRegister.Content = LocalizationManager.GetString("RegisterButton");
            QuitBtn.Content = LocalizationManager.GetString("Quit");
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Application.Current.Shutdown();
        }

        // -------- Réponse minimaliste KeyAuth 1.3 --------
        private sealed class KeyAuthReply
        {
            public bool success { get; set; }
            public string? message { get; set; }
            public string? sessionid { get; set; }
        }
    }
}
