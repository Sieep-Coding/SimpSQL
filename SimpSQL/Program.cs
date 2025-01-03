// Maintainer:
// https://www.github.com/Sieep-Coding/
// nickstambaugh@proton.me
// LICENSE: MIT

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;

public class SimpSQL
{
    private readonly List<Dictionary<string, string>> _table = new();

    public void Execute(string command)
    {
        var token = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (token.Length == 0 || token == null || string.IsNullOrWhiteSpace(command))
        {
            Console.WriteLine("Invalid command");
            return;
        }
        var operation = token[0].ToUpper();
        switch (operation)
        {
            case "INSERT":
                ExecuteInsert(token);
                break;
            case "SELECT":
                ExecuteSelect(token);
                break;
            case "UPDATE":
                ExecuteUpdate(token);
                break;
            case "DELETE":
                ExecuteDelete(token);
                break;
            case "HELP":
                PrintHelp(token);
                break;
            case "RANDOM":
                ExecuteRandomQuery(token);
                break;
            case "INSIGHTS":
                PrintTableInsights();
                break;
            case "SAVE":
                ExecuteSave(token);
                break;
            case "LOAD":
                ExecuteLoad(token);
                break;
            default:
                Console.WriteLine("Invalid operation. Type 'HELP' for a quick-start.");
                break;
        }
    }

    private void ExecuteInsert(string[] tokens)
    {

        if (tokens.Length < 3 || !tokens[1].Equals("INTO", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Invalid INSERT syntax");
            return;
        }

        var fields = tokens[2].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length % 2 != 0)
        {
            Console.WriteLine("Use field-value pairs.");
            return;
        }

        var row = new Dictionary<string, string>();
        for (int i = 0; i < fields.Length; i += 2)
        {
            row[fields[i]] = fields[i + 1];
        }
        _table.Add(row);
        Console.WriteLine("Row inserted.");
    }

    private void ExecuteRandomQuery(string[] tokens)
    {
        if (_table.Count == 0)
        {
            Console.WriteLine("The table is empty. No random queries can be generated.");
            return;
        }

        var random = new Random();
        var operation = random.Next(0, 2) == 0 ? "SELECT" : "UPDATE";

        if (operation == "SELECT")
        {
            // Randomly select columns for the SELECT query
            var allColumns = _table[0].Keys.ToArray();
            var selectedColumns = allColumns
                .OrderBy(_ => random.Next())
                .Take(random.Next(1, allColumns.Length + 1))
                .ToArray();

            Console.WriteLine($"Generated Query: SELECT {string.Join(",", selectedColumns)}");
            ExecuteSelect(new[] { "SELECT", string.Join(",", selectedColumns) });
        }
        else if (operation == "UPDATE")
        {

            var allColumns = _table[0].Keys.ToArray();
            var conditionColumn = allColumns[random.Next(allColumns.Length)];

            var conditionValues = _table
                .Select(row => row[conditionColumn])
                .Where(val => val != null)
                .ToArray();

            if (conditionValues.Length > 0)
            {
                var conditionValue = conditionValues[random.Next(conditionValues.Length)];

                var setColumns = allColumns
                    .OrderBy(_ => random.Next())
                    .Take(random.Next(1, allColumns.Length + 1))
                    .ToArray();

                var setClause = string.Join(",", setColumns.Select(col => $"{col}={GenerateRandomValue()}"));

                Console.WriteLine($"Generated Query: UPDATE SET {setClause} WHERE {conditionColumn}={conditionValue}");
                ExecuteUpdate(new[] { "UPDATE", "SET", setClause, "WHERE", $"{conditionColumn}={conditionValue}" });
            }
            else
            {
                Console.WriteLine($"Generated Query: UPDATE SET (no valid condition found)");
            }
        }
    }

