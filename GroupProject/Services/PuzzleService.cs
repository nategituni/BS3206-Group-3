using Microsoft.Data.SqlClient;
using GroupProject.Models;

namespace GroupProject.Services
{
    public static class PuzzleService
    {
        private static readonly string connectionString = "Server=tcp:bs3206server.database.windows.net,1433;Initial Catalog=BS3206;Persist Security Info=False;User ID=sqladmin;Password=BS3206!!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        public static async Task SavePuzzleAsync(int userId, string puzzleName)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "State.xml");
            string xmlContent = await File.ReadAllTextAsync(filePath);

            using var conn = new SqlConnection(connectionString);

            string query = @"
                INSERT INTO Puzzles (UserId, PuzzleName, PuzzleData)
                VALUES (@UserId, @PuzzleName, @PuzzleData)";

            var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@PuzzleName", puzzleName);
            cmd.Parameters.AddWithValue("@PuzzleData", xmlContent);

            try
            {
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL ERROR (SavePuzzle): " + ex.Message);
            }
        }

        public static async Task LoadPuzzleAsync(int puzzleId)
        {
            string destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "State.xml");
            using var conn = new SqlConnection(connectionString);

            string selectQuery = "SELECT PuzzleData FROM Puzzles WHERE Id = @PuzzleId";
            string updateViewsQuery = "UPDATE Puzzles SET Views = Views + 1 WHERE Id = @PuzzleId";


            var selectCmd = new SqlCommand(selectQuery, conn);
            selectCmd.Parameters.AddWithValue("@PuzzleId", puzzleId);

            var updateCmd = new SqlCommand(updateViewsQuery, conn);
            updateCmd.Parameters.AddWithValue("@PuzzleId", puzzleId);

            try
            {
                await conn.OpenAsync();
                await updateCmd.ExecuteNonQueryAsync();
                var result = await selectCmd.ExecuteScalarAsync();

                if (result != null)
                {
                    string xmlContent = result.ToString();
                    await File.WriteAllTextAsync(destinationPath, xmlContent);
                }
                else
                {
                    Console.WriteLine("Puzzle not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL ERROR (LoadPuzzle): " + ex.Message);
            }
        }

        public static async Task DeletePuzzleAsync(int puzzleId)
        {
            using var conn = new SqlConnection(connectionString);

            string query = "DELETE FROM Puzzles WHERE Id = @PuzzleId";

            var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PuzzleId", puzzleId);

            try
            {
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL ERROR (DeletePuzzle): " + ex.Message);
            }
        }

        public static async Task<List<Puzzle>> GetUserPuzzlesAsync(int userId)
        {
            var puzzles = new List<Puzzle>();

            using var conn = new SqlConnection(connectionString);
            string query = "SELECT Id, PuzzleName, PuzzleData, CreatedAt FROM Puzzles WHERE UserId = @UserId";

            var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            try
            {
                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    puzzles.Add(new Puzzle
                    {
                        Id = reader.GetInt32(0),
                        UserId = userId,
                        PuzzleName = reader.GetString(1),
                        PuzzleData = reader.GetString(2),
                        CreatedAt = reader.GetDateTime(3)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL ERROR (GetUserPuzzles): " + ex.Message);
            }

            return puzzles;
        }

    }
}
