# HTFDrone

An FPV kamikaze drone mod for *How to Fish*. Turn a stick of dynamite into a hand-flown,
acro-mode quadcopter with a real Betaflight-style OSD, then fly it into something and watch it go
off. Buy one from a stand on every island, or convert a TNT you're already holding on the spot.

No custom drone asset is used — the "drone" *is* the game's own explosive item, wearing a
quadcopter frame built at runtime out of primitives and flown with real rigidbody physics. That's
what makes it work in multiplayer with no custom networking: the game's existing item-sync
already replicates a thrown TNT to everyone else, modded or not.

![FPV view with the Betaflight-style OSD](.ghimages/fpv-view-osd.png)

## Requirements

- [BepInEx](https://github.com/BepInEx/BepInEx) (Mono, x64) installed for *How to Fish*
- **CTDynamicModMenu** — this mod's commands and toggles are registered into it; build it
  alongside this project (see `../CTDynamicModMenu`)
- A joystick, gamepad, or RC transmitter in USB/HID mode to actually fly the thing (see
  [Flying it](#flying-it) below — there is no keyboard flight mode)

## Installing

Drop the built `HTFDrone.dll` into `BepInEx/plugins/` alongside CTDynamicModMenu. Building the
project (`dotnet build`) copies it there automatically if the game is installed at the path
recorded in `path.txt` — see [Building](#building) below.

## Getting a drone

- **Buy one.** Every island gets a drone stand next to its existing shop, priced the same as
  whatever explosive it's built from (dynamite, by default). Interact with it like any other
  purchase.
- **Convert one you're holding.** Pick up an explosive and press the game's skin-swap key (**Y**/
  **C** by default) to assemble it into a drone in your hands, or back into plain TNT. This
  repurposes the existing skin-cycle binding rather than adding a new key — explosives have no
  skins to cycle anyway, so nothing is lost.
- **Launch it straight away** with `/fpvdrone` (optionally `/fpvdrone <item name>` for a payload
  other than dynamite) — spawns one where you're looking and hands off to the FPV camera
  immediately.

Throwing (or dropping) a drone you're holding launches it. Only one drone can be in flight at a
time.

![A drone assembled in hand, payload slung underneath](.ghimages/held-drone.png)

## Flying it

Flight is real acro/rate-mode physics, not scripted movement — the drone has actual mass, gravity,
and drag, and pitch/roll/yaw are pure angular rates with no auto-leveling and no tilt limit. It
can flip, loop, and fly inverted exactly like a real acro quad.

Flying needs a transmitter, gamepad, or joystick — there's no keyboard stick input. The mod reads
through Unity's Input System (the game's shipped Input Manager has no joystick axes registered),
trying standard Gamepad mode first and falling back to raw HID Joystick axes. It's built around a
FrSky Taranis QX7 in Mode 2 over USB, but anything that shows up as a gamepad or joystick should
work.

| Control | Default mapping |
|---|---|
| Throttle / Yaw | Left stick (vertical / horizontal) |
| Pitch / Roll | Right stick (vertical / horizontal) |
| Detonate in flight | Right or left trigger, south button, start button, transmitter trigger, or **Enter** on keyboard |

Run `/dronediag` to list every detected axis with its live value — move one stick at a time to
see which index responds, then use `/droneaxis <roll\|pitch\|throttle\|yaw> <index>` to remap a
HID joystick's raw axes, or `/dronestick` to swap which stick is throttle/yaw vs. pitch/roll for
gamepad-mode transmitters.

While piloting, your view switches to a nose-mounted FPV camera and your own inputs are frozen
(same mechanism the game uses for pausing or dying) until the drone is destroyed or the flight
timer runs out, at which point control returns to your own body.

## The OSD

The FPV feed carries a Betaflight/INAV-style on-screen display: an artificial horizon derived
from the camera's actual view direction (not Euler angles, so it stays correct through flips and
inverted flight), a fixed centre crosshair, altitude, speed, a battery/mAh readout, link stats,
and a home arrow pointing back at the launch point.

## Commands

All commands also appear as buttons/toggles in the CTDynamicModMenu UI (default key **F4**), under
its own "Drone" category:

![The Drone category in the mod menu](.ghimages/mod-menu.png)

| Command | Description |
|---|---|
| `/fpvdrone [item]` | Launch a drone immediately, optionally with a payload other than dynamite |
| `/dronediag` | List detected transmitter/joystick axes and their live values |
| `/droneaxis <stick> <index>` | Map a raw HID joystick axis index to roll/pitch/throttle/yaw |
| `/dronestick` | Swap which physical stick is throttle+yaw vs. pitch+roll |
| `/dronecam <forward> [height]` | Set the FPV camera's mount position on the frame |
| `/dronetilt <degrees>` | Set the FPV camera's uptilt (0–90°, default 30°) |
| `/dronemodel` | Toggle whether you see your own drone's airframe in FPV |
| `/droneinfinite` | Toggle infinite flight time (ignores the flight timer) |
| `/droneimpact` | Toggle whether hitting something detonates the drone |
| `/droneothers` | Toggle whether other modded players' drones render as drones for you |
| `/dronestanddiag` | Dump a drone stand's object hierarchy and live scales (debugging) |

## How it looks to other players

A flying drone is still, under the hood, the game's stock explosive item — so a player without
the mod sees ordinary flying TNT and takes normal explosion damage. Nothing about shared game
state depends on who has the mod installed.

A player *with* the mod sees an actual quadcopter: a frame with spinning props, built the moment
another client is detected simulating the item's physics and flying it (as opposed to just having
thrown it). This is purely cosmetic and entirely local — turn it off with `/droneothers` if you'd
rather see plain TNT.

## Building

```sh
dotnet build
```

Expects the game's managed assemblies and CTDynamicModMenu.dll under `Dependencies/` — see
`deps.txt` for the full manifest and where each file comes from. The build copies the finished
DLL into the game's `BepInEx/plugins/` folder automatically; override the target with
`-p:PluginDir=...` or by editing `path.txt`.
