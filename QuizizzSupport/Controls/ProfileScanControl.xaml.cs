using System;
using System.Windows;

namespace QuizizzSupport.Controls
{
    /// <summary>
    /// Interaction logic for ProfileScanControl.xaml
    /// </summary>
    public partial class ProfileScanControl : Window
    {
        private string baseUrl;
        public event ContinueClickedDel OnContinueClicked;
        public delegate void ContinueClickedDel(string text);

        public ProfileScanControl(string Url)
        {
            InitializeComponent();

            baseUrl = Url;

            urlBox.Text = Url;
        }

        private void PageOpenedClicked(object sender, RoutedEventArgs e)
        {
            Uri uri = new Uri(baseUrl);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            //var myProcess = new Process();
            //myProcess.StartInfo.UseShellExecute = true;
            //myProcess.StartInfo.FileName = baseUrl;
            //myProcess.Start();
        }

        private void UrlCopyClicked(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(baseUrl);
        }

        private void ContinueClicked(object sender, RoutedEventArgs e)
        {
            if (returnBox.Text.Length < 2) return;
            OnContinueClicked.Invoke(returnBox.Text);
        }

        private void MinimalizeClicked(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow).QuizNotFound();
            this.Close();
        }

        private void MouseTabDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private async void QuizizzLoginClicked(object sender, RoutedEventArgs e)
        {
            var mainWindow = (App.Current.MainWindow as MainWindow);
            Controls.LoginWindow loginWindow = null;
            if (mainWindow.loginWindow == null)
            {
                loginWindow = new Controls.LoginWindow();
                mainWindow.loginWindow = loginWindow;
                loginWindow.OnLoginButtonClicked += mainWindow.LoginWindow_OnLoginButtonClicked;
            }
            else
                loginWindow = mainWindow.loginWindow;
            loginWindow.Show();
            await loginWindow.AuthorizationSuccessful.Task;
            OnContinueClicked.Invoke("LOGIN");
            Close();
        }
    }
}
