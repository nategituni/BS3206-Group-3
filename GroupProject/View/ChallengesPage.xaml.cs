using System;
using GroupProject.ViewModel;
using Microsoft.Maui.Controls;
using GroupProject.Model;
using GroupProject.Services;

namespace GroupProject.View
{
    public partial class ChallengesPage : ContentPage
    {
        public ChallengesPage()
        {
            InitializeComponent();
            BindingContext = new ChallengesViewModel();

        }

        private async void OnChallengeSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0)
                return;

            var selected = e.CurrentSelection[0] as Challenge;
            if (selected == null)
                return;

            try
            {
                var service = new ChallengeService();
                service.LoadChallenge(selected.Filename); // ← Copy selected XML into State.xml

                // Navigate to sandbox or puzzle page
                // In ChallengesPage.xaml.cs
                await Shell.Current.GoToAsync("///PuzzlePage?challengeMode=true");

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load challenge: {ex.Message}", "OK");
            }

            ((CollectionView)sender).SelectedItem = null;
        }

    }
}
