
public abstract class Node
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string Relation { get; set; }
    public List<string> Variables { get; set; } = new();
    public List<Node> Children { get; set; } = new();
    public Node Parent { get; set; }
}