using QuickGed;

//var defaultGedPath = args.Length > 0 ? args[0] : string.Empty;

var defaultGedPath = @"C:\Users\gntha\Downloads\dev test tree\dev.ged";
var shouldExit = false;
QuickGed.QuickGed? app = null;

while (!shouldExit)
{
    SafeClearConsole();
    Console.WriteLine("QuickGed Main Menu");
    Console.WriteLine("==================");
    Console.WriteLine("1. ParseLabelledTree");
    Console.WriteLine("2. DummyEntry");
    Console.WriteLine("3. Dry Run Ancestor Branch Delete");
    Console.WriteLine("4. Delete Ancestor Branches");
    Console.WriteLine("5. Add Excluded Person Id");
    Console.WriteLine("6. Remove Excluded Person Id");
    Console.WriteLine("7. List Excluded Person Ids");
    Console.WriteLine("8. Export Current GED");
    Console.WriteLine("9. List Sub-Trees");
    Console.WriteLine("10. Dump Person Origins");
    Console.WriteLine("11. Export Tree as CSV");
    Console.WriteLine("0. Exit");
    Console.WriteLine();
    Console.Write("Select an option: ");

    var input = Console.ReadLine();

    switch (input)
    {
        case "1":
            RunParseLabelledTree(defaultGedPath, ref app);
            break;
        case "2":
            RunDummyEntry(app);
            break;
        case "3":
            RunAncestorBranchDelete(app, true);
            break;
        case "4":
            RunAncestorBranchDelete(app, false);
            break;
        case "5":
            AddExcludedPersonId(app);
            break;
        case "6":
            RemoveExcludedPersonId(app);
            break;
        case "7":
            ListExcludedPersonIds(app);
            break;
        case "8":
            ExportCurrentGed(app);
            break;
        case "9":
            ListSubTrees(app);
            break;
        case "10":
            RunDumpPersonOrigins(app);
            break;
        case "11":
            RunExportTreeAsCsv(app);
            break;
        case "0":
            shouldExit = true;
            break;
        default:
            Console.WriteLine("Invalid option. Press Enter to try again.");
            Console.ReadLine();
            break;
    }
}

static void SafeClearConsole()
{
    try
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
    }
    catch (IOException)
    {
        // Some debug hosts do not provide a clear-capable console handle.
    }
    catch (InvalidOperationException)
    {
        // No interactive console is available in this host.
    }
}