    private string GenerateRandomValue()
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, 5)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }

    private void PrintTableInsights()
    {
        Console.WriteLine($"Table has {_table.Count} rows.");

        if (_table.Count == 0)
        {
            Console.WriteLine("The table is empty.");
            return;
        }

        var columns = _table[0].Keys.ToList();
        Console.WriteLine($"Columns: {string.Join(", ", columns)}");

        foreach (var column in columns)
        {
            var numericValues = _table
                .Select(row => row[column])
                .Where(value => value != null && IsNumericValue(value))
                .Select(value => double.Parse(value))
                .ToList();

            if (numericValues.Any())
            {
                var average = numericValues.Average();
                var min = numericValues.Min();
                var max = numericValues.Max();
                var sum = numericValues.Sum();
                var count = numericValues.Count;

                Console.WriteLine($"\nColumn '{column}' Statistics:");
                Console.WriteLine($"  Count: {count}");
                Console.WriteLine($"  Sum: {sum}");
                Console.WriteLine($"  Average: {average}");
                Console.WriteLine($"  Min: {min}");
                Console.WriteLine($"  Max: {max}");

                Console.WriteLine("\n  Value Distribution:");
                var bins = 10;
                var range = (max - min) / bins;
                var histogram = new int[bins];

                foreach (var value in numericValues)
                {
                    var binIndex = (int)((value - min) / range);
                    binIndex = Math.Min(binIndex, bins - 1);
                    histogram[binIndex]++;
                }

                for (int i = 0; i < bins; i++)
                {
                    var binStart = min + i * range;
                    var binEnd = binStart + range;
                    var bar = new string('#', histogram[i]);
                    Console.WriteLine($"  {binStart:F2} - {binEnd:F2}: {bar}");
                }
            }
            else
            {
                Console.WriteLine($"\nColumn '{column}' Statistics:");
                Console.WriteLine("  Non-numeric column - no statistics available.");
            }
        }
    }

    private bool IsNumericValue(string value)
    {
        return double.TryParse(value, out _);
    }

    private void ExecuteSelect(string[] tokens)
    {
        if (tokens.Length < 2)
        {
            Console.WriteLine("Invalid SELECT syntax");
            return;
        }
        var columns = tokens[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var row in _table)
        {
            foreach (var column in columns)
            {
                if (row.TryGetValue(column, out var value))
                {
                    Console.Write($"{column}:{value}\t");
                }
                else
                {
                    Console.Write($"{column}:NULL\t");
                }
            }
            Console.WriteLine();
        }
    }

    //private void DeleteAllRows(string[] tokens)
    //{
    //    _table.Clear();
    //}

    private void ExecuteDelete(string[] tokens)
    {
        if (_table.Count == 0)
        {
            Console.WriteLine("The table is empty. Cannot be deleted.");
            return;
        }
        if (tokens.Length < 3 || !tokens[1].Equals("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Invalid DELETE syntax");
            return;
        }
        var condition = tokens[2].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
        if (condition.Length != 2)
        {
            Console.WriteLine("Invalid condition");
            return;
        }

        var column = condition[0];
        var value = condition[1];
        _table.RemoveAll(row => row.TryGetValue(column, out var rowValue) && rowValue == value);
        Console.WriteLine("Rows deleted.");
    }

    private void PrintHelp(string[] tokens)
    {
        if (tokens.Length == 1 && tokens[0].Equals("HELP", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("  Guide for the SQL-SIMPS:");
            Console.WriteLine("  INSERT INTO <field1>,<value1>,<field2>,<value2>...");
            Console.WriteLine("  SELECT <field1>,<field2>,...");
            Console.WriteLine("  DELETE WHERE <field>=<value>");
            Console.WriteLine("  UPDATE SET <field1>=<value1>,<field2>=<value2> WHERE <field>=<value>");
            Console.WriteLine("  RANDOM (Run SELECT or UPDATE randomly)");
            Console.WriteLine("  INSIGHTS (View your table insights quickly");
        }
    }

    private void ExecuteUpdate(string[] tokens)
    {
        if (tokens.Length < 4 || !tokens[1].Equals("SET", StringComparison.OrdinalIgnoreCase) || !tokens[3].Equals("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Invalid UPDATE syntax");
            return;
        }

        var setPart = tokens[2].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var updates = new Dictionary<string, string>();
        foreach (var pair in setPart)
        {
            var fieldValue = pair.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (fieldValue.Length != 2)
            {
                Console.WriteLine($"Invalid SET clause: {pair}");
                return;
            }
            updates[fieldValue[0].Trim()] = fieldValue[1].Trim();
        }

        var conditionPart = string.Join(" ", tokens.Skip(4)).Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
        if (conditionPart.Length != 2)
        {
            Console.WriteLine("Invalid WHERE clause");
            return;
        }

        var conditionField = conditionPart[0].Trim();
        var conditionValue = conditionPart[1].Trim();

        bool updated = false;
        foreach (var row in _table.Where(row => row.TryGetValue(conditionField, out var val) && val == conditionValue))
        {
            foreach (var update in updates)
            {
                row[update.Key] = update.Value;
            }
            updated = true;
        }

        Console.WriteLine(updated ? "Rows updated." : "No rows matched the condition.");
    }

    private void ExecuteSave(string[] tokens)
    {
        if (tokens.Length < 2)
        {
            Console.WriteLine("Please specify a format: CSV, XML, or JSON.");
            return;
        }

        var format = tokens[1].ToUpper();
        if (format == "CSV")
        {
            SaveToCsv();
        }
        else if (format == "JSON")
        {
            SaveToJson();
        }
        else if (format == "XML")
        {
            SaveToXML();
        }
        else
        {
            Console.WriteLine("Invalid format. Use CSV, XML, or JSON.");
        }
    }

    private void ExecuteLoad(string[] tokens)
    {
        if (tokens.Length < 2)
        {
            Console.WriteLine("Please specify a format: CSV, XML, or JSON.");
            return;
        }

        var format = tokens[1].ToUpper();
        if (format == "CSV")
        {
            LoadFromCsv();
        }
        else if (format == "JSON")
        {
            LoadFromJson();
        }
        else if (format == "XML")
        {
            LoadFromXML();
        }
        else
        {
            Console.WriteLine("Invalid format. Use CSV, XML, or JSON.");
        }
    }

    private void LoadFromCsv()
    {
        Console.WriteLine("Enter the file to load from:");
        var filePath = Console.ReadLine();
        if (!File.Exists(filePath))
        {
            Console.WriteLine("No data file found.");
            return;
        }
        using (var reader = new StreamReader(filePath))
        {
            var columns = reader.ReadLine()?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (columns == null)
            {
                Console.WriteLine("Invalid data file.");
                return;
            }
            _table.Clear();
            while (!reader.EndOfStream)
            {
                var values = reader.ReadLine()?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values == null || values.Length != columns.Length)
                {
                    Console.WriteLine("Invalid data file.");
                    return;
                }
                var row = new Dictionary<string, string>();
                for (int i = 0; i < columns.Length; i++)
                {
                    row[columns[i]] = values[i];
                }
                _table.Add(row);
            }
        }
        Console.WriteLine($"Table loaded from {filePath}.");
    }

    private void SaveToCsv()
    {
        while (true)
        {
            Console.WriteLine("Enter the file path to save the table to:");
            var filePath = Console.ReadLine();
            filePath = filePath + ".csv";
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.WriteLine("Invalid file path.");
                continue;
            }
            using (var writer = new StreamWriter(filePath))
            {
                var columns = _table[0].Keys.ToList();
                writer.WriteLine(string.Join(",", columns));
                foreach (var row in _table)
                {
                    writer.WriteLine(string.Join(",", row.Values));
                }
            }
            Console.WriteLine($"Table saved to {filePath}.");
            break;
        }
    }

    private void LoadFromJson()
    {
        Console.WriteLine("Enter the file to load from");
        var filePath = CheckPath();
        var json = File.ReadAllText(filePath);
        var data = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
        if (data != null)
        {
            _table.Clear();
            _table.AddRange(data);
            Console.WriteLine($"Table loaded from {filePath}.");
        }
        else
        {
            Console.WriteLine("Failed to load data from JSON.");
        }
    }

    public string CheckPath()
    {
        var filePath = Console.ReadLine();
        if (!File.Exists(filePath))
        {
            Console.WriteLine("No data file found.");
            return "";
        }
        return filePath;
    }

    private void SaveToJson()
    {
        Console.WriteLine("Enter the file path to save the table to:");
        var filePath = Console.ReadLine();
        filePath = filePath + ".json";
        var json = JsonConvert.SerializeObject(_table, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(filePath, json);
        Console.WriteLine($"Table saved to {filePath}.");
    }

    private void LoadFromXML()
    {
        Console.WriteLine("Enter the file to load from");
        var filePath = Console.ReadLine();
        filePath = filePath + ".xml";

        if (!File.Exists(filePath))
        {
            Console.WriteLine("No data file found.");
            return;
        }

        try
        {
            var xml = new XmlDocument();
            xml.Load(filePath);

            var root = xml.DocumentElement;
            if (root == null || root.Name != "Table")
            {
                Console.WriteLine("Invalid XML format.");
                return;
            }

            _table.Clear();

            var rowNodes = root.SelectNodes("Row");
            if (rowNodes == null)
            {
                Console.WriteLine("No rows found in the XML file.");
                return;
            }

            foreach (XmlNode rowNode in rowNodes)
            {
                var row = new Dictionary<string, string>();

                foreach (XmlNode fieldNode in rowNode.ChildNodes)
                {
                    row[fieldNode.Name] = fieldNode.InnerText;
                }

                _table.Add(row);
            }

            Console.WriteLine($"Table loaded from {filePath}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while loading the XML file: {ex.Message}");
        }
    }


    private void SaveToXML()
    {
        Console.WriteLine("Enter the file path to save the table to:");
        var filePath = Console.ReadLine();
        filePath = filePath + ".xml";
        var xml = new XmlDocument();
        var root = xml.CreateElement("Table");
        xml.AppendChild(root);

        foreach (var row in _table)
        {
            var rowElement = xml.CreateElement("Row");
            root.AppendChild(rowElement);

            foreach (var pair in row)
            {
                var fieldElement = xml.CreateElement(pair.Key);
                fieldElement.InnerText = pair.Value;
                rowElement.AppendChild(fieldElement);
            }
        }

        xml.Save(filePath);
        Console.WriteLine($"Table saved to {filePath}.");
    }

    public void InitMain()
    {
        Console.WriteLine("  SimpSQL REPL -- For The Simpers");
        Console.WriteLine("  --------------------------");
        Console.WriteLine("  Run 'HELP' for a quick-start.");
        Console.WriteLine("  Enter commands (type 'EXIT' to quit):");
    }

    public static void Main(string[] args)
    {
        var sql = new SimpSQL();
        sql.InitMain();

        while (true)
        {
            Console.Write("    >> ");
            var input = Console.ReadLine();
            if (input == null) { throw new ArgumentNullException($"error {input}"); };
            if (input?.ToUpper() == "EXIT") { Console.WriteLine("Quitting..."); break; };

            if (input?.ToUpper() == "HELP")
            {
                sql.PrintHelp(input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                continue;
            }
            sql.Execute(input);
        }
    }
}