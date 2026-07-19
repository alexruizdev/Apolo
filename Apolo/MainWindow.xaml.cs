using Apolo.Pages;
using Apolo.Services;
using Apolo.ViewModels;
using Apolo.Views;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using WinRT;

namespace Apolo
{
    public static class NavigationTags
    {
        public const string Lessons = "lessons";
        public const string Invoicing = "invoicing";
        public const string Payers = "payers";
        public const string Students = "students";
        public const string Services = "services";
        public const string Specifications = "specifications";
        public const string Dashboard = "dashboard";
        public const string Proposal = "proposal";
        public const string Settings = "settings";
    }
    public sealed partial class MainWindow : Window
    {
        MicaController? _mica;
        SystemBackdropConfiguration? _config;
        public MainWindow()
        {
            InitializeComponent();
            TrySetMicaBackdrop();
            ExtendsContentIntoTitleBar = true;
            Loc.LanguageChanged += Loc_LanguageChanged;
            NavView.SelectedItem = NavView.MenuItems[0] as NavigationViewItem;
            RootFrame.Navigate(typeof(LessonsPage));
        }

        private void Loc_LanguageChanged(object? sender, EventArgs e)
        {
            // This forces all {x:Bind} elements in this XAML file to re-fetch their properties
            this.Bindings.Update();

            if (RootFrame.CurrentSourcePageType != null)
            {
                // Capture the current page and any parameters it was passed
                var currentPageType = RootFrame.CurrentSourcePageType;

                // Temporarily disable the cache. 
                // If we don't do this, WinUI might pull the old English/Spanish page from memory instead of rebuilding it.
                var oldCacheSize = RootFrame.CacheSize;
                RootFrame.CacheSize = 0;

                // Re-navigate to the exact same page, suppressing the animation so it looks like an instant text swap
                RootFrame.Navigate(
                    currentPageType,
                    null,
                    new SuppressNavigationTransitionInfo());

                // Restore the original cache size
                RootFrame.CacheSize = oldCacheSize;
            }
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

        private void Window_Activated(object sender, WindowActivatedEventArgs e)
        {
            System.ArgumentNullException.ThrowIfNull(e);
            // Do something when the window is activated
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
            switch (tag)
            {
                case NavigationTags.Lessons:
                    RootFrame.Navigate(typeof(LessonsPage));
                    break;
                case NavigationTags.Invoicing:
                    RootFrame.Navigate(typeof(InvoicesPage));
                    break;
                case NavigationTags.Payers:
                    RootFrame.Navigate(typeof(PayersPage)); 
                    break;
                case NavigationTags.Students:
                    RootFrame.Navigate(typeof(StudentsPage));
                    break;
                case NavigationTags.Services:
                    RootFrame.Navigate(typeof(ServicesPage));
                    break;
                case NavigationTags.Specifications:
                    RootFrame.Navigate(typeof(SpecificationsPage));
                    break;
                case NavigationTags.Dashboard:
                    RootFrame.Navigate(typeof(DashboardPage));
                    break;
                case NavigationTags.Proposal:
                    RootFrame.Navigate(typeof(ProposalPage));
                    break;
                case NavigationTags.Settings:
                    RootFrame.Navigate(typeof(SettingsPage));
                    break;
            }
        }
    }
}
