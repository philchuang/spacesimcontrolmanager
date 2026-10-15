# Joystick binding names

This is the author's personal mapping of his customized VKB device configuration. These values are mapped to the actual Elite Dangerous and Star Citizen binding values in order to provide a meaningful and descriptive name for the generated docs.

Syntax: {handedness}-{device}.{literal_name_or_button_number}{friendly_name}

Other terms:

* LT: left
* RT: right
* EVO: VKB Gladiator NXT EVO joystick
* SEM: VKB Side Extension Module

Note that EVO and SEM are attached together so its not as important to consider as the handedness.

Bindings are a YAML mapping. Each joystick has an Elite Dangerous device id and a Star Citizen device product string. Match that product string to a joystick `instance` in actionmaps.xml, then prefix `js#_` onto the `star_citizen` value (`button3` on instance 1 is `js1_button3`).

Bindings:

```yaml
LT-EVO:
  elite_dangerous_device_id: "231D3205"
  star_citizen_device_id: " VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}"
  bindings:
    "LT-EVO.01(FIRE)":
      elite_dangerous: Joy_1
      star_citizen: button1
    "LT-EVO.03(A2)":
      elite_dangerous: Joy_3
      star_citizen: button3
    "LT-EVO.04(B1)":
      elite_dangerous: Joy_4
      star_citizen: button4
    "LT-EVO.05(D1)":
      elite_dangerous: Joy_5
      star_citizen: button5
    "LT-EVO.06(A3_UP)":
      elite_dangerous: Joy_6
      star_citizen: button6
    "LT-EVO.07(A3_RT)":
      elite_dangerous: Joy_7
      star_citizen: button7
    "LT-EVO.08(A3_DN)":
      elite_dangerous: Joy_8
      star_citizen: button8
    "LT-EVO.09(A3_LT)":
      elite_dangerous: Joy_9
      star_citizen: button9
    "LT-EVO.10(A3_BTN)":
      elite_dangerous: Joy_10
      star_citizen: button10
    "LT-EVO.11(A4_UP)":
      elite_dangerous: Joy_11
      star_citizen: button11
    "LT-EVO.12(A4_RT)":
      elite_dangerous: Joy_12
      star_citizen: button12
    "LT-EVO.13(A4_DN)":
      elite_dangerous: Joy_13
      star_citizen: button13
    "LT-EVO.14(A4_LT)":
      elite_dangerous: Joy_14
      star_citizen: button14
    "LT-EVO.15(A4_BTN)":
      elite_dangerous: Joy_15
      star_citizen: button15
    "LT-EVO.16(C1_UP)":
      elite_dangerous: Joy_16
      star_citizen: button16
    "LT-EVO.17(C1_RT)":
      elite_dangerous: Joy_17
      star_citizen: button17
    "LT-EVO.18(C1_DN)":
      elite_dangerous: Joy_18
      star_citizen: button18
    "LT-EVO.19(C1_LT)":
      elite_dangerous: Joy_19
      star_citizen: button19
    "LT-EVO.20(C1_BTN)":
      elite_dangerous: Joy_20
      star_citizen: button20
    "LT-EVO.21(RFIRE_FW)":
      elite_dangerous: Joy_21
      star_citizen: button21
    "LT-EVO.22(RFIRE_BK)":
      elite_dangerous: Joy_22
      star_citizen: button22
    "LT-EVO.23(EN1_UP)":
      elite_dangerous: Joy_23
      star_citizen: button23
    "LT-EVO.24(EN1_DN)":
      elite_dangerous: Joy_24
      star_citizen: button24
    "LT-EVO.25(EN2_UP)":
      elite_dangerous: Joy_25
      star_citizen: button25
    "LT-EVO.26(EN2_DN)":
      elite_dangerous: Joy_26
      star_citizen: button26
    "LT-EVO.27(F1)":
      elite_dangerous: Joy_27
      star_citizen: button27
    "LT-EVO.28(F2)":
      elite_dangerous: Joy_28
      star_citizen: button28
    "LT-EVO.29(F3)":
      elite_dangerous: Joy_29
      star_citizen: button29
    "LT-EVO.30(A1_UP)":
      elite_dangerous: Joy_30
      star_citizen: button30
    "LT-EVO.31(A1_RT)":
      elite_dangerous: Joy_31
      star_citizen: button31
    "LT-EVO.32(A1_DN)":
      elite_dangerous: Joy_32
      star_citizen: button32
    "LT-EVO.33(A1_LT)":
      elite_dangerous: Joy_33
      star_citizen: button33
    "LT-EVO.81(SHIFT+FIRE)":
      elite_dangerous: Joy_81
      star_citizen: button81
    "LT-EVO.82(SHIFT+FIRE-DEEP)":
      elite_dangerous: Joy_82
      star_citizen: button82
    "LT-EVO.83(SHIFT+A2)":
      elite_dangerous: Joy_83
      star_citizen: button83
    "LT-EVO.84(SHIFT+B1)":
      elite_dangerous: Joy_84
      star_citizen: button84
    "LT-EVO.85(SHIFT+D1)":
      elite_dangerous: Joy_85
      star_citizen: button85
    "LT-EVO.86(SHIFT+A3_UP)":
      elite_dangerous: Joy_86
      star_citizen: button86
    "LT-EVO.87(SHIFT+A3_RT)":
      elite_dangerous: Joy_87
      star_citizen: button87
    "LT-EVO.88(SHIFT+A3_DN)":
      elite_dangerous: Joy_88
      star_citizen: button88
    "LT-EVO.89(SHIFT+A3_LT)":
      elite_dangerous: Joy_89
      star_citizen: button89
    "LT-EVO.90(SHIFT+A3_BTN)":
      elite_dangerous: Joy_90
      star_citizen: button90
    "LT-EVO.91(SHIFT+A4_UP)":
      elite_dangerous: Joy_91
      star_citizen: button91
    "LT-EVO.92(SHIFT+A4_RT)":
      elite_dangerous: Joy_92
      star_citizen: button92
    "LT-EVO.93(SHIFT+A4_DN)":
      elite_dangerous: Joy_93
      star_citizen: button93
    "LT-EVO.94(SHIFT+A4_LT)":
      elite_dangerous: Joy_94
      star_citizen: button94
    "LT-EVO.95(SHIFT+A4_BTN)":
      elite_dangerous: Joy_95
      star_citizen: button95
    "LT-EVO.96(SHIFT+C1_UP)":
      elite_dangerous: Joy_96
      star_citizen: button96
    "LT-EVO.97(SHIFT+C1_RT)":
      elite_dangerous: Joy_97
      star_citizen: button97
    "LT-EVO.98(SHIFT+C1_DN)":
      elite_dangerous: Joy_98
      star_citizen: button98
    "LT-EVO.99(SHIFT+C1_LT)":
      elite_dangerous: Joy_99
      star_citizen: button99
    "LT-EVO.100(SHIFT+C1_BTN)":
      elite_dangerous: Joy_100
      star_citizen: button100
    "LT-EVO.101(SHIFT+RFIRE_FW)":
      elite_dangerous: Joy_101
      star_citizen: button101
    "LT-EVO.102(SHIFT+RFIRE_BK)":
      elite_dangerous: Joy_102
      star_citizen: button102
    "LT-EVO.107(SHIFT+F1)":
      elite_dangerous: Joy_107
      star_citizen: button107
    "LT-EVO.108(SHIFT+F2)":
      elite_dangerous: Joy_108
      star_citizen: button108
    "LT-EVO.109(SHIFT+F3)":
      elite_dangerous: Joy_109
      star_citizen: button109
    "LT-EVO.110(SHIFT+A1_UP)":
      elite_dangerous: Joy_110
      star_citizen: button110
    "LT-EVO.111(SHIFT+A1_RT)":
      elite_dangerous: Joy_111
      star_citizen: button111
    "LT-EVO.112(SHIFT+A1_DN)":
      elite_dangerous: Joy_112
      star_citizen: button112
    "LT-EVO.113(SHIFT+A1_LT)":
      elite_dangerous: Joy_113
      star_citizen: button113
    "LT-EVO.A1_X_Axis":
      elite_dangerous: Joy_RXAxis
      star_citizen: rotx
    "LT-EVO.A1_Y_Axis":
      elite_dangerous: Joy_RYAxis
      star_citizen: roty
    "LT-EVO.POV1(A1_DN)":
      elite_dangerous: Joy_POV1Down
      star_citizen: hat1_down
    "LT-EVO.POV1(A1_LT)":
      elite_dangerous: Joy_POV1Left
      star_citizen: hat1_left
    "LT-EVO.POV1(A1_RT)":
      elite_dangerous: Joy_POV1Right
      star_citizen: hat1_right
    "LT-EVO.POV1(A1_UP)":
      elite_dangerous: Joy_POV1Up
      star_citizen: hat1_up
    "LT-EVO.X_Axis":
      elite_dangerous: Joy_XAxis
      star_citizen: x
    "LT-EVO.Y_Axis":
      elite_dangerous: Joy_YAxis
      star_citizen: y
    "LT-EVO.Z_Axis":
      elite_dangerous: Joy_ZAxis
      star_citizen: z
    "LT-EVO.Z_Rot":
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

RT-EVO:
  elite_dangerous_device_id: "231D0200"
  star_citizen_device_id: " VKBsim Gladiator NXT  R    {0200231D-0000-0000-0000-504944564944}"
  bindings:
    "RT-EVO.01(FIRE)":
      elite_dangerous: Joy_1
      star_citizen: button1
    "RT-EVO.02(FIRE-DEEP)":
      elite_dangerous: Joy_2
      star_citizen: button2
    "RT-EVO.03(A2)":
      elite_dangerous: Joy_3
      star_citizen: button3
    "RT-EVO.04(B1)":
      elite_dangerous: Joy_4
      star_citizen: button4
    "RT-EVO.05(D1)":
      elite_dangerous: Joy_5
      star_citizen: button5
    "RT-EVO.06(A3_UP)":
      elite_dangerous: Joy_6
      star_citizen: button6
    "RT-EVO.07(A3_RT)":
      elite_dangerous: Joy_7
      star_citizen: button7
    "RT-EVO.08(A3_DN)":
      elite_dangerous: Joy_8
      star_citizen: button8
    "RT-EVO.09(A3_LT)":
      elite_dangerous: Joy_9
      star_citizen: button9
    "RT-EVO.10(A3_BTN)":
      elite_dangerous: Joy_10
      star_citizen: button10
    "RT-EVO.11(A4_UP)":
      elite_dangerous: Joy_11
      star_citizen: button11
    "RT-EVO.12(A4_RT)":
      elite_dangerous: Joy_12
      star_citizen: button12
    "RT-EVO.13(A4_DN)":
      elite_dangerous: Joy_13
      star_citizen: button13
    "RT-EVO.14(A4_LT)":
      elite_dangerous: Joy_14
      star_citizen: button14
    "RT-EVO.15(A4_BTN)":
      elite_dangerous: Joy_15
      star_citizen: button15
    "RT-EVO.16(C1_UP)":
      elite_dangerous: Joy_16
      star_citizen: button16
    "RT-EVO.17(C1_RT)":
      elite_dangerous: Joy_17
      star_citizen: button17
    "RT-EVO.18(C1_DN)":
      elite_dangerous: Joy_18
      star_citizen: button18
    "RT-EVO.19(C1_LT)":
      elite_dangerous: Joy_19
      star_citizen: button19
    "RT-EVO.20(C1_BTN)":
      elite_dangerous: Joy_20
      star_citizen: button20
    "RT-EVO.21(RFIRE_FW)":
      elite_dangerous: Joy_21
      star_citizen: button21
    "RT-EVO.22(RFIRE_BK)":
      elite_dangerous: Joy_22
      star_citizen: button22
    "RT-EVO.23(EN1_UP)":
      elite_dangerous: Joy_23
      star_citizen: button23
    "RT-EVO.24(EN1_DN)":
      elite_dangerous: Joy_24
      star_citizen: button24
    "RT-EVO.25(EN2_UP)":
      elite_dangerous: Joy_25
      star_citizen: button25
    "RT-EVO.26(EN2_DN)":
      elite_dangerous: Joy_26
      star_citizen: button26
    "RT-EVO.27(F1)":
      elite_dangerous: Joy_27
      star_citizen: button27
    "RT-EVO.28(F2)":
      elite_dangerous: Joy_28
      star_citizen: button28
    "RT-EVO.29(F3)":
      elite_dangerous: Joy_29
      star_citizen: button29
    "RT-EVO.30(A1_UP)":
      elite_dangerous: Joy_30
      star_citizen: button30
    "RT-EVO.31(A1_RT)":
      elite_dangerous: Joy_31
      star_citizen: button31
    "RT-EVO.32(A1_DN)":
      elite_dangerous: Joy_32
      star_citizen: button32
    "RT-EVO.33(A1_LT)":
      elite_dangerous: Joy_33
      star_citizen: button33
    "RT-EVO.81(SHIFT+FIRE)":
      elite_dangerous: Joy_81
      star_citizen: button81
    "RT-EVO.82(SHIFT+FIRE-DEEP)":
      elite_dangerous: Joy_82
      star_citizen: button82
    "RT-EVO.83(SHIFT+A2)":
      elite_dangerous: Joy_83
      star_citizen: button83
    "RT-EVO.84(SHIFT+B1)":
      elite_dangerous: Joy_84
      star_citizen: button84
    "RT-EVO.86(SHIFT+A3_UP)":
      elite_dangerous: Joy_86
      star_citizen: button86
    "RT-EVO.87(SHIFT+A3_RT)":
      elite_dangerous: Joy_87
      star_citizen: button87
    "RT-EVO.88(SHIFT+A3_DN)":
      elite_dangerous: Joy_88
      star_citizen: button88
    "RT-EVO.89(SHIFT+A3_LT)":
      elite_dangerous: Joy_89
      star_citizen: button89
    "RT-EVO.90(SHIFT+A3_BTN)":
      elite_dangerous: Joy_90
      star_citizen: button90
    "RT-EVO.91(SHIFT+A4_UP)":
      elite_dangerous: Joy_91
      star_citizen: button91
    "RT-EVO.92(SHIFT+A4_RT)":
      elite_dangerous: Joy_92
      star_citizen: button92
    "RT-EVO.93(SHIFT+A4_DN)":
      elite_dangerous: Joy_93
      star_citizen: button93
    "RT-EVO.94(SHIFT+A4_LT)":
      elite_dangerous: Joy_94
      star_citizen: button94
    "RT-EVO.95(SHIFT+A4_BTN)":
      elite_dangerous: Joy_95
      star_citizen: button95
    "RT-EVO.96(SHIFT+C1_UP)":
      elite_dangerous: Joy_96
      star_citizen: button96
    "RT-EVO.97(SHIFT+C1_RT)":
      elite_dangerous: Joy_97
      star_citizen: button97
    "RT-EVO.98(SHIFT+C1_DN)":
      elite_dangerous: Joy_98
      star_citizen: button98
    "RT-EVO.99(SHIFT+C1_LT)":
      elite_dangerous: Joy_99
      star_citizen: button99
    "RT-EVO.100(SHIFT+C1_BTN)":
      elite_dangerous: Joy_100
      star_citizen: button100
    "RT-EVO.101(SHIFT+RFIRE_FW)":
      elite_dangerous: Joy_101
      star_citizen: button101
    "RT-EVO.102(SHIFT+RFIRE_BK)":
      elite_dangerous: Joy_102
      star_citizen: button102
    "RT-EVO.107(SHIFT+F1)":
      elite_dangerous: Joy_107
      star_citizen: button107
    "RT-EVO.108(SHIFT+F2)":
      elite_dangerous: Joy_108
      star_citizen: button108
    "RT-EVO.109(SHIFT+F3)":
      elite_dangerous: Joy_109
      star_citizen: button109
    "RT-EVO.110(SHIFT+A1_UP)":
      elite_dangerous: Joy_110
      star_citizen: button110
    "RT-EVO.111(SHIFT+A1_RT)":
      elite_dangerous: Joy_111
      star_citizen: button111
    "RT-EVO.112(SHIFT+A1_DN)":
      elite_dangerous: Joy_112
      star_citizen: button112
    "RT-EVO.113(SHIFT+A1_LT)":
      elite_dangerous: Joy_113
      star_citizen: button113
    "RT-EVO.A1_X_Axis":
      elite_dangerous: Joy_RXAxis
      star_citizen: rotx
    "RT-EVO.A1_Y_Axis":
      elite_dangerous: Joy_RYAxis
      star_citizen: roty
    "RT-EVO.POV1(A1_DN)":
      elite_dangerous: Joy_POV1Down
      star_citizen: hat1_down
    "RT-EVO.POV1(A1_LT)":
      elite_dangerous: Joy_POV1Left
      star_citizen: hat1_left
    "RT-EVO.POV1(A1_RT)":
      elite_dangerous: Joy_POV1Right
      star_citizen: hat1_right
    "RT-EVO.POV1(A1_UP)":
      elite_dangerous: Joy_POV1Up
      star_citizen: hat1_up
    "RT-EVO.X_Axis":
      elite_dangerous: Joy_XAxis
      star_citizen: x
    "RT-EVO.Y_Axis":
      elite_dangerous: Joy_YAxis
      star_citizen: y
    "RT-EVO.Z_Axis":
      elite_dangerous: Joy_ZAxis
      star_citizen: z
    "RT-EVO.Z_Rot":
      elite_dangerous: Joy_RZAxis
      star_citizen: rotz
```
