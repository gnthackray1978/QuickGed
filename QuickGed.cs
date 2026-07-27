using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using QuickGed.Domain;
using QuickGed.Services;
using QuickGed.Types;

namespace QuickGed
{
    // Reading the gedfile into a list of relationships, persons, and childrelationships
    // labelling file
    // get lists of tree by group name
    public class QuickGed
    {
        private string _gedPath;
        private readonly string _exclusionFilePath;
        private readonly HashSet<int> _excludedPersonIds;
        private readonly HashSet<string> _deletedPersonReferences;
        private readonly INodeTypeCalculator _nodeTypeCalculator;

        public GedDb _GedDb { get; set; }
        public string GedPath => _gedPath;

        public QuickGed(string filePath)
        {
            this._gedPath = filePath;
            _exclusionFilePath = Path.Combine(AppContext.BaseDirectory, "quickged.exclusions.json");
            _excludedPersonIds = LoadExcludedPersonIds();
            _deletedPersonReferences = new HashSet<string>();
            _nodeTypeCalculator = new NodeTypeCalculator();
            _GedDb = new GedDb();
        }

        public bool IsParsed => _GedDb != null;

        public int GetMyId()
        {
            return -1;
        }

        public HashSet<int> GetListOfTreeIds()
        {
            var lst = GetTreeRootPersons().Select(s => s.Id);
            var set = new HashSet<int>();

            foreach (var i in lst)
            {
                set.Add(i);
            }

            return set;
        }

        public Dictionary<int, string> GetTreeRootNameDictionary()
        {
            var nameDictionary = new Dictionary<int, string>();
            var lst = GetTreeRootPersons();

            foreach (var i in lst)
            {
                nameDictionary.Add(i.Id, i.FullName);
            }

            return nameDictionary;
        }

        public Dictionary<int, string> GetTreeGroupNameDictionary()
        {
            var nameDictionary = new Dictionary<int, string>();
            var gps = GetGroupPerson();

            foreach (var i in gps)
            {
                nameDictionary.Add(i.Id, i.FullName);
            }

            return nameDictionary;
        }

        public IReadOnlyList<Node> GetTreeRootPersons()
        {
            int personId = GetMyId();

            var result = _GedDb.Persons.Where(p =>
                p != null && !string.IsNullOrEmpty(p.FullName) &&
                _nodeTypeCalculator.IsRootPerson(p.FullName) &&
                (!p.FullName.ToLower().Contains("group") || p.Id == personId));

            return result.ToList();
        }

        public List<IPerson> GetGroupPerson()
        {
            var groups = this._GedDb.Persons.Where(p => p != null && !string.IsNullOrEmpty(p.FullName) && p.FullName.ToLower().Contains("group"));
            return groups.Cast<IPerson>().ToList();
        }

        public Dictionary<string, List<string>> GetGroups()
        {
            var results = new Dictionary<string, List<string>>();
            var treeIds = GetListOfTreeIds();

            var tp = this._GedDb.Relationships
                .Select(s => new RelationSubSet() { Person1Id = s.Person1Id, Person2Id = s.Person2Id }).ToList();

            var nameDict = GetTreeRootNameDictionary();
            var groupNames = GetTreeGroupNameDictionary();

            foreach (var treeId in treeIds)
            {
                var groupMembers = tp.Where(t => t.MatchEither(treeId)).Select(s => s.GetOtherSide(treeId)).Distinct().ToList();
                var names = IdsToNames(groupMembers, groupNames);
                results.Add(nameDict[treeId], names);
            }

            return results;
        }

        private static List<string> IdsToNames(List<int> groupMembers, Dictionary<int, string> nameDict)
        {
            return (from gm in groupMembers where nameDict.ContainsKey(gm) select nameDict[gm]).ToList();
        }


        public void DummyEntry()
        {
            Console.WriteLine("dummy");
        }

        public bool AddExcludedPersonId(int personId)
        {
            if (!_excludedPersonIds.Add(personId))
            {
                return false;
            }

            SaveExcludedPersonIds();
            return true;
        }

        public bool RemoveExcludedPersonId(int personId)
        {
            if (!_excludedPersonIds.Remove(personId))
            {
                return false;
            }

            SaveExcludedPersonIds();
            return true;
        }

