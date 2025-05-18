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
        
        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is ChallengesViewModel vm)
            {
                vm.LoadChallengesFromFiles(); // Refresh on return
            }
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
                service.LoadChallenge(selected.Filename);

                ChallengeSession.CurrentFilename = selected.Filename;
                await Shell.Current.GoToAsync($"///ChallengePage");



            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load challenge: {ex.Message}", "OK");
            }

            ((CollectionView)sender).SelectedItem = null;
        }

    }
}
