using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupProject.Models
{
    public class User
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsMfaVerified { get; set; }
        public string Role { get; set; }
        public string? ProfilePicture { get; set; }
    }
}
