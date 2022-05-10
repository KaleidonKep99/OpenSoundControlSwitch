# YeelightOSC
Just a tiny app that allows you to connect your Yeelight/Xiaomi bulb to VRChat!

https://user-images.githubusercontent.com/11065274/161112218-3b4a58e8-9a4d-49c3-9548-49a9870f4789.mp4

## Available parameters
The base for the parameters is, of course, `/avatar/parameters/`.
- `Brightness`: float, ranges from 0.0f (0%) to 1.0f (100%)
- `Temperature`: float, ranges from 0.0f (1700K) to 1.0f (6500K)
- `ColorR`: float, ranges from 0.0f (0) to 1.0f (255)
- `ColorG`: float, ranges from 0.0f (0) to 1.0f (255)
- `ColorB`: float, ranges from 0.0f (0) to 1.0f (255)
- `SendUpdate`: int, its possible values for now are
    - `1`: Update lamp color to selected temperature
    - `2`: Update lamp color to selected RGB color
    - _might add more values as flows, TBD_
- `LightToggle`: bool, toggles the light on and off

## Dependencies
- [YeelightAPI](https://github.com/roddone/YeelightAPI) by roddone
- [Bespoke.Osc](https://opensoundcontrol.stanford.edu/implementations/Bespoke-OSC.html) by matt from Standford University.