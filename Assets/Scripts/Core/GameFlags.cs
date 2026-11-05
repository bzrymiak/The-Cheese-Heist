using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameFlags
{
    private static readonly HashSet<string> flags = new HashSet<string>();

    // fires when a flag is newly set or removed, so objects in the current scene can react live
    public static event Action<string> FlagChanged;

    public static void Set(string id)
    {
        if (flags.Add(id)) FlagChanged?.Invoke(id);
    }

    public static void Remove(string id)
    {
        if (flags.Remove(id)) FlagChanged?.Invoke(id);
    }

    public static bool Has(string id) => flags.Contains(id);
    public static void Clear() => flags.Clear();

    // runs at the start of every play session, even with domain reload disabled
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => flags.Clear();
}