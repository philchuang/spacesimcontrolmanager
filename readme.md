# Star Citizen Control Mapper

Utility to help players retain and migrate their control mappings between SC version releases.

## Standard Usage

### Import

Imports the Star Citizen actionmaps.xml and saves it locally in a mappings JSON file.

```cmd
> SCCM.exe import
TODO sample output
```

### Edit

Opens the mappings JSON file in the system default editor. Edit the `Preserve` property to affect the export behavior.

```cmd
> SCCM.exe edit
TODO sample output
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

In order to force the import, use the `-force` option.

```cmd
> SCCM.exe import -force
TODO sample output
```

### Back up the Star Citizen actionmaps.xml

Makes a local copy of the Star Citizen actionmaps.xml which can be restored later.

```cmd
> SCCM.exe backup
TODO sample output
```

### Restore the backed-up Star Citizen actionmaps.xml

Restores the latest local backup of the Star Citizen actionmaps.xml.

```cmd
> SCCM.exe restore
TODO sample output
```
