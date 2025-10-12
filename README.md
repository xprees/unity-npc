# Unity NPC AI Package

# [![NPM Version](https://img.shields.io/npm/v/cz.xprees.npc)](https://www.npmjs.com/package/cz.xprees.npc)

This package provides a NPC-AI solution for Unity build on top of
the [Unity AI - Navmesh package](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0//manual/CreateNavMesh.html)

## Features

- **NPC Controller** - A base class for controlling NPCs, which can be extended to create custom NPC behaviors.
- **NPC Animation Controller** - A base class for controlling NPC animations, which can be extended to create custom NPC animations.
- **Patrolling** - Using custom [Route](Runtime/Navigation/Route.cs) and [Waypoints](Runtime/Navigation/Waypoint.cs) to define the path for the NPC to
  follow and how long to rest on waypoint.
- **State Machine** - Using [StateMachine](Runtime/StateMachine/INpcStateMachine.cs) to define the behavior of the NPC.

## Installation

Add to your Unity project following **OpenUPM** and **xprees-NPM** scoped registries. So you can install the package with the all dependencies
automatically with [Unity Package Manager](https://docs.unity3d.com/6000.1/Documentation/Manual/upm-scoped.html).

Either do it manually or by using the Unity Package Manager UI.
`Packages/manifest.json`

```json
{
    "scopedRegistries": [
        {
            "name": "OpenUPM",
            "url": "https://package.openupm.com",
            "scopes": [
                "com.cysharp.unitask",
                "com.github.siccity.xnode",
                "com.dbrizov.naughtyattributes"
            ]
        },
        {
            "name": "xprees-NPM",
            "url": "https://registry.npmjs.org",
            "scopes": [
                "cz.xprees"
            ]
        }
    ]
}
```

Then simply install the package using the Unity Package Manager using the _NPM - xprees_ scope or by the package name `cz.xprees.npc`.
