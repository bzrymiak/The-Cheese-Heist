using UnityEngine;

public class LaserFlagReactor : MonoBehaviour
{
    [SerializeField] private string flagId = "LasersDodged";

    [Header("Laser")]
    [SerializeField] private SpriteRenderer laserRenderer;
    [SerializeField] private Sprite disabledSprite;

    [Header("Also switch off")]
    [SerializeField] private GameObject[] objectsToDisable;

    void Awake()
    {
        if (laserRenderer == null)
            laserRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        GameFlags.FlagChanged += OnFlagChanged;

        if (GameFlags.Has(flagId)) Apply();
    }

    void OnDisable()
    {
        GameFlags.FlagChanged -= OnFlagChanged;
    }

    void OnFlagChanged(string id)
    {
        if (id == flagId && GameFlags.Has(flagId)) Apply();
    }

    void Apply()
    {
        if (laserRenderer != null && disabledSprite != null)
            laserRenderer.sprite = disabledSprite;

        foreach (GameObject go in objectsToDisable)
            if (go != null) go.SetActive(false);
    }
}