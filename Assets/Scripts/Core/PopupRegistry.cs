using System.Collections.Generic;
using UnityEngine;

// Put this on an always-ACTIVE object in your persistent UI (e.g. the root Canvas),
// then list each popup with an ID. Scene objects look popups up by ID at runtime.
public class PopupRegistry : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string id;
        public GameObject popup;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public static PopupRegistry Instance { get; private set; }
    public static event System.Action CloseRequested;

    void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
        
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static GameObject Get(string id)
    {
        if (Instance == null) return null;

        foreach (Entry e in Instance.entries)
            if (e.id == id) return e.popup;

        return null;
    }

    public void RequestClose()
    {
        Debug.Log($"[PopupRegistry] RequestClose. subscribers={(CloseRequested != null ? CloseRequested.GetInvocationList().Length : 0)}");
        CloseRequested?.Invoke();
    }
}