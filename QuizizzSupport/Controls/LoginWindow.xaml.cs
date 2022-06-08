using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuizizzSupport.Controls
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public Utils.LoginInfo lastLoginInfo;
        public TaskCompletionSource<bool> AuthorizationSuccessful = new TaskCompletionSource<bool>();
        public event EventHandler OnLoginButtonClicked;

        public LoginWindow()
        {
            InitializeComponent();

            var mainWindow = App.Current.MainWindow as MainWindow;

            if (mainWindow.currentUserData != null && mainWindow.currentUserData.password.Length > 3)
            {
                passwordBox.Password = mainWindow.currentUserData.password;
                userNameBox.Text = mainWindow.currentUserData.username;
                rememberMeCheckbox.IsChecked = true;
            }
        }

        private async void LoginClicked(object sender, RoutedEventArgs e)
        {
            if (passwordBox.Password.Length < 3 || userNameBox.Text.Length < 1)
            {
                SetLoginFeedback("Please enter a password and username first", true);
                return;
            }
            (sender as Button).IsEnabled = false;
            var loginResult = await Utils.AuthorizeUserClient(userNameBox.Text, passwordBox.Password);
            if (loginResult != null)
            {
                loginStatus.Content = "Logged in";
                loginStatus.Foreground = Brushes.LimeGreen;
                (sender as Button).IsEnabled = true;
                SetLoginFeedback("Logged in successfuly", false);
                lastLoginInfo = loginResult;
                lastLoginInfo.rememberCreds = rememberMeCheckbox.IsChecked.Value;
                lastLoginInfo.username = userNameBox.Text;
                lastLoginInfo.password = passwordBox.Password;
                AuthorizationSuccessful?.TrySetResult(true);
                //loginButton.Content = "Log out";
                await Task.Delay(500);
                Hide();
            }
            else
            {
                //loginButton.Content = "Log in";
                SetLoginFeedback("Invalid password / username combination", true);
                loginStatus.Content = "Not logged in";
                loginStatus.Foreground = Resources["ValidationErrorBrush"] as Brush;
                (sender as Button).IsEnabled = true;
            }
            (sender as Button).IsEnabled = true;
            if (OnLoginButtonClicked != null)
                OnLoginButtonClicked.Invoke(sender, e);
        }

        public void LogOut()
        {
            loginButton.Content = "Log in";
            loginStatus.Content = "Not logged in";
            loginStatus.Foreground = Resources["ValidationErrorBrush"] as Brush;
            lastLoginInfo = null;
        }

        private void SetLoginFeedback(string text, bool isError)
        {
            loginFeedbackLab.Content = text;
            if (isError)
                loginFeedbackLab.Foreground = Resources["ValidationErrorBrush"] as Brush;
            else
                loginFeedbackLab.Foreground = Brushes.LimeGreen;
        }

        private void MinimalizeClicked(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void MouseTabDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
