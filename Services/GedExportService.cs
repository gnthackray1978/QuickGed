namespace QuickGed.Services;

public class GedExportService
{
    public void ExportWithoutDeletedPeople(string sourcePath, string destinationPath, ISet<string> deletedPersonReferences)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source GED path is required.");
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("Destination GED path is required.");
        }

        var inputLines = TestData.source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var outputLines = new List<string>();

        var currentRecord = new List<string>();

        foreach (var line in inputLines)
        {
            if (IsLevelZero(line) && currentRecord.Count > 0)
            {
                AppendRecordIfIncluded(currentRecord, outputLines, deletedPersonReferences);
                currentRecord = new List<string>();
            }

            currentRecord.Add(line);
        }

        if (currentRecord.Count > 0)
        {
            AppendRecordIfIncluded(currentRecord, outputLines, deletedPersonReferences);
        }

        File.WriteAllLines(destinationPath, outputLines);
    }

    private static bool IsLevelZero(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var trimmed = line.TrimStart();
        return trimmed.StartsWith("0 ", StringComparison.Ordinal);
    }

    private static void AppendRecordIfIncluded(List<string> recordLines, List<string> output, ISet<string> deletedPersonReferences)
    {
        if (recordLines.Count == 0)
        {
            return;
        }

        var header = GedcomLine.Parse(recordLines[0]);
        if (header == null)
        {
            output.AddRange(recordLines);
            return;
        }

        if (header.Type == "INDI" && !string.IsNullOrEmpty(header.Id))
        {
            if (deletedPersonReferences.Contains(header.Id))
            {
                return;
            }

            output.AddRange(recordLines);
            return;
        }

        if (header.Type == "FAM")
        {
            foreach (var line in recordLines)
            {
                var parsed = GedcomLine.Parse(line);
                if (parsed == null)
                {
                    continue;
                }

                if ((parsed.Type == "HUSB" || parsed.Type == "WIFE" || parsed.Type == "CHIL") &&
                    !string.IsNullOrEmpty(parsed.Reference) &&
                    deletedPersonReferences.Contains(parsed.Reference))
                {
                    return;
                }
            }

            output.AddRange(recordLines);
            return;
        }

        output.AddRange(recordLines);
    }
}
