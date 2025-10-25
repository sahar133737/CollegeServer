using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


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

        public byte[]? Photo { get; set; }  // Измените на nullable
        public string? PhotoFiletype { get; set; }  // Измените на nullable
        public string? Group { get; set; }  // Измените на nullable
    }


    public class CreateUserDto
    {
        [Required]
        [JsonPropertyName("full_name")]
        public string FIO { get; set; }
        [Required]
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("photo")]
        public IFormFile? Photo { get; set; }  // Сделайте nullable

        [JsonPropertyName("group")]
        public string? Group { get; set; }  // Сделайте nullable
    }

        public class UserResponseDto
        {
            public int Id { get; set; }
            public string FIO { get; set; }
            public string Email { get; set; }
        public string? PhotoFiletype { get; set; }  // Сделайте nullable
        public string? Group { get; set; }  // Сделайте nullable
    }

        public class UpdateUserDto
        {
            [Required]
            public string FIO { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

        public string? Password { get; set; }  // Сделайте nullable
        public IFormFile? Photo { get; set; }  // Сделайте nullable
        public string? Group { get; set; }  // Сделайте nullable
    }

        public class LoginDto
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }
        }
    public class CreateUserWithoutPhotoDto
    {
        public string FIO { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? Group { get; set; }  // Сделайте nullable
    }
}