using CollegeServer.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CollegeServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public UsersController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
    {
        var users = await _context.User
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                FIO = u.FIO,
                Email = u.Email,
                PhotoFiletype = u.PhotoFiletype,
                Group = u.Group
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET: api/users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        var user = await _context.User
            .Where(u => u.Id == id)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                FIO = u.FIO,
                Email = u.Email,
                PhotoFiletype = u.PhotoFiletype,  // Может быть null
                Group = u.Group  // Может быть null
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return user;
    }

    // POST: api/users

    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUserWithoutPhoto([FromBody] CreateUserWithoutPhotoDto createUserDto)
    {
        // Проверяем, существует ли пользователь с таким email
        var existingUser = await _context.User.FirstOrDefaultAsync(u => u.Email == createUserDto.Email);
        if (existingUser != null)
        {
            return BadRequest("Пользователь с таким email уже существует");
        }

        var user = new User
        {
            FIO = createUserDto.FIO,
            Email = createUserDto.Email,
            Password = HashPassword(createUserDto.Password),
            Group = createUserDto.Group,  // Может быть null
            Photo = null,
            PhotoFiletype = null  // Явно устанавливаем null
        };

        _context.User.Add(user);
        await _context.SaveChangesAsync();

        var responseDto = new UserResponseDto
        {
            Id = user.Id,
            FIO = user.FIO,
            Email = user.Email,
            PhotoFiletype = user.PhotoFiletype,  // Будет null
            Group = user.Group  // Может быть null
        };

        return Ok(responseDto);
    }

    // POST: api/users/login
    [HttpPost("login")]
    public async Task<ActionResult<UserResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var hashedPassword = HashPassword(loginDto.Password);

            // Безопасный запрос с проверкой на null
            var user = await _context.User
                .Where(u => u.Email == loginDto.Email && u.Password == hashedPassword)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    FIO = u.FIO,
                    Email = u.Email,
                    PhotoFiletype = u.PhotoFiletype,  // Может быть null - это нормально
                    Group = u.Group  // Может быть null - это нормально
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return Unauthorized("Неверный email или пароль");

            return Ok(user);
        }
        catch (Exception ex)
        {
            // Логируем ошибку для отладки
            Console.WriteLine($"Login error: {ex.Message}");
            return StatusCode(500, "Произошла ошибка при входе в систему");
        }
    }

    // PUT: api/users/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromForm] UpdateUserDto updateUserDto)
    {
        var user = await _context.User.FindAsync(id);
        if (user == null)
            return NotFound();

        // Проверяем, не занят ли email другим пользователем
        var existingUser = await _context.User.FirstOrDefaultAsync(u => u.Email == updateUserDto.Email && u.Id != id);
        if (existingUser != null)
        {
            return BadRequest("Пользователь с таким email уже существует");
        }

        user.FIO = updateUserDto.FIO;
        user.Email = updateUserDto.Email;
        
        // Обновляем пароль только если он предоставлен
        if (!string.IsNullOrEmpty(updateUserDto.Password))
        {
            user.Password = HashPassword(updateUserDto.Password);
        }
        
        user.Group = updateUserDto.Group;

        // Обработка обновления фото
        if (updateUserDto.Photo != null && updateUserDto.Photo.Length > 0)
        {
            using var memoryStream = new MemoryStream();
            await updateUserDto.Photo.CopyToAsync(memoryStream);
            user.Photo = memoryStream.ToArray();
            user.PhotoFiletype = Path.GetExtension(updateUserDto.Photo.FileName).TrimStart('.');
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
                return NotFound();
            else
                throw;
        }

        return NoContent();
    }

    // DELETE: api/users/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.User.FindAsync(id);
        if (user == null)
            return NotFound();

        _context.User.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/users/5/photo
    [HttpGet("{id}/photo")]
    public async Task<IActionResult> GetUserPhoto(int id)
    {
        var user = await _context.User.FindAsync(id);
        if (user == null || user.Photo == null || user.PhotoFiletype == null)
            return NotFound();

        return File(user.Photo, $"image/{user.PhotoFiletype}");
    }

    // POST: api/users/5/photo
    [HttpPost("{id}/photo")]
    public async Task<IActionResult> UploadUserPhoto(int id, IFormFile photo)
    {
        var user = await _context.User.FindAsync(id);
        if (user == null)
            return NotFound();

        if (photo == null || photo.Length == 0)
            return BadRequest("Файл не выбран");

        using var memoryStream = new MemoryStream();
        await photo.CopyToAsync(memoryStream);
        user.Photo = memoryStream.ToArray();
        user.PhotoFiletype = Path.GetExtension(photo.FileName).TrimStart('.');

        await _context.SaveChangesAsync();

        return Ok("Фото успешно загружено");
    }

    // DELETE: api/users/5/photo
    [HttpDelete("{id}/photo")]
    public async Task<IActionResult> DeleteUserPhoto(int id)
    {
        var user = await _context.User.FindAsync(id);
        if (user == null)
            return NotFound();

        user.Photo = null;
        user.PhotoFiletype = null;

        await _context.SaveChangesAsync();

        return Ok("Фото успешно удалено");
    }

    private bool UserExists(int id)
    {
        return _context.User.Any(e => e.Id == id);
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
}
   