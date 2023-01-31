namespace SSCM.Elite;

public class EDBinding
{
    public static EDBinding UNBOUND() => new EDBinding {
        Key = new EDBindingKey("{NoDevice}", ""),
        Preserve = false,
    };

    [Newtonsoft.Json.JsonIgnore]
    public bool IsUnbound => string.Equals("{NoDevice}", this.Key.Device, StringComparison.OrdinalIgnoreCase);

    public EDBindingKey Key { get; set; } = new EDBindingKey();
    public IList<EDBindingKey> Modifiers { get; set; } = new List<EDBindingKey>();
    public bool Preserve { get; set; }

    public EDBinding()
    {
    }

    public EDBinding(string device, string key, IList<EDBindingKey>? modifiers = null, bool preserve = true)
    {
        this.Key = new EDBindingKey(device, key);
        this.Modifiers = modifiers ?? this.Modifiers;
        this.Preserve = preserve;
    }

    public EDBinding(EDBindingKey key, IList<EDBindingKey>? modifiers = null, bool preserve = true)
    {
        this.Key = key;
        this.Modifiers = modifiers ?? this.Modifiers;
        this.Preserve = preserve;
    }

    public override string ToString()
    {
        return string.Join(" + ", new[] { this.Key }.Concat(this.Modifiers));
    }
}