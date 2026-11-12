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
            case "LaserVault": StartCoroutine(LaserVault()); break;
            case "DiningRoom2": StartCoroutine(DiningRoom()); break;
        }
        HideObjectivesTab();
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

        // the note joann drew on the fridge
        var fridge = TileSpot("kitchen_16"); // bottom half of the fridge, so you can reach it from the floor
        if (fridge.HasValue)
            StartCoroutine(FridgeNote(fridge.Value, FindItem("StickyNoteItem")));

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
        if (npc.LeftScene) return;
        // the fight choice ends on gus's "you wanna fight me?" line, then the fight loads
        if (GameFlags.Has("GusPickedFight")) { StartCoroutine(Transition.To("GusFight")); return; }
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
        yield return WalkOut(gus);
    }

    // gus heads for the dining room door and leaves, instead of just fading where he stands
    IEnumerator WalkOut(GameObject gus)
    {
        if (gus == null) yield break;
        Vector3 door = gus.transform.position + new Vector3(4f, 0, 0);
        foreach (var d in FindObjectsByType<RoomDoor>(FindObjectsInactive.Exclude))
            if (d.targetScene == "DiningRoom2") { door = d.transform.position; break; }
        door.z = gus.transform.position.z;

        var sr = gus.GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = door.x < gus.transform.position.x;
        Vector3 from = gus.transform.position;
        float dur = Mathf.Clamp(Vector2.Distance(from, door) / 2.5f, 0.6f, 3f);
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            // little walking bob
            gus.transform.position = Vector3.Lerp(from, door, k) + new Vector3(0, Mathf.Abs(Mathf.Sin(k * dur * 10f)) * 0.06f, 0);
            yield return null;
        }
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
        yield return Say("Tony", Radio, new[] {
            "*kssht* ...What was all that yelling?! Is somebody in there with you?",
        });
        yield return Say("You", art.playerPortrait, new[] { "Uh... no! That was me. I stubbed my toe. Really badly." });
        yield return Say("Tony", Radio, new[] { "...Uh huh. Keep it down. I said no mistakes." });
        Story.AddTrust(-1);
    }

    IEnumerator ReggieShowsUp(GameObject gusGo)
    {
        var reggie = SpawnCharacter("Reggie", ReggieSprite(), gusGo, new Vector3(1.6f, 0.2f, 0));
        bool gusOut = GameFlags.Has(GusAftermath.KnockedOutFlag);
        if (gusGo != null) gusGo.SetActive(false); // gus already bolted when the alarm went off
        yield return new WaitForSeconds(0.5f);

        yield return Say("Reggie", ReggiePortrait(), new[] {
            "What is going on here?!",
            gusOut
                ? "I heard the alarm, ran down the street as fast as I could, and saw Gus running out with a BLACK EYE!!!"
                : "I heard the alarm, ran down the street as fast as I could, and saw Gus running out the door!!!",
            "Who dares to set foot in MY restaurant?!",
            "Put 'em up, thief!",
        });
        GameFlags.Set(Story.ReggieMet);
        yield return Transition.To("ReggieFight");
    }

    IEnumerator ReggieLyingThere(GameObject gusGo)
    {
        var reggie = SpawnCharacter("Reggie", ReggieSprite(), gusGo, new Vector3(1.6f, 0.2f, 0));
        reggie.AddComponent<GusAftermath>(); // same knocked out look, stars and all
        if (gusGo != null) gusGo.SetActive(false);

        if (GameFlags.Has("ReggieDownLine")) yield break;
        GameFlags.Set("ReggieDownLine");
        yield return new WaitForSeconds(0.6f);
        yield return Say("You", art.playerPortrait, new[] {
            "Sorry Reggie. Nothing personal.",
            "My getaway car is waiting out back. Back through the closet window, go go go!",
        });
    }

    Sprite ReggieSprite() => art.reggieSprite != null ? art.reggieSprite : null;
    Sprite ReggiePortrait() => art.reggiePortrait != null ? art.reggiePortrait : art.reggieSprite;
    Sprite Radio => art.tonyRadio != null ? art.tonyRadio : art.tonyPortrait; // tony's only ever on the walkie talkie

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

        AddInspect(FindObject("Vent"), "You", new[] {
            "The vents must lead somewhere... but this one's way too tiny for me to get through.",
        });
        var cab = FindObject("cabinets");
        if (cab != null)
            AddInspectAt(Center(cab), () => Clue(art.clueNewspaper, new[] {
                "An old newspaper stuffed in the cabinet...",
                "...No wonder Tony hates this guy.",
            }), Top(cab));

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
            foreach (var r in bubble.GetComponentsInChildren<SpriteRenderer>(true)) r.sortingOrder = 500;
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
        // runs over to the desk, smacks the alarm, then makes a break for his vent
        var desk = FindObject("Desk");
        var deskSr = desk != null ? desk.GetComponent<SpriteRenderer>() : null;
        if (deskSr != null)
            yield return Hop(django, DeskPoint(deskSr, new Vector2(122f, 262f)), 0.45f);
        yield return new WaitForSeconds(0.15f);
        yield return Say("Django", art.djangoPortrait, new[] { "*SLAM*" });
        GameFlags.Set(Story.AlarmOn);
        GameFlags.Set(Story.GotRecipe);
        yield return new WaitForSeconds(0.5f);
        var vent = FindObject("Vent");
        if (vent != null && vent.GetComponent<Renderer>() != null)
            yield return IntoVent(django, vent.GetComponent<Renderer>().bounds.center);
        else
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

        yield return Say("Tony", Radio, new[] {
            "*kssht* WHAT IS THAT NOISE?! I can hear the alarm from the car! And sirens. LOTS of sirens.",
        });
        // a beat to think before answering, so the choice feels like it matters
        yield return Say("You", art.playerPortrait, new[] {
            Story.Trust <= -1
                ? "(Hmm... I'm already on thin ice with Tony. Better choose my words carefully.)"
                : "(Hmm... will Tony freak out if I tell him the truth? Or is honesty the best policy?)",
        });

        var call = MakeDialogue("Tony", Radio, new[] {
            "Talk to me. Did you run into anybody in there? Any trouble?",
            "...Huh. Must be some fancy safe. Fine, just get out of there.",
            "DOUBLE?! You were supposed to be in and out! I said NO mistakes!",
        });
        call.endDialogueLines = new[] { false, true, true };
        call.choices = new[] {
            new DialogueChoice {
                dialogueIndex = 0,
                choices = new[] {
                    "Nope! The alarm just went off when I grabbed the recipe.",
                    "You SET ME UP! There's a chef AND the co-owner here. Double my pay.",
                },
                nextDialogueIndexes = new[] { 1, 2 },
                actions = new[] {
                    new ChoiceAction { type = ChoiceActionType.None },
                    new ChoiceAction { type = ChoiceActionType.SetFlag, parameter = "trust:-2" },
                },
            }
        };
        yield return Run(call);

        if (Story.TonyGone)
        {
            GameFlags.Set(Story.Abandoned);
            yield return Say("Tony", Radio, new[] {
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
            yield return Say("Tony", Radio, new[] {
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

        if (!GameFlags.Has("CameraHowTo"))
        {
            GameFlags.Set("CameraHowTo");
            var canvasGo = new GameObject("CameraHowTo");
            SceneManager.MoveGameObjectToScene(canvasGo, SceneManager.GetActiveScene());
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1440);
            scaler.matchWidthOrHeight = 0.5f;

            PauseController.SetPause(true);
            Time.timeScale = 0f; // cameras hold still while you read
            var card = HowToPlay.Show(canvasGo.transform, "SECURITY CAMERAS", new[] {
                "The cameras sweep back and forth. Stay out of the red light!",
                "If a camera sees you for about half a second, you're caught.",
                "Hide behind the boxes and slip past between sweeps.",
            });
            while (!card.Done) yield return null;
            Destroy(canvasGo);
            Time.timeScale = 1f;
            PauseController.SetPause(false);
        }
        if (!GameFlags.Has(Story.AlarmOn) || !Story.TonyGone) yield break;

        // the way out is the big vent on the wall
        var vent = FindObject("Vent");
        var ventR = vent != null ? vent.GetComponent<Renderer>() : null;

        // the stairs up to the kitchen are off limits now
        foreach (var door in FindObjectsByType<RoomDoor>(FindObjectsInactive.Include))
        {
            if (door.targetScene != "Kitchen") continue;
            // trigger callbacks still fire on disabled scripts, so the door has to go completely
            var go = door.gameObject;
            Destroy(door);
            if (ventR != null) go.AddComponent<StairsBlocked>();
            else go.AddComponent<VentEscape>(); // no vent art found, fall back to the old stairs exit
        }
        if (ventR != null)
        {
            var exit = new GameObject("VentEscape");
            exit.transform.position = ventR.bounds.center + new Vector3(0, -ventR.bounds.extents.y, 0);
            var box = exit.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(ventR.bounds.size.x + 0.4f, 1.6f);
            exit.AddComponent<VentEscape>();
        }

        if (GameFlags.Has("VentHint")) yield break;
        GameFlags.Set("VentHint");
        yield return new WaitForSeconds(0.5f);
        yield return Say("Gus", art.gusPortrait, new[] {
            "*from upstairs* THE COPS ARE HERE!! I need to get out of here, AHHHH!",
        });
        yield return Say("You", art.playerPortrait, new[] {
            "Well... guess going upstairs really isn't an option.",
            "There must be another way out of here.",
        });
    }

    public void StairsNope()
    {
        StartCoroutine(Say("You", art.playerPortrait, new[] { "No way. The cops are right up there." }));
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
            "This vent's big enough to fit through... into the vents it is.",
            "*squeeeeze*",
        });
        yield return Transition.To("VentMaze");
    }

    // the hat sticky on the fridge is a pickup, so the second note only shows up once that one's gone
    IEnumerator FridgeNote(Vector3 at, GameObject sticky)
    {
        while (sticky != null && sticky.activeInHierarchy) yield return null;
        AddInspectAt(at, () => Clue(art.clueSticky, new[] {
            "There was another note stuck under that one...",
            "\"Clean out the poop in the vents on Monday. Too many laxatives for D!!\"",
            "...Who's D? And why is something pooping in the vents?",
        }), at.y + 1.2f);
    }

    // ---------------- dining room ----------------

    IEnumerator DiningRoom()
    {
        yield return null;
        yield return null;

        var trophies = FindObject("trophies");
        if (trophies != null)
            AddInspectAt(Center(trophies), () => Say("You", art.playerPortrait, new[] {
                "Three Golden Whisks in a row...",
                "Wow, Reggie's has been on top for a few years now.",
            }), Top(trophies));

        // the key hangs in a little nook behind reggie's portrait now
        var frame = FindObject("Portrait");
        var frameR = frame != null ? frame.GetComponentInChildren<SpriteRenderer>() : null;
        var key = FindItem("Key");
        if (frameR == null || key == null) yield break;

        var spot = frameR.bounds.center;
        var nook = new GameObject("KeyNook").AddComponent<SpriteRenderer>();
        SceneManager.MoveGameObjectToScene(nook.gameObject, frame.scene);
        nook.sprite = art.clueKeyWall;
        nook.sortingLayerID = frameR.sortingLayerID;
        nook.sortingOrder = frameR.sortingOrder + 1;
        frameR.sortingOrder += 2; // frame covers the nook until it's taken down
        nook.transform.position = new Vector3(spot.x, spot.y, frameR.transform.position.z + 0.01f);
        if (nook.sprite != null) nook.transform.localScale = Vector3.one * (frameR.bounds.size.x * 0.45f / nook.sprite.bounds.size.x); // small nook so the key reads as a key, not person-sized

        bool down = GameFlags.Has("PortraitDown");
        if (down) frame.SetActive(false);
        if (GameFlags.Has("BasementKey")) { nook.gameObject.SetActive(false); key.SetActive(false); yield break; }

        if (!down)
            AddInspectAt(spot, () => PortraitChoice(frame), frameR.bounds.max.y, "PortraitDown");

        // brooke's plaque inspect switches the key on, so keep it off the whole time and let the nook hand it over
        GameObject bubble = null;
        var player = GameObject.FindWithTag("Player");
        while (!GameFlags.Has("BasementKey"))
        {
            if (key != null && key.activeSelf) key.SetActive(false);
            if (GameFlags.Has("PortraitDown") && player != null)
            {
                if (bubble == null && art.pickupTooltip != null)
                {
                    bubble = Instantiate(art.pickupTooltip);
                    SceneManager.MoveGameObjectToScene(bubble, frame.scene);
                    bubble.transform.position = new Vector3(spot.x, frameR.bounds.max.y + 0.3f, 0);
                    // the prefab's bubble is on the default order, which puts it under the wall art here
                    foreach (var r in bubble.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        r.sortingLayerID = frameR.sortingLayerID;
                        r.sortingOrder = 500;
                    }
                }
                bool near = Vector2.Distance(player.transform.position, spot) < 1.8f;
                if (bubble != null) bubble.SetActive(near && !PauseController.IsGamePaused);
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (near && !PauseController.IsGamePaused && kb != null && kb.eKey.wasPressedThisFrame)
                    TakeKey(key);
            }
            yield return null;
        }
        if (bubble != null) Destroy(bubble);
        if (nook != null) nook.gameObject.SetActive(false);
    }

    // same steps as PlayerItemCollector, just started from the nook
    void TakeKey(GameObject key)
    {
        var inv = InventoryController.Instance;
        if (inv == null || key == null) return;
        key.SetActive(true); // the backpack copies this object, so it has to be on
        foreach (var r in key.GetComponentsInChildren<Renderer>(true)) if (r.gameObject == key) r.enabled = false;
        bool added = inv.AddItem(key);
        if (added)
        {
            var trig = key.GetComponent<ItemPickupTrigger>();
            if (trig != null) trig.Fire(); else GameFlags.Set("BasementKey");
            var pp = key.GetComponent<PersistentPickup>();
            if (pp != null) pp.MarkCollected();
            Destroy(key);
        }
        else key.SetActive(false);
    }

    IEnumerator PortraitChoice(GameObject frame)
    {
        var d = MakeDialogue("You", art.playerPortrait, new[] {
            "Reggie and... is that a mouse in a chef hat? This frame's hanging a little crooked.",
            "*lifts the frame off the wall* Well, well. What do we have here?",
            "Better leave it alone.",
        });
        d.endDialogueLines = new[] { false, true, true };
        d.choices = new[] {
            new DialogueChoice {
                dialogueIndex = 0,
                choices = new[] { "Take the painting down", "Leave it" },
                nextDialogueIndexes = new[] { 1, 2 },
                actions = new[] {
                    new ChoiceAction { type = ChoiceActionType.SetFlag, parameter = "PortraitDown" },
                    new ChoiceAction { type = ChoiceActionType.None },
                },
            }
        };
        yield return Run(d);
        if (!GameFlags.Has("PortraitDown")) yield break;

        // slide the frame down to the floor and away
        var sr = frame.GetComponentInChildren<SpriteRenderer>();
        Vector3 from = frame.transform.position;
        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            float k = t / 0.4f;
            frame.transform.position = from + new Vector3(0, -1.2f * k * k, 0);
            if (sr != null) sr.color = new Color(1, 1, 1, 1f - k);
            yield return null;
        }
        frame.SetActive(false);
    }

    // ---------------- laser vault ----------------

    IEnumerator LaserVault()
    {
        yield return null;
        yield return null;
        PlaceRiddleNote();
        if (GameFlags.Has(Story.AlarmOn)) yield break;

        if (!LaserDodge.Passed && !GameFlags.Has("LaserHint"))
        {
            GameFlags.Set("LaserHint");
            yield return new WaitForSeconds(0.4f);
            yield return Say("You", art.playerPortrait, new[] {
                "I can see the safe room entrance... but how do I get past all these lasers?",
            });
        }
        else if (LaserDodge.Passed && !GameFlags.Has("LaserDoneHint"))
        {
            GameFlags.Set("LaserDoneHint");
            yield return new WaitForSeconds(0.4f);
            yield return Say("You", art.playerPortrait, new[] {
                "Good thing I can switch the lasers off from this side, or that would've been trouble on the way out.",
                "Now... I should go over my clues and figure out this password.",
            });
        }
    }

    // joann's riddle sticky, stuck on the wall just right of the keypad
    void PlaceRiddleNote()
    {
        var pad = FindObject("CodeScreen");
        var padR = pad != null ? pad.GetComponent<SpriteRenderer>() : null;
        if (padR == null || art.clueRiddle == null) return;

        var note = new GameObject("RiddleNote").AddComponent<SpriteRenderer>();
        SceneManager.MoveGameObjectToScene(note.gameObject, pad.scene);
        note.sprite = art.clueRiddle;
        note.sortingLayerID = padR.sortingLayerID;
        note.sortingOrder = padR.sortingOrder + 1;
        // about half the keypad's height, like the little note in joann's mockup
        float h = padR.bounds.size.y * 0.55f;
        note.transform.localScale = Vector3.one * (h / note.sprite.bounds.size.y);
        float w = note.sprite.bounds.size.x * note.transform.localScale.x;
        var b = padR.bounds;
        float x = b.min.x - w * 0.5f - 0.35f;
        var btn = FindObject("button");
        var btnR = btn != null ? btn.GetComponent<SpriteRenderer>() : null;
        if (btnR != null) x = (btnR.bounds.max.x + b.min.x) * 0.5f; // centred in the gap between the button and the keypad
        var at = new Vector3(x, b.center.y + b.extents.y * 0.15f, pad.transform.position.z);
        note.transform.position = at;

        AddInspectAt(at, () => Clue(art.clueRiddle, new string[0]), at.y + h * 0.5f);
    }

    // the backpack's objectives and instructions tabs were never filled in, so they're just empty pages
    static void HideObjectivesTab()
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "ObjectiveTab" || t.name == "ObjectivePage" || t.name == "InstructionsTab" || t.name == "InstructionsPage")
                t.gameObject.SetActive(false);
    }

    // ---------------- closet (tony's waiting) ----------------

    IEnumerator Closet()
    {
        yield return null;
        yield return null;
        MakeHatPickup();
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

    void MakeHatPickup()
    {
        var hat = FindObject("TinyHatItem");
        if (hat == null) return;
        if (GameFlags.Has("HatTaken")) { hat.SetActive(false); return; }

        // switch off the "I to inspect" that pointed at the hat
        var tipChild = hat.transform.Find("tooltip");
        var tipField = typeof(InspectableItem).GetField("tooltip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var insp in FindObjectsByType<InspectableItem>(FindObjectsInactive.Include))
        {
            var t = tipField != null ? tipField.GetValue(insp) as GameObject : null;
            if (insp.gameObject == hat || (tipChild != null && t == tipChild.gameObject) || insp.name.Contains("Hat")) insp.enabled = false;
        }
        if (tipChild != null) Destroy(tipChild.gameObject);

        var sr = hat.GetComponent<SpriteRenderer>();
        var item = hat.GetComponent<Item>();
        if (item == null) item = hat.AddComponent<Item>();
        item.title = "Tiny Chef's Hat";
        item.description = "Whose head could this possibly fit?";
        if (sr != null) item.detailedView = sr.sprite;
        hat.tag = "Item";
        if (hat.GetComponent<Collider2D>() == null)
        {
            var c = hat.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.3f;
        }

        var trigger = hat.GetComponent<ItemPickupTrigger>();
        if (trigger == null) trigger = hat.AddComponent<ItemPickupTrigger>();
        trigger.flagId = "HatTaken";
        if (trigger.onPickedUp == null) trigger.onPickedUp = new UnityEngine.Events.UnityEvent();
        var talk = FindNPC("TonyHat");
        if (talk != null) trigger.onPickedUp.AddListener(() => { if (talk != null) talk.StartDialogue(); });

        // same "E to pickup" bubble as everything else, off the hat so the backpack copy doesn't take it along
        if (art.pickupTooltip != null)
        {
            var holder = new GameObject("HatTooltip");
            SceneManager.MoveGameObjectToScene(holder, hat.scene);
            holder.transform.position = sr != null ? sr.bounds.center : hat.transform.position;
            var bubble = Instantiate(art.pickupTooltip, holder.transform);
            bubble.transform.localPosition = art.pickupTooltip.transform.localPosition;
            foreach (var r in bubble.GetComponentsInChildren<SpriteRenderer>(true)) r.sortingOrder = 500;
            holder.AddComponent<Tooltip>().tooltip = bubble;
            StartCoroutine(DropWhenFlag(holder, "HatTaken"));
        }
    }

    IEnumerator DropWhenFlag(GameObject holder, string flag)
    {
        while (holder != null && !GameFlags.Has(flag)) yield return null;
        if (holder != null) Destroy(holder);
    }

    public void WindowOut()
    {
        StartCoroutine(WindowOutRoutine());
    }

    IEnumerator WindowOutRoutine()
    {
        yield return Say("Tony", art.tonyPortrait, new[] { "GET IN, GET IN, GET IN!" });
        yield return Transition.Out();
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

    // a spot on the safe room desk art, in pixels from its bottom-left
    static Vector3 DeskPoint(SpriteRenderer deskSr, Vector2 px)
    {
        var r = deskSr.sprite.rect;
        var b = deskSr.bounds;
        return new Vector3(b.min.x + px.x / r.width * b.size.x, b.min.y + px.y / r.height * b.size.y, 0);
    }

    // little hopping run from where you are to a point
    static IEnumerator Hop(GameObject go, Vector3 to, float dur)
    {
        if (go == null) yield break;
        Vector3 from = go.transform.position;
        to.z = from.z;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && Mathf.Abs(to.x - from.x) > 0.05f) sr.flipX = to.x < from.x;
        float t = 0;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            go.transform.position = Vector3.Lerp(from, to, k) + new Vector3(0, Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f)) * 0.18f, 0);
            yield return null;
        }
        go.transform.position = to;
    }

    // runs over to the vent and shrinks away into it
    static IEnumerator IntoVent(GameObject go, Vector3 vent)
    {
        if (go == null) yield break;
        float dist = Vector2.Distance(go.transform.position, vent);
        yield return Hop(go, vent, Mathf.Clamp(dist * 0.18f, 0.5f, 1.4f));
        var sr = go.GetComponent<SpriteRenderer>();
        Vector3 scale = go.transform.localScale;
        float t = 0;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float k = t / 0.35f;
            go.transform.localScale = scale * (1f - 0.8f * k);
            if (sr != null) sr.color = new Color(1, 1, 1, 1f - k);
            yield return null;
        }
        go.SetActive(false);
    }

    // close-up of the clue, then the robber's take on it
    IEnumerator Clue(Sprite art, string[] lines)
    {
        yield return CluePopup.Show(art);
        if (lines != null && lines.Length > 0) yield return Say("You", this.art.playerPortrait, lines);
    }

    static Vector3 Center(GameObject go)
    {
        var r = go.GetComponentInChildren<Renderer>();
        return r != null ? r.bounds.center : go.transform.position;
    }

    static float Top(GameObject go)
    {
        var r = go.GetComponentInChildren<Renderer>();
        return r != null ? r.bounds.max.y : go.transform.position.y + 0.5f;
    }

    // middle of the first tile in the room drawn with this sprite (things that only exist as tiles, like the fridge)
    static Vector3? TileSpot(string spriteName)
    {
        foreach (var map in FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsSortMode.None))
            foreach (var cell in map.cellBounds.allPositionsWithin)
            {
                var sp = map.GetSprite(cell);
                if (sp != null && sp.name == spriteName) return map.GetCellCenterWorld(cell);
            }
        return null;
    }

    static GameObject FindItem(string objectName)
    {
        foreach (var it in FindObjectsByType<Item>(FindObjectsInactive.Include))
            if (it.gameObject.name == objectName && it.gameObject.scene.IsValid()) return it.gameObject;
        return null;
    }

    void AddInspectAt(Vector3 at, System.Func<IEnumerator> action, float? top = null, string doneFlag = null)
    {
        var go = new GameObject("InspectSpot");
        SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
        go.transform.position = at;
        var spot = go.AddComponent<InspectSpot>();
        spot.center = at;
        spot.top = top ?? at.y + 0.5f;
        spot.bubble = art.inspectTooltip;
        spot.doneFlag = doneFlag;
        spot.onInspect = () => StartCoroutine(action());
    }

    // look-at spots that just get a line of dialogue, with the same "I to inspect" bubble as brooke's
    void AddInspect(GameObject target, string who, string[] lines)
    {
        if (target == null) return;
        var r = target.GetComponent<Renderer>();
        var spot = target.AddComponent<InspectSpot>();
        spot.center = r != null ? r.bounds.center : target.transform.position;
        spot.top = r != null ? r.bounds.max.y : target.transform.position.y + 0.5f;
        spot.bubble = art.inspectTooltip;
        spot.onInspect = () => StartCoroutine(Say(who, who == "You" ? art.playerPortrait : null, lines));
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

// the camera room stairs once the cops are upstairs
public class StairsBlocked : MonoBehaviour
{
    float cooldown;
    void Update() { cooldown -= Time.deltaTime; }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (cooldown > 0 || !other.CompareTag("Player")) return;
        cooldown = 3f;
        StoryDirector.Instance.StairsNope();
    }
}

