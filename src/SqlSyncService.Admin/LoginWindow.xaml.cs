using System.Windows;
using System.Windows.Input;
using SqlSyncService.Admin.Services;

namespace SqlSyncService.Admin;

public partial class LoginWindow : Window
{
    private readonly AdminAuthService _authService;

    public bool IsAuthenticated { get; private set; }

    public LoginWindow(AdminAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        
        // Set focus after window is loaded
        Loaded += (s, e) => {
            PassphraseBox.Focus();
            Keyboard.Focus(PassphraseBox);
        };
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        AttemptLogin();
    }

    private void PassphraseBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AttemptLogin();
        }
    }

    private void AttemptLogin()
    {
        var passphrase = PassphraseBox.Password;

        if (string.IsNullOrWhiteSpace(passphrase))
        {
            ShowError("Please enter a passphrase");
            return;
        }

        if (_authService.ValidatePassphrase(passphrase))
        {
            IsAuthenticated = true;
            DialogResult = true;
            Close();
        }
        else
        {
            ShowError("Invalid passphrase. Please try again.");
            PassphraseBox.Clear();
            PassphraseBox.Focus();
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }
}
