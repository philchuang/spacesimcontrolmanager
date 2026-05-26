# DEVELOPMENT NOTES

## TERMINOLOGY

### ACTIONS

- import: load data from game config into sscm data
- merge: merge data from game config into sscm data
- export: update game config with data from sscm data

### DATA MODEL

- source: collection of captured values and whether or not they should be preserved
- target: game-specific configuration, actionmaps.xml for SC, custom.4.0.binds for ED

### Star Citizen

- input (joysticks, etc.) :: &lt;options>
- input setting :: children of &lt;options>
- mapping group :: &lt;actionMap>
- mapping :: &lt;action>
- binding :: &lt;rebind>

### Elite Dangerous

- mapping group - determined by EDMappingConfig.yml
- mapping :: any element with &lt;Binding>, &lt;Primary>, or &lt;Secondary>
- binding :: &lt;Binding>, &lt;Primary>, or &lt;Secondary>
- mapping setting :: any child element of a mapping that is not &lt;Binding>, &lt;Primary>, or &lt;Secondary> and has a @Value attribute
- setting :: any element that is not a mapping and has a @Value attribute

### UNIT TESTS

- current vs updated
- original vs final

## TODOs

[ ] new interactive mode, with terminal UI
    * separate from current interactive mode that only operates one mapping at a time and only works forward
    * display all possible changes, with up/down row cursor
    * current row is contrast highlighted
    * each row will display "[ ]" for selection, id (setting id, mapping id, or attribute name, etc), current saved value, new value
    * use full terminal width, full data width for all columns except use ellipses to truncate final column
    * spacebar to select a row
    * import/export command will act on all selected
    * remove acted rows afterwards
[ ] star citizen to elite dangerous cross-game synchronization
[ ] fix github pipeline action to correctly generate windows package (no exe right now)
