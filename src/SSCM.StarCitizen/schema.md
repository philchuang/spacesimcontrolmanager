# ActionMaps schema

As observed from alpha 3.17.4

## General structure

```yml
ActionMaps
- 1..M ActionProfiles
    - 1..M options
        - 1..M {setting node}
    - modifiers
    - 1..M actionmap
        - 1..M action
            - 1..M rebind
```

## &lt;ActionMaps>

root-level node

## &lt;ActionProfiles>

Parent object of multiple inputs & mappings. SSCM is only concerned with the default profile.

### Attributes

- version: not used
- optionsVersion: not used
- rebindVersion: not used
- profileName: looking for `default`

## &lt;options>

Describes an input device and its settings, i.e. `<options type="joystick" instance="1" Product="Fancy Joystick Name and GUID">`

### Attributes

- type: keyboard, gamepad, joystick
- instance: 1-based index for each type, i.e. joystick 1, joystick 2, gamepad 1, etc.
- Product: hardware/driver name of the input device

### Children

Each setting is a child node, with either attributes (simple) or more child nodes (complex).

Simple example:

```xml
<flight_move_strafe_vertical invert="1"/>
```

Complex example:

```xml
<flight_move_pitch>
    <nonlinearity_curve>
        <point in="0" out="0"/>
        <!-- etc -->
    </nonlinearity_curve>
</flight_move_pitch>
```

## &lt;modifiers/>

Have not analyzed yet.

## &lt;actionmap>

Category grouping for actions, i.e. `<actionmap name="seat_general">`.

## &lt;action>

Contains the bindings for a game action, i.e. `<action name="v_toggle_mining_mode">`

## &lt;rebind>

Defines the input binding for the parent action. Can be multiple rebinds per action, but I'm assuming that only a single rebind for a given input type.

`@input` has a prefix that relates to the input, i.e. `js1_*` applies to joystick 1, `kb1_*` applies to keyboard 1, etc.

Can also have a `@multiTap` attribute, which is a number.
