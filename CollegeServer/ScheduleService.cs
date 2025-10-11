using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

public class ScheduleService
{
    private string _weekStateText = "чётной"; // Можно получать из базы или конфигурации

    /// <summary>
    /// Парсит Excel файл расписания и возвращает данные в виде списка
    /// </summary>
    public List<List<string>> ParseExcelFile(Stream fileStream, int sheetNumber = 0)
    {
        var data = new List<List<string>>();

        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.Worksheet(sheetNumber + 1);

            var range = worksheet.RangeUsed();
            if (range == null)
                return data;

            int firstRow = range.FirstRow().RowNumber();
            int lastRow = range.LastRow().RowNumber();
            int firstCol = range.FirstColumn().ColumnNumber();
            int lastCol = range.LastColumn().ColumnNumber();

            for (int r = firstRow; r <= lastRow; r++)
            {
                var rowData = new List<string>();
                for (int c = firstCol; c <= lastCol; c++)
                {
                    var cell = worksheet.Cell(r, c);
                    string value;
                    if (cell.IsMerged())
                    {
                        value = cell.MergedRange().FirstCell().GetString();
                    }
                    else
                    {
                        value = cell.GetString();
                    }
                    rowData.Add((value ?? string.Empty).Trim());
                }
                data.Add(rowData);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка парсинга Excel файла: {ex.Message}", ex);
        }

        return data;
    }

    /// <summary>
    /// Получает значение ячейки с учетом объединенных областей
    /// </summary>
    // ClosedXML already flattens merged ranges for value retrieval; helper no longer needed
    private string GetMergedCellValuePlaceholder() => string.Empty;

