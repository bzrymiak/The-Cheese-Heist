using System.Collections;
using UnityEngine;

public class CutsceneLoader : MonoBehaviour
{
    public float delaySeconds = 3f;

    private IEnumerator Start()
    {
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(delaySeconds);
        SceneLoader.Load(SceneLoader.Closet);
    }
}
