using CollegeServer.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Cryptography;
using System.Text;

namespace CollegeServer.Tests
{
    public class UsersControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CollegeServer.Controllers.UsersController _controller;
        private readonly IWebHostEnvironment _environment;

        public UsersControllerTests()
        {
            // Настройка In-Memory базы данных
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            
            // Создание мока для IWebHostEnvironment
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(m => m.WebRootPath).Returns("/wwwroot");
            mockEnvironment.Setup(m => m.ContentRootPath).Returns("/");
            _environment = mockEnvironment.Object;

            _controller = new CollegeServer.Controllers.UsersController(_context, _environment);
        }

        [Fact]
        public async Task GetUsers_ReturnsAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = 1, FIO = "Иванов Иван", Email = "ivan@test.com", Password = "password1", Group = "Студент" },
                new User { Id = 2, FIO = "Петров Петр", Email = "petr@test.com", Password = "password2", Group = "Преподаватель" }
            };

            _context.User.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetUsers();

            // Assert
            var okResult = Assert.IsType<ActionResult<IEnumerable<UserResponseDto>>>(result);
            var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
            var usersList = Assert.IsAssignableFrom<IEnumerable<UserResponseDto>>(returnValue.Value);
            Assert.Equal(2, usersList.Count());
        }

        [Fact]
        public async Task GetUser_WithValidId_ReturnsUser()
        {
            // Arrange
            var user = new User 
            { 
                Id = 1, 
                FIO = "Иванов Иван", 
                Email = "ivan@test.com", 
                Password = "password1", 
                Group = "Студент" 
            };

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetUser(1);

            // Assert
            var okResult = Assert.IsType<ActionResult<UserResponseDto>>(result);
            var returnValue = Assert.IsType<UserResponseDto>(okResult.Value);
            Assert.Equal("Иванов Иван", returnValue.FIO);
            Assert.Equal("ivan@test.com", returnValue.Email);
            Assert.Equal("Студент", returnValue.Group);
        }

        [Fact]
        public async Task GetUser_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetUser(999);

            // Assert
            var actionResult = Assert.IsType<ActionResult<UserResponseDto>>(result);
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsUser()
        {
            // Arrange
            var hashedPassword = HashPassword("password123");
            var user = new User 
            { 
                Id = 1, 
                FIO = "Иванов Иван", 
                Email = "ivan@test.com", 
                Password = hashedPassword, 
                Group = "Студент" 
            };

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "ivan@test.com",
                Password = "password123"
            };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<UserResponseDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var userResponse = Assert.IsType<UserResponseDto>(okResult.Value);
            
            Assert.Equal("Иванов Иван", userResponse.FIO);
            Assert.Equal("ivan@test.com", userResponse.Email);
            Assert.Equal("Студент", userResponse.Group);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var hashedPassword = HashPassword("password123");
            var user = new User 
            { 
                Id = 1, 
                FIO = "Иванов Иван", 
                Email = "ivan@test.com", 
                Password = hashedPassword, 
                Group = "Студент" 
            };

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "ivan@test.com",
                Password = "wrongpassword" // Неверный пароль
            };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<UserResponseDto>>(result);
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
            Assert.Equal("Неверный email или пароль", unauthorizedResult.Value);
        }

        [Fact]
        public async Task DeleteUser_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var user = new User 
            { 
                Id = 1, 
                FIO = "Пользователь для удаления", 
                Email = "delete@test.com", 
                Password = "password", 
                Group = "Студент" 
            };

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteUser(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Проверяем, что пользователь удален из базы данных
            var deletedUser = await _context.User.FindAsync(1);
            Assert.Null(deletedUser);
        }

        [Fact]
        public async Task DeleteUser_WithNonExistentId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.DeleteUser(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}