        public IReadOnlyList<int> GetExcludedPersonIds()
        {
            return _excludedPersonIds.OrderBy(i => i).ToList();
        }

        public AncestorBranchDeletionResult DeleteAncestorBranchesForPerson(int personId, bool dryRun = false)
        {
            if (_GedDb == null)
            {
                throw new InvalidOperationException("No parsed GED data found. Run ParseLabelledTree first.");
            }

            var deletionService = new AncestorBranchDeletionService();
            var result = deletionService.CalculateDeleteSet(_GedDb, personId, _excludedPersonIds);

            if (!dryRun)
            {
                foreach (var id in result.FinalDeleteIds)
                {
                    if (_GedDb.PersonReferenceById.TryGetValue(id, out var personRef) && !string.IsNullOrWhiteSpace(personRef))
                    {
                        _deletedPersonReferences.Add(personRef);
                    }
                }

                result.DeletedCount = _GedDb.DeletePeopleAndRebuild(result.FinalDeleteIds);
            }

            return result;
        }

        public void ParseLabelledTree()
        {
            var gp = new GedParser(_nodeTypeCalculator);

            _GedDb = gp.Parse(this._gedPath);
            Console.WriteLine($"[TRACE] QuickGed database populated with {_GedDb.Persons.Count} persons.");
            _deletedPersonReferences.Clear();

            var rootPersons = this.GetTreeRootPersons();

            Console.WriteLine($"[TRACE] Found {rootPersons.Count} root persons matching the default pattern for labelling.");

            foreach (var rp in rootPersons)
            {
                Console.WriteLine($"[TRACE] Root person: '{rp.FullName}' (ID: {rp.Id})");
            }

            var timer = new Stopwatch();
            timer.Start();

            var idx = 0;
            foreach (var rp in rootPersons)
            {
                TreeLabeller.LabelTree(this._GedDb.ParentDictionary, rp, rp.FullName);
                Console.Write("\r{0}%   ", idx);
                idx++;
            }

            timer.Stop();
            TimeSpan timeTaken = timer.Elapsed;
            Console.WriteLine("\nTime taken: " + timeTaken.ToString(@"m\:ss\.fff"));
            Console.WriteLine("finished");
        }

        public string GetPersonDisplayName(int personId)
        {
            if (_GedDb == null)
            {
                return $"{personId} (unloaded)";
            }

            if (_GedDb.PersonDictionary.TryGetValue(personId, out var person))
            {
                return $"{person.Id}: {person.FullName}";
            }

            return $"{personId} (not found)";
        }

        public void DumpPersonOrigins()
        {
            if (_GedDb == null)
            {
                Console.WriteLine("No parsed GED data found.");
                return;
            }

            Console.WriteLine($"Dumping origins for {_GedDb.Persons.Count} persons:");
            foreach (var person in _GedDb.Persons)
            {
                var originDisplay = string.IsNullOrWhiteSpace(person.Origin) ? "(None)" : person.Origin;
                Console.WriteLine($"- ID: {person.Id} | Name: {person.FullName} | Origin: {originDisplay}");
            }
        }

        public void ExportCurrentGed(string outputPath)
        {
            if (_GedDb == null)
            {
                throw new InvalidOperationException("No parsed GED data found. Run ParseLabelledTree first.");
            }

            var exportService = new GedExportService();
            exportService.ExportWithoutDeletedPeople(_gedPath, outputPath, _deletedPersonReferences);
        }

