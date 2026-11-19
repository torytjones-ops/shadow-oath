# Shadow Oath

A playable Unity prototype of an original 2.5D action-stealth game. You swear a bloodline in a moonlit hamlet, cross one guarded ward, and duel a single human-sized rival on a bridge.

The full design intent is in [DESIGN.md](DESIGN.md).

## Unity version

Install **Unity 6.3 LTS**, editor **6000.3.28f1**.

Unity Hub → Installs → Install Editor → Archive, or search for `6000.3.28f1`. Add the module **Windows Build Support (Mono)**. Add IL2CPP later if you want a shipping player. Visual Studio or Rider is optional; the project is plain C#.

This repository was authored against that LTS. A newer 6000.3 patch of the same LTS line should open it. If Hub offers to upgrade the project, stay on 6000.3 unless you mean to move the whole project.

## Open the project

1. Install the editor above.
2. Unity Hub → Projects → Add → choose this folder (the one that contains `Assets`, `Packages`, and `ProjectSettings`).
3. Open it with 6000.3.28f1. The first import downloads packages and can take several minutes.
4. On the first open, `Tools → Shadow Oath → Configure Project` runs by itself if the Universal Render Pipeline asset or the main scene is missing. You can run that menu again any time.
5. Open `Assets/ShadowOath/Scenes/Main.unity` if it is not already the open scene.
6. Press Play.

If the first Play is unlit magenta, run the configure menu and press Play again. The game builds its levels in code when Play starts. You do not place characters by hand.

## How to play

The whole loop is: title → bloodline → Okumura → the moonlit gate → duel with Kenzo → ending.

Talk to Elder Senae and Toma before the torii will let you leave. The training post in the hamlet can be cut as much as you like.

| Action | DualSense | Keyboard / mouse |
| --- | --- | --- |
| Move | Left stick or D-pad | A D or arrows |
| Jump | Cross | Space |
| Dodge | Circle | Left Shift |
| Light combo | Square | J or left mouse |
| Heavy | Triangle | K or right mouse |
| Talk / depart | Triangle, near someone | E |
| Silent takedown | Square from behind, or R3 | V, or J from behind |
| Throw a stone | L2 | G or middle mouse |
| Water Oath | R1 | R |
| Mirror Shade | L1 | Q |
| Moon Focus (hold) | R2 | F |
| Pause | Options | Esc |

Menus use the stick or arrows, Cross or Enter to confirm, Circle or Esc to go back. The in-game Controls screen lists the same map.

### DualSense on Windows

Plug the controller in with a USB cable before you press Play. Unity's Input System reads it as a gamepad, including the triggers, sticks, and the Options button. When the pad exposes a light bar, it takes the color of the bloodline you swore.

Quit Steam, or any other app that has grabbed the controller, before testing in the editor. Bluetooth often works; USB is the reliable check. Keyboard and mouse keep working if the pad is asleep.

## Windows build

1. Install **Windows Build Support (Mono)** for this editor in Unity Hub.
2. File → Build Profiles (in this editor the older Build Settings window still exists under File as well).
3. Choose **Windows** and switch platform if Hub asks. Architecture **x86_64**.
4. Make sure `Assets/ShadowOath/Scenes/Main.unity` is the only enabled scene.
5. Build.

The project records Windows 64-bit as the selected standalone target. A build made on Windows is the one to ship. Mono is enough for this prototype. Switch the scripting backend to IL2CPP in Player Settings when you want a release player.

Player settings already use the product name **Shadow Oath**, the company name **Oathbound**, a 1920×1080 default window, and linear color space.

## Steam, later

Nothing here requires the Steam SDK, an App ID, or the Steam client. Calls go through `IPlatformService` in `Assets/ShadowOath/Scripts/Core.cs`. The default is `NullPlatformService`, which logs presence and achievement ids locally.

When you are ready:

1. Import [Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET) into the project (or adopt Unity's `com.unity.steamworks-api` package and point the same methods at it).
2. Add the scripting define **STEAMWORKS_NET** under Project Settings → Player → Scripting Define Symbols for Standalone.
3. That define compiles `SteamworksPlatformService`, which calls `SteamAPI.Init`, rich presence, and `SteamUserStats.SetAchievement`.
4. Put your App ID in `steam_appid.txt` next to the editor when playing in the editor, and next to the built executable. Do not commit that file if the ID should stay private. The gitignore already excludes it and the native Steam libraries.
5. Achievement API names already used: `oath_sworn`, `gate_crossed`, `oath_kept`. Create the same API names in the Steamworks partner site.

Until step 2, the game compiles and runs with no Steam types referenced.

## Project layout

- `Assets/ShadowOath/Scripts` — input, clans, player, guards, the duel, levels, UI, and the platform stub
- `Assets/ShadowOath/Shaders` — ink outline, unlit color, ink particles
- `Assets/ShadowOath/Editor/ProjectAutoConfig.cs` — URP asset, Windows target, main scene, new Input System
- `Assets/ShadowOath/Scenes/Main.unity` — created on first configure; Play starts from here
- `DESIGN.md` — fantasy, verbs, and what the prototype refuses to become

Levels, characters, and UI are built at runtime from primitives. There is no imported art and no third-party audio.
