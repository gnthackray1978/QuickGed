using QuickGed.Types;

namespace QuickGed;

public class GedDb
{
    private int _startId = 0;

    public List<RelationSubSet> Relationships;
    public List<ChildRelationship> ChildRelationships;
    public List<Person> Persons;
    public Dictionary<int, Person> PersonDictionary;

    public Dictionary<int, List<Node>> ParentDictionary;

    public string FileName { get; set; }

    public long FileSize { get; set; }

    public GedDb(int startId =0)
    {
        ParentDictionary = new Dictionary<int, List<Node>>();
        Relationships = new List<RelationSubSet>();
        ChildRelationships = new List<ChildRelationship>();
        PersonDictionary = new Dictionary<int, Person>();
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
            //possibly not the best place to put this! but can move later
            currentPerson.SetIsRootPerson();

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
}