# Repository Guidelines

## Project Structure & Module Organization

The playable Tuanjie project is in `DoNotForgetMe/New Tuanjie Project/`.
Gameplay code lives in `Assets/_Project/Scripts/`, organized by concern:
`Core/` contains game-wide managers and camera code, `Player/` contains
controllers, input, components, and state-machine states, `MiniGame/` contains
the mini-game framework and implementations, and `Data/` contains
ScriptableObject settings. Put scenes, prefabs, sprites, and other runtime
assets under `Assets/_Project/`. Keep engine configuration in
`ProjectSettings/` and package declarations in `Packages/`. Architecture
decisions belong in `docs/adr/`; shared game terminology is defined in
`CONTEXT.md`.

## Build, Test, and Development Commands

Use Tuanjie Engine 1.6.12 (Unity 2022.3.61 base) to open
`DoNotForgetMe/New Tuanjie Project/`. Run the current scene with the Editor
Play button. Create distributable builds through **File > Build Settings**;
keep build settings changes intentional and reviewable. No command-line build
script or automated test project is currently checked in.

## Coding Style & Naming Conventions

Follow the existing C# style: four-space indentation, braces on their own
line, PascalCase for classes, methods, properties, and events, and camelCase
for private fields and parameters. Use `[SerializeField] private` for Inspector
fields, `I` prefixes for interfaces (for example, `IPlayerState`), and one
public type per appropriately named `.cs` file. Group scripts by gameplay
responsibility rather than by scene. Preserve Unity `.meta` files when adding,
moving, or deleting assets.

## Testing Guidelines

Validate gameplay changes in the Editor before review. For networking work,
exercise both Host and Client paths, since the accepted design uses Photon
Fusion host authority. Add focused NUnit tests under `Assets/**/Tests/` when
logic can run independently of a scene; name test files `*Tests.cs` and test
methods for the observable behavior, such as `Respawn_ResetsHealth()`.

## Commit & Pull Request Guidelines

This checkout has no Git history, so no existing commit convention can be
inferred. Use short, imperative Conventional Commit-style subjects, such as
`feat: add host player spawn`. Keep each commit scoped to one change. Pull
requests should summarize behavior, link the relevant issue or ADR, note
manual test coverage, and include screenshots or a short video for visual or
interaction changes. Call out any changes to scenes, prefabs, packages, or
network authority explicitly.

## Architecture & Configuration

Use the terms in `CONTEXT.md` consistently: a **session** has one Host and one
Client; only the Host makes authoritative gameplay decisions. Do not commit
Photon App IDs, access tokens, or other secrets. Update an ADR when changing
the networking model or another durable architectural decision.
