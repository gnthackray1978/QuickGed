using QuickGed.Domain;

namespace QuickGed.Types;

public class Person : Node, IPerson
{
    public Person(int id, INodeTypeCalculator nodeTypeCalculator) : base(id, nodeTypeCalculator)
    {
        
    }
        public void SetIsRootPerson()
    {
        IsRootPerson = _nodeTypeCalculator.IsRootPerson(Forename ?? "", FamilyName ?? "");
        IsLinkNode = _nodeTypeCalculator.IsLinkNode(Forename ?? "", FamilyName ?? "");
    }

        public string? Sex { get; set; }
    public string? BirthDate { get; set; }
    public int BirthYearFrom { get; set; }
    public int BirthYearTo { get; set; }
    public string? BirthLocation { get; set; }
    public string? BirthNote { get; set; }
    public int DeathYear { get; set; }
    public string? DeathDate { get; set; }
    public string? DeathNote { get; set; }

    public string? DeathLocation { get; set; }
    public string? Forename { get; set; }
    public string? FamilyName { get; set; }
    public override string FullName => (Forename + " " + FamilyName).Trim();
    public string? Occupation { get; set; }
    public int ResidenceYear { get; set; }
    public string? ResidenceDate { get; set; }
    public string? Residence { get; set; }
    public string? ResidenceDescription { get; set; }
    public string? Gender { get; set; }
    public string? Title { get; set; }

    public bool IsValidLocation()
    {
        return !string.IsNullOrEmpty(BirthLocation) || !string.IsNullOrEmpty(DeathLocation) || !string.IsNullOrEmpty(Residence);
    }



    public string AllLocations()
    {
        var result = "";

        if (!string.IsNullOrEmpty(BirthLocation))
            result += BirthLocation + ",";

        if (!string.IsNullOrEmpty(DeathLocation))
            result += DeathLocation + ",";

        if (!string.IsNullOrEmpty(Residence))
            result += Residence;

        result = result.TrimEnd(',');

        return result;
    }

}