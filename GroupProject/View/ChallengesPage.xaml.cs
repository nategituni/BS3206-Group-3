using System;
using GroupProject.ViewModel;
using Microsoft.Maui.Controls;
using GroupProject.Model;
//placeholder
namespace GroupProject.View
{
    public partial class ChallengesPage : ContentPage
    {
        public ChallengesPage()
        {
            InitializeComponent();
            BindingContext = new ChallengesViewModel();

        }

        private void OnChallengeSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0)
                return;

            var selected = e.CurrentSelection[0] as Challenge;
            if (selected == null)
                return;

            // Example navigation or popup
            DisplayAlert("Challenge Selected", $"You selected: {selected.Name}", "OK");

            // Optionally clear selection
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}
