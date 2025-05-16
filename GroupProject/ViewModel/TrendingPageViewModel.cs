using System.Collections.ObjectModel;
using System.Windows.Input;
using GroupProject.Models;
using GroupProject.Services;
using Microsoft.Data.SqlClient;

namespace GroupProject.ViewModel
{
    public class TrendingPageViewModel : BindableObject
    {
        private const string ConnectionString = "Server=tcp:bs3206server.database.windows.net,1433;Initial Catalog=BS3206;Persist Security Info=False;User ID=sqladmin;Password=BS3206!!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        public ObservableCollection<Puzzle> Puzzles { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand LoadPuzzleCommand { get; }

        public TrendingPageViewModel()
        {
            RefreshCommand = new Command(async () => await LoadTrendingPuzzlesAsync());
            LoadPuzzleCommand = new Command<Puzzle>(async (puzzle) => await LoadPuzzleAsync(puzzle));

            LoadTrendingPuzzlesAsync().ConfigureAwait(false);
        }

        private async Task LoadTrendingPuzzlesAsync()
        {
            try
            {
                Puzzles.Clear();

                using var conn = new SqlConnection(ConnectionString);
                string query = @"
                    SELECT TOP 10 Id, UserId, PuzzleName, PuzzleData, CreatedAt, Views
                    FROM Puzzles
                    WHERE CreatedAt >= DATEADD(DAY, -7, GETDATE())
                    ORDER BY Views DESC";

                var cmd = new SqlCommand(query, conn);

                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var puzzle = new Puzzle
                    {
                        Id = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        PuzzleName = reader.GetString(2),
                        PuzzleData = reader.IsDBNull(3) ? null : reader.GetString(3),
                        CreatedAt = reader.GetDateTime(4),
                        Views = reader.GetInt32(5)
                    };

                    Puzzles.Add(puzzle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL ERROR (LoadTrendingPuzzles): " + ex.Message);
            }
        }

        private async Task LoadPuzzleAsync(Puzzle puzzle)
        {
            if (puzzle == null) return;

            try
            {
                await PuzzleService.LoadPuzzleAsync(puzzle.Id);
                await Shell.Current.GoToAsync("///PuzzlePage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading puzzle: {ex.Message}");
            }
        }
    }
}
