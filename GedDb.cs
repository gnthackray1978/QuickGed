﻿using QuickGed.Types;

namespace QuickGed;

public class GedDb
{
    private int _startId = 0;

    public List<RelationSubSet> Relationships;
    public List<ChildRelationship> ChildRelationships;
    public List<Person> Persons;
    public Dictionary<int, Person> PersonDictionary;
    public Dictionary<int, string> PersonReferenceById;

    public Dictionary<int, List<Node>> ParentDictionary;

    public string FileName { get; set; }

    public long FileSize { get; set; }

    public GedDb(int startId =0)
    {
        ParentDictionary = new Dictionary<int, List<Node>>();
        Relationships = new List<RelationSubSet>();
        ChildRelationships = new List<ChildRelationship>();
        PersonDictionary = new Dictionary<int, Person>();
        PersonReferenceById = new Dictionary<int, string>();
        Persons = new List<Person>();
        _startId = startId;
    }

    public int NewId()
    {
        return _startId + (Persons.Count + 1);
    }

    public void Insert(Person currentPerson)
    {
        if (currentPerson != null)
        {
            currentPerson.Id = this._startId+ (Persons.Count + 1);
            
            Persons.Add(currentPerson);
            PersonDictionary.Add(currentPerson.Id, currentPerson);
        }


    }

    public static GedDb Create(int startId=0)
    {
        var g = new GedDb(startId);
            
        return g;
    }

    public int DeletePeopleAndRebuild(ISet<int> idsToDelete)
    {
        if (idsToDelete == null || idsToDelete.Count == 0)
        {
            return 0;
        }

        var removedRelationshipIds = new HashSet<int>(
            Relationships
                .Where(r => idsToDelete.Contains(r.Person1Id.GetValueOrDefault()) || idsToDelete.Contains(r.Person2Id.GetValueOrDefault()))
                .Select(r => r.Id));

        Relationships.RemoveAll(r => removedRelationshipIds.Contains(r.Id));

        ChildRelationships.RemoveAll(cr => idsToDelete.Contains(cr.PersonId) || removedRelationshipIds.Contains(cr.RelationshipId));

        var deletedCount = Persons.RemoveAll(p => idsToDelete.Contains(p.Id));

        foreach (var id in idsToDelete)
        {
            PersonDictionary.Remove(id);
            PersonReferenceById.Remove(id);
        }

        ParentDictionary = ParentDictionary
            .Where(kvp => !idsToDelete.Contains(kvp.Key))
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Where(p => !idsToDelete.Contains(p.Id)).ToList());

        foreach (var person in Persons)
        {
            if (idsToDelete.Contains(person.FatherId))
            {
                person.FatherId = 0;
            }

            if (idsToDelete.Contains(person.MotherId))
            {
                person.MotherId = 0;
            }

            person.Spouses = person.Spouses.Where(s => !idsToDelete.Contains(s.Id)).ToList();
            person.Children = person.Children.Where(c => !idsToDelete.Contains(c.Id)).ToList();
            person.Siblings = person.Siblings.Where(s => !idsToDelete.Contains(s.Id)).ToList();
        }

        RebuildDerivedLinks();

        return deletedCount;
    }

    public void RebuildDerivedLinks()
    {
        ParentDictionary = new Dictionary<int, List<Node>>();

        foreach (var person in Persons)
        {
            person.Spouses = new List<Node>();
            person.Children = new List<Node>();
            person.Siblings = new List<Node>();
            person.IsParent = false;
        }

        var relationshipById = Relationships.ToDictionary(r => r.Id, r => r);

        foreach (var relationship in Relationships)
        {
            if (PersonDictionary.TryGetValue(relationship.Person1Id.GetValueOrDefault(), out Person? person1) &&
                PersonDictionary.TryGetValue(relationship.Person2Id.GetValueOrDefault(), out Person? person2))
            {
                AddUniqueNode(person1.Spouses, person2);
                AddUniqueNode(person2.Spouses, person1);
            }
        }

        foreach (var childRelationship in ChildRelationships)
        {
            if (!PersonDictionary.TryGetValue(childRelationship.PersonId, out Person? child))
            {
                continue;
            }

            if (!relationshipById.TryGetValue(childRelationship.RelationshipId, out RelationSubSet? relationship))
            {
                continue;
            }

            var parents = new List<Node>();

            var fatherId = relationship.Person1Id.GetValueOrDefault();
            if (fatherId != 0 && PersonDictionary.TryGetValue(fatherId, out Person? father))
            {
                AddUniqueNode(father.Children, child);
                father.IsParent = true;
                parents.Add(father);
                child.FatherId = fatherId;
            }

            var motherId = relationship.Person2Id.GetValueOrDefault();
            if (motherId != 0 && PersonDictionary.TryGetValue(motherId, out Person? mother))
            {
                AddUniqueNode(mother.Children, child);
                mother.IsParent = true;
                parents.Add(mother);
                child.MotherId = motherId;
            }

            if (parents.Count > 0)
            {
                if (!ParentDictionary.ContainsKey(child.Id))
                {
                    ParentDictionary[child.Id] = new List<Node>();
                }
                foreach (var p in parents)
                {
                    AddUniqueNode(ParentDictionary[child.Id], p);
                }
            }
        }

        foreach (var relationshipId in ChildRelationships.Select(c => c.RelationshipId).Distinct())
        {
            var siblingIds = ChildRelationships
                .Where(c => c.RelationshipId == relationshipId)
                .Select(c => c.PersonId)
                .Where(PersonDictionary.ContainsKey)
                .Distinct()
                .ToList();

            foreach (var siblingId in siblingIds)
            {
                var person = PersonDictionary[siblingId];
                person.Siblings = siblingIds
                    .Where(id => id != siblingId)
                    .Select(id => (Node)PersonDictionary[id])
                    .ToList();
            }
        }
    }

    private static void AddUniqueNode(List<Node> nodes, Node candidate)
    {
        if (!nodes.Any(n => n.Id == candidate.Id))
        {
            nodes.Add(candidate);
        }
    }
}