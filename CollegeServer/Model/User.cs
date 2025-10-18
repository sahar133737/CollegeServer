using System.ComponentModel.DataAnnotations;


namespace CollegeServer.Model
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FIO { get; set; }

        [Required]
       
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public byte[] Photo { get; set; }
        public string PhotoFiletype { get; set; }
        public string Group { get; set; }
    }


    public class CreateUserDto
    {
        [Required]
        public string FIO { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public IFormFile Photo { get; set; }
        public string Group { get; set; }
    }

    public class UserResponseDto
    {
        public int Id { get; set; }
        public string FIO { get; set; }
        public string Email { get; set; }
        public string PhotoFiletype { get; set; }
        public string Group { get; set; }
    }

    public class UpdateUserDto
    {
        [Required]
        public string FIO { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Password { get; set; }
        public IFormFile Photo { get; set; }
        public string Group { get; set; }
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
