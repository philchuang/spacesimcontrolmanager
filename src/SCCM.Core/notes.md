# TODO make consistent nomenclature

ACTIONS

- import: load data from actionmaps.xml into scmappings.json
- merge: merge data from actionmaps.xml into scmappings.json
- export: update target with data from scmappings.json

DATA MODEL

- source: collection of captured values and whether or not they should be preserved
- target: game-specific configuration, actionmaps.xml for SC
- input (joysticks, etc.) :: &lt;options>
- input setting :: children of &lt;options>
- mapping (bindings) :: &lt;action>

UNIT TESTS

- original vs updated
- testdata + testxml
