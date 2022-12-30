# Star Citizen Control Manager

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

### Export Preview

Previews updates to the Star Citizen bindings based on the locally saved mappings file.

```cmd
> SCCM.exe export
Updating seat_general-v_toggle_mining_mode to js2_button56...
Updating seat_general-v_toggle_quantum_mode to js2_button19...
Updating seat_general-v_toggle_scan_mode to js2_button54...
CONFIGURATION NOT UPDATED: Execute "export apply" to apply these changes.
```

### Export Apply

Updates the Star Citizen bindings based on the locally saved mappings file.

```cmd
> SCCM.exe export apply
Updating seat_general-v_toggle_mining_mode to js2_button56...
Updating seat_general-v_toggle_quantum_mode to js2_button19...
Updating seat_general-v_toggle_scan_mode to js2_button54...
Saving updated actionmaps.xml...
Saved, run "restore" command to revert.
CONFIGURATION UPDATED: Changes applied to [C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default\actionmaps.xml].
MUST RESTART STAR CITIZEN FOR CHANGES TO TAKE AFFECT.
```

## Advanced Usage

### Importing when there is already saved mappings

Instead of importing, this command displays the differences between the current and saved mappings for review.

```cmd
> SCCM.exe import
MAPPING changed and will merge: [seat_general-v_toggle_mining_mode] js2_button55 => js2_button54
MAPPING changed and will not merge: [seat_general-v_toggle_quantum_mode] => js2_button56, preserving js2_button19
MAPPING changed and will not merge: [seat_general-v_toggle_scan_mode] => js2_button55, preserving js2_button54
1 changes NOT saved! Run in merge or overwrite modes to save changes.
```

#### Merge mappings

```cmd
> SCCM.exe import merge
MAPPING changed and will merge: [seat_general-v_toggle_mining_mode] js2_button55 => js2_button54
MAPPING changed and will not merge: [seat_general-v_toggle_quantum_mode] => js2_button56, preserving js2_button19
MAPPING changed and will not merge: [seat_general-v_toggle_scan_mode] => js2_button55, preserving js2_button54
Mappings backed up to [C:\Users\chubl\Documents\SCCM\scmappings.json].
```

#### Overwrite mappings

```cmd
> SCCM.exe import overwrite
Read in 4 input devices.
Read in 114 mappings.
Overwriting existing mappings data!
Mappings backed up to [My Documents\SCCM\scmappings.json].
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

### Edit the Star Citizen actionmaps.xml

Opens the Star Citizen actionmaps.xml in the system default editor.

```cmd
> SCCM.exe editsc
Opening [C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default\actionmaps.xml] in the default editor.
```
