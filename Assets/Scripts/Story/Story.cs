using UnityEngine;
using UnityEngine.SceneManagement;

// everything the ending depends on lives here: tony's trust + the story flag names.
// flags themselves go through GameFlags so brooke's doors/lasers can read them too.
public static class Story
{
    // flag names
    public const string FoughtGus = "FoughtGus";
    public const string TalkedGus = "TalkedGus";
    public const string GusLowScore = "GusLowScore";     // beat him but under 75%, he yelled for help
    public const string GusTalkDone = "GusAfterFightTalked";
    public const string AlarmOn = "AlarmOn";
    public const string GotRecipe = "GotRecipe";
    public const string TonyCalled = "TonyAlarmCall";
    public const string Abandoned = "TonyAbandoned";
    public const string ReggieMet = "ReggieMet";
    public const string ReggieDown = "ReggieKnockedOut";

    public const float GusScoreNeeded = 0.75f;
    public const int AbandonAt = -2;                     // trust this low and tony drives off

    public static int Trust { get; private set; }
    public static float LastFightScore;

    public static bool TonyGone => Trust <= AbandonAt || GameFlags.Has(Abandoned);

    public static void AddTrust(int amount)
    {
        Trust += amount;
        Debug.Log("[story] tony trust " + (amount >= 0 ? "+" : "") + amount + " -> " + Trust);
    }

    // used by dialogue choice actions. "trust:-1", "AlarmOn", or a few at once "trust:-1,SomethingElse"
    public static void Apply(string parameter)
    {
        if (string.IsNullOrEmpty(parameter)) return;
        foreach (var raw in parameter.Split(','))
        {
            var p = raw.Trim();
            if (p.Length == 0) continue;
            if (p.StartsWith("trust:") && int.TryParse(p.Substring(6), out int n)) AddTrust(n);
            else GameFlags.Set(p);
        }
    }

    public static void ResetAll()
    {
        Trust = 0;
        LastFightScore = 0;
        GameFlags.Clear();
        LaserDodge.Passed = false;
        NPC.ForgetConversations();
        if (InventoryController.Instance != null) InventoryController.Instance.ClearItems();
    }

    // play mode start + going back to the menu both count as a fresh run
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void OnPlay()
    {
        Trust = 0;
        LastFightScore = 0;
        LaserDodge.Passed = false;
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
    }

    static void OnLoaded(Scene s, LoadSceneMode m)
    {
        if (s.name == "Menu") ResetAll();
    }
}
