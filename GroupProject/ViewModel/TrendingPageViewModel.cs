using System.Collections.ObjectModel;
using System.Windows.Input;
using GroupProject.Models;
using Microsoft.Data.SqlClient;

namespace GroupProject.ViewModels
{
    public class TrendingPageViewModel : BindableObject
    {
        private const string connectionString = "Server=tcp:bs3206server.database.windows.net,1433;Initial Catalog=BS3206;Persist Security Info=False;User ID=sqladmin;Password=BS3206!!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        public ObservableCollection<Puzzle> Puzzles { get; } = new();

        public ICommand RefreshCommand { get; }

        public TrendingPageViewModel()
        {
            RefreshCommand = new Command(async () => await LoadTrendingPuzzlesAsync());
            LoadTrendingPuzzlesAsync().ConfigureAwait(false);
        }

        private async Task LoadTrendingPuzzlesAsync()
        {
            try
            {
                Puzzles.Clear();

                using var conn = new SqlConnection(connectionString);
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
    }
}