        public void ExportTreeAsCsv(string outputPath, string? treeOriginPattern = null)
        {
            if (_GedDb == null)
            {
                throw new InvalidOperationException("No parsed GED data found. Run ParseLabelledTree first.");
            }

            var personsToExport = string.IsNullOrWhiteSpace(treeOriginPattern)
                ? _GedDb.Persons
                : _GedDb.Persons.Where(p => p.Origin != null && p.Origin.Contains(treeOriginPattern, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!personsToExport.Any())
            {
                throw new InvalidOperationException("No persons found matching the specified criteria.");
            }

            using var writer = new StreamWriter(outputPath);

            // Write headers
            writer.WriteLine("Id,FullName,Gender,BirthDate,BirthLocation,DeathDate,DeathLocation,Origin,Component,Lineage,cm,tester,TreeName,IsDirectAncestor,FatherId,MotherId");

            foreach (var p in personsToExport)
            {
                var birthLocation = CleanupLocation(p.BirthLocation);
                var deathLocation = CleanupLocation(p.DeathLocation);
                var component = ExtractComponent(p.Origin);
                var lineage = ExtractLineage(p.Origin);
                var cm = ExtractCm(p.Origin);
                var tester = ExtractTester(p.Origin);
                var treeName = ExtractTreeName(p.Origin);

                var line = $"{p.Id}," +
                           $"{EscapeCsv(p.FullName)}," +
                           $"{EscapeCsv(p.Gender)}," +
                           $"{EscapeCsv(p.BirthDate)}," +
                           $"{EscapeCsv(birthLocation)}," +
                           $"{EscapeCsv(p.DeathDate)}," +
                           $"{EscapeCsv(deathLocation)}," +
                           $"{EscapeCsv(p.Origin)}," +
                           $"{EscapeCsv(component)}," +
                           $"{lineage}," +
                           $"{EscapeCsv(cm)}," +
                           $"{EscapeCsv(tester)}," +
                           $"{EscapeCsv(treeName)}," +
                           $"{p.IsDirectAncestor}," +
                           $"{p.FatherId}," +
                           $"{p.MotherId}";
                writer.WriteLine(line);
            }
        }

        private static string ExtractTreeName(string? origin)
        {
            if (string.IsNullOrWhiteSpace(origin)) return string.Empty;
            var segments = origin.Split('|');
            for (int i = 0; i < segments.Length; i++)
            {
                // Find the segment that is the numeric 'cm'
                if (double.TryParse(segments[i].Trim(), out _))
                {
                    // The tree name is the segment immediately following it
                    if (i + 1 < segments.Length)
                    {
                        // Replace '!' with space and remove GEDCOM surname slashes '/'
                        return segments[i + 1].Trim().Replace('!', ' ').Replace("/", "");
                    }
                    break;
                }
            }
            return string.Empty;
        }

        private static string ExtractTester(string? origin)
        {
            if (string.IsNullOrWhiteSpace(origin)) return string.Empty;
            var segments = origin.Split('|');
            foreach (var segment in segments)
            {
                var trimmed = segment.Trim();
                if (trimmed.Contains("ct", StringComparison.OrdinalIgnoreCase)) return "ct";
                if (trimmed.Contains("ah", StringComparison.OrdinalIgnoreCase)) return "ah";
            }
            return string.Empty;
        }

        private static string ExtractCm(string? origin)
        {
            if (string.IsNullOrWhiteSpace(origin)) return string.Empty;
            var segments = origin.Split('|');
            foreach (var segment in segments)
            {
                var trimmed = segment.Trim();
                if (double.TryParse(trimmed, out _))
                {
                    return trimmed;
                }
            }
            return string.Empty;
        }

        private static int ExtractLineage(string? origin)
        {
            if (string.IsNullOrWhiteSpace(origin)) return -1;
            if (origin.Contains("pat", StringComparison.OrdinalIgnoreCase)) return 1;
            if (origin.Contains("mat", StringComparison.OrdinalIgnoreCase)) return 0;
            return -1;
        }

        private static string ExtractComponent(string? origin)
        {
            if (string.IsNullOrWhiteSpace(origin)) return string.Empty;
            // Look for '|c' followed by anything that isn't a pipe
            var match = Regex.Match(origin, @"\|(c[^|]+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static string? CleanupLocation(string? location)
        {
            if (string.IsNullOrWhiteSpace(location)) return location;
            location = Regex.Replace(location, @"\s*,\s*", ",");
            return Regex.Replace(location.Replace(',', '/'), @"\s+", " ");
        }

        private static string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;

            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        private HashSet<int> LoadExcludedPersonIds()
        {
            try
            {
                if (!File.Exists(_exclusionFilePath))
                {
                    return new HashSet<int>();
                }

                var raw = File.ReadAllText(_exclusionFilePath);
                var values = JsonSerializer.Deserialize<List<int>>(raw);

                return values == null ? new HashSet<int>() : new HashSet<int>(values);
            }
            catch
            {
                return new HashSet<int>();
            }
        }

        private void SaveExcludedPersonIds()
        {
            var ordered = _excludedPersonIds.OrderBy(i => i).ToList();
            var json = JsonSerializer.Serialize(ordered, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_exclusionFilePath, json);
        }
    }
}
