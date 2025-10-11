using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

public class ScheduleParserService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://college.tu-bryansk.ru";

    public ScheduleParserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<Stream> GetScheduleFileAsync()
    {
        try
        {
            // Загружаем HTML страницы
            var html = await _httpClient.GetStringAsync($"{BaseUrl}/?page_id=4043");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Ищем ссылку на файл расписания для очной формы
            var scheduleLink = doc.DocumentNode
                .SelectNodes("//a[contains(@href, 'Расписание-учебных-занятий-ОЧНОЙ-формы')]")
                ?.FirstOrDefault();

            if (scheduleLink == null)
                throw new Exception("Ссылка на расписание не найдена");

            var scheduleUrl = scheduleLink.GetAttributeValue("href", "");

            // Если ссылка относительная, добавляем базовый URL
            if (!scheduleUrl.StartsWith("http"))
                scheduleUrl = BaseUrl + scheduleUrl;

            // Скачиваем файл
            var response = await _httpClient.GetAsync(scheduleUrl);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при получении расписания: {ex.Message}", ex);
        }
    }


    public async Task<Stream> GetReplacementFileAsync()
    {
        try
        {
            // Загружаем HTML страницы
            var html = await _httpClient.GetStringAsync($"{BaseUrl}/?page_id=4043");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Ищем ссылки на файлы замен в разделе "Изменения в расписании"
            var replacementLinks = doc.DocumentNode
                .SelectNodes("//h3[contains(text(), 'Изменения в расписании')]/following-sibling::p/a")
                ?.ToList();

            if (replacementLinks == null || !replacementLinks.Any())
                throw new Exception("Ссылки на замены не найдены");

            // Берем первую (самую актуальную) ссылку на замены
            var replacementLink = replacementLinks.First();
            var replacementUrl = replacementLink.GetAttributeValue("href", "");

            // Если ссылка относительная, добавляем базовый URL
            if (!replacementUrl.StartsWith("http"))
                replacementUrl = BaseUrl + replacementUrl;

            // Скачиваем файл
            var response = await _httpClient.GetAsync(replacementUrl);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при получении замен: {ex.Message}", ex);
        }
    }


    public async Task<string> GetCurrentWeekInfoAsync()
    {
        try
        {
            var html = await _httpClient.GetStringAsync($"{BaseUrl}/?page_id=4043");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Ищем информацию о текущей неделе
            var weekInfoNode = doc.DocumentNode
                .SelectSingleNode("//strong[contains(text(), 'нечётной') or contains(text(), 'чётной')]");

            return weekInfoNode?.InnerText.Trim() ?? "Информация о неделе не найдена";
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при получении информации о неделе: {ex.Message}", ex);
        }
    }
}