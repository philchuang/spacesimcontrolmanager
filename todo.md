# Development TO-DOs

[o] (Low) New release including interactive mode
  [x] dotnet -> interactive
  [ ] interactive -> master
  [ ] Release Notes
[ ] (Low) live editing of mappings in TUI
[ ] (Medium) star citizen to elite dangerous cross-game synchronization

## To-Done

[x] (High) new interactive mode, with terminal UI
  [x] separate from current interactive mode that only operates one mapping at a time and only works forward
  [x] display all possible changes, with up/down row cursor
  [x] current row is contrast highlighted
  [x] each row will display "[ ]" for selection, id (setting id, mapping id, or attribute name, etc), current saved value, new value
  [x] use full terminal width, full data width for all columns except use ellipses to truncate final column
  [x] spacebar to select a row
  [x] import/export command will act on all selected
  [x] remove acted rows afterwards
[x] (Low) fix github pipeline action to correctly generate windows package (no exe right now)
[x] (Low) Upgrade to dotnet 10
[x] (Low) Review build scripts and platform targets, test
[x] (Low) Review github CI/CD actions
[x] (Low) Make environment a variable (default to LIVE)
  [x] Create GameRoot setting = `C:\Program Files\Roberts Space Industries\StarCitizen`
  [x] Create GameEnvironment setting = `LIVE`
[x] (Medium) Upgrade System.CommandLine from beta to stable - minor rewrite
[x] (Medium) Analyze unit test coverage gaps
  [x] (Medium) Implement tests
