using Microsoft.EntityFrameworkCore;
using CollegeServer;

var builder = WebApplication.CreateBuilder(args);

// ���������� ��������
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ��������� Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<ScheduleParserService>();
builder.Services.AddScoped<ScheduleParserService>();
builder.Services.AddScoped<ScheduleService>();

var app = builder.Build();

// ������������ HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();