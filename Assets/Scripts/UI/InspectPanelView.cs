using UnityEngine;
using UnityEngine.UI;
public class InspectPanelView : MonoBehaviour
{
    [Tooltip("The Image component that shows the detail view.")]
    [SerializeField] private Image detailImage;

    public void SetImage(Sprite sprite)
    {
        if (detailImage == null) return;

        detailImage.sprite = sprite;
        detailImage.preserveAspect = true;   
        detailImage.enabled = sprite != null;
    }
}