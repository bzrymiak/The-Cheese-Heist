using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// runs the story beats that depend on what you did earlier, without anyone having to touch the scenes.
// lives across scenes and reacts whenever a room loads:
//   kitchen     gus after the fight (75% rule), gus leaving after the talk route, reggie showing up
//   safe room   django sets off the alarm and runs, tony's panicked walkie-talkie call
//   camera room tony bailed -> the kitchen is full of cops, out through the vent
//   closet      tony's still waiting -> climb out the window
public class StoryDirector : MonoBehaviour
{
    static StoryDirector instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("StoryDirector");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<StoryDirector>();
        go.AddComponent<AlarmOverlay>();
        go.AddComponent<GameAudio>();
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
        instance.OnRoom(SceneManager.GetActiveScene().name);
    }

    // static so it can be unhooked, and guarded because unity fires this once more after you stop playing
    static void Loaded(Scene s, LoadSceneMode m)
    {
        if (instance != null) instance.OnRoom(s.name);
    }

    StoryContent art;

    void Awake() { art = StoryContent.Get(); }

    void OnRoom(string room)
    {
        StopAllCoroutines();
        switch (room)
        {
            case "Kitchen": StartCoroutine(Kitchen()); break;
            case "SafeRoom": StartCoroutine(SafeRoom()); break;
            case "CameraRoom": StartCoroutine(CameraRoom()); break;
            case "Closet": StartCoroutine(Closet()); break;
        }
    }

    // ---------------- kitchen ----------------

    IEnumerator Kitchen()
    {
        yield return null; // let the room's own Start()s run first
        yield return null;

        var gus = FindNPC("Gus");
        var gusGo = gus != null ? gus.gameObject : FindObject("Gus");

        if (GameFlags.Has(Story.TalkedGus))
        {
            if (gusGo != null) gusGo.SetActive(false); // he stepped out
        }
        else if (gus != null && !GameFlags.Has(Story.FoughtGus))
        {
            gus.Finished += OnGusTalkOver;
        }

        if (GameFlags.Has(GusAftermath.KnockedOutFlag) && !GameFlags.Has(Story.GusTalkDone))
            yield return GusAfterFight();

        if (GameFlags.Has(Story.ReggieDown))
        {
            yield return ReggieLyingThere(gusGo);
        }
        else if (GameFlags.Has(Story.AlarmOn) && !Story.TonyGone)
        {
            yield return ReggieShowsUp(gusGo);
        }
    }

    void OnGusTalkOver(NPC npc)
    {
        npc.Finished -= OnGusTalkOver;
        if (npc.LeftScene) return; // picked the fight
        StartCoroutine(GusLeaves(npc.gameObject));
    }

    IEnumerator GusLeaves(GameObject gus)
    {
        GameFlags.Set(Story.TalkedGus);
        yield return new WaitForSeconds(0.3f);
        yield return Say("Gus", art.gusPortrait, new[] {
            "Oh, and if you're here for Reggie's recipe... good luck.",
            "I tried once. Never found the basement key. He hides it somewhere out front.",
        });
        yield return FadeOut(gus);
    }

    IEnumerator GusAfterFight()
    {
        GameFlags.Set(Story.GusTalkDone);
        yield return new WaitForSeconds(0.6f);

        bool messy = GameFlags.Has(Story.GusLowScore);
        yield return Say("Gus", art.gusPortrait, new[] {
            "Ugh... okay, okay, you win...",
            "If you want the basement... Reggie hides the key out in the dining room somewhere...",
            messy ? "...HELP!!! SOMEBODY HELP ME!! THERE'S A THIEF IN THE KITCHEN!!" : "...ugh... *passes out*",
        });
        if (!messy) yield break;

        yield return Say("You", art.playerPortrait, new[] { "SHHH! *bonk*" });
        yield return new WaitForSeconds(0.4f);
        yield return Say("Tony", art.tonyPortrait, new[] {
            "*kssht* ...What was all that yelling?! Is somebody in there with you?",
        });
        yield return Say("You", art.playerPortrait, new[] { "Uh... no! That was me. I stubbed my toe. Really badly." });
        yield return Say("Tony", art.tonyPortrait, new[] { "...Uh huh. Keep it down. I said no mistakes." });
        Story.AddTrust(-1);
    }

    IEnumerator ReggieShowsUp(GameObject gusGo)
    {
        var reggie = SpawnCharacter("Reggie", ReggieSprite(), gusGo, new Vector3(1.6f, 0.2f, 0));
        bool gusOut = GameFlags.Has(GusAftermath.KnockedOutFlag);
        yield return new WaitForSeconds(0.5f);

        yield return Say("Reggie", ReggiePortrait(), new[] {
            "DJANGO?! Are you okay?! I heard the alarm from down the street!",
            gusOut ? "...Gus? GUS! What did you do to my sous chef?!" : "And where's Gus?! He was supposed to lock up tonight!",
            "Nobody breaks into MY kitchen and walks out with OUR recipe.",
            "Put 'em up, thief!",
        });
        GameFlags.Set(Story.ReggieMet);
        SceneManager.LoadScene("ReggieFight");
    }

    IEnumerator ReggieLyingThere(GameObject gusGo)
    {
        var reggie = SpawnCharacter("Reggie", ReggieSprite(), gusGo, new Vector3(1.6f, 0.2f, 0));
        reggie.AddComponent<GusAftermath>(); // same knocked out look, stars and all

        if (GameFlags.Has("ReggieDownLine")) yield break;
        GameFlags.Set("ReggieDownLine");
        yield return new WaitForSeconds(0.6f);
        yield return Say("You", art.playerPortrait, new[] {
            "Sorry Reggie. Nothing personal.",
            "Tony's waiting out back. Back through the closet window, go go go!",
        });
    }

    Sprite ReggieSprite() => art.reggieSprite != null ? art.reggieSprite : null;
    Sprite ReggiePortrait() => art.reggiePortrait != null ? art.reggiePortrait : art.gusPortrait;

    // ---------------- safe room ----------------

    IEnumerator SafeRoom()
    {
        // grab django before his Start() so he doesn't talk until the book's taken
        var django = FindNPC("Django");
        bool alarmDone = GameFlags.Has(Story.AlarmOn);
        if (django != null && !alarmDone) django.manualOnly = true;

        yield return null;
        yield return null;

        if (alarmDone)
        {
            if (django != null) django.gameObject.SetActive(false); // he's long gone
            if (!GameFlags.Has(Story.TonyCalled)) yield return TonyAlarmCall();
            yield break;
        }

        if (!GameFlags.Has(Story.GotRecipe))
        {
            PlaceRecipeBook();
            yield return new WaitForSeconds(0.6f);
            yield return Say("You", art.playerPortrait, new[] {
                "There it is. The recipe book, right on the desk.",
                "Grab it and go. Easy.",
            });
            while (!GameFlags.Has(Story.GotRecipe)) yield return null;
            yield return new WaitForSeconds(0.4f);
        }

        if (django == null) yield break;
        django.Finished += OnDjangoDone;
        while (PauseController.IsGamePaused) yield return null;
        django.StartDialogue();
    }

    // joann drew the book into the desk art, so the desk gets swapped for a copy without it
    // and the book goes back on top as its own pickup in the exact same spot
    void PlaceRecipeBook()
    {
        if (art.recipeBook == null) { Debug.LogWarning("no recipe book in StoryContent, run Build Recipe Book"); return; }
        var desk = FindObject("Desk");
        var deskSr = desk != null ? desk.GetComponent<SpriteRenderer>() : null;
        Vector3 spot = Vector3.zero;
        if (deskSr != null && deskSr.sprite != null)
        {
            // book centre in the desk picture, in pixels from its bottom-left
            var r = deskSr.sprite.rect;
            Vector2 px = new Vector2(275.5f, 231f);
            var b = deskSr.bounds;
            spot = new Vector3(b.min.x + px.x / r.width * b.size.x, b.min.y + px.y / r.height * b.size.y, 0);
            if (art.deskNoBook != null) deskSr.sprite = art.deskNoBook;
        }

        var book = Instantiate(art.recipeBook);
        SceneManager.MoveGameObjectToScene(book, SceneManager.GetActiveScene());
        book.name = "RecipeBook";
        book.transform.position = spot;
        var sr = book.GetComponent<SpriteRenderer>();
        if (sr != null && deskSr != null)
        {
            book.transform.localScale = desk.transform.lossyScale; // same pixel size as the desk it came off
            sr.sortingLayerID = deskSr.sortingLayerID;
            sr.sortingOrder = deskSr.sortingOrder + 1;
        }

        var trigger = book.GetComponent<ItemPickupTrigger>();
        if (trigger == null) trigger = book.AddComponent<ItemPickupTrigger>();
        trigger.flagId = Story.GotRecipe;

        // kept off the book itself, otherwise the backpack copy would carry the bubble with it
        if (art.pickupTooltip != null)
        {
            var holder = new GameObject("RecipeTooltip");
            SceneManager.MoveGameObjectToScene(holder, SceneManager.GetActiveScene());
            holder.transform.position = spot;
            var bubble = Instantiate(art.pickupTooltip, holder.transform);
            bubble.transform.localPosition = art.pickupTooltip.transform.localPosition;
            var tip = holder.AddComponent<Tooltip>();
            tip.tooltip = bubble;
            StartCoroutine(DropWhenTaken(holder));
        }
    }

    IEnumerator DropWhenTaken(GameObject holder)
    {
        while (holder != null && !GameFlags.Has(Story.GotRecipe)) yield return null;
        if (holder != null) Destroy(holder);
    }

    void OnDjangoDone(NPC npc)
    {
        npc.Finished -= OnDjangoDone;
        StartCoroutine(DjangoBolts(npc.gameObject));
    }

    IEnumerator DjangoBolts(GameObject django)
    {
        // smacks the alarm and scurries off into his little vent
        GameFlags.Set(Story.AlarmOn);
        GameFlags.Set(Story.GotRecipe);
        yield return Scurry(django);
        yield return new WaitForSeconds(0.6f);
        yield return Say("You", art.playerPortrait, new[] {
            "...Did a mouse in a chef hat just set off the alarm and run into a vent?",
            "Whatever. The recipe's mine. Now I just need to get out of here.",
        });
        yield return new WaitForSeconds(0.8f);
        yield return TonyAlarmCall();
    }

    IEnumerator TonyAlarmCall()
    {
        GameFlags.Set(Story.TonyCalled);

        var call = MakeDialogue("Tony", art.tonyPortrait, new[] {
            "*kssht* WHAT IS THAT NOISE?! I can hear the alarm from the car! And sirens. LOTS of sirens.",
            "Talk to me. Did you run into anybody in there? Any trouble?",
            "...Huh. Must be some fancy safe. Fine, just get out of there.",
            "...You tripped a laser. Sure you did. Just hurry up.",
            "DOUBLE?! You were supposed to be in and out! I said NO mistakes!",
        });
        call.endDialogueLines = new[] { false, false, true, true, true };
        call.choices = new[] {
            new DialogueChoice {
                dialogueIndex = 1,
                choices = new[] {
                    "Nope! The alarm just went off when I grabbed the recipe.",
                    "Nobody's here! I just... kinda tripped a laser.",
                    "You SET ME UP! There's a chef AND the co-owner here. Double my pay.",
                },
                nextDialogueIndexes = new[] { 2, 3, 4 },
                actions = new[] {
                    new ChoiceAction { type = ChoiceActionType.None },
                    new ChoiceAction { type = ChoiceActionType.SetFlag, parameter = "trust:-1" },
                    new ChoiceAction { type = ChoiceActionType.SetFlag, parameter = "trust:-2" },
                },
            }
        };
        yield return Run(call);

        if (Story.TonyGone)
        {
            GameFlags.Set(Story.Abandoned);
            yield return Say("Tony", art.tonyPortrait, new[] {
                "You know what? I'm out. Cops are pulling up out front and I've got a restaurant to run.",
                "You're on your own, kid. *click*",
            });
            yield return Say("You", art.playerPortrait, new[] {
                "Tony? TONY?! ...Great. No getaway car, and the cops are on their way in.",
                "I can't go back up through the kitchen. There has to be another way out of this basement...",
            });
        }
        else
        {
            yield return Say("Tony", art.tonyPortrait, new[] {
                "I'm parked out back by the storage closet window. Get up here before the cops do. MOVE!",
            });
        }
    }

    // ---------------- camera room (no tony, out through the vent) ----------------

    IEnumerator CameraRoom()
    {
        yield return null;
        yield return null;
        ArmCameras();
        if (!GameFlags.Has(Story.AlarmOn) || !Story.TonyGone) yield break;

        // the stairs up to the kitchen are off limits now
        foreach (var door in FindObjectsByType<RoomDoor>(FindObjectsInactive.Include))
        {
            if (door.targetScene != "Kitchen") continue;
            // trigger callbacks still fire on disabled scripts, so the door has to go completely
            var go = door.gameObject;
            Destroy(door);
            go.AddComponent<VentEscape>();
        }

        if (GameFlags.Has("VentHint")) yield break;
        GameFlags.Set("VentHint");
        yield return new WaitForSeconds(0.5f);
        yield return Say("You", art.playerPortrait, new[] {
            "I can hear the cops stomping around upstairs...",
            "That big vent though. If a mouse can get out through the vents, so can I. Head for the stairs and I'll squeeze in.",
        });
    }

    // turns brooke's camera sprites into sweeping security cameras, aimed into the room
    void ArmCameras()
    {
        SecurityCamera.ResetAll();
        var bg = FindObject("Background");
        Vector3 middle = bg != null ? bg.GetComponent<Renderer>().bounds.center : Vector3.zero;

        var cams = new List<SpriteRenderer>();
        foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
        {
            var n = sr.gameObject.name;
            if ((n == "Camera" || n == "VentCamera") && sr.GetComponent<SecurityCamera>() == null) cams.Add(sr);
        }
        if (cams.Count == 0) return;

        // the one highest up is on the top wall and looks down into the room,
        // the others are on the side walls and look across it
        cams.Sort((x, y) => y.bounds.center.y.CompareTo(x.bounds.center.y));
        for (int i = 0; i < cams.Count; i++)
        {
            var sr = cams[i];
            var cam = sr.gameObject.AddComponent<SecurityCamera>();
            if (i == 0)
            {
                cam.baseAngle = -90f;
                // watches the middle strip only, a short way down
                cam.sweep = 22f;
                cam.halfAngle = 12f;
                cam.range = 6f;
                cam.period = 6f;
            }
            else
            {
                cam.baseAngle = sr.bounds.center.x > middle.x ? 180f : 0f;
                // tilted a bit down so it only guards its own side
                cam.baseAngle += cam.baseAngle > 90f ? 15f : -15f;
                cam.sweep = 18f;
                cam.halfAngle = 12f;
                cam.range = 7f;
                cam.period = 5f;
            }
            cam.phase = i * 0.5f; // opposite ends of their pan
        }
    }

    public void VentOut()
    {
        StartCoroutine(VentOutRoutine());
    }

    IEnumerator VentOutRoutine()
    {
        yield return Say("You", art.playerPortrait, new[] {
            "Nope, not going up there. Into the vent it is...",
            "*squeeeeze*",
        });
        SceneManager.LoadScene("VentMaze");
    }

    // ---------------- closet (tony's waiting) ----------------

    IEnumerator Closet()
    {
        yield return null;
        yield return null;
        if (!GameFlags.Has(Story.AlarmOn) || Story.TonyGone || !GameFlags.Has(Story.ReggieDown)) yield break;

        // the window is where you dropped in, right by the start spawn
        foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include))
        {
            if (sp.id != "start") continue;
            var win = new GameObject("WindowEscape");
            win.transform.position = sp.transform.position;
            var box = win.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.4f, 1.4f);
            win.AddComponent<WindowEscape>();
        }

        if (GameFlags.Has("WindowHint")) yield break;
        GameFlags.Set("WindowHint");
        yield return new WaitForSeconds(0.4f);
        yield return Say("You", art.playerPortrait, new[] { "There's the window. Tony should be right outside!" });
    }

    public void WindowOut()
    {
        StartCoroutine(WindowOutRoutine());
    }

    IEnumerator WindowOutRoutine()
    {
        yield return Say("Tony", art.tonyPortrait, new[] { "GET IN, GET IN, GET IN!" });
        Ending.Show(true);
    }

    public static StoryDirector Instance => instance;

    // ---------------- helpers ----------------

    // one character talking, no choices
    IEnumerator Say(string who, Sprite portrait, string[] lines)
    {
        yield return Run(MakeDialogue(who, portrait, lines));
    }

    NPCDialogue MakeDialogue(string who, Sprite portrait, string[] lines)
    {
        var d = ScriptableObject.CreateInstance<NPCDialogue>();
        d.name = "story_" + who + "_" + Time.frameCount;
        d.npcName = who;
        d.npcPortrait = portrait;
        d.dialogueLines = lines;
        d.endDialogueLines = new bool[lines.Length];
        d.choices = new DialogueChoice[0];
        d.typingSpeed = 0.03f;
        return d;
    }

    // plays a dialogue through the normal dialogue box and waits for it to close
    IEnumerator Run(NPCDialogue data)
    {
        // don't talk over another conversation
        while (PauseController.IsGamePaused) yield return null;

        var go = new GameObject("StoryLine");
        var npc = go.AddComponent<NPC>();
        npc.dialogueData = data;
        npc.manualOnly = true;
        npc.repeatable = true;

        bool done = false;
        npc.Finished += _ => done = true;
        yield return null; // NPC.Start grabs the dialogue box
        npc.StartDialogue();
        while (!done && go != null) yield return null;
        if (go != null) Destroy(go);
    }

    static NPC FindNPC(string objectName)
    {
        foreach (var n in FindObjectsByType<NPC>(FindObjectsInactive.Exclude))
            if (n.gameObject.name == objectName) return n;
        return null;
    }

    static GameObject FindObject(string objectName)
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == objectName && t.gameObject.scene.IsValid()) return t.gameObject;
        return null;
    }

    // drops a character into the room next to where gus is (or was), same sorting as him
    GameObject SpawnCharacter(string name, Sprite sprite, GameObject near, Vector3 offset)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        var nearSr = near != null ? near.GetComponent<SpriteRenderer>() : null;

        if (sprite != null) sr.sprite = sprite;
        else if (nearSr != null) { sr.sprite = nearSr.sprite; sr.color = new Color(0.75f, 0.8f, 1f); } // stand-in until reggie's art is in

        if (nearSr != null)
        {
            sr.sortingLayerID = nearSr.sortingLayerID;
            sr.sortingOrder = nearSr.sortingOrder;

            // exported art can come in at any size, so match gus's height (reggie's a touch taller)
            if (sr.sprite != null && nearSr.sprite != null && sprite != null)
            {
                float k = nearSr.sprite.bounds.size.y * near.transform.lossyScale.y / sr.sprite.bounds.size.y * 1.1f;
                go.transform.localScale = Vector3.one * k;
            }
        }
        Vector3 basePos = near != null ? near.transform.position : Vector3.zero;
        if (near != null && near.transform.rotation != Quaternion.identity) basePos += new Vector3(0, 0.3f, 0); // gus is lying down
        go.transform.position = basePos + offset;
        var box = go.AddComponent<BoxCollider2D>();
        if (sr.sprite != null) box.size = new Vector2(sr.sprite.bounds.size.x * 0.8f, sr.sprite.bounds.size.y * 0.5f);
        return go;
    }

    static IEnumerator FadeOut(GameObject go)
    {
        if (go == null) yield break;
        var sr = go.GetComponent<SpriteRenderer>();
        float t = 0;
        while (t < 0.6f && sr != null)
        {
            t += Time.deltaTime;
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f - t / 0.6f);
            yield return null;
        }
        go.SetActive(false);
    }

    static IEnumerator Scurry(GameObject go)
    {
        if (go == null) yield break;
        var sr = go.GetComponent<SpriteRenderer>();
        Vector3 start = go.transform.position;
        Vector3 scale = go.transform.localScale;
        float t = 0;
        while (t < 0.7f)
        {
            t += Time.deltaTime;
            float k = t / 0.7f;
            // little hops off to the side while shrinking into the vent
            go.transform.position = start + new Vector3(-2.2f * k, Mathf.Abs(Mathf.Sin(k * 18f)) * 0.15f, 0);
            go.transform.localScale = scale * (1f - 0.6f * k);
            if (sr != null) sr.color = new Color(1, 1, 1, 1f - k * k);
            yield return null;
        }
        go.SetActive(false);
    }
}

// on the camera room stairs once tony's bailed: you can't go up, so you take the vent
public class VentEscape : MonoBehaviour
{
    bool used;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player")) return;
        used = true;
        StoryDirector.Instance.VentOut();
    }
}

// closet window once reggie's down and tony's still waiting
public class WindowEscape : MonoBehaviour
{
    bool used;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player")) return;
        used = true;
        StoryDirector.Instance.WindowOut();
    }
}
