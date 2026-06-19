using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ViewModels;

namespace Apolo.Views
{
    public sealed partial class ProposalPage : Page
    {
        public ProposalViewModel ViewModel => (ProposalViewModel)DataContext;
        public ProposalPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<ProposalViewModel>();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e) =>
            await ViewModel.LoadAsync();
    }
}
