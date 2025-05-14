using Microsoft.Data.SqlClient;

namespace GroupProject.Services
{
    public static class AuthService
    {
        private static readonly string connectionString = "Server=tcp:bs3206server.database.windows.net,1433;Initial Catalog=BS3206;Persist Security Info=False;User ID=sqladmin;Password=BS3206!!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        public static async Task<bool> RegisterUserAsync(string fullName, string email, string password)
        {
            using SqlConnection conn = new SqlConnection(connectionString);

            string hashedPassword = HashHelper.HashPassword(password);

            string query = @"
        INSERT INTO Users (FullName, Email, PasswordHash, IsMfaVerified, Role)
        VALUES (@FullName, @Email, @PasswordHash, @IsMfaVerified, @Role)";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@FullName", fullName);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            cmd.Parameters.AddWithValue("@IsMfaVerified", true); // Change to false if you want real MFA
            cmd.Parameters.AddWithValue("@Role", "User");

            try
            {
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL ERROR: " + ex.Message);
                return false;
            }
        }


        public static async Task<bool> ValidateLoginAsync(string email, string password)
        {
            using var conn = new SqlConnection(connectionString);
            var cmd = new SqlCommand("SELECT PasswordHash FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            if (result == null) return false;

            return HashHelper.VerifyPassword(password, result.ToString());
        }

        public static async Task<bool> IsEmailTakenAsync(string email)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var cmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email);
            var count = (int)await cmd.ExecuteScalarAsync();
            return count > 0;
        }

        public static async Task<bool> IsUsernameTakenAsync(string fullName)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var cmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE FullName = @FullName", conn);
            cmd.Parameters.AddWithValue("@FullName", fullName);
            var count = (int)await cmd.ExecuteScalarAsync();
            return count > 0;
        }

        public static async Task<string?> GetProfilePictureAsync(string email)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("SELECT ProfilePicture FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email);
            var result = await cmd.ExecuteScalarAsync();

            return result?.ToString();
        }
        public static async Task UpdateProfilePictureAsync(string email, string base64Image)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var cmd = new SqlCommand("UPDATE Users SET ProfilePicture = @ProfilePicture WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@ProfilePicture", base64Image);
            cmd.Parameters.AddWithValue("@Email", email);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<int> GetUserIdByEmailAsync(string email)
        {
            using var conn = new SqlConnection(connectionString);
            var cmd = new SqlCommand("SELECT Id FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();

            return result != null ? Convert.ToInt32(result) : -1;
        }


    }
}
