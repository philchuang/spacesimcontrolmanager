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

1. star citizen to elite dangerous cross-game synchronization