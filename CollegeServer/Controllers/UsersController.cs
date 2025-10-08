using CollegeServer.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                PhotoFiletype = u.PhotoFiletype,
                Group = u.Group
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return user;
    }
}

  
   