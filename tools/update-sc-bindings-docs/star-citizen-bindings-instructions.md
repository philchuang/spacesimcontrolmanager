# Star Citizen Bindings AI Instructions

INSTRUCTIONS:

1. Read `src\SSCM.StarCitizen\defaultProfile.xml`
   1. Iterate on `<actionMap>` elements
      1. Capture the `@name` attribute
      2. Capture the `@UILabel` attribute
      3. Iterate on the child `<action>` elements
         1. Capture the `@name` attribute as name
         2. Capture the `@UILabel` attribute as label
         3. Capture the `@keyboard` attribute as defaultKeyboard
            1. If keyboard is empty or blank string, then capture the `@mouse` attribute
         4. Capture the `@joystick` attribute as defaultJoystick
         5. Copy the parent actionMap name as categoryName
         6. Copy the parent actionMap UILabel as categoryLabel
         7. Generate an ID as `{categoryName}-{name}`
   2. Create a flat list of action data as `actionData`
2. Read `C:\Program Files\Roberts Space Industries\StarCitizen\{SC_BINDINGS_ENVIRONMENT}\data\Localization\english\global.ini`
   1. `SC_BINDINGS_ENVIRONMENT` defaults to `HOTFIX` in `sc_bindings_core.py` and can be overridden from the environment.
   1. This is a keyed list of strings, with one key-value pair per line, delimited by the first `=` character
   2. Iterate over the actions list from the previous step
      1. For each `label` and `categoryLabel`, look up the key in the global.ini, and if found replace it with the value
3. Read `C:\Program Files\Roberts Space Industries\StarCitizen\{SC_BINDINGS_ENVIRONMENT}\USER\Client\0\Profiles\default\actionmaps.xml`
   1. Iterate on `<actionMap>` elements
      1. Capture the `@name` attribute as categoryName
      2. Iterate on `<action>` elements
         1. Capture the `@name` attribute as name
         2. Iterate on `<rebind>` elements
            1. Capture the `@input` attribute
               1. If the input value ends with a space, replace the space with `UNBOUND`
            2. Look up the related actionData from step 1.2 using categoryName and name as `{categoryName}-{name}`
               1. if the input starts with `kb`, add it to the actionData as `keyboard`
               2. if the input starts with `js`, add it to the actionData as `joystick`
   2. Iterate on the actionData
      1. If there is no value for `keyboard`, copy the value from `defaultKeyboard`
      2. If there is no value for `joystick`, copy the value from `defaultJoystick`
4. Read `docs\joystick-bindings.md`
   1. Capture the list of Binding names
   2. Iterate over actionData from step 3 if it has a joystick binding that is not `UNBOUND`
      1. Using `js1` as `LT` and `js2` as `RT`, attempt to map a binding name to the joystick input.
         1. Any input that contains `button` followed by a number should map cleanly to a binding name like:
            1. `js1_button99` => `LT-EVO.99(SHIFT+C1_LT)`
         2. If the input is not a button, then analyze the input value against the binding names and build mapping logic into the script
         3. Copy the binding name to the actionData as `joystickDesc`
5. Generate output documents
   1. Create `docs\starcitizen\bindings\bindings.json`.
   2. Create `docs\starcitizen\bindings\bindings.md` with a table under the `GENERATED BINDINGS` section, where each row is an actionData item.
   3. Create `docs\starcitizen\bindings\bindings.tsv` with all rows.
   4. Create `docs\starcitizen\bindings\bindings.html`.


## TODO

[ ] look at activationMode in defaultProfile (i.e. spaceship_power-v_engineering_assignment_engine_max) and incorporate into description
[x] add favoriting capability to bindings html