// press I near it to get a line of dialogue (for things that aren't worth a popup)
public class InspectSpot : MonoBehaviour
{
    public Vector3 center;
    public float top;
    public float radius = 1.5f;
    public Sprite bubble;
    public string doneFlag; // stops offering once this flag is set
    public System.Action onInspect;

    Transform player;
    GameObject tip;

    static readonly System.Collections.Generic.List<InspectSpot> spots = new System.Collections.Generic.List<InspectSpot>();
    void OnEnable() { spots.Add(this); }
    void OnDisable() { spots.Remove(this); }

    bool Offers(Vector2 from, out float dist)
    {
        dist = Vector2.Distance(from, center);
        return !(!string.IsNullOrEmpty(doneFlag) && GameFlags.Has(doneFlag)) && dist <= radius;
    }

    // true if some other inspectable thing is nearer to the player than `dist` (only the closest gets the I bubble)
    public static bool SomethingCloser(Vector2 from, float dist, UnityEngine.Object self)
    {
        foreach (var s in spots)
            if (s != self && s.Offers(from, out float d) && d < dist) return true;
        foreach (var it in InspectableItem.All)
            if (it != self && it.Offers(from, out float d) && d < dist) return true;
        return false;
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        if (bubble != null)
        {
            tip = new GameObject("tooltip");
            tip.transform.position = new Vector3(center.x, top + 0.35f, 0);
            var sr = tip.AddComponent<SpriteRenderer>();
            sr.sprite = bubble;
            sr.sortingOrder = 500;
            tip.SetActive(false);
        }
    }

    void Update()
    {
        if (player == null) return;
        if (!string.IsNullOrEmpty(doneFlag) && GameFlags.Has(doneFlag)) { if (tip != null) tip.SetActive(false); return; }
        bool near = Vector2.Distance(player.position, center) <= radius;
        if (near && SomethingCloser(player.position, Vector2.Distance(player.position, center), this)) near = false;
        if (tip != null) tip.SetActive(near && !PauseController.IsGamePaused);
        if (!near || PauseController.IsGamePaused) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.iKey.wasPressedThisFrame) onInspect?.Invoke();
    }

    void OnDestroy() { if (tip != null) Destroy(tip); }
}
