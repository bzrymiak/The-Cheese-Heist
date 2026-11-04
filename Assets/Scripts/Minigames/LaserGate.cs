using UnityEngine;

// sits on the caution line in the laser vault. step past it and the laser mini game starts.
// once you've beaten it, it switches itself off so you can walk to the safe
[RequireComponent(typeof(BoxCollider2D))]
public class LaserGate : MonoBehaviour
{
    public string minigameScene = "LaserDodge";

    void Start()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (LaserDodge.Passed) gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        RoomTransition.Go(minigameScene, "");
    }
}
