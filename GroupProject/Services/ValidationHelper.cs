using System.Text.RegularExpressions;

namespace GroupProject.Services
{
    public static class ValidationHelper
    {
        public static bool IsValidPassword(string password)
        {
            var pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!?@#]).{7,}$";
            return Regex.IsMatch(password, pattern);
        }

        public static bool IsValidEmail(string email)
        {
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }
    }
}
