using UnityEngine;
using UnityEngine.SceneManagement;

// all the sound in one place. music per scene, the alarm + distant sirens once django hits the alarm,
// and quick one-shots (punch, pickup, click) anyone can call with GameAudio.Punch() etc.
// lives on the StoryDirector object so it carries across rooms.
public class GameAudio : MonoBehaviour
{
    static GameAudio instance;

    AudioSource music, alarm, siren, sfx;
    StoryContent clips;

    static readonly string[] quietScenes = { "Menu", "Ending", "GameOver", "LaserDodge", "VentMaze", "ReggieFight" }; // no alarm/sirens here

    void Awake()
    {
        instance = this;
        clips = StoryContent.Get();
        music = Source(true, 0.45f);
        alarm = Source(true, 0.25f);
        siren = Source(true, 0.35f);
        sfx = Source(false, 0.8f);
        ouchSrc = Source(false, 0.9f);

        // none of the scenes have an AudioListener on their camera, so the game was deaf.
        // this object never unloads, so it listens for every room (and steps aside if a scene has its own)
        ears = gameObject.AddComponent<AudioListener>();
        SceneManager.sceneLoaded += (sc, m) => { if (this != null) checkEars = true; };
        checkEars = true;
    }

    AudioListener ears;
    AudioSource ouchSrc;
    bool checkEars;

    AudioSource Source(bool loop, float vol)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.loop = loop;
        s.volume = vol;
        s.playOnAwake = false;
        return s;
    }

    void Update()
    {
        string room = SceneManager.GetActiveScene().name;

        if (checkEars)
        {
            checkEars = false;
            int others = 0;
            foreach (var l in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
                if (l != ears && l.enabled) others++;
            ears.enabled = others == 0;
        }

        // music: title theme on the menu, its own track for the laser game
        AudioClip want = null;
        if (room == "Menu") want = clips.menuMusic;
        else if (room == "LaserDodge") want = clips.laserMusic;
        else if (room == "GusFight" || room == "ReggieFight") want = clips.fightMusic;
        else if (room == "VentMaze") want = clips.mazeMusic;
        Loop(music, want);

        bool alarmOn = GameFlags.Has(Story.AlarmOn) && System.Array.IndexOf(quietScenes, room) < 0;
        Loop(alarm, alarmOn ? clips.alarm : null);
        Loop(siren, alarmOn ? clips.siren : null);
    }

    static void Loop(AudioSource s, AudioClip c)
    {
        if (c == null) { if (s.isPlaying) s.Stop(); s.clip = null; return; }
        if (s.clip != c) { s.clip = c; s.Play(); }
        else if (!s.isPlaying) s.Play();
    }

    static void Shot(AudioClip c, float vol = 1f)
    {
        if (instance == null || c == null) return;
        instance.sfx.PlayOneShot(c, vol);
    }

    public static void Punch(float vol = 1f) { if (instance != null) Shot(instance.clips.punch, vol); }
    public static void Pickup() { if (instance != null) Shot(instance.clips.pickup); }
    // the ouch file is a bunch of ouches in a row, the first one is 0.7s to 1.6s in
    public static void Ouch()
    {
        if (instance == null || instance.clips.ouch == null) return;
        var s = instance.ouchSrc;
        s.Stop();
        s.clip = instance.clips.ouch;
        s.time = 0.7f;
        s.Play();
        s.SetScheduledEndTime(AudioSettings.dspTime + 0.9);
    }

    public static void Click() { if (instance != null) Shot(instance.clips.click, 0.7f); }
}
