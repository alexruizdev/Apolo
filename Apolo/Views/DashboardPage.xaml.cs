using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ViewModels;

namespace Apolo.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel => (DashboardViewModel)DataContext;

        public DashboardPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<DashboardViewModel>();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e) =>
            await ViewModel.LoadAsync();

    }
}
