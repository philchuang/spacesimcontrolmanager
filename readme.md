# Space Sim Control Manager

Utility to help players retain and migrate their control mappings for space-sim games, especially for new SC version releases. Elite Dangerous is also supported.

## Standard Usage (Star Citizen)

### Import

Reads the Star Citizen `actionmaps.xml` file and stores the input device settings and mappings.

```text
> SSCM.exe sc import
Read in 4 input devices.
Read in 114 mappings.
Mappings backed up to [My Documents\SSCM\SC\scmappings.json].
```

### Edit

Opens the captured mappings file in the system default editor. Set the `Preserve` property to `true` to overwrite the game configuration when exporting.

```text
> SSCM.exe sc edit
Opening [My Documents\SSCM\SC\scmappings.json] in the default editor, change the Preserve property to choose which settings are overwritten.
```

### Export Preview

Previews changes to the Star Citizen game configuration based on the preserved captured mappings.

```text
> SSCM.exe sc export
Updating seat_general-v_toggle_mining_mode to js2_button56...
Updating seat_general-v_toggle_quantum_mode to js2_button19...
Updating seat_general-v_toggle_scan_mode to js2_button54...
CONFIGURATION NOT UPDATED: Execute "export apply" to apply these changes.
```

### Export Apply

Updates the Star Citizen game configuration based on the preserved captured mappings.

```text
> SSCM.exe sc export apply
Updating seat_general-v_toggle_mining_mode to js2_button56...
Updating seat_general-v_toggle_quantum_mode to js2_button19...
Updating seat_general-v_toggle_scan_mode to js2_button54...
Saving updated actionmaps.xml...
Saved, run "restore" command to revert.
CONFIGURATION UPDATED: Changes applied to [C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default\actionmaps.xml].
MUST RESTART STAR CITIZEN FOR CHANGES TO TAKE EFFECT.
```

### Report

Creates a plain-text report of the captured mappings (multiple formats available).

```text
> SSCM.exe sc report > starcitizen_mappings.md
```

```text
> SSCM.exe sc report -f csv > starcitizen_mappings.scv
```

```text
> SSCM.exe sc report -f json > starcitizen_mappings.json
```

## Advanced Usage

### Importing when there is already saved mappings

Instead of automatically importing, this will display the differences between the captured and latest mappings for review.

```text
> SSCM.exe sc import
MAPPING changed and will merge: [seat_general-v_toggle_mining_mode] js2_button55 => js2_button54
MAPPING changed and will not merge: [seat_general-v_toggle_quantum_mode] => js2_button56, preserving js2_button19
MAPPING changed and will not merge: [seat_general-v_toggle_scan_mode] => js2_button55, preserving js2_button54
1 changes NOT saved! Run in merge or overwrite modes to save changes.
```

#### Merge mappings

Merges the latest changes for the non-preserved mappings.

```text
> SSCM.exe sc import merge
MAPPING changed and will merge: [seat_general-v_toggle_mining_mode] js2_button55 => js2_button54
MAPPING changed and will not merge: [seat_general-v_toggle_quantum_mode] => js2_button56, preserving js2_button19
MAPPING changed and will not merge: [seat_general-v_toggle_scan_mode] => js2_button55, preserving js2_button54
Mappings backed up to [My Documents\SSCM\SC\scmappings.json].
```

#### Interactive Merge mappings

TODO-WIP finalize and document

#### Overwrite mappings

Overwrite all the captured mappings with the latest changes.

```text
> SSCM.exe sc import overwrite
Read in 4 input devices.
Read in 114 mappings.
Overwriting existing mappings data!
Mappings backed up to [My Documents\SSCM\SC\scmappings.json].
```

### Back up the Star Citizen actionmaps.xml

Makes a local copy of the Star Citizen actionmaps.xml, which can be restored later.

```text
> SSCM.exe sc backup
actionmaps.xml backed up to [My Documents\SSCM\SC\actionmaps.xml.20221223022032.bak].
```

### Restore the backed-up Star Citizen actionmaps.xml

Restores the latest local backup of the Star Citizen actionmaps.xml.

```text
> SSCM.exe sc restore
actionmaps.xml restored from [My Documents\SSCM\SC\actionmaps.xml.20221223022032.bak].
```

### Edit the Star Citizen actionmaps.xml

Opens the Star Citizen game configuration in the system default editor.

```text
> SSCM.exe sc editgame
Opening [C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default\actionmaps.xml] in the default editor.
```
