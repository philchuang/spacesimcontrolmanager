# Joystick binding names

This file is read during bindings doc generation so that these button names are mapped to the actual Elite Dangerous and Star Citizen binding values in order to provide a meaningful and descriptive button name in the generated docs.

Bindings are a YAML mapping. Each joystick has an Elite Dangerous device id and a Star Citizen device product string. Match that product string to a joystick `instance` in actionmaps.xml, then prefix `js#_` onto the `star_citizen` value (`button3` on instance 1 is `js1_button3`).

---

This is the author's personal mapping of his customized VKB device configuration, feel free to customize it to match your own.

I have dual VKB Gladiator joysticks. Note that left-hand joystick and SEM are attached together but I wanted to define their buttons separately.

You can use your own naming convention, but here is mine: {handedness}-{device}.{literal_name_or_button_number}{friendly_name}

Other abbreviations I use:

* LT: left
* RT: right
* JOY: VKB Gladiator joystick
* SEM: VKB Side Extension Module

Bindings:

```yaml
LT-JOY:
  elite_dangerous_device_id: "231D3205"
  star_citizen_device_id: " VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}"
  bindings:
    "LT-JOY.01(FIRE)":
      elite_dangerous: Joy_1
      star_citizen: button1
    "LT-JOY.02(FIRE-DEEP)":
      elite_dangerous: Joy_2
      star_citizen: button2
    "LT-JOY.03(A2)":
      elite_dangerous: Joy_3
      star_citizen: button3
    "LT-JOY.04(B1)":
      elite_dangerous: Joy_4
      star_citizen: button4
    "LT-JOY.05(D1)":
      elite_dangerous: Joy_5
      star_citizen: button5
    "LT-JOY.06(A3_UP)":
      elite_dangerous: Joy_6
      star_citizen: button6
    "LT-JOY.07(A3_RT)":
      elite_dangerous: Joy_7
      star_citizen: button7
    "LT-JOY.08(A3_DN)":
      elite_dangerous: Joy_8
      star_citizen: button8
    "LT-JOY.09(A3_LT)":
      elite_dangerous: Joy_9
      star_citizen: button9
    "LT-JOY.10(A3_BTN)":
      elite_dangerous: Joy_10
      star_citizen: button10
    "LT-JOY.11(A4_UP)":
      elite_dangerous: Joy_11
      star_citizen: button11
    "LT-JOY.12(A4_RT)":
      elite_dangerous: Joy_12
      star_citizen: button12
    "LT-JOY.13(A4_DN)":
      elite_dangerous: Joy_13
      star_citizen: button13
    "LT-JOY.14(A4_LT)":
      elite_dangerous: Joy_14
      star_citizen: button14
    "LT-JOY.15(A4_BTN)":
      elite_dangerous: Joy_15
      star_citizen: button15
    "LT-JOY.16(C1_UP)":
      elite_dangerous: Joy_16
      star_citizen: button16
    "LT-JOY.17(C1_RT)":
      elite_dangerous: Joy_17
      star_citizen: button17
    "LT-JOY.18(C1_DN)":
      elite_dangerous: Joy_18
      star_citizen: button18
    "LT-JOY.19(C1_LT)":
      elite_dangerous: Joy_19
      star_citizen: button19
    "LT-JOY.20(C1_BTN)":
      elite_dangerous: Joy_20
      star_citizen: button20
    "LT-JOY.21(RFIRE_FW)":
      elite_dangerous: Joy_21
      star_citizen: button21
    "LT-JOY.22(RFIRE_BK)":
      elite_dangerous: Joy_22
      star_citizen: button22
    "LT-JOY.23(EN1_UP)":
      elite_dangerous: Joy_23
      star_citizen: button23
    "LT-JOY.24(EN1_DN)":
      elite_dangerous: Joy_24
      star_citizen: button24
    "LT-JOY.25(EN2_UP)":
      elite_dangerous: Joy_25
      star_citizen: button25
    "LT-JOY.26(EN2_DN)":
      elite_dangerous: Joy_26
      star_citizen: button26
    "LT-JOY.27(F1)":
      elite_dangerous: Joy_27
      star_citizen: button27
    "LT-JOY.28(F2)":
      elite_dangerous: Joy_28
      star_citizen: button28
    "LT-JOY.29(F3)":
      elite_dangerous: Joy_29
      star_citizen: button29
    "LT-JOY.30(A1_UP)":
      elite_dangerous: Joy_30
      star_citizen: button30
    "LT-JOY.31(A1_RT)":
      elite_dangerous: Joy_31
      star_citizen: button31
    "LT-JOY.32(A1_DN)":
      elite_dangerous: Joy_32
      star_citizen: button32
    "LT-JOY.33(A1_LT)":
      elite_dangerous: Joy_33
      star_citizen: button33
    "LT-JOY.81(SHIFT+FIRE)":
      elite_dangerous: Joy_81
      star_citizen: button81
    "LT-JOY.82(SHIFT+FIRE-DEEP)":
      elite_dangerous: Joy_82
      star_citizen: button82
    "LT-JOY.83(SHIFT+A2)":
      elite_dangerous: Joy_83
      star_citizen: button83
    "LT-JOY.84(SHIFT+B1)":
      elite_dangerous: Joy_84
      star_citizen: button84
    "LT-JOY.85(SHIFT+D1)":
      elite_dangerous: Joy_85
      star_citizen: button85
    "LT-JOY.86(SHIFT+A3_UP)":
      elite_dangerous: Joy_86
      star_citizen: button86
    "LT-JOY.87(SHIFT+A3_RT)":
      elite_dangerous: Joy_87
      star_citizen: button87
    "LT-JOY.88(SHIFT+A3_DN)":
      elite_dangerous: Joy_88
      star_citizen: button88
    "LT-JOY.89(SHIFT+A3_LT)":
      elite_dangerous: Joy_89
      star_citizen: button89
    "LT-JOY.90(SHIFT+A3_BTN)":
      elite_dangerous: Joy_90
      star_citizen: button90
    "LT-JOY.91(SHIFT+A4_UP)":
      elite_dangerous: Joy_91
      star_citizen: button91
    "LT-JOY.92(SHIFT+A4_RT)":
      elite_dangerous: Joy_92
      star_citizen: button92
    "LT-JOY.93(SHIFT+A4_DN)":
      elite_dangerous: Joy_93
      star_citizen: button93
    "LT-JOY.94(SHIFT+A4_LT)":
      elite_dangerous: Joy_94
      star_citizen: button94
    "LT-JOY.95(SHIFT+A4_BTN)":
      elite_dangerous: Joy_95
      star_citizen: button95
    "LT-JOY.96(SHIFT+C1_UP)":
      elite_dangerous: Joy_96
      star_citizen: button96
    "LT-JOY.97(SHIFT+C1_RT)":
      elite_dangerous: Joy_97
      star_citizen: button97
    "LT-JOY.98(SHIFT+C1_DN)":
      elite_dangerous: Joy_98
      star_citizen: button98
    "LT-JOY.99(SHIFT+C1_LT)":
      elite_dangerous: Joy_99
      star_citizen: button99
    "LT-JOY.100(SHIFT+C1_BTN)":
      elite_dangerous: Joy_100
      star_citizen: button100
    "LT-JOY.101(SHIFT+RFIRE_FW)":
      elite_dangerous: Joy_101
      star_citizen: button101
    "LT-JOY.102(SHIFT+RFIRE_BK)":
      elite_dangerous: Joy_102
      star_citizen: button102
    "LT-JOY.107(SHIFT+F1)":
      elite_dangerous: Joy_107
      star_citizen: button107
    "LT-JOY.108(SHIFT+F2)":
      elite_dangerous: Joy_108
      star_citizen: button108
    "LT-JOY.109(SHIFT+F3)":
      elite_dangerous: Joy_109
      star_citizen: button109
    "LT-JOY.110(SHIFT+A1_UP)":
      elite_dangerous: Joy_110
      star_citizen: button110
    "LT-JOY.111(SHIFT+A1_RT)":
      elite_dangerous: Joy_111
      star_citizen: button111
    "LT-JOY.112(SHIFT+A1_DN)":
      elite_dangerous: Joy_112
      star_citizen: button112
    "LT-JOY.113(SHIFT+A1_LT)":
      elite_dangerous: Joy_113
      star_citizen: button113
    "LT-JOY.A1_X_Axis":
      elite_dangerous: Joy_RXAxis
      star_citizen: rotx
    "LT-JOY.A1_Y_Axis":
      elite_dangerous: Joy_RYAxis
      star_citizen: roty
    "LT-JOY.POV1(A1_DN)":
      elite_dangerous: Joy_POV1Down
      star_citizen: hat1_down
    "LT-JOY.POV1(A1_LT)":
      elite_dangerous: Joy_POV1Left
      star_citizen: hat1_left
    "LT-JOY.POV1(A1_RT)":
      elite_dangerous: Joy_POV1Right
      star_citizen: hat1_right
    "LT-JOY.POV1(A1_UP)":
      elite_dangerous: Joy_POV1Up
      star_citizen: hat1_up
    "LT-JOY.X_Axis":
      elite_dangerous: Joy_XAxis
      star_citizen: x
    "LT-JOY.Y_Axis":
      elite_dangerous: Joy_YAxis
      star_citizen: y
    "LT-JOY.Z_Axis":
      elite_dangerous: Joy_ZAxis
      star_citizen: z
    "LT-JOY.Z_Rot":
      elite_dangerous: Joy_RZAxis
      star_citizen: rotz

LT-SEM:
  elite_dangerous_device_id: "231D3205"
  star_citizen_device_id: " VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}"
  bindings:
    "LT-SEM.41(A1)":
      elite_dangerous: Joy_41
      star_citizen: button41
    "LT-SEM.42(A2)":
      elite_dangerous: Joy_42
      star_citizen: button42
    "LT-SEM.43(B1)":
      elite_dangerous: Joy_43
      star_citizen: button43
    "LT-SEM.44(B2)":
      elite_dangerous: Joy_44
      star_citizen: button44
    "LT-SEM.45(B3)":
      elite_dangerous: Joy_45
      star_citizen: button45
    "LT-SEM.46(SW1_UP)":
      elite_dangerous: Joy_46
      star_citizen: button46
    "LT-SEM.47(SW1_BTN)":
      elite_dangerous: Joy_47
      star_citizen: button47
    "LT-SEM.48(SW1_DN)":
      elite_dangerous: Joy_48
      star_citizen: button48
    "LT-SEM.49(SW2_UP)":
      elite_dangerous: Joy_49
      star_citizen: button49
    "LT-SEM.50(SW2_BTN)":
      elite_dangerous: Joy_50
      star_citizen: button50
    "LT-SEM.51(SW2_DN)":
      elite_dangerous: Joy_51
      star_citizen: button51
    "LT-SEM.52(Start)":
      elite_dangerous: Joy_52
      star_citizen: button52
    "LT-SEM.53(Mode_1)":
      elite_dangerous: Joy_53
      star_citizen: button53
    "LT-SEM.54(Mode_2)":
      elite_dangerous: Joy_54
      star_citizen: button54
    "LT-SEM.55(Mode_3)":
      elite_dangerous: Joy_55
      star_citizen: button55
    "LT-SEM.56(Mode_4)":
      elite_dangerous: Joy_56
      star_citizen: button56
    "LT-SEM.57(C1)":
      elite_dangerous: Joy_57
      star_citizen: button57
    "LT-SEM.58(Flaps_1)":
      elite_dangerous: Joy_58
      star_citizen: button58
    "LT-SEM.59(Flaps_2)":
      elite_dangerous: Joy_59
      star_citizen: button59
    "LT-SEM.61(Flaps_4)":
      elite_dangerous: Joy_61
      star_citizen: button61
    "LT-SEM.62(Gear_DN)":
      elite_dangerous: Joy_62
      star_citizen: button62
    "LT-SEM.63(Gear_MID)":
      elite_dangerous: Joy_63
      star_citizen: button63
    "LT-SEM.64(Gear_UP)":
      elite_dangerous: Joy_64
      star_citizen: button64

RT-JOY:
  elite_dangerous_device_id: "231D0200"
  star_citizen_device_id: " VKBsim Gladiator NXT  R    {0200231D-0000-0000-0000-504944564944}"
  bindings:
    "RT-JOY.01(FIRE)":
      elite_dangerous: Joy_1
      star_citizen: button1
    "RT-JOY.02(FIRE-DEEP)":
      elite_dangerous: Joy_2
      star_citizen: button2
    "RT-JOY.03(A2)":
      elite_dangerous: Joy_3
      star_citizen: button3
    "RT-JOY.04(B1)":
      elite_dangerous: Joy_4
      star_citizen: button4
    "RT-JOY.05(D1)":
      elite_dangerous: Joy_5
      star_citizen: button5
    "RT-JOY.06(A3_UP)":
      elite_dangerous: Joy_6
      star_citizen: button6
    "RT-JOY.07(A3_RT)":
      elite_dangerous: Joy_7
      star_citizen: button7
    "RT-JOY.08(A3_DN)":
      elite_dangerous: Joy_8
      star_citizen: button8
    "RT-JOY.09(A3_LT)":
      elite_dangerous: Joy_9
      star_citizen: button9
    "RT-JOY.10(A3_BTN)":
      elite_dangerous: Joy_10
      star_citizen: button10
    "RT-JOY.11(A4_UP)":
      elite_dangerous: Joy_11
      star_citizen: button11
    "RT-JOY.12(A4_RT)":
      elite_dangerous: Joy_12
      star_citizen: button12
    "RT-JOY.13(A4_DN)":
      elite_dangerous: Joy_13
      star_citizen: button13
    "RT-JOY.14(A4_LT)":
      elite_dangerous: Joy_14
      star_citizen: button14
    "RT-JOY.15(A4_BTN)":
      elite_dangerous: Joy_15
      star_citizen: button15
    "RT-JOY.16(C1_UP)":
      elite_dangerous: Joy_16
      star_citizen: button16
    "RT-JOY.17(C1_RT)":
      elite_dangerous: Joy_17
      star_citizen: button17
    "RT-JOY.18(C1_DN)":
      elite_dangerous: Joy_18
      star_citizen: button18
    "RT-JOY.19(C1_LT)":
      elite_dangerous: Joy_19
      star_citizen: button19
    "RT-JOY.20(C1_BTN)":
      elite_dangerous: Joy_20
      star_citizen: button20
    "RT-JOY.21(RFIRE_FW)":
      elite_dangerous: Joy_21
      star_citizen: button21
    "RT-JOY.22(RFIRE_BK)":
      elite_dangerous: Joy_22
      star_citizen: button22
    "RT-JOY.23(EN1_UP)":
      elite_dangerous: Joy_23
      star_citizen: button23
    "RT-JOY.24(EN1_DN)":
      elite_dangerous: Joy_24
      star_citizen: button24
    "RT-JOY.25(EN2_UP)":
      elite_dangerous: Joy_25
      star_citizen: button25
    "RT-JOY.26(EN2_DN)":
      elite_dangerous: Joy_26
      star_citizen: button26
    "RT-JOY.27(F1)":
      elite_dangerous: Joy_27
      star_citizen: button27
    "RT-JOY.28(F2)":
      elite_dangerous: Joy_28
      star_citizen: button28
    "RT-JOY.29(F3)":
      elite_dangerous: Joy_29
      star_citizen: button29
    "RT-JOY.30(A1_UP)":
      elite_dangerous: Joy_30
      star_citizen: button30
    "RT-JOY.31(A1_RT)":
      elite_dangerous: Joy_31
      star_citizen: button31
    "RT-JOY.32(A1_DN)":
      elite_dangerous: Joy_32
      star_citizen: button32
    "RT-JOY.33(A1_LT)":
      elite_dangerous: Joy_33
      star_citizen: button33
    "RT-JOY.81(SHIFT+FIRE)":
      elite_dangerous: Joy_81
      star_citizen: button81
    "RT-JOY.82(SHIFT+FIRE-DEEP)":
      elite_dangerous: Joy_82
      star_citizen: button82
    "RT-JOY.83(SHIFT+A2)":
      elite_dangerous: Joy_83
      star_citizen: button83
    "RT-JOY.84(SHIFT+B1)":
      elite_dangerous: Joy_84
      star_citizen: button84
    "RT-JOY.86(SHIFT+A3_UP)":
      elite_dangerous: Joy_86
      star_citizen: button86
    "RT-JOY.87(SHIFT+A3_RT)":
      elite_dangerous: Joy_87
      star_citizen: button87
    "RT-JOY.88(SHIFT+A3_DN)":
      elite_dangerous: Joy_88
      star_citizen: button88
    "RT-JOY.89(SHIFT+A3_LT)":
      elite_dangerous: Joy_89
      star_citizen: button89
    "RT-JOY.90(SHIFT+A3_BTN)":
      elite_dangerous: Joy_90
      star_citizen: button90
    "RT-JOY.91(SHIFT+A4_UP)":
      elite_dangerous: Joy_91
      star_citizen: button91
    "RT-JOY.92(SHIFT+A4_RT)":
      elite_dangerous: Joy_92
      star_citizen: button92
    "RT-JOY.93(SHIFT+A4_DN)":
      elite_dangerous: Joy_93
      star_citizen: button93
    "RT-JOY.94(SHIFT+A4_LT)":
      elite_dangerous: Joy_94
      star_citizen: button94
    "RT-JOY.95(SHIFT+A4_BTN)":
      elite_dangerous: Joy_95
      star_citizen: button95
    "RT-JOY.96(SHIFT+C1_UP)":
      elite_dangerous: Joy_96
      star_citizen: button96
    "RT-JOY.97(SHIFT+C1_RT)":
      elite_dangerous: Joy_97
      star_citizen: button97
    "RT-JOY.98(SHIFT+C1_DN)":
      elite_dangerous: Joy_98
      star_citizen: button98
    "RT-JOY.99(SHIFT+C1_LT)":
      elite_dangerous: Joy_99
      star_citizen: button99
    "RT-JOY.100(SHIFT+C1_BTN)":
      elite_dangerous: Joy_100
      star_citizen: button100
    "RT-JOY.101(SHIFT+RFIRE_FW)":
      elite_dangerous: Joy_101
      star_citizen: button101
    "RT-JOY.102(SHIFT+RFIRE_BK)":
      elite_dangerous: Joy_102
      star_citizen: button102
    "RT-JOY.107(SHIFT+F1)":
      elite_dangerous: Joy_107
      star_citizen: button107
    "RT-JOY.108(SHIFT+F2)":
      elite_dangerous: Joy_108
      star_citizen: button108
    "RT-JOY.109(SHIFT+F3)":
      elite_dangerous: Joy_109
      star_citizen: button109
    "RT-JOY.110(SHIFT+A1_UP)":
      elite_dangerous: Joy_110
      star_citizen: button110
    "RT-JOY.111(SHIFT+A1_RT)":
      elite_dangerous: Joy_111
      star_citizen: button111
    "RT-JOY.112(SHIFT+A1_DN)":
      elite_dangerous: Joy_112
      star_citizen: button112
    "RT-JOY.113(SHIFT+A1_LT)":
      elite_dangerous: Joy_113
      star_citizen: button113
    "RT-JOY.A1_X_Axis":
      elite_dangerous: Joy_RXAxis
      star_citizen: rotx
    "RT-JOY.A1_Y_Axis":
      elite_dangerous: Joy_RYAxis
      star_citizen: roty
    "RT-JOY.POV1(A1_DN)":
      elite_dangerous: Joy_POV1Down
      star_citizen: hat1_down
    "RT-JOY.POV1(A1_LT)":
      elite_dangerous: Joy_POV1Left
      star_citizen: hat1_left
    "RT-JOY.POV1(A1_RT)":
      elite_dangerous: Joy_POV1Right
      star_citizen: hat1_right
    "RT-JOY.POV1(A1_UP)":
      elite_dangerous: Joy_POV1Up
      star_citizen: hat1_up
    "RT-JOY.X_Axis":
      elite_dangerous: Joy_XAxis
      star_citizen: x
    "RT-JOY.Y_Axis":
      elite_dangerous: Joy_YAxis
      star_citizen: y
    "RT-JOY.Z_Axis":
      elite_dangerous: Joy_ZAxis
      star_citizen: z
    "RT-JOY.Z_Rot":
      elite_dangerous: Joy_RZAxis
      star_citizen: rotz
```
