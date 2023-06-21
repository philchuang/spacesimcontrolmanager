using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;

namespace SSCM.StarCitizen.Tests;

[TestFixture]
public class MappingImportMerger_MergeInteractive_Tests : MappingImportMerger_TestBase
{
    private UserInputForTest _userinput = new UserInputForTest(TestContext.Out) { Strict = true };
    private SCMappingData _result = new SCMappingData();
    private SCMappingData _expected = new SCMappingData();

    public MappingImportMerger_MergeInteractive_Tests()
    {
        this._userinput.Answers["Start interactive merge?"] = "Y";
        this._userinput.Answers["Finish interactive merge and save changes?"] = "Y";
    }

    private SCMappingData Act()
    {
        var current = this._current.JsonCopy(); // prevent direct modification
        var updated = this._updated.JsonCopy(); // prevent direct modification
        return this._result = this._merger.MergeInteractive(current, updated, this._userinput);
    }

    private void Assert()
    {
        AssertSC.AreEqual(this._expected, this._result);
    }

    [Test]
    public void InputAdded_UserDefault_Does_Add()
    {
        // Arrange
        this.Detects_Inputs_Added_Arrange();
        this._userinput.Answers[$"Add INPUT [{this._updated.Inputs[1].Id}]?"] = string.Empty;
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputAdded_UserNo_Doesnt_Add()
    {
        // Arrange
        this.Detects_Inputs_Added_Arrange();
        this._userinput.Answers[$"Add INPUT [{this._updated.Inputs[1].Id}]?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputAdded_UserYes_Does_Add()
    {
        // Arrange
        this.Detects_Inputs_Added_Arrange();
        this._userinput.Answers[$"Add INPUT [{this._updated.Inputs[1].Id}]?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputRemoved_NotPreserved_UserDefault_Does_Remove()
    {
        // Arrange
        this.Detects_Inputs_Removed_Arrange();
        this._userinput.Answers[$"Remove INPUT [{this._current.Inputs[1].Id}]?"] = string.Empty;
        this._userinput.Answers[$"Remove MAPPING [{this._current.Mappings[0].Id}] -= {this._current.Mappings[0].InputToString} ?"] = string.Empty;
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputRemoved_NotPreserved_UserNo_Doesnt_Remove()
    {
        // Arrange
        this.Detects_Inputs_Removed_Arrange();
        this._userinput.Answers[$"Remove INPUT [{this._current.Inputs[1].Id}]?"] = "N";
        this._userinput.Answers[$"Remove MAPPING [{this._current.Mappings[0].Id}] -= {this._current.Mappings[0].InputToString} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputRemoved_NotPreserved_UserYes_Does_Remove()
    {
        // Arrange
        this.Detects_Inputs_Removed_Arrange();
        this._userinput.Answers[$"Remove INPUT [{this._current.Inputs[1].Id}]?"] = "Y";
        this._userinput.Answers[$"Remove MAPPING [{this._current.Mappings[0].Id}] -= {this._current.Mappings[0].InputToString} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }


    [Test]
    public void InputRemoved_Preserved_UserDefault_Doesnt_Remove()
    {
        // Arrange
        this.Detects_Inputs_Removed_Arrange(true);
        this._userinput.Answers[$"Remove PRESERVED INPUT [{this._current.Inputs[1].Id}]?"] = string.Empty;
        this._userinput.Answers[$"Remove PRESERVED MAPPING [{this._current.Mappings[0].Id}] -= {this._current.Mappings[0].InputToString} ?"] = string.Empty;
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputRemoved_Preserved_UserNo_Doesnt_Remove()
    {
        // Arrange
        this.Detects_Inputs_Removed_Arrange(true);
        this._userinput.Answers[$"Remove PRESERVED INPUT [{this._current.Inputs[1].Id}]?"] = "N";
        this._userinput.Answers[$"Remove PRESERVED MAPPING [{this._current.Mappings[0].Id}] -= {this._current.Mappings[0].InputToString} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputRemoved_Preserved_UserYes_Does_Remove()
    {
        // Arrange
        this.Detects_Inputs_Removed_Arrange(true);
        this._userinput.Answers[$"Remove PRESERVED INPUT [{this._current.Inputs[1].Id}]?"] = "Y";
        this._userinput.Answers[$"Remove PRESERVED MAPPING [{this._current.Mappings[0].Id}] -= {this._current.Mappings[0].InputToString} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingAdded_UserDefault_Does_Add()
    {
         // Arrange
        this.Detects_InputSettings_Added_Arrange();
        var setting = this._updated.Inputs[0].Settings.Last();
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Add INPUT SETTING {parentProduct} [{setting.Name}] => {setting.Properties.EntriesToString()} ?"] = string.Empty;
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingAdded_UserNo_Doesnt_Add()
    {
         // Arrange
        this.Detects_InputSettings_Added_Arrange();
        var setting = this._updated.Inputs[0].Settings.Last();
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Add INPUT SETTING {parentProduct} [{setting.Name}] => {setting.Properties.EntriesToString()} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingAdded_UserYes_Does_Add()
    {
         // Arrange
        this.Detects_InputSettings_Added_Arrange();
        var setting = this._updated.Inputs[0].Settings.Last();
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Add INPUT SETTING {parentProduct} [{setting.Name}] => {setting.Properties.EntriesToString()} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingRemoved_NotPreserved_UserDefault_Does_Remove()
    {
         // Arrange
        this.Detects_InputSettings_Removed_Arrange();
        var setting = this._current.Inputs[0].Settings.Last();
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Remove INPUT SETTING {parentProduct} [{setting.Name}] -= {setting.Properties.EntriesToString()} ?"] = string.Empty;
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingRemoved_NotPreserved_UserNo_Doesnt_Remove()
    {
         // Arrange
        this.Detects_InputSettings_Removed_Arrange();
        var setting = this._current.Inputs[0].Settings.Last();
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Remove INPUT SETTING {parentProduct} [{setting.Name}] -= {setting.Properties.EntriesToString()} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingRemoved_NotPreserved_UserYes_Does_Remove()
    {
         // Arrange
        this.Detects_InputSettings_Removed_Arrange();
        var setting = this._current.Inputs[0].Settings.Last();
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Remove INPUT SETTING {parentProduct} [{setting.Name}] -= {setting.Properties.EntriesToString()} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingRemoved_Preserved_UserDefault_Doesnt_Remove()
    {
         // Arrange
        this.Detects_InputSettings_Removed_Arrange(true);
        var setting = this._current.Inputs[0].Settings.Last();
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Remove PRESERVED INPUT SETTING {parentProduct} [{setting.Name}] -= {setting.Properties.EntriesToString()} ?"] = string.Empty;
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingRemoved_Preserved_UserNo_Doesnt_Remove()
    {
         // Arrange
        this.Detects_InputSettings_Removed_Arrange(true);
        var setting = this._current.Inputs[0].Settings.Last();
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Remove PRESERVED INPUT SETTING {parentProduct} [{setting.Name}] -= {setting.Properties.EntriesToString()} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingRemoved_Preserved_UserYes_Does_Remove()
    {
         // Arrange
        this.Detects_InputSettings_Removed_Arrange(true);
        var setting = this._current.Inputs[0].Settings.Last();
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Remove PRESERVED INPUT SETTING {parentProduct} [{setting.Name}] -= {setting.Properties.EntriesToString()} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingChanged_NotPreserved_UserDefault_Does_Update()
    {
        // Arrange
        this.Detects_InputSettings_Changed_Arrange();
        var setting = this._updated.Inputs[0].Settings[0];
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Update INPUT SETTING {parentProduct} [{setting.Name}] => {setting.Properties.EntriesToString()} ?"] = string.Empty;
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingChanged_NotPreserved_UserNo_Doesnt_Update()
    {
        // Arrange
        this.Detects_InputSettings_Changed_Arrange();
        var setting = this._updated.Inputs[0].Settings[0];
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Update INPUT SETTING {parentProduct} [{setting.Name}] => {setting.Properties.EntriesToString()} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingChanged_NotPreserved_UserYes_Does_Update()
    {
        // Arrange
        this.Detects_InputSettings_Changed_Arrange();
        var setting = this._updated.Inputs[0].Settings[0];
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Update INPUT SETTING {parentProduct} [{setting.Name}] => {setting.Properties.EntriesToString()} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }


    [Test]
    public void InputSettingChanged_Preserved_UserDefault_Doesnt_Update()
    {
        // Arrange
        this.Detects_InputSettings_Changed_Arrange(true);
        var setting = this._updated.Inputs[0].Settings[0];
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Update PRESERVED INPUT SETTING {parentProduct} [{setting.Name}] => {setting.Properties.EntriesToString()} ?"] = string.Empty;
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingChanged_Preserved_UserNo_Doesnt_Update()
    {
        // Arrange
        this.Detects_InputSettings_Changed_Arrange(true);
        var setting = this._updated.Inputs[0].Settings[0];
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Update PRESERVED INPUT SETTING {parentProduct} [{setting.Name}] => {setting.Properties.EntriesToString()} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void InputSettingChanged_Preserved_UserYes_Does_Update()
    {
        // Arrange
        this.Detects_InputSettings_Changed_Arrange(true);
        var setting = this._updated.Inputs[0].Settings[0];
        var parentProduct = setting.Parent.Split("-")[2];
        this._userinput.Answers[$"Update PRESERVED INPUT SETTING {parentProduct} [{setting.Name}] => {setting.Properties.EntriesToString()} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingAdded_UserDefault_Does_Add()
    {
         // Arrange
        this.Detects_Mapping_Added_Arrange();
        var mapping = this._updated.Mappings.Last();
        this._userinput.Answers[$"Add MAPPING [{mapping.Id}] => {mapping.InputToString} ?"] = string.Empty;
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingAdded_UserNo_Doesnt_Add()
    {
         // Arrange
        this.Detects_Mapping_Added_Arrange();
        var mapping = this._updated.Mappings.Last();
        this._userinput.Answers[$"Add MAPPING [{mapping.Id}] => {mapping.InputToString} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingAdded_UserYes_Does_Add()
    {
         // Arrange
        this.Detects_Mapping_Added_Arrange();
        var mapping = this._updated.Mappings.Last();
        this._userinput.Answers[$"Add MAPPING [{mapping.Id}] => {mapping.InputToString} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingRemoved_NotPreserved_UserDefault_Does_Remove()
    {
         // Arrange
        this.Detects_Mapping_Removed_Arrange();
        var mapping = this._current.Mappings.Last();
        this._userinput.Answers[$"Remove MAPPING [{mapping.Id}] -= {mapping.InputToString} ?"] = string.Empty;
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingRemoved_NotPreserved_UserNo_Doesnt_Remove()
    {
         // Arrange
        this.Detects_Mapping_Removed_Arrange();
        var mapping = this._current.Mappings.Last();
        this._userinput.Answers[$"Remove MAPPING [{mapping.Id}] -= {mapping.InputToString} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingRemoved_NotPreserved_UserYes_Does_Remove()
    {
         // Arrange
        this.Detects_Mapping_Removed_Arrange();
        var mapping = this._current.Mappings.Last();
        this._userinput.Answers[$"Remove MAPPING [{mapping.Id}] -= {mapping.InputToString} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingRemoved_Preserved_UserDefault_Doesnt_Remove()
    {
         // Arrange
        this.Detects_Mapping_Removed_Arrange(true);
        var mapping = this._current.Mappings.Last();
        this._userinput.Answers[$"Remove PRESERVED MAPPING [{mapping.Id}] -= {mapping.InputToString} ?"] = string.Empty;
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingRemoved_Preserved_UserNo_Doesnt_Remove()
    {
         // Arrange
        this.Detects_Mapping_Removed_Arrange(true);
        var mapping = this._current.Mappings.Last();
        this._userinput.Answers[$"Remove PRESERVED MAPPING [{mapping.Id}] -= {mapping.InputToString} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingRemoved_Preserved_UserYes_Does_Remove()
    {
         // Arrange
        this.Detects_Mapping_Removed_Arrange(true);
        var mapping = this._current.Mappings.Last();
        this._userinput.Answers[$"Remove PRESERVED MAPPING [{mapping.Id}] -= {mapping.InputToString} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingChanged_NotPreserved_UserDefault_Does_Update()
    {
        // Arrange
        this.Detects_Mapping_Changed_Arrange();
        var mapping = this._updated.Mappings.Last();
        this._userinput.Answers[$"Update MAPPING [{mapping.Id}] => {mapping.InputToString} ?"] = string.Empty;
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingChanged_NotPreserved_UserNo_Doesnt_Update()
    {
        // Arrange
        this.Detects_Mapping_Changed_Arrange();
        var mapping = this._updated.Mappings.Last();
        this._userinput.Answers[$"Update MAPPING [{mapping.Id}] => {mapping.InputToString} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingChanged_NotPreserved_UserYes_Does_Update()
    {
        // Arrange
        this.Detects_Mapping_Changed_Arrange();
        var mapping = this._updated.Mappings.Last();
        this._userinput.Answers[$"Update MAPPING [{mapping.Id}] => {mapping.InputToString} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingChanged_Preserved_UserDefault_Doesnt_Update()
    {
        // Arrange
        this.Detects_Mapping_Changed_Arrange(true);
        var mapping = this._updated.Mappings.Last();
        this._userinput.Answers[$"Update PRESERVED MAPPING [{mapping.Id}] => {mapping.InputToString} ?"] = string.Empty;
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingChanged_Preserved_UserNo_Doesnt_Update()
    {
        // Arrange
        this.Detects_Mapping_Changed_Arrange(true);
        var mapping = this._updated.Mappings.Last();
        this._userinput.Answers[$"Update PRESERVED MAPPING [{mapping.Id}] => {mapping.InputToString} ?"] = "N";
        this._expected = this._current;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void MappingChanged_Preserved_UserYes_Does_Update()
    {
        // Arrange
        this.Detects_Mapping_Changed_Arrange(true);
        var mapping = this._updated.Mappings.Last();
        this._userinput.Answers[$"Update PRESERVED MAPPING [{mapping.Id}] => {mapping.InputToString} ?"] = "Y";
        this._expected = this._updated;

        // Act
        this.Act();

        // Assert
        this.Assert();
    }
}