# Scripts Layout

This folder is organized by feature so runtime code is easier to find and the experimental work is kept separate from the main gameplay scripts.

## Main Folders

- `Dungeon/`: dungeon data models, generation algorithms, and runtime dungeon systems such as spawning and portals.
- `Gameplay/`: gameplay-specific systems that are not tied to one dungeon subsystem. Right now this contains enemies.
- `Player/`: player movement and player-controlled behavior.
- `UI/`: user interface scripts such as the minimap.
- `World/`: world helpers and environment-facing utilities such as autotiling.
- `Debug/`: visualization and debug-only helpers.
- `Prototypes/`: numbered test iterations and shared prototype helpers.

## Prototype Structure

- `Prototypes/DungeonGeneration/`: the `Test1` to `Test12` scripts used to iterate on dungeon generation features.
- `Prototypes/Shared/`: shared base classes used by prototype scenes, such as `TestBase`.

## Navigation Rule

If a script is part of the playable runtime, prefer placing it under the feature it belongs to.
If a script only exists to validate an idea or scene experiment, keep it under `Prototypes/`.
