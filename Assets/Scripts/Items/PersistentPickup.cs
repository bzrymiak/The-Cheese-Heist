using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentPickup : MonoBehaviour
{
    [Tooltip("Optional. Leave empty to auto-generate from scene name, object name and position.")]
    [SerializeField] private string customId;

    private string key;

    void Awake()
    {
        if (GetComponentInParent<Slot>() != null) return;

        if (GameFlags.Has(GetKey()))
            Destroy(gameObject);
    }

    public void MarkCollected()
    {
        GameFlags.Set(GetKey());
    }

    string GetKey()
    {
        if (key == null)
        {
            Vector3 p = transform.position;
            string id = string.IsNullOrEmpty(customId)
                ? $"{SceneManager.GetActiveScene().name}/{name}/{p.x:F1},{p.y:F1}"
                : customId;
            key = "Collected:" + id;
        }
        return key;
    }
}