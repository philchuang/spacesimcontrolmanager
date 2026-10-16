namespace SSCM.StarCitizen.Bindings;

public class BindingAction
{
    public string Id { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DefaultKeyboard { get; set; } = string.Empty;

    /// <summary><c>keyboard</c>, or <c>mouse</c> when the default keyboard value fell back to the mouse attribute.</summary>
    public string DefaultKeyboardSource { get; set; } = "keyboard";

    public string DefaultJoystick { get; set; } = string.Empty;
    public string Keyboard { get; set; } = string.Empty;
    public string Joystick { get; set; } = string.Empty;
    public string JoystickDescription { get; set; } = string.Empty;
}

public class BindingDocument
{
    public string GeneratedAt { get; set; } = string.Empty;
    public List<BindingAction> Actions { get; set; } = new();
}
