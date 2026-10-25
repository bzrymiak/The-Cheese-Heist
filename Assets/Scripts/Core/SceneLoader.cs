using UnityEngine.SceneManagement;

// keeping scene names in one spot so we don't typo them everywhere
public static class SceneLoader
{
    public const string Menu = "Menu";
    public const string Intro = "Intro";
    public const string Kitchen = "Kitchen";

    public static void Load(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
