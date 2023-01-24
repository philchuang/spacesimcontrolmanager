namespace SSCM.Elite;

public class EDBinding
{
    public EDBindingKey Key { get; set; } = new EDBindingKey();
    public IList<EDBindingKey> Modifiers { get; set; } = new List<EDBindingKey>();
    public bool Preserve { get; set; }

    public bool IsUnbound => string.Equals("{NoDevice}", this.Key.Device, StringComparison.OrdinalIgnoreCase);

    public override string ToString()
    {
        return string.Join(" + ", new[] { this.Key }.Concat(this.Modifiers));
    }
}