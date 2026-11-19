# Shadow Oath

Working title for an original 2.5D action-stealth game. The play space is a side-scrolling plane. The scene is three-dimensional: a perspective camera, parallax hills and pines, lantern light, and rooftops set back from the street.

This document is the prototype's intent. It is not a promise of a finished commercial design.

## Fantasy

Haru of Yamigura comes back to a massacre. The hidden village is ash. A handful of survivors keep a hamlet called Okumura, and three bloodlines still have a spark. Haru swears one of them, crosses a moonlit ward the raiders now patrol, and meets Kenzo of Clan Akagiri on a cedar bridge.

Kenzo is one human-sized swordsman. He is not a monster and not a giant. The oath is kept by beating him, not by toppling a castle.

## What the prototype is trying to feel like

These are feelings only. Shadow Oath does not use their names, characters, moves, or assets.

- A moonlit feudal-Japan night, duels that read as two people with swords.
- Fast side-view weapon strings, wall-runs, and dodges.
- Stealth that is about light, cones of vision, noise, and a takedown from behind.
- One elemental sword meditation (Water Oath) and one ninja art (Mirror Shade) paid for with a personal energy, Seiki.
- A focus mode that slows the world so a committed player can thread a defense.
- A short revenge arc with a stylish finisher, then quiet.

## The loop

1. Title.
2. Choose one of three bloodlines. The gift is passive and on for the whole run.
3. Okumura. Talk to Elder Senae and the scout Toma. Hit the training post if you want to feel the blade. Leave through the torii.
4. The moonlit gate. Sneak, distract, or fight. Wall-run the ravine. Cross the roof. Deal with the courtyard.
5. One-on-one duel against Kenzo.
6. Ending. Return to the hamlet or the title. The mission can be walked again.

## Bloodlines

| Clan | Epithet | Gift | What it changes |
| --- | --- | --- | --- |
| Kurayami | Black Night | Umbral Step | Faster in shadow, much harder to see there, silent takedowns restore Moon Focus |
| Shiranami | White Wave | Tideborn | Water Oath costs less Seiki, hits harder, and Seiki returns faster |
| Shiramine | White Peak | Still Blade | Moon Focus drains slowly. Dodge travels farther and the invulnerable window is longer |

## Player verbs

- Run, jump, and a short coyote / jump-buffer so late jumps still work.
- Wall-run on a tall marked face, then jump to kick off it.
- Dodge with invulnerability. Shiramine's lasts longer.
- Light combo of three hits, and a slower heavy cut.
- Silent takedown on a guard who has not fully spotted you, from behind and in reach.
- Throw a stone. Guards who are not already in a fight go look. A bell does the same, louder.
- Water Oath, **Still Current**: a piercing wave of Seiki.
- Mirror Shade: one echo that runs at enemies, draws eyes, and cuts until it breaks or fades.
- Moon Focus: hold to slow enemy time. Haru keeps full speed. The meter drains, then refills.

Stealth is a volume on the ground, not a lighting trick the player has to guess. In a shadow pool the vision cone shortens. Kurayami shortens it further. Attacking, a heavy landing in the light, and a ringing bell are noise. A takedown is quiet.

## Guards and the duel

Guards patrol, become suspicious, investigate a sound, or commit and chase. Their cone is the tell. They are human-sized and can be fought if stealth fails; a takedown is simply cleaner. Killing one loudly wakes neighbors.

Kenzo does not use stealth. He keeps a dueling distance, telegraphs a cut by drawing the sword back and flashing, sometimes commits to a heavy, and sometimes steps out of Haru's swing. Below half his nerve he presses harder. The killing blow plays a short slow finisher. He never grows, never summons a monster, and never leaves the bridge as a set piece larger than a person.

## Presentation

Night indigo, ink black, paper white, vermilion gates, gold lanterns, one cool moon. Characters are built from primitives with ink outlines so the prototype reads as a picture instead of a graybox. Hits throw black and white splashes. Audio is synthesized placeholder: a low pentatonic bed, a pulse when a fight starts, and short stings for steel, water, and the bell.

## Camera

Perspective, pulled back on Z, pitched slightly down so roofs and the street have depth. Gameplay position is locked to Z = 0. Hills and pines parallax behind the action. The moon rides with the camera.

## What this prototype is not

- Not an open world, not a skill tree, not a village simulation.
- Not a roster of breathing styles. One style is enough to feel the idea.
- Not giant bosses, not a demon form, not a war battle.
- Not tied to Steam yet. Platform calls go through `IPlatformService`. The shipping build uses a local stub. A Steamworks.NET implementation is already in the project behind the `STEAMWORKS_NET` scripting define and is not compiled until that package is imported.

## Steam, later

Achievements the stub already names:

- `oath_sworn` — a bloodline was chosen
- `gate_crossed` — the cedar bridge was reached
- `oath_kept` — Kenzo fell

Presence strings follow the title, the hamlet, the mission, the duel, and the ending. None of this contacts Steam until a real SDK and an App ID are added.
