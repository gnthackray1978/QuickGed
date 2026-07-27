using System.Text.RegularExpressions;

namespace QuickGed.Domain
{
    public class NodeTypeCalculator : INodeTypeCalculator
    {
        private static readonly Regex RootRegex = new Regex(@"^(\[[\d.]+\]|[\d.]+\||\|\[[\d.]+\]\||\|[\d.]+\|)", RegexOptions.Compiled);

        public bool IsRootPerson(string forename, string surname)
        {
            return IsRootPerson(forename + " " + surname);
        }

        public bool IsRootPerson(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return false;
            
            var lower = fullName.ToLower();
            if (lower.Contains("group")) return false;

            if (lower.Contains("chr") && !lower.Contains("christ")) return false;

            return RootRegex.IsMatch(fullName.Trim());
        }

        public bool IsLinkNode(string forename, string surname)
        {
            return IsLinkNode(forename + " " + surname);
        }

        public bool IsLinkNode(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return false;
            var lower = fullName.ToLower();

            if (lower.Contains("group")) return true;
            if (lower.Contains("chr")) return true;
 
            return false;
        }
    }
}
