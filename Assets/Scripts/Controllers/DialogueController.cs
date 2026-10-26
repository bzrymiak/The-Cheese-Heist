using UnityEngine;

public class DialogueController : MonoBehaviour
{
    void Awake()
    {
        // Set the window size, windowed mode
        int width = 1920;
        int height = 1440;
        bool isFullscreen = false; 
        
        Screen.SetResolution(width, height, isFullscreen);
    }
}