using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

var languageOption = new Option<string[]>(
    "--language",
    "List of programming languages (comma-separated) or 'all'. This option is required.")
{ IsRequired = true };

var outputOption = new Option<FileInfo>(
    "--output",
    "File path and name for the bundled file")
{ IsRequired = true };

var noteOption = new Option<bool>(
    "--note",
    "Include the source file's relative path as a comment in the bundle");

var sortOption = new Option<string>(
    "--sort",
    () => "name",  // ברירת המחדל תהיה לפי שם הקובץ
    "Sort files by 'name' or 'type'");

var removeEmptyLinesOption = new Option<bool>(
    "--remove-empty-lines",
    "Remove empty lines from the source code files");

var authorOption = new Option<string>(
    "--author",
    "Specify the author's name to include as a comment in the bundle file");

var bundleCommand = new Command("bundle", "Bundle Code Files into a Single File");
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);

bundleCommand.SetHandler((string[] languages, FileInfo output, bool note, string sort, bool removeEmptyLines, string author) =>
{
    try
    {
        // בודק אם המשתמש בחר "all" או רשימה של שפות
        var allLanguages = languages.Contains("all");

        // מוצא את כל קבצי הקוד בתיקיה
        var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
                              .Where(file =>
                                  allLanguages || languages.Contains(Path.GetExtension(file).TrimStart('.')))
                              .ToList();

        // מסדר את הקבצים לפי אופציה שנבחרה
        if (sort == "type")
        {
            files = files.OrderBy(Path.GetExtension).ToList();  // לפי סוג הקובץ
        }
        else
        {
            files = files.OrderBy(Path.GetFileName).ToList();  // לפי א"ב של שם הקובץ
        }

        using var writer = new StreamWriter(output.FullName);

        // כותב את שם היוצר אם סופק
        if (!string.IsNullOrWhiteSpace(author))
        {
            writer.WriteLine($"// Author: {author}");
        }

        // כותב את הקבצים לקובץ הפלט
        foreach (var file in files)
        {
            if (note)
            {
                // אם האופציה 'note' נבחרה, מוסיף את נתיב הקובץ כהערה
                writer.WriteLine($"// Source: {Path.GetRelativePath(Directory.GetCurrentDirectory(), file)}");
            }

            var lines = File.ReadAllLines(file);

            if (removeEmptyLines)
            {
                // אם האופציה 'remove-empty-lines' נבחרה, מסיר שורות ריקות
                lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            }

            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
        }

        Console.WriteLine($"Bundled {files.Count} files into {output.FullName}");
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("Error: file path is invalid");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}, languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

var rootCommand = new RootCommand("Root Command For File Bundler CLI");
rootCommand.AddCommand(bundleCommand);

// הרצת הפקודה
await rootCommand.InvokeAsync(args);