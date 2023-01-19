# DEVELOPMENT NOTES

## TERMINOLOGY

### ACTIONS

- import: load data from actionmaps.xml into scmappings.json
- merge: merge data from actionmaps.xml into scmappings.json
- export: update target with data from scmappings.json

### DATA MODEL

- source: collection of captured values and whether or not they should be preserved
- target: game-specific configuration, actionmaps.xml for SC
- input (joysticks, etc.) :: &lt;options>
- input setting :: children of &lt;options>
- mapping (bindings) :: &lt;action>

### UNIT TESTS

- original vs updated
- testdata + testxml

## TODOs

### Rename SCCM to ...? Space Sim Controls Manager

### make SC a subcommand from the root for the CLI
