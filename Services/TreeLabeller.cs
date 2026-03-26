using QuickGed.Types;

namespace QuickGed.Services;

public static class TreeLabeller
{
    private static IEnumerable<Node> GetPartners(Node current, Dictionary<int, List<Node>> parentsDictionary)
    {
        var partners = new HashSet<Node>(current.Spouses);
        
        if (current.Children != null)
        {
            foreach (var child in current.Children)
            {
                if (parentsDictionary.TryGetValue(child.Id, out var parents))
                {
                    foreach (var parent in parents.Where(p => p.Id != current.Id))
                    {
                        partners.Add(parent);
                    }
                }
            }
        }
        
        return partners;
    }

    public static void LabelAncestors(Dictionary<int, List<Node>> parentsDictionary,
        HashSet<Node> parentsToLookup, List<Node> siblingList, Node startNode, string label, bool isDirectAncestor =true)
    {
        var stack = new Stack<Node>();
        stack.Push(startNode);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            //if (current.FullName.Contains("Estelle A Belcher"))
            //{
            //    Console.WriteLine("z");
            //}


            current.Origin = label;
            current.IsDirectAncestor = isDirectAncestor;

            if (!current.IsLinkNode && !current.IsRootPerson)
            {
                var partners = GetPartners(current, parentsDictionary);
                parentsToLookup.UnionWith(partners.Where(w => !w.IsLinkNode));
                parentsToLookup.Add(current); //for single parents - this does lead to so redundancy down
            }


            if (parentsDictionary.ContainsKey(current.Id))
            {
                foreach (var parent in parentsDictionary[current.Id])
                {
                    stack.Push(parent);
                }
            }

            siblingList.AddRange(current.Siblings);

        }


    }

    public static void LabelDescendants(Dictionary<int, List<Node>> parentsDictionary, IEnumerable<Node> originator, string label, bool isDirectAncestor)
    {
        foreach (var nodeWithChildren in originator)
        {
            LabelDescendant(parentsDictionary, nodeWithChildren, label,isDirectAncestor);
        }
    }

    public static void LabelDescendant(Dictionary<int, List<Node>> parentsDictionary, Node originator, string label, bool isDirectAncestor)
    {
        var stack = new Stack<Node>();
        stack.Push(originator);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            current.Origin = label;
            
            if(!current.IsDirectAncestor) //if something has already been set, don't reset it.
                current.IsDirectAncestor = isDirectAncestor;

            var partners = GetPartners(current, parentsDictionary);

            foreach (var spouse in partners)
            {
                if (string.IsNullOrEmpty(spouse.Origin))
                    stack.Push(spouse);

                spouse.Origin = label;

                if (!spouse.IsDirectAncestor)
                    spouse.IsDirectAncestor = isDirectAncestor;

                // Optional: Traverse UP from spouses of descendants/siblings to pull in extended in-laws
                if (parentsDictionary.TryGetValue(spouse.Id, out var spouseParents))
                {
                    foreach (var parent in spouseParents)
                    {
                        if (string.IsNullOrEmpty(parent.Origin))
                            stack.Push(parent);
                    }
                }
            }
            
            foreach (var child in current.Children.Where(w => string.IsNullOrEmpty(w.Origin)))
                stack.Push(child);

        }
    }


    public static void LabelTree(Dictionary<int, List<Node>> parentsCache, Node startNode, string label)
    {
        var siblings = new List<Node>();
        var parentsToLookup = new HashSet<Node>();

        LabelAncestors(parentsCache, parentsToLookup, siblings, startNode, label);

        LabelDescendants(parentsCache, siblings, label,false);

        LabelDescendants(parentsCache, parentsToLookup, label,false);

        foreach (var spouse in parentsToLookup.ToList())
        {
            LabelAncestors(parentsCache, parentsToLookup, siblings, spouse, label,true);

            LabelDescendants(parentsCache, siblings, label, false);

            LabelDescendants(parentsCache, parentsToLookup, label, false);
        }


    }
}