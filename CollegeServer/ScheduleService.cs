using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

public class ScheduleService
{
    /*private string _weekStateText = "чётной"; */ // Можно получать из базы или конфигурации
    private string _weekStateText = new ScheduleParserService(new HttpClient()).GetCurrentWeekInfoAsync().Result;

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
                { 4, (7, 8) },   // 4-я пара: строки dayIndex+7 и dayIndex+8
                { 5, (9, 10) }   // 5-я пара: строки dayIndex+9 и dayIndex+10 (если есть)
            };

            // Сначала проверяем, есть ли в 4-й паре информация о 5-й паре
            bool hasFifthPairInFourth = false;
            var fourthPairFirstData = GetCellData(data, dayIndex + 7, groupIndex);
            var fourthPairSecondData = GetCellData(data, dayIndex + 8, groupIndex);
            
            if (fourthPairFirstData.Contains("5п") || fourthPairSecondData.Contains("5п") ||
                fourthPairFirstData.Contains("5 пара") || fourthPairSecondData.Contains("5 пара"))
            {
                hasFifthPairInFourth = true;
            }

            // Фильтруем каждую пару с использованием правильной логики
            int maxPairs = hasFifthPairInFourth ? 5 : 4;
            for (int pairNumber = 1; pairNumber <= maxPairs; pairNumber++)
            {
                if (pairOffsets.ContainsKey(pairNumber))
                {
                    var offsets = pairOffsets[pairNumber];
                    FilterClass(pairNumber, offsets.first-1, offsets.second-1, dayIndex, groupIndex, data, filteredData);
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

        Console.WriteLine($"Пара {classNumber}: first='{firstParaData}', second='{secondParaData}', неделя='{_weekStateText}'");

        // Обработка 4-й и 5-й пар с учетом подгрупп
        if (classNumber == 4)
        {
            // Проверяем, содержит ли ячейка информацию о 4-й и 5-й парах
            bool hasFourthPair = (firstParaData.Contains("4п") || secondParaData.Contains("4п") ||
                                firstParaData.Contains("4 пара") || secondParaData.Contains("4 пара"));
            bool hasFifthPair = (firstParaData.Contains("5п") || secondParaData.Contains("5п") ||
                               firstParaData.Contains("5 пара") || secondParaData.Contains("5 пара"));

            if (hasFourthPair || hasFifthPair)
            {
                // Разделяем данные на 4-ю и 5-ю пары
                var fourthPairData = new List<string>();
                var fifthPairData = new List<string>();

                // Функция для разделения строки на части по маркерам пар
                var splitPairData = new Action<string>((data) =>
                {
                    if (string.IsNullOrEmpty(data)) return;

                    // Если строка содержит информацию о 5-й паре
                    if (data.Contains("5 пара") || data.Contains("5п"))
                    {
                        // Разделяем по "5 пара" или "5п"
                        string[] separators = { "5 пара", "5п" };
                        var parts = data.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length >= 2)
                        {
                            // Первая часть - 4-я пара
                            var fourthPart = parts[0].Trim();
                            if (!string.IsNullOrEmpty(fourthPart))
                            {
                                fourthPairData.Add(fourthPart);
                            }

                            // Вторая часть - 5-я пара
                            var fifthPart = parts[1].Trim();
                            if (!string.IsNullOrEmpty(fifthPart))
                            {
                                fifthPairData.Add(fifthPart);
                            }
                        }
                        else if (parts.Length == 1)
                        {
                            // Если только одна часть, но есть маркер 5-й пары
                            fifthPairData.Add(data);
                        }
                    }
                    else if (data.Contains("4п") || data.Contains("4 пара"))
                    {
                        fourthPairData.Add(data);
                    }
                    else
                    {
                        // Если нет маркеров, считаем это данными для 4-й пары
                        fourthPairData.Add(data);
                    }
                });

                // Анализируем первую строку
                splitPairData(firstParaData);

                // Анализируем вторую строку
                splitPairData(secondParaData);

                // Добавляем 4-ю пару только если есть данные
                if (fourthPairData.Count > 0)
                {
                    string fourthPairText = string.Join(" \n", fourthPairData).Trim();
                    if (!string.IsNullOrEmpty(fourthPairText))
                    {
                        filteredData.Add($"4пара: {fourthPairText}");
                    }
                }

                // Добавляем 5-ю пару только если есть данные
                if (fifthPairData.Count > 0)
                {
                    string fifthPairText = string.Join(" \n", fifthPairData).Trim();
                    if (!string.IsNullOrEmpty(fifthPairText))
                    {
                        filteredData.Add($"5пара: {fifthPairText}");
                    }
                }

                return;
            }
        }

        // Обработка 5-й пары (если она обрабатывается отдельно)
        if (classNumber == 5)
        {
            // Проверяем, была ли 5-я пара уже обработана в 4-й паре
            var fourthPairFirstData = GetCellData(data, dayIndex + 7, groupIndex);
            var fourthPairSecondData = GetCellData(data, dayIndex + 8, groupIndex);

            if (fourthPairFirstData.Contains("5п") || fourthPairSecondData.Contains("5п") ||
                fourthPairFirstData.Contains("5 пара") || fourthPairSecondData.Contains("5 пара"))
            {
                // 5-я пара уже обработана в 4-й паре, пропускаем
                return;
            }
        }

        // НОВАЯ ЛОГИКА: Определяем тип данных в ячейках
        bool isEvenWeek = _weekStateText?.ToLower().Contains("нечёт") == false;

        // Случай 1: Обе ячейки пустые
        if (string.IsNullOrEmpty(firstParaData) && string.IsNullOrEmpty(secondParaData))
        {
            return; // Пара отсутствует
        }
        // Случай 2: Только одна ячейка содержит данные - пара не зависит от недели
        else if (string.IsNullOrEmpty(firstParaData) && !string.IsNullOrEmpty(secondParaData))
        {
            // Одна пара для обеих недель (во второй ячейке)
            filteredData.Add($"{classNumber}пара: {secondParaData}");
        }
        else if (!string.IsNullOrEmpty(firstParaData) && string.IsNullOrEmpty(secondParaData))
        {
            // Одна пара для обеих недель (в первой ячейке)
            filteredData.Add($"{classNumber}пара: {firstParaData}");
        }
        // Случай 3: Обе ячейки содержат одинаковые данные - пара не зависит от недели
        else if (firstParaData.Trim() == secondParaData.Trim())
        {
            // Одинаковые данные - пара не зависит от недели
            filteredData.Add($"{classNumber}пара: {firstParaData}");
        }
        // Случай 4: Разные данные в ячейках - зависит от недели
        else if (!string.IsNullOrEmpty(firstParaData) && !string.IsNullOrEmpty(secondParaData))
        {
            if (isEvenWeek)
            {
                // ЧЕТНАЯ неделя - берем вторую строку (нижнюю ячейку)
                filteredData.Add($"{classNumber}пара: {secondParaData}");
            }
            else
            {
                // НЕЧЕТНАЯ неделя - берем первую строку (верхнюю ячейку)
                filteredData.Add($"{classNumber}пара: {firstParaData}");
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