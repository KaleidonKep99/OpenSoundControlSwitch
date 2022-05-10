# VRChatOSCSwitch
Ever wanted to run multiple OSC apps at once while playing VRChat?<br />
If so, you might know that VRChat will not communicate with multiple apps on the same UDP ports (9000 and 9001).

This is why I created this app, which will act as a "switch" between the OSC apps (VRCFT, YeelightOSC etc.) and the target app (VRChat).

This is how the program works:<br/>
![diagram](https://i.imgur.com/7Y0KDit.png)

## Example JSON
```json
{
  "InPort": 9000,
  "OutPort": 9001,
  "ControlInPort": 8000,
  "ControlOutPort": 8001,
  "OSCPrograms": [
    {
      "Name": "VRCFT",
      "ExecutablePath": "F:\\VRChat Cache\\Tools\\VRCFaceTracking.exe",
      "CommandLine": "--osc=$InPort$:127.0.0.1:$OutPort$",
      "FwdInPort": 10000,
      "FwdOutPort": 10001,
      "SeparateConsole": true,
      "Addresses": [
        {
          "Address": "/avatar/parameters",
          "Parameters": [
            "EyesX",
            "EyesY",
            "CombinedEyeLid*",
            "LeftEye*",
            "RightEye*",
            "Jaw*",
            "Mouth*",
            "Cheek*",
            "Smile*",
            "Puff*",
            "Tongue*"
          ]
        },
        {
          "Address": "/avatar",
          "Parameters": [
            "change"
          ]
        }
      ]
    }
  ]
}
```
You can make the program generate an example JSON automatically by running it once.

## Dependencies
- [Newtonsoft.Json](https://www.newtonsoft.com/json) by Netwonsoft.
- [Bespoke.Osc](https://opensoundcontrol.stanford.edu/implementations/Bespoke-OSC.html) by matt from Standford University.