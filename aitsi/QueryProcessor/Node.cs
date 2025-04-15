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
    public QueryNode() { this.name = "Query"; }
}

public class DeclarationNode : Node
{
    public DeclarationNode(string type, List<string> variables) 
    { 
        this.name = $"Declaration: {type} ({string.Join(", ", variables)})"; 
        this.variables = variables;
    }
}

public class SelectNode : Node
{
    public SelectNode(string variable, List<ClauseNode> clauses)
    {
        this.name = $"Select: {variable}";
        this.variables.Add(variable);
        foreach (var clause in clauses)
        {
            this.addChild(clause);
        }
    }
}

public class ClauseNode : Node
{
    public ClauseNode(string relation, string left, string right)
    {
        this.name = $"Clause: {relation}({left}, {right})";
        this.variables.Add(left);
        this.variables.Add(right);
    }
}