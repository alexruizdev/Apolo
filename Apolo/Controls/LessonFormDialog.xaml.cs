using Apolo.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Models;
using ViewModels;

namespace Apolo.Controls
{
    public sealed partial class LessonFormDialog : UserControl
    {
        public LessonFormViewModel ViewModel { get; }
        public LessonFormDialog(LessonsViewModel parentVM)
        {
            InitializeComponent();
            ViewModel = new LessonFormViewModel(parentVM);
        }

        public LessonFormDialog(LessonsViewModel parentVM, LessonSummary lesson)
        {
            InitializeComponent();
            ViewModel = new LessonFormViewModel(parentVM, lesson);
        }

        private void StudentBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is StudentOption selectedStudent)
            {
                ViewModel.SelectedStudent = selectedStudent;
                sender.Text = selectedStudent.FullName;
            }
        }

        private void StudentBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // We only care about user input adjustments (Reason == UserInput)
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                // If they cleared the box completely, force-reset the viewmodel state
                if (string.IsNullOrWhiteSpace(sender.Text))
                {
                    ViewModel.SelectedStudent = null;
                }
            }
        }
    }
}
