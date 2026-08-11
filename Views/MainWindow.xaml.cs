using System.Windows;
using SpamBot.Services;
using SpamBot.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;

namespace SpamBot.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();

        // The Native Caption Frame Can Only Be Themed Once The Handle Exists
        SourceInitialized += (_, _) => ApplySystemTheme();
    }

    private void ApplySystemTheme()
    {
        bool isLight =
            ApplicationThemeManager.GetSystemTheme()
            is SystemTheme.Light
                or SystemTheme.HCWhite
                or SystemTheme.Sunrise
                or SystemTheme.Flow;

        // ApplicationThemeManager.Apply Would Also Reset The Frame And Drop The Caption Title
        ThemesDictionary? themes = Application
            .Current.Resources.MergedDictionaries.OfType<ThemesDictionary>()
            .FirstOrDefault();

        if (themes is not null)
        {
            themes.Theme = isLight ? ApplicationTheme.Light : ApplicationTheme.Dark;
        }

        WindowCaptionTheme.Apply(this, !isLight);
    }
}
