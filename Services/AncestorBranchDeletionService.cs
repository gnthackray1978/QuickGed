using QuickGed.Domain;
using QuickGed.Types;

namespace QuickGed.Services;

public class AncestorBranchDeletionService
{
    public AncestorBranchDeletionResult CalculateDeleteSet(GedDb db, int personId, ISet<int> excludedIds)
    {
        if (!db.PersonDictionary.ContainsKey(personId))
        {
            throw new ArgumentException($"Person id '{personId}' was not found.");
        }

        var ancestors = GetAncestors(db, personId);
        var descendants = GetDescendants(db, ancestors);

        var candidates = new HashSet<int>(ancestors);
        candidates.UnionWith(descendants);

        var excluded = new HashSet<int>(candidates.Where(excludedIds.Contains));
        var finalDelete = new HashSet<int>(candidates.Where(id => !excludedIds.Contains(id)));

        return new AncestorBranchDeletionResult
        {
            PersonId = personId,
            AncestorIds = ancestors,
            CandidateDeleteIds = candidates,
            ExcludedIds = excluded,
            FinalDeleteIds = finalDelete
        };
    }

    private static HashSet<int> GetAncestors(GedDb db, int personId)
    {
        var result = new HashSet<int>();
        var stack = new Stack<int>(GetParentIds(db, personId));

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current == 0 || !result.Add(current))
            {
                continue;
            }

            foreach (var parentId in GetParentIds(db, current))
            {
                stack.Push(parentId);
            }
        }

        return result;
    }

    private static HashSet<int> GetDescendants(GedDb db, IEnumerable<int> startIds)
    {
        var result = new HashSet<int>();
        var stack = new Stack<int>(startIds);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current == 0 || !result.Add(current))
            {
                continue;
            }

            foreach (var childId in GetChildIds(db, current))
            {
                stack.Push(childId);
            }
        }

        return result;
    }

    private static IEnumerable<int> GetParentIds(GedDb db, int personId)
    {
        var results = new HashSet<int>();

        if (db.PersonDictionary.TryGetValue(personId, out Person? person))
        {
            if (person.FatherId != 0)
            {
                results.Add(person.FatherId);
            }

            if (person.MotherId != 0)
            {
                results.Add(person.MotherId);
            }
        }

        if (db.ParentDictionary.TryGetValue(personId, out List<Node>? parents))
        {
            foreach (var parent in parents)
            {
                if (parent.Id != 0)
                {
                    results.Add(parent.Id);
                }
            }
        }

        return results;
    }

    private static IEnumerable<int> GetChildIds(GedDb db, int personId)
    {
        var results = new HashSet<int>();

        if (db.PersonDictionary.TryGetValue(personId, out Person? person))
        {
            foreach (var childId in person.Children.Select(c => c.Id).Where(id => id != 0))
            {
                results.Add(childId);
            }
        }

        foreach (var child in db.Persons.Where(p => p.FatherId == personId || p.MotherId == personId))
        {
            results.Add(child.Id);
        }

        return results;
    }
}
