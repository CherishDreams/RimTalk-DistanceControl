# RimTalk Distance Control

**English** | [中文](README.md)

A RimWorld mod that allows customization of [RimTalk](https://steamcommunity.com/sharedfiles/filedetails/?id=3365145210)'s distance and range limits for pawn conversations.

## Features

### Distance & Range Control

- **Conversation Distance** — Adjust maximum distance between pawns for dialogue (default: 20, 0 = unlimited)
- **Same Room Requirement** — Toggle whether pawns must be in the same room to talk (default: on)
- **Hearing Range** — Adjust the detection range for hearing-based pawn selection (default: 10)
- **Viewing Range** — Adjust the detection range for sight-based pawn selection (default: 20)
- **Context Distance** — Adjust environment context collection radius for buildings, items, flora (default: 5)
- **Announcement Hearing Range** — Adjust detection range for announcement-type conversations (default: 30)

### Social Effect Control

- **Block Slighted Debuff** — Optionally block RimTalk's `Slighted` (insult/slight) negative thought from being applied (default: off)

## Requirements

- RimWorld 1.5+
- [RimTalk](https://steamcommunity.com/sharedfiles/filedetails/?id=3365145210) (`cj.rimtalk`)
- Harmony (bundled with RimWorld)

## Installation

1. Subscribe to this mod on Steam Workshop, or copy the folder to `RimWorld/Mods/`
2. Ensure **RimTalk** is loaded before this mod
3. Configure settings in Options → Mod Settings → RimTalk Distance Control

## Build

```bash
cd Source
dotnet build
```

Output: `1.6/Assemblies/RimTalkDistanceControl.dll`

## License

MIT