    /// <summary>
    /// Фильтрует расписание по группе и дню
    /// </summary>
    public List<string> FilterSchedule(List<List<string>> data, string group, string day)
    {
        var filteredData = new List<string>();

        try
        {
            if (data.Count == 0)
                return new List<string> { "Пустой файл расписания" };

            // Найти строку заголовка, где встречается точное название группы (без учета регистра)
            int groupIndex = -1;
            int headerRowIndex = -1;
            int scanHeaderRows = Math.Min(50, data.Count);
            for (int r = 0; r < scanHeaderRows; r++)
            {
                var row = data[r];
                for (int c = 0; c < row.Count; c++)
                {
                    if (string.Equals(row[c]?.Trim(), group?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        groupIndex = c;
                        headerRowIndex = r;
                        break;
                    }
                }
                if (groupIndex >= 0) break;
            }
            if (groupIndex < 0)
                return new List<string> { "Группа не найдена" };

            // Найти строку дня (ключевое слово дня встречается в любой ячейке строки)
            int dayIndex = -1;
            int scanRows = Math.Min(200, data.Count);
            for (int r = 0; r < scanRows; r++)
            {
                var row = data[r];
                if (row.Any(c => string.Equals(c?.Trim(), day?.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    dayIndex = r;
                    break;
                }
            }
            if (dayIndex < 0)
                return new List<string> { "День не найден" };

            // Собрать пары под столбцом группы, пока не встретим следующий заголовок дня
            int pair = 1;
            for (int r = dayIndex + 1; r < data.Count && pair <= 8; r++)
            {
                var row = data[r];
                if (IsDayHeaderRow(row)) break;
                string val = groupIndex < row.Count ? (row[groupIndex] ?? string.Empty).Trim() : string.Empty;
                filteredData.Add(string.IsNullOrEmpty(val) ? $"{pair}пара: отсутствует" : $"{pair}пара: {val}");
                pair++;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка фильтрации расписания: {ex.Message}", ex);
        }

        return filteredData;
    }

    /// <summary>
    /// Находит индекс строки с указанным днем
    /// </summary>
    private int FindDayIndex(List<List<string>> data, string day)
    {
        for (int i = 0; i < Math.Min(61, data.Count); i++)
        {
            if (data[i].Count > 0 && data[i][0] == day && data[i][0] != "Воскресенье")
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Фильтрует конкретную пару
    /// </summary>
    private void FilterClass(int classNumber, int firstOffset, int secondOffset, int dayIndex,
                           int groupIndex, List<List<string>> data, List<string> filteredData)
    {
        var firstParaData = GetCellData(data, dayIndex + firstOffset, groupIndex);
        var secondParaData = GetCellData(data, dayIndex + secondOffset, groupIndex);

        // Обработка 4-й пары с учетом подгрупп
        if (classNumber == 4 && firstParaData.Contains("4п"))
        {
            filteredData.Add($"{firstParaData} \n{secondParaData}");
            return;
        }

        // Логика фильтрации в зависимости от недели
        if (string.IsNullOrEmpty(firstParaData) && string.IsNullOrEmpty(secondParaData))
        {
            filteredData.Add($"{classNumber}пара: отсутствует");
        }
        else if (string.IsNullOrEmpty(firstParaData) && !string.IsNullOrEmpty(secondParaData) && _weekStateText != "чётной")
        {
            filteredData.Add($"{classNumber}пара: {secondParaData}");
        }
        else if (!string.IsNullOrEmpty(firstParaData) && string.IsNullOrEmpty(secondParaData) && _weekStateText == "чётной")
        {
            filteredData.Add($"{classNumber}пара: {firstParaData}");
        }
        else if (!string.IsNullOrEmpty(firstParaData) && !string.IsNullOrEmpty(secondParaData))
        {
            if (secondParaData == firstParaData)
            {
                filteredData.Add($"{classNumber}пара: {firstParaData}");
            }
            else if (_weekStateText == "чётной")
            {
                filteredData.Add($"{classNumber}пара: {firstParaData}");
            }
            else
            {
                filteredData.Add($"{classNumber}пара: {secondParaData}");
            }
        }
    }

    /// <summary>
    /// Безопасно получает данные ячейки
    /// </summary>
    private string GetCellData(List<List<string>> data, int row, int col)
    {
        if (row < data.Count && col < data[row].Count)
            return data[row][col] ?? "";
        return "";
    }

    /// <summary>
    /// Фильтрует замены по группе и дню
    /// </summary>
    public List<string> FilterReplacement(List<List<string>> data, string group, string day)
    {
        var filteredData = new List<string>();

        try
        {
            // Находим индекс дня
            var dayIndex = FindReplacementDayIndex(data, day);
            if (dayIndex == -1)
                return new List<string> { "Замен на этот день не найдено" };

            var groupFound = false;

            for (int i = dayIndex + 3; i < data.Count; i++)
            {
                // Проверяем конец секции замен
                if (IsEmptyRow(data, i))
                    break;

                // Ищем группу
                if (data[i].Count > 0 && data[i][0] == group)
                {
                    groupFound = true;
                }

                // Добавляем замены для найденной группы
                if (groupFound && (data[i].Count > 0 && (data[i][0] == group || string.IsNullOrEmpty(data[i][0]))))
                {
                    if (data[i].Count >= 4)
                    {
                        var para = GetCellData(data, i, 1);
                        var insteadOf = GetCellData(data, i, 2);
                        var replacement = GetCellData(data, i, 3);

                        if (!string.IsNullOrEmpty(para) && !string.IsNullOrEmpty(insteadOf) && !string.IsNullOrEmpty(replacement))
                        {
                            var replacementInfo = $"{para}: {insteadOf} <-> {replacement}";
                            filteredData.Add(replacementInfo);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка фильтрации замен: {ex.Message}", ex);
        }

        return filteredData;
    }

    /// <summary>
    /// Находит индекс строки с днем в файле замен
    /// </summary>
    private int FindReplacementDayIndex(List<List<string>> data, string day)
    {
        for (int i = 0; i < data.Count; i++)
        {
            if (data[i].Count > 0 && data[i][0].Contains(day))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Проверяет, является ли строка пустой
    /// </summary>
    private bool IsEmptyRow(List<List<string>> data, int rowIndex)
    {
        if (rowIndex >= data.Count) return true;

        var row = data[rowIndex];
        return row.Count < 3 ||
               (string.IsNullOrEmpty(row[0]) &&
                string.IsNullOrEmpty(row[1]) &&
                string.IsNullOrEmpty(row[2]));
    }

    /// <summary>
    /// Проверяет, является ли строка заголовком дня недели
    /// </summary>
    private static bool IsDayHeaderRow(List<string> row)
    {
        if (row == null) return false;
        var days = new[] { "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье" };
        for (int i = 0; i < row.Count; i++)
        {
            var cell = row[i]?.Trim();
            if (string.IsNullOrEmpty(cell)) continue;
            foreach (var d in days)
            {
                if (string.Equals(cell, d, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }
}