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

## TODO

### Handle multiple rebind elements per action

Just realized that binding different input types to the same action results in multiple rebind elements. I'm assuming that there can only be one rebind per input type. Now have to update the importers and exporters to handle this, I think the core data model can remain the same.