[center][size=6]HTFDrone[/size]
[size=3]An FPV kamikaze drone for How to Fish[/size][/center]

[center][img]https://raw.githubusercontent.com/Toemmsen96/HTFDrone/master/.ghimages/fpv-view-osd.png[/img][/center]

Turns a stick of dynamite into a hand-flown, acro-mode quadcopter with a real Betaflight-style FPV overlay. Buy one from a stand on every island, or convert TNT you're already holding on the spot - then fly it into something and watch it go off.

No custom drone model is loaded in - the "drone" [i]is[/i] the game's own explosive item wearing a quadcopter frame built at runtime, flown with real rigidbody physics. That's why it works in multiplayer with no extra networking: everyone else already sees the thrown item, modded or not - they just see a flying stick of TNT instead of a quad.

[size=4]Features[/size]
[list]
[*]Real acro/rate flight physics - mass, gravity, drag, no auto-level, can flip and fly inverted like a real quad
[*]Betaflight/INAV-style OSD: artificial horizon, altitude, speed, battery, home arrow
[*]Buy a drone from a shop stand on every island, or convert a held explosive with one keypress
[*]Full mod menu integration (CTDynamicModMenu) - every setting is a slider, button or toggle, no config file editing
[*]Per-axis stick sensitivity, deadzone and channel reversal - set your rates in the menu instead of on the radio
[*]Tune thrust, hover point, rotation rate, drag, flight time, camera mount, uptilt and FOV with sliders - live, mid-flight
[*]Other modded players see your drone as a drone, not as TNT
[/list]

[size=4]Tuning[/size]
[center][img]https://raw.githubusercontent.com/Toemmsen96/HTFDrone/master/.ghimages/mod-menu-new.png[/img][/center]

Open the mod menu ([b]F4[/b]) and expand anything in the [b]Drone[/b] category - every value is a slider, and every one of them applies immediately to a drone that is already in the air. No relaunch, no config file.
[list]
[*][b]Flight model[/b] - max thrust, hover throttle point, rotation rate, drag, flight time
[*][b]Camera[/b] - forward and height mount offset, field of view, uptilt
[*][b]Input[/b] - stick deadzone, per-axis sensitivity, per-axis inversion, raw HID axis mapping
[/list]
Settings persist between sessions.

[size=4]Requirements[/size]
[list]
[*][url=https://github.com/BepInEx/BepInEx]BepInEx[/url] (Mono, x64)
[*]CTDynamicModMenu (required dependency - install this first)
[*]A gamepad, joystick, or RC transmitter (USB/HID) to fly it - [b]there is no keyboard flight mode[/b]
[/list]

[size=4]Installation[/size]
Extract into your [b]BepInEx/plugins[/b] folder, alongside CTDynamicModMenu.

[size=4]Quick start[/size]
[list=1]
[*]Buy a drone from the stand next to any island shop (or press [b]Y[/b]/[b]C[/b] while holding an explosive to convert it)
[*]Throw it to launch, or use [b]/fpvdrone[/b] to launch immediately
[*]Fly with your gamepad/transmitter sticks - open the mod menu ([b]F4[/b]) for every other setting
[/list]

[i]Tip: the on-screen keybind text and the keybind labels on menu buttons can each be toggled off in the mod menu's Settings tab, if you'd rather have a cleaner HUD.[/i]

Full command list, control mapping, and technical details are on the [url=https://github.com/Toemmsen96/HTFDrone]GitHub page[/url].
