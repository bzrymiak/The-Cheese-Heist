using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;

    public static MenuController Instance;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(transform.root.gameObject); 
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject); 
    }

    void Start()
    {
        menuCanvas.SetActive(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.tabKey.wasPressedThisFrame)
        {
            if (!menuCanvas.activeSelf && PauseController.IsGamePaused) return;
            
            menuCanvas.SetActive(!menuCanvas.activeSelf);
            PauseController.SetPause(menuCanvas.activeSelf);
        }
    }

    public void CloseMenu()
    {
        // SaveController.SaveGame();
        menuCanvas.SetActive(false);
        PauseController.SetPause(false);
    }
}
