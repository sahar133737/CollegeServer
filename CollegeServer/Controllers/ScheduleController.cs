using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly ScheduleParserService _parserService;
    private readonly ScheduleService _scheduleService;

    public ScheduleController(ScheduleParserService parserService, ScheduleService scheduleService)
    {
        _parserService = parserService;
        _scheduleService = scheduleService;
    }

    [HttpGet("schedule")]
    public async Task<IActionResult> DownloadSchedule()
    {
        try
        {
            var stream = await _parserService.GetScheduleFileAsync();
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "schedule.xlsx");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("replacements")]
    public async Task<IActionResult> DownloadReplacements()
    {
        try
        {
            var stream = await _parserService.GetReplacementFileAsync();
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "replacements.xlsx");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("week-info")]
    public async Task<ActionResult<string>> GetWeekInfo()
    {
        try
        {
            var weekInfo = await _parserService.GetCurrentWeekInfoAsync();
            return Ok(weekInfo);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    

    [HttpGet("schedule/{group}/{day}")]
    public async Task<ActionResult<List<string>>> GetSchedule(string group, string day)
    {
        try
        {
            // Получаем файл расписания
            using var scheduleStream = await _parserService.GetScheduleFileAsync();

            // Парсим Excel
            var scheduleData = _scheduleService.ParseExcelFile(scheduleStream);

            // Фильтруем по группе и дню
            var result = _scheduleService.FilterSchedule(scheduleData, group, day);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Ошибка: {ex.Message}");
        }
    }

    [HttpGet("replacements/{group}/{day}")]
    public async Task<ActionResult<List<string>>> GetReplacements(string group, string day)
    {
        try
        {
            // Получаем файл замен
            using var replacementsStream = await _parserService.GetReplacementFileAsync();

            // Парсим Excel
            var replacementsData = _scheduleService.ParseExcelFile(replacementsStream);

            // Фильтруем по группе и дню
            var result = _scheduleService.FilterReplacement(replacementsData, group, day);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Ошибка: {ex.Message}");
        }
    }

    [HttpGet("full-schedule/{group}")]
    public async Task<ActionResult<Dictionary<string, List<string>>>> GetFullWeekSchedule(string group)
    {
        try
        {
            var weekSchedule = new Dictionary<string, List<string>>();
            var days = new[] { "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };

            using var scheduleStream = await _parserService.GetScheduleFileAsync();
            var scheduleData = _scheduleService.ParseExcelFile(scheduleStream);

            foreach (var day in days)
            {
                var daySchedule = _scheduleService.FilterSchedule(scheduleData, group, day);
                weekSchedule.Add(day, daySchedule);
            }

            return Ok(weekSchedule);
        }
        catch (Exception ex)
        {
            return BadRequest($"Ошибка: {ex.Message}");
        }
    }

    [HttpGet("test")]
    public async void Test() {
        var _scheduleParserService = new ScheduleParserService(new HttpClient());
        var _scheduleService = new ScheduleService();
        var data = _scheduleService.ParseExcelFile(_scheduleParserService.GetScheduleFileAsync().Result, 0);
        Console.WriteLine("AAAAAAAAAALLLLLLLLLLLLLLLEEEEEEEEEEEEEE");

        foreach (string str in data[10])
            {
                Console.WriteLine(str);
            }
    }
}