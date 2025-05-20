public abstract class Node
{
    public string type { get; set; }
    public virtual string name { get; set; }
    public string relation { get; set; }
    public List<string> variables { get; set; } = new();
    public List<Node> children { get; set; } = new();
    public Node parent { get; set; }

    public Node getCurrentNode()
    {
        return this;
    }

    public void addChild(Node child)
    {
        children.Add(child);
    }

    public Node getChildByIndex(int index)
    {
        return children[index];
    }

    public Node getChildByName(string name)
    {
        return children.Find(a => a.name.ToLower().Contains(name.ToLower()));
    }

    public Node getChildByType(string type)
    {
        return children.Find(a => a.type == type);
    }

    public Node[] getChildreenByName(string name)
    {
        return children.Where(a => a.name.ToLower().Contains(name.ToLower())).ToArray();
    }
}

public class QueryNode : Node
{
    public QueryNode()
    {
        this.name = "Query";
    }
}

public class DeclarationNode : Node
{
    public DeclarationNode(string type, List<string> variables)
    {
        this.name = $"Declaration: {type} ({string.Join(", ", variables)})";
        this.type = type;
        this.variables = variables;
    //    this.type = "Declaration";
    }
}

public class SelectNode : Node
{
    public SelectNode(string variable)
    {
        this.name = $"Select: {variable}";
        this.variables.Add(variable);
        this.type = "Select"; 
    }
}

public class ClauseNode : Node
{
    public ClauseNode(string relation, string left, string right)
    {
        this.name = $"Clause: {relation}({left}, {right})";
        this.relation = relation;
        this.variables.Add(left);
        this.variables.Add(right);
    }
}

public class WithNode : Node
{
    public WithNode(string left, string right)
    {
        this.name = $"With:({left}, {right})";
        this.variables.Add(left);
        this.variables.Add(right);
    }
}

public class PatternNode : Node
{
    public PatternNode(string assignVal, string left, string right, string matchType)
    {
        this.name = $"Pattern: {assignVal}({left}, {matchType}:{right})";
        this.type = matchType;
        this.variables.Add(assignVal);
        this.variables.Add(left);
        this.variables.Add(right);
    }
}