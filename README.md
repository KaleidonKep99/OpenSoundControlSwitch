# OpenSoundControlSwitch
Ever wanted to run multiple OpenSoundContro apps at once on the same input and output ports, but for some reason one of those same apps locks access to them?<br />
If so, you might know that games like VRChat, or ChilloutVR with CVROSC, will not communicate with multiple apps on the same UDP ports (9000 and 9001), mostly because these same apps like to lock the ports, which will make them unavailable for other apps that are trying to communicate with the OSC server.<br />
For example, VRChat itself is able to receive messages from multiple apps on the same ports, but the way those same apps implement OSC support can lead to issues like this.

This is why I created this app, which will act as a "switch" between the OSC apps (VRCFT, YeelightOSC etc.) and the target app (VRChat, ChilloutVR), making life easier for both sides.

It is also a really good way to allow port forwarding of only two ports on your router, instead of multiple ones at once.

This is how the program works:<br/>
![diagram](https://i.imgur.com/7Y0KDit.png)

## How to set up the program
For more information, take a look at the wiki here: https://github.com/KaleidonKep99/OpenSoundControlSwitch/wiki/OpenSoundControlSwitch-guide

## Dependencies
- [Costura.Fody](https://github.com/Fody/Costura) by Fody.
- [Newtonsoft.Json](https://www.newtonsoft.com/json) by Netwonsoft.
- [Bespoke.Osc](https://opensoundcontrol.stanford.edu/implementations/Bespoke-OSC.html) by matt from Standford University.
