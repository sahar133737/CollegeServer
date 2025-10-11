using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

public class ScheduleService
{
    private string _weekStateText = "чётной"; // Можно получать из базы или конфигурации

    public List<List<string>> ParseExcelFile(Stream fileStream, int sheetNumber = 0)
    {
        var data = new List<List<string>>();

        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.Worksheet(sheetNumber + 1);

            var range = worksheet.RangeUsed();
            if (range == null)
            {
                Console.WriteLine("SHEET IS NULL");
                return data;
            }

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
            int scanHeaderRows = 11;
            for (int r = 0; r < scanHeaderRows; r++)
            {
                var row = data[r];
                for (int c = 0; c < row.Count; c++)
                {
                    if (string.Equals(row[c]?.Trim(), group?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        groupIndex = c;
                        headerRowIndex = r;
                        Console.WriteLine(group + groupIndex);
                        break;
                    }
                }
                if (groupIndex >= 0) break;
            }
            if (groupIndex < 0)
                return new List<string> { "Группа не найдена" };

            // Используем правильный метод поиска дня
            int dayIndex = FindDayIndex(data, day);
            if (dayIndex < 0)
                return new List<string> { "День не найден" };

            // Используем правильную логику фильтрации пар с учетом недель
            // Определяем смещения для пар (обычно каждая пара занимает 2 строки - четная и нечетная недели)
            var pairOffsets = new Dictionary<int, (int first, int second)>
            {
                { 1, (1, 2) },   // 1-я пара: строки dayIndex+1 и dayIndex+2
                { 2, (3, 4) },   // 2-я пара: строки dayIndex+3 и dayIndex+4
                { 3, (5, 6) },   // 3-я пара: строки dayIndex+5 и dayIndex+6
                { 4, (7, 8) }    // 4-я пара: строки dayIndex+7 и dayIndex+8
            };

            // Фильтруем каждую пару с использованием правильной логики (максимум 4 пары)
            for (int pairNumber = 1; pairNumber <= 4; pairNumber++)
            {
                if (pairOffsets.ContainsKey(pairNumber))
                {
                    var offsets = pairOffsets[pairNumber];
                    FilterClass(pairNumber, offsets.first, offsets.second, dayIndex, groupIndex, data, filteredData);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка фильтрации расписания: {ex.Message}", ex);
        }

        return filteredData;
    }


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

    private void FilterClass(int classNumber, int firstOffset, int secondOffset, int dayIndex,
                           int groupIndex, List<List<string>> data, List<string> filteredData)
    {
        var firstParaData = GetCellData(data, dayIndex + firstOffset, groupIndex);
        var secondParaData = GetCellData(data, dayIndex + secondOffset, groupIndex);

        // Обработка 4-й пары с учетом подгрупп и возможности размещения 4-й и 5-й пар
        if (classNumber == 4)
        {
            // Проверяем, содержит ли ячейка информацию о 4-й и 5-й парах
            bool hasFourthPair = firstParaData.Contains("4п") || secondParaData.Contains("4п");
            bool hasFifthPair = firstParaData.Contains("5п") || secondParaData.Contains("5п");
            
            if (hasFourthPair || hasFifthPair)
            {
                var result = "4пара: ";
                var parts = new List<string>();
                
                // Обрабатываем первую строку
                if (!string.IsNullOrEmpty(firstParaData))
                {
                    parts.Add(firstParaData);
                }
                
                // Обрабатываем вторую строку
                if (!string.IsNullOrEmpty(secondParaData))
                {
                    parts.Add(secondParaData);
                }
                
                // Объединяем все части
                if (parts.Count > 0)
                {
                    result += string.Join(" \n", parts);
                    filteredData.Add(result);
                }
                else
                {
                    filteredData.Add("4пара: отсутствует");
                }
                return;
            }
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

    private string GetCellData(List<List<string>> data, int row, int col)
    {
        if (row < data.Count && col < data[row].Count)
            return data[row][col] ?? "";
        return "";
    }


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


    private bool IsEmptyRow(List<List<string>> data, int rowIndex)
    {
        if (rowIndex >= data.Count) return true;

        var row = data[rowIndex];
        return row.Count < 3 ||
               (string.IsNullOrEmpty(row[0]) &&
                string.IsNullOrEmpty(row[1]) &&
                string.IsNullOrEmpty(row[2]));
    }


    private static bool IsDayHeaderRow(List<string> row)
    {
        if (row == null) 
           
            return false; 


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