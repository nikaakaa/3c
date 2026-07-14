using System;
using System.Collections.Generic;
using System.Reflection;
using Fantasy.Async;
using GameLogic;
using GameLogic.Network.Fantasy;
using TEngine;

public static class GameApp
{
    private static IReadOnlyList<Assembly> _hotUpdateAssemblies = Array.Empty<Assembly>();

    public static void Entrance(object[] objects)
    {
        _hotUpdateAssemblies = ExtractAssemblies(objects);
        Log.Warning("GameLogic hot update entrance entered.");
        Utility.Unity.AddDestroyListener(Release);
        StartGameLogic();
    }

    private static void StartGameLogic()
    {
        FantasyClientBootstrap.InitializeAsync().Coroutine();
        Log.Info($"GameLogic runtime initialized, assembly count: {_hotUpdateAssemblies.Count}");
    }

    private static IReadOnlyList<Assembly> ExtractAssemblies(object[] objects)
    {
        if (objects is { Length: > 0 } && objects[0] is IReadOnlyList<Assembly> assemblies)
        {
            return assemblies;
        }

        if (objects is { Length: > 0 } && objects[0] is List<Assembly> assemblyList)
        {
            return assemblyList;
        }

        return Array.Empty<Assembly>();
    }

    private static void Release()
    {
        FantasyClientBootstrap.Shutdown();
        Log.Warning("GameLogic runtime released.");
    }
}
