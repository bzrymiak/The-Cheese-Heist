using UnityEngine;

// art the story code needs at runtime (portraits for walkie-talkie calls, reggie, etc).
// built by Tools > Cheese Heist > Build Phase 1 into Resources/StoryContent.asset
public class StoryContent : ScriptableObject
{
    public Sprite playerPortrait;
    public Sprite tonyPortrait;
    public Sprite gusPortrait;
    public Sprite djangoPortrait;
    public Sprite reggiePortrait;
    public Sprite reggieSprite;
    public Sprite robberSprite;
    public Sprite tonySprite;
    public GameObject recipeBook; // inventory item for the book on the safe room desk
    public Sprite deskNoBook;      // safe room desk with the book taken off it
    public Sprite tonyRadio;         // walkie talkie portrait for tony's calls
    public Sprite inspectTooltip;    // brooke's "I to inspect" bubble
    public Sprite clueKeyWall;      // key in its nook behind reggie's portrait
    public Sprite clueSticky;       // fridge note close-up
    public Sprite clueNewspaper;    // "tony's overthrown by reggie"
    public Font pixelFont;          // same font the minigame cards use
    public GameObject pickupTooltip; // the same "E to pickup" bubble the other items use

    [Header("Audio (Assets/Audio)")]
    public AudioClip menuMusic;
    public AudioClip laserMusic;
    public AudioClip alarm;
    public AudioClip siren;
    public AudioClip punch;
    public AudioClip pickup;
    public AudioClip click;
    public AudioClip fightMusic;  // gus + reggie fights
    public AudioClip mazeMusic;   // vent maze
    public AudioClip ouch;        // first ouch in the clip is the one we want

    static StoryContent cached;
    public static StoryContent Get()
    {
        if (cached == null) cached = Resources.Load<StoryContent>("StoryContent");
        if (cached == null) cached = CreateInstance<StoryContent>(); // nothing built yet, still runs without portraits
        return cached;
    }
}
