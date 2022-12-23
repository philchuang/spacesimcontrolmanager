# Star Citizen Control Mapper

Utility to help players retain and migrate their control mappings between SC version releases.

## Standard Usage

### Import

Imports the Star Citizen actionmaps.xml and saves it locally in a mappings JSON file.

```cmd
> SCCM.exe import
Read in 4 input devices.
Read in 114 mappings.
Mappings backed up to [My Documents\SCCM\scmappings.json].
```

### Edit

Opens the mappings JSON file in the system default editor. Edit the `Preserve` property to affect the export behavior.

```cmd
> SCCM.exe edit
Opening [My Documents\SCCM\scmappings.json] in the default editor, change the Preserve property to choose which settings are overwritten.
```

### Export

Updates the Star Citizen bindings based on the locally saved mappings file.

```cmd
> SCCM.exe export
TODO sample output
```

## Advanced Usage

### Importing when there is already saved mappings

Instead of importing, this command displays the differences between the current and saved mappings for review.

```cmd
> SCCM.exe import
TODO sample output
```

#### Merge mappings

```cmd
> SCCM.exe import merge
TODO sample output
```

#### Overwrite mappings

```cmd
> SCCM.exe import overwrite
TODO sample output
```

### Back up the Star Citizen actionmaps.xml

Makes a local copy of the Star Citizen actionmaps.xml which can be restored later.

```cmd
> SCCM.exe backup
actionmaps.xml backed up to [My Documents\SCCM\actionmaps.xml.20221223022032.bak].
```

### Restore the backed-up Star Citizen actionmaps.xml

Restores the latest local backup of the Star Citizen actionmaps.xml.

```cmd
> SCCM.exe restore
actionmaps.xml restored from [My Documents\SCCM\actionmaps.xml.20221223022032.bak].
```
