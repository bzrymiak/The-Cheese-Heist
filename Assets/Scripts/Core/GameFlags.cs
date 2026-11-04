using System.Collections.Generic;
using UnityEngine;
public static class GameFlags
{
    private static readonly HashSet<string> flags = new HashSet<string>();

    public static void Set(string id) => flags.Add(id);
    public static bool Has(string id) => flags.Contains(id);
    public static void Clear() => flags.Clear();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => flags.Clear();
}