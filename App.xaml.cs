using MessagesEncrypter.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using Windows.Globalization;

namespace MessagesEncrypter
{
    public partial class App : Application
    {
        private const string MainInstanceKey = "main";

        private AppInstance? _mainInstance;
        private Window? _window;

        public App()
        {
            try
            {
                ApplicationLanguages.PrimaryLanguageOverride = LanguageSettings.LoadResolvedLanguage();
            }
            catch
            {
                // Language preference must never prevent the app from starting.
            }

            InitializeComponent();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            AppInstance currentInstance = AppInstance.GetCurrent();
            AppActivationArguments activationArguments = currentInstance.GetActivatedEventArgs();

            _mainInstance = AppInstance.FindOrRegisterForKey(MainInstanceKey);
            if (!_mainInstance.IsCurrent)
            {
                await _mainInstance.RedirectActivationToAsync(activationArguments);
                Exit();
                return;
            }

            _mainInstance.Activated += (_, _) =>
            {
                _window?.DispatcherQueue.TryEnqueue(ActivateMainWindow);
            };

            ActivateMainWindow();
        }

        private void ActivateMainWindow()
        {
            _window ??= new MainWindow();
            _window.Activate();
        }
    }
}
