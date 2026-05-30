using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace MessagesEncrypter
{
    public partial class App : Application
    {
        private const string MainInstanceKey = "main";

        private AppInstance? _mainInstance;
        private Window? _window;

        public App()
        {
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
