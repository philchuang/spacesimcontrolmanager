# Unit Test Coverage Checklist

## Highest Value

[x] Add `ControlManagerBase` orchestration tests

- [x] Create a fake concrete control manager in tests with fake repository, importer, merger, exporter, upgrader, reporter, platform, and user input.
- [x] Verify `Import(Overwrite)` saves imported data when no current data exists.
- [x] Verify `Import(Overwrite)` saves imported data and emits an overwrite warning when current data exists.
- [x] Verify `Import(Merge)` backs up before saving merged data.
- [x] Verify `Import(Preview)` does not save and reports changes/no changes correctly.
- [x] Verify `Import(Serial)` saves when interactive merge returns data.
- [x] Verify `Import(Serial)` reports cancellation and does not save when user input is cancelled.
- [x] Verify `Import(Tui)` throws when selector is null.
- [x] Verify `Import(Tui)` does not back up or save when the interactive session has no rows.
- [x] Verify `Import(Tui)` backs up and saves only when selector applies changes.
- [x] Verify `Upgrade(Preview)` does not back up or save.
- [x] Verify `Upgrade(Apply)` backs up before saving upgraded data.
- [x] Verify `Upgrade` exits quietly when repository load returns null.
- [x] Verify `Export(Preview)` does not back up or update.
- [x] Verify `Export(Apply)` backs up before update.
- [x] Verify `Export(Serial)` handles no changes, successful changes, and cancellation.
- [x] Verify `Export(Tui)` throws when selector is null.
- [x] Verify `Export(Tui)` does not back up or save when the session has no rows.
- [x] Verify `Export(Tui)` backs up and saves only when selector applies changes.
- [x] Verify `Report` uses repository data when available.
- [x] Verify `Report` uses `CreateNew()` when repository load returns null.
- [x] Verify `Backup` and `Restore` delegate to exporter and emit expected output.
- [x] Verify `Open` calls platform with the mappings JSON path.
- [x] Verify `Open` reports a friendly message when the mappings JSON file is missing.
- [x] Verify `OpenGameConfig` calls platform with the game config path.
- [x] Verify `OpenGameConfig` reports a friendly message when the game config file is missing.

[x] Add `MappingDataRepositoryDefault` persistence tests

- [x] Verify `Load` reads valid JSON from the default save path.
- [x] Verify `Load(customPath)` reads valid JSON from the supplied path.
- [x] Verify `Load` returns null and emits warning/debug output when the file is missing.
- [x] Verify `Load` returns null and emits warning/debug output when JSON is malformed.
- [x] Verify `Save` creates parent directories.
- [x] Verify `Save(customPath)` writes to the supplied path.
- [x] Verify `Save(null)` throws `ArgumentNullException`.
- [x] Verify `Backup` copies the current mappings file using the platform timestamp.
- [x] Verify `Backup` returns null and emits a warning when the mappings file does not exist.
- [x] Verify `RestoreLatest` restores the ordinally latest timestamped backup.
- [x] Verify `RestoreLatest` returns null and emits a warning when no backups exist.

[x] Add CLI command binding tests

- [x] Consider a new `SSCM.cli.Tests` project or expose command construction to tests with `InternalsVisibleTo`.
- [x] Use a fake `IControlManager` to capture called modes, options, and export/report options.
- [x] Verify `sc import` calls `ImportMode.Tui`.
- [x] Verify `sc import preview` calls `ImportMode.Preview`.
- [x] Verify `sc import merge` calls `ImportMode.Merge`.
- [x] Verify `sc import overwrite` calls `ImportMode.Overwrite`.
- [x] Verify `sc import serial` calls `ImportMode.Serial`.
- [x] Verify `sc import tui` calls `ImportMode.Tui`.
- [x] Verify `sc export` calls `ExportMode.Tui`.
- [x] Verify `sc export preview` calls `ExportMode.Preview`.
- [x] Verify `sc export apply` calls `ExportMode.Apply`.
- [x] Verify `sc export serial` calls `ExportMode.Serial`.
- [x] Verify `sc export tui` calls `ExportMode.Tui`.
- [x] Verify `export --matches` sets `ExportOptions.OnlyMatches`.
- [x] Verify global options such as `--environment` and `-e` reach the manager options dictionary.
- [x] Verify `report --format md`, `report --format csv`, and `report --format json` map to the correct `ReportingFormat`.
- [x] Verify `report --preserved` sets `ReportingOptions.PreservedOnly`.
- [x] Verify `report --names` sets `ReportingOptions.HeadersOnly`.
- [x] Verify `backup`, `restore`, `edit`, and `editgame` delegate to the expected manager methods.

[x] Add Star Citizen `ExportOptions.OnlyMatches` tests

- [x] Verify missing actionmap/action is skipped when `OnlyMatches = true`.
- [x] Verify missing attribute is skipped when `OnlyMatches = true`.
- [x] Verify missing actionmap/action is created when `OnlyMatches = false`.
- [x] Verify missing attribute is created when `OnlyMatches = false`.
- [x] Decide and test current intended behavior for an existing action with a missing rebind.
- [x] Decide and test current intended behavior for input device restoration when `OnlyMatches = true`.
- [x] Mirror key `OnlyMatches` expectations in `CreateInteractiveSession`, not only `Update`.

[x] Add shared interactive export flow tests

- [x] Use a fake `MappingExporterBase<TData>` implementation with controlled interactive rows.
- [x] Verify no rows returns false and does not prompt.
- [x] Verify user declines `Start interactive export?` throws `UserInputCancelledException`.
- [x] Verify user rejects every row returns false and does not call `SaveInteractive`.
- [x] Verify user applies at least one row but declines finish throws and does not save.
- [x] Verify user applies rows and confirms finish calls `SaveInteractive` once and returns true.
- [x] Verify prompt values display `<none>` for blank current/new values.

## Secondary

[ ] Add exporter backup/restore failure path tests

- Verify backup throws `FileNotFoundException` when the target game config file is missing.
- Verify restore throws `FileNotFoundException` when the backup directory contains no matching backups.
- Apply this to both Elite and Star Citizen concrete exporters where useful.

[ ] Add exporter `SaveInteractive` guard tests

- Verify Star Citizen `SaveInteractive` throws before `CreateInteractiveSession` has been called.
- Verify Elite `SaveInteractive` throws before `CreateInteractiveSession` has been called.

[ ] Add Star Citizen exporter validation tests

- Verify preserved joystick/gamepad input with instance `0` or less throws `SscmException`.
- Verify non-contiguous preserved joystick/gamepad input instances throw `SscmException`.
- Verify preserved mapping with malformed binding input throws `SscmException`.
- Verify preserved mapping referencing an unknown input prefix throws `SscmException`.

[ ] Add Elite folder detection tests

- Verify configured `GameConfigDir` is used when supplied.
- Verify configured `EliteDataDir` is used when supplied.
- Verify `Custom.4.0.binds` is preferred over `Custom.3.0.binds` when both exist.
- Verify `Custom.3.0.binds` is used when `Custom.4.0.binds` is absent.
- Verify a useful exception is thrown when no `.binds` file exists.

## Suggested Order

[x] Implement `MappingDataRepositoryDefault` tests first.

[x] Implement `ControlManagerBase` fake-manager tests second.

[x] Implement CLI command binding tests third.

[x] Implement Star Citizen `OnlyMatches` tests fourth.

[ ] Implement remaining failure-path tests last.
