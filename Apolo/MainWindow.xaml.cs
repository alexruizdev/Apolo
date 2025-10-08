using Apolo.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition.SystemBackdrops;
using WinRT;
using Microsoft.UI.Composition;
using Apolo.ViewModels;

namespace Apolo
{
    public sealed partial class MainWindow : Window
    {
        MicaController? _mica;
        SystemBackdropConfiguration? _config;
        public MainWindow()
        {
            InitializeComponent();
            TrySetMicaBackdrop();
            NavView.SelectedItem = NavView.MenuItems[0] as NavigationViewItem;
            RootFrame.Navigate(typeof(PayersPage));
        }

        bool TrySetMicaBackdrop()
        {
            if (!MicaController.IsSupported())
                return false;

            _config = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Default
            };

            _mica = new MicaController();
            _mica.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            _mica.SetSystemBackdropConfiguration(_config);
            return true;
        }

        private void Window_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs e)
        {
            // Do something when the window is activated
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
            switch (tag)
            {
                case "payers":
                    RootFrame.Navigate(typeof(PayersPage)); 
                    break;
                case "customers":
                    RootFrame.Navigate(typeof(StudentsPage));
                    break;
            }
        }
    }
}
