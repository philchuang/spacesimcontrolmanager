namespace SSCM.Elite;

public class EDBinding
{
    public EDBindingKey Key { get; set; } = new EDBindingKey();
    public IList<EDBindingKey> Modifiers { get; set; } = new List<EDBindingKey>();
    public bool Preserve { get; set; }

    public override string ToString()
    {
        return string.Join(" + ", new[] { this.Key }.Concat(this.Modifiers));
    }
}