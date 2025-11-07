using System;
using System.Windows;
using System.Windows.Threading;

namespace Trident.MITM
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Gestion des exceptions non gérées
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                // Initialiser la localisation
                LocalizationManager.SetLanguage(Trident.MITM.Language.French);

                var lic = new LicenseWindow();
                var ok = lic.ShowDialog() == true;
                if (!ok) { Shutdown(); return; }

                var main = new MainWindow();
                MainWindow = main;
                
                // Passer les informations de licence à MainWindow
                if (lic.ValidatedKey != null && lic.ExpirationDate.HasValue)
                {
                    main.UpdateLicenseInfo(lic.ValidatedKey, lic.ExpirationDate.Value);
                }
                
                main.Show();

                ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur au démarrage: {ex.Message}\n\n{ex.StackTrace}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Erreur non gérée: {e.Exception.Message}\n\n{e.Exception.StackTrace}", 
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Erreur critique: {ex.Message}\n\n{ex.StackTrace}", 
                    "Erreur Critique", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
