using UnityEngine;

public class PauseController : MonoBehaviour
{
    public static bool IsGamePaused {get; private set;} = false;

    public static void SetPause(bool pause)
    {
        IsGamePaused = pause;
    }

    // make sure game is always unpaused on start
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        IsGamePaused = false;
    }
}
