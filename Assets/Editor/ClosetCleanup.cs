#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Cheese Heist > Clean Up Closet
// removes the old background + invisible colliders the room builder left in the closet
// (brooke rebuilt it with tilemaps so those are in the wrong spots now)
public static class ClosetCleanup
{
    [MenuItem("Tools/Cheese Heist/Clean Up Closet")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Closet.unity");

        int removed = 0;
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "Background" || go.name == "Walls" || go.name == "Furniture")
            {
                Object.DestroyImmediate(go);
                removed++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("closet cleaned up, removed " + removed + " old objects");
    }
}
#endif