static void RunParseLabelledTree(string defaultGedPath, ref QuickGed.QuickGed? app)
{
    Console.WriteLine();
    Console.Write("Enter GED file path");

    if (!string.IsNullOrWhiteSpace(defaultGedPath))
    {
        Console.Write($" (press Enter for '{defaultGedPath}')");
    }

    Console.Write(": ");
    var pathInput = Console.ReadLine();
    var gedPath = string.IsNullOrWhiteSpace(pathInput) ? defaultGedPath : pathInput.Trim();

    if (string.IsNullOrWhiteSpace(gedPath))
    {
        Console.WriteLine("No GED path provided. Press Enter to return to menu.");
        Console.ReadLine();
        return;
    }

    try
    {
        app = new QuickGed.QuickGed(gedPath);
        app.ParseLabelledTree();
        Console.WriteLine($"GED file parsed and loaded. Total people in DB: {app._GedDb?.Persons?.Count ?? 0}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine("Press Enter to return to menu.");
    Console.ReadLine();
}

static void RunExportTreeAsCsv(QuickGed.QuickGed? app)
{
    Console.WriteLine();

    if (app == null || !app.IsParsed)
    {
        Console.WriteLine("No parsed GED data found. Run ParseLabelledTree first.");
        Console.WriteLine("Press Enter to return to menu.");
        Console.ReadLine();
        return;
    }

    var defaultCsvPath = Path.ChangeExtension(app.GedPath, ".csv");
    Console.Write($"Enter output CSV path (press Enter for '{defaultCsvPath}'): ");
    var outputPath = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(outputPath))
    {
        outputPath = defaultCsvPath;
    }

    Console.Write("Enter Tree Origin pattern to filter by (or press Enter to export all): ");
    var origin = Console.ReadLine()?.Trim();

    try
    {
        app.ExportTreeAsCsv(outputPath, origin);
        Console.WriteLine($"Successfully exported CSV to: {outputPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine("Press Enter to return to menu.");
    Console.ReadLine();
}

static void RunDumpPersonOrigins(QuickGed.QuickGed? app)
{
    Console.WriteLine();

    if (app == null || !app.IsParsed)
    {
        Console.WriteLine("No parsed GED data found. Run ParseLabelledTree first.");
        Console.WriteLine("Press Enter to return to menu.");
        Console.ReadLine();
        return;
    }

    try
    {
        app.DumpPersonOrigins();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine("Press Enter to return to menu.");
    Console.ReadLine();
}

static void RunDummyEntry(QuickGed.QuickGed? app)
{
    Console.WriteLine();
    if (app == null)
    {
        Console.WriteLine("No active QuickGed session. Parse a GED file first.");
    }
    else
    {
        app.DummyEntry();
    }

    Console.WriteLine("Press Enter to return to menu.");
    Console.ReadLine();
}

static void RunAncestorBranchDelete(QuickGed.QuickGed? app, bool dryRun)
{
    Console.WriteLine();

    if (app == null || !app.IsParsed)
    {
        Console.WriteLine("No parsed GED data found. Run ParseLabelledTree first.");
        Console.WriteLine("Press Enter to return to menu.");
        Console.ReadLine();
        return;
    }

    Console.Write("Enter person id: ");
    var input = Console.ReadLine();

    if (!int.TryParse(input, out var personId) || personId <= 0)
    {
        Console.WriteLine("Invalid person id.");
        Console.WriteLine("Press Enter to return to menu.");
        Console.ReadLine();
        return;
    }

    try
    {
        var result = app.DeleteAncestorBranchesForPerson(personId, dryRun);

        Console.WriteLine(dryRun ? "Dry run complete." : "Delete complete.");
        Console.WriteLine($"Ancestors found: {result.AncestorIds.Count}");
        Console.WriteLine($"Delete candidates: {result.CandidateDeleteIds.Count}");
        Console.WriteLine($"Excluded (kept): {result.ExcludedIds.Count}");
        Console.WriteLine($"Final delete set: {result.FinalDeleteIds.Count}");

        if (dryRun)
        {
            PrintPreview("Delete candidates", result.CandidateDeleteIds, app);
            PrintPreview("Excluded (kept)", result.ExcludedIds, app);
            PrintPreview("Final delete", result.FinalDeleteIds, app);
        }

        if (!dryRun)
        {
            Console.WriteLine($"Deleted people: {result.DeletedCount}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine("Press Enter to return to menu.");
    Console.ReadLine();
}

static void AddExcludedPersonId(QuickGed.QuickGed? app)
{
    Console.WriteLine();

    if (app == null)
    {
        Console.WriteLine("No active QuickGed session. Parse a GED file first.");
        Console.WriteLine("Press Enter to return to menu.");
        Console.ReadLine();
        return;
    }

    Console.Write("Enter person id to exclude: ");
    var input = Console.ReadLine();

    if (!int.TryParse(input, out var personId) || personId <= 0)
    {
        Console.WriteLine("Invalid person id.");
    }
    else
    {
        var added = app.AddExcludedPersonId(personId);
        Console.WriteLine(added ? "Person id added to exclusion list." : "Person id is already excluded.");
    }

    Console.WriteLine("Press Enter to return to menu.");
    Console.ReadLine();
}

static void RemoveExcludedPersonId(QuickGed.QuickGed? app)
{
    Console.WriteLine();

    if (app == null)
    {
        Console.WriteLine("No active QuickGed session. Parse a GED file first.");
        Console.WriteLine("Press Enter to return to menu.");
        Console.ReadLine();
        return;
    }

    Console.Write("Enter person id to remove from exclusions: ");
    var input = Console.ReadLine();

    if (!int.TryParse(input, out var personId) || personId <= 0)
    {
        Console.WriteLine("Invalid person id.");
    }
    else
    {
        var removed = app.RemoveExcludedPersonId(personId);
        Console.WriteLine(removed ? "Person id removed from exclusion list." : "Person id was not excluded.");
    }

    Console.WriteLine("Press Enter to return to menu.");
    Console.ReadLine();
}

static void ListExcludedPersonIds(QuickGed.QuickGed? app)
{
    Console.WriteLine();

    if (app == null)
    {
        Console.WriteLine("No active QuickGed session. Parse a GED file first.");
        Console.WriteLine("Press Enter to return to menu.");
        Console.ReadLine();
        return;
    }

    var excluded = app.GetExcludedPersonIds();
    if (excluded.Count == 0)
    {
        Console.WriteLine("Exclusion list is empty.");
    }
    else
    {
        Console.WriteLine("Excluded person ids:");
        foreach (var id in excluded)
        {
            Console.WriteLine($"- {id}");
        }
    }

    Console.WriteLine("Press Enter to return to menu.");
    Console.ReadLine();
}

static void ExportCurrentGed(QuickGed.QuickGed? app)
{
    Console.WriteLine();

    if (app == null || !app.IsParsed)
    {
        Console.WriteLine("No parsed GED data found. Run ParseLabelledTree first.");
        Console.WriteLine("Press Enter to return to menu.");
        Console.ReadLine();
        return;
    }

    Console.Write("Enter output GED path: ");
    var outputPath = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(outputPath))
    {
        Console.WriteLine("Output path is required.");
        Console.WriteLine("Press Enter to return to menu.");
        Console.ReadLine();
        return;
    }

    try
    {
        app.ExportCurrentGed(outputPath);
        Console.WriteLine($"Exported GED to: {outputPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine("Press Enter to return to menu.");
    Console.ReadLine();
}

static void PrintPreview(string title, IEnumerable<int> ids, QuickGed.QuickGed app)
{
    var preview = ids.OrderBy(i => i).Take(10).ToList();

    Console.WriteLine($"{title} preview (up to 10):");

    if (preview.Count == 0)
    {
        Console.WriteLine("- none");
        return;
    }

    foreach (var id in preview)
    {
        Console.WriteLine($"- {app.GetPersonDisplayName(id)}");
    }
}

static void ListSubTrees(QuickGed.QuickGed? app)
{
    Console.WriteLine();

    if (app == null || !app.IsParsed)
    {
        Console.WriteLine("No parsed GED data found. Run ParseLabelledTree first.");
        Console.WriteLine("Press Enter to return to menu.");
        Console.ReadLine();
        return;
    }

    try
    {
        var subTrees = app.GetTreeRootPersons();

        if (subTrees.Count == 0)
        {
            Console.WriteLine("No sub-trees found based on the specified naming convention.");
        }
        else
        {
            Console.WriteLine($"Found {subTrees.Count} sub-tree roots:");
            foreach (var root in subTrees)
            {
                Console.WriteLine($"- {root.FullName}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine("Press Enter to return to menu.");
    Console.ReadLine();
}