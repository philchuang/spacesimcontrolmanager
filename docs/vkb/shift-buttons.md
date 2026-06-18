# How to create a "shift" button with VKB Device Config

Double your amount of binding options with this configuration!

Some games don't recognize button combinations, only single button presses, you are very limited in the number of binds available to you. Elite Dangerous does, but Star Citizen does not.

But with this configuration modification, you can designate a button (like the pinky button) as a shift key, and get more bindable buttons!

Instructions on how to enable a "shift" key on a VKB:

1. In the VKB Dev Config software, select the desired joystick in the top window
2. Click on the Profile tab at the bottom
3. Click on the Buttons tab in the middle
4. Click on the Physical Layer tab underneath
5. Check the Poll checkbox
6. Press the desired shift key and see which physical button lights up, click on it
7. Change the mode from Buttons to SHIFT
8. In the dropdowns, select SHIFT1 and Momentary (and optionally Track as button if you still want it to register as the original logical button)
9. For each button you want to have a separate shifted value, discover & click on it
10. Check `Use SHIFT1` and enter a virtual button number for it. I simply added 80 to the existing virtual button number, e.g. button 5 -> 85.
11. When done, click on the Action tab near the top
12. Then click on the Set button
13. Repeat for other joysticks

## How to use SHIFT with HAT

The POV hat (labeled A1) is configured as a POV-type hat, so it doesn't have a typical button number associated with the directions. There is currently no shift capability for this hat, like having 2 logical POV hats and a shift button to switch between them. So in order to have this functionality you have to reprogram the POV functionality so that it behaves like buttons, not POV.

**NOTE** This functionality only worked with a newer unofficial firmware/vkbdevcfg. For my Gladiator NXT, I used firmware `v2_22_5(3)` and vkbdevcfg `v0.94_05`. Versions `94_40` through `94_44` threw constant errors.

1. In the VKB Dev Config software, select the desired joystick in the top window
2. Click on the Profile tab at the bottom
3. Click on the POVs tab in the middle
4. Change POV #1 POV Type to `HAT 4w`
5. Change Output to `ButtonsP`
6. Change the next field to the first unused block of 4 physical lines. I used 37 because 37-40 were unused.
7. Click on the Buttonss tab in the middle
8. Click on the Physical Layer tab underneath
9. Click on the physical line from step 6
10. Change type to `Buttons`
11. Click on the Button icon on the bottom, uncheck Automapping, then select a logical button number in the BUT1 field. I used 30 because 30-33 were unused.
12. Check `Use SHIFT1` and enter a virtual button number for it. I used an offset of +80, so 30/110, 31/111, etc.
13. When done, click on the Action tab near the top
14. Then click on the Set button
15. Repeat for other joysticks
