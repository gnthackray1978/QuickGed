﻿using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using QuickGed.Domain;
using QuickGed.Services;
using QuickGed.Types;

namespace QuickGed
{
    //reading the gedfile into a list of relationships, persons, and childrelationships
    //labelling file
    //get lists of tree by group name
    public class QuickGed
    {
        private string _gedPath;
        private readonly string _exclusionFilePath;
        private readonly HashSet<int> _excludedPersonIds;
        private readonly HashSet<string> _deletedPersonReferences;
        public GedDb _GedDb { get; set; }
        public string GedPath => _gedPath;

        public QuickGed(string filePath)
        {
            this._gedPath = filePath;
            _exclusionFilePath = Path.Combine(AppContext.BaseDirectory, "quickged.exclusions.json");
            _excludedPersonIds = LoadExcludedPersonIds();
            _deletedPersonReferences = new HashSet<string>();
        }

        public bool IsParsed => _GedDb != null;

        #region uninterested right now

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

            Regex r = new Regex(@"^(\[\d+\]|\d+\s|\|\[\d+\]\||\|\d+\|)");


            var result = _GedDb.Persons.Where(p =>
                p != null && !string.IsNullOrEmpty(p.FullName) && (!p.FullName.ToLower().Contains("group") || p.Id == personId));

            var persons = new List<Node>();
            
            // Added the loop here so your traces print out during ParseLabelledTree
            foreach (var person in result)
            {
                // Console.WriteLine($"[TRACE] Checking potential root: '{person.FullName}'");
                if (!string.IsNullOrEmpty(person.FullName) && r.IsMatch(person.FullName.Trim()))
                {
                     Console.WriteLine($"[TRACE] Checking potential root: '{person.FullName}'");
                    persons.Add(person);
                }
            }
            
            return persons;
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


        #endregion

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
            var gp = new GedParser(new NodeTypeCalculator());

            _GedDb = gp.Parse(this._gedPath);
            Console.WriteLine($"[TRACE] QuickGed database populated with {_GedDb.Persons.Count} persons.");
            _deletedPersonReferences.Clear();



            var rootPersons = this.GetTreeRootPersons();

            Console.WriteLine($"[TRACE] Found {rootPersons.Count} root persons matching the default pattern for labelling.");

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
            string foo = "Time taken: " + timeTaken.ToString(@"m\:ss\.fff");

            Console.WriteLine(foo);

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
            writer.WriteLine("Id,FullName,Gender,BirthDate,BirthLocation,DeathDate,DeathLocation,Origin,IsDirectAncestor,FatherId,MotherId");

            foreach (var p in personsToExport)
            {
                var line = $"{p.Id}," +
                           $"{EscapeCsv(p.FullName)}," +
                           $"{EscapeCsv(p.Gender)}," +
                           $"{EscapeCsv(p.BirthDate)}," +
                           $"{EscapeCsv(p.BirthLocation)}," +
                           $"{EscapeCsv(p.DeathDate)}," +
                           $"{EscapeCsv(p.DeathLocation)}," +
                           $"{EscapeCsv(p.Origin)}," +
                           $"{p.IsDirectAncestor}," +
                           $"{p.FatherId}," +
                           $"{p.MotherId}";
                writer.WriteLine(line);
            }
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
