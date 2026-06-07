# HotSpringsExpanded Mod

A mod for the game [Vintage Story](https://VintageStory.at) that expands upon the hot springs gameplay mechanics. This is a code-based mod that modifies game behavior through C# code and asset modifications.

**Mod ID**: `hotspringsexpanded`  
**Target Game Version**: 1.22.0-rc.8  
**Framework**: .NET 8.0

## Mod Types

Vintage Story supports three types of mods:

1. **Theme Packs**: Visual-only changes (textures, sounds). Cannot add new content or change shapes.
2. **Content Mods**: Add blocks, items, recipes via JSON files. Limited to static content without complex behaviors.
3. **Code Mods**: Full control via C# code. Can implement complex mechanics, behaviors, and systems.

This mod is a **Code Mod** that extends game functionality through C# programming.

## Architecture

Vintage Story uses a **server-client architecture**:
- Single-player: One server + one client running on the same machine
- Multiplayer: Server runs separately, clients connect remotely
- Code mods load on both server and client sides independently

## Mod Lifecycle

Mods are loaded via the `ModSystem` base class with these key methods:
- `StartPre(ICoreAPI)`: Early initialization, before world load
- `Start(ICoreAPI)`: General initialization (runs on both server and client)
- `StartServerSide(ICoreServerAPI)`: Server-only initialization
- `StartClientSide(ICoreClientAPI)`: Client-only initialization

Use `api.Side` to detect current side (Client/Server/Shared).

## Mod Interaction

Mods can interact with each other through the `IModLoader`:
- Access other mods' public properties and methods
- Create modpacks with logical separation
- Build extendable mods with plugin-style architecture

**Note**: Dependencies on other mods make your mod dependent on their presence.
