using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// the gus fight.
// up arrow = punch. down arrow = dodge (gus winds up first so you can see it coming)
// perfect = full effect, too early / too slow = half, miss = nothing (or you eat the whole punch)
public class RhythmFight : MonoBehaviour
{
    public enum Dir { Left, Down, Up, Right }

    class Note
    {
        public Dir dir;
        public float time;      // when it should be hit (seconds after the fight starts)
        public RectTransform rect;
        public bool judged;
        public bool telegraphed;
    }

    enum Grade { Perfect, Half, Miss }

    [Header("UI")]
    public RectTransform stage;       // everything that shakes
    public RectTransform track;       // arrows get spawned in here
    public RectTransform hitCircle;
    public Image hitInner;
    public Text judgeText;
    public Text centerText;           // countdown + results
    public RectTransform playerHealth;
    public RectTransform gusHealth;
    public RectTransform robber;
    public RectTransform gus;
    public Text gusAlert;             // the "!" over gus when he's about to swing
    public Image hurtFlash;
    public Image guide;               // faint outline in the circle showing which arrow is coming next

    [Header("Arrow art (left, down, up, right)")]
    public Sprite[] arrows = new Sprite[4];
    public Sprite[] arrowsHit = new Sprite[4];
    public Sprite[] arrowsOff = new Sprite[4];
    public float arrowSize = 110f;

    [Header("Timing")]
    public float scrollSpeed = 600f;  // pixels per second
    public float perfectWindow = 0.07f;
    public float okWindow = 0.17f;    // outside perfect but inside this = too early / too slow
    public float ignoreWindow = 0.3f; // presses further out than this don't count at all
    public float windUp = 0.55f;      // how long before a dodge gus starts his swing
    public float countdown = 3f;

    [Header("Pattern")]
    public int noteCount = 22;
    public float gap = 0.65f;
    public int seed = 7;

    [Header("Damage")]
    public float playerHitDamage = 0.2f;   // eating a full punch

    [Header("After the fight")]
    public string nextScene = "Kitchen";
    public string nextSpawn = "";

    public static bool LastFightWon;
    public static int LastHits, LastTotal;

    readonly List<Note> notes = new List<Note>();
    float startTime;
    bool running, finished;
    int hits, perfects;
    float judgeTimer;
    float playerHp = 1f, gusHp = 1f;
    float punchDamage;
    Color innerColor;
    Vector2 robberHome, gusHome;
    Coroutine robberAnim, gusAnim, shakeAnim;
    Image gusImage;

    void Start()
    {
        BuildPattern();

        int punches = notes.FindAll(n => n.dir == Dir.Up).Count;
        punchDamage = 1f / Mathf.Max(1, punches * 0.75f); // need roughly 3/4 of punches perfect to drop him

        robberHome = robber.anchoredPosition;
        gusHome = gus.anchoredPosition;
        gusImage = gus.GetComponent<Image>();
        if (hitInner != null) innerColor = hitInner.color;
        if (gusAlert != null) gusAlert.text = "";
        if (hurtFlash != null) hurtFlash.color = new Color(1, 0, 0, 0);
        judgeText.text = "";

        UpdateBars();
        startTime = Time.time + countdown;
        running = true;
    }

    void BuildPattern()
    {
        var rng = new System.Random(seed);
        float t = 0.6f;
        for (int i = 0; i < noteCount; i++)
        {
            // first few are punches so you learn it, then dodges start mixing in
            bool dodge = i >= 4 && rng.NextDouble() < 0.4;
            var dir = dodge ? Dir.Down : Dir.Up;

            // give the wind up room to read
            if (dodge) t += 0.25f;
            notes.Add(new Note { dir = dir, time = t });

            // quick double punch now and then
            if (!dodge && i > 6 && rng.NextDouble() < 0.18)
            {
                t += 0.2f;
                notes.Add(new Note { dir = Dir.Up, time = t });
            }

            // starts slow, speeds up as it goes
            t += Mathf.Lerp(gap * 1.35f, gap * 0.8f, i / (float)noteCount);
        }
    }

    void Update()
    {
        if (finished)
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                Leave();
            return;
        }
        if (!running) return;

        float songTime = Time.time - startTime;
        centerText.text = songTime < 0 ? Mathf.CeilToInt(-songTime).ToString() : "";

        MoveNotes(songTime);
        Telegraph(songTime);
        UpdateGuide();
        if (songTime >= 0) ReadInput(songTime);
        CheckMissed(songTime);

        if (judgeTimer > 0)
        {
            judgeTimer -= Time.deltaTime;
            if (judgeTimer <= 0) judgeText.text = "";
        }

        if (hurtFlash != null && hurtFlash.color.a > 0)
            hurtFlash.color = new Color(1, 0, 0, Mathf.MoveTowards(hurtFlash.color.a, 0, Time.deltaTime * 1.5f));

        if (notes.TrueForAll(n => n.judged)) EndFight();
    }

    void MoveNotes(float songTime)
    {
        float hitX = hitCircle.anchoredPosition.x;
        float rightEdge = track.rect.width / 2f + arrowSize;

        foreach (var n in notes)
        {
            if (n.judged) continue;
            float x = hitX + (n.time - songTime) * scrollSpeed;

            if (n.rect == null)
            {
                if (x > rightEdge) continue;
                n.rect = MakeArrow(n.dir);
            }
            n.rect.anchoredPosition = new Vector2(x, hitCircle.anchoredPosition.y);
        }
    }

    // swap the outline in the circle to match whatever arrow is next,
    // so you line the arrow up with its own shape
    void UpdateGuide()
    {
        if (guide == null) return;
        Note next = null;
        foreach (var n in notes)
            if (!n.judged && (next == null || n.time < next.time)) next = n;

        guide.enabled = next != null;
        if (next == null) return;
        int i = (int)next.dir;
        guide.sprite = arrowsOff[i] != null ? arrowsOff[i] : arrows[i];
        guide.rectTransform.localScale = next.dir == Dir.Right ? new Vector3(-1, 1, 1) : Vector3.one;
    }

    // gus leans back and flashes before each dodge arrow arrives
    void Telegraph(float songTime)
    {
        foreach (var n in notes)
        {
            if (n.judged || n.telegraphed || n.dir != Dir.Down) continue;
            if (n.time - songTime <= windUp)
            {
                n.telegraphed = true;
                Play(ref gusAnim, gus, GusWindUp(n.time - songTime));
            }
        }
    }

    void ReadInput(float songTime)
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Press(Dir.Up, songTime);
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Press(Dir.Down, songTime);
    }

    void Press(Dir dir, float songTime)
    {
        Note target = null;
        foreach (var n in notes)
        {
            if (n.judged) continue;
            if (target == null || Mathf.Abs(n.time - songTime) < Mathf.Abs(target.time - songTime)) target = n;
        }
        if (target == null) return;

        float diff = target.time - songTime; // positive = early
        if (Mathf.Abs(diff) > ignoreWindow) return;

        if (target.dir != dir) { Resolve(target, Grade.Miss, "WRONG WAY!"); return; }

        if (Mathf.Abs(diff) <= perfectWindow) Resolve(target, Grade.Perfect, null);
        else if (Mathf.Abs(diff) <= okWindow) Resolve(target, Grade.Half, diff > 0 ? "TOO EARLY!" : "TOO SLOW!");
        else Resolve(target, Grade.Miss, diff > 0 ? "TOO EARLY!" : "TOO SLOW!");
    }

    void CheckMissed(float songTime)
    {
        foreach (var n in notes)
            if (!n.judged && songTime - n.time > okWindow)
                Resolve(n, Grade.Miss, "MISSED!");
    }

    void Resolve(Note n, Grade g, string label)
    {
        n.judged = true;
        if (n.rect != null) StartCoroutine(ArrowFeedback(n, g != Grade.Miss));

        if (n.dir == Dir.Up) Punch(g, label);
        else Dodge(g, label);

        UpdateBars();
        if (playerHp <= 0 || gusHp <= 0) EndFight();
    }

    void Punch(Grade g, string label)
    {
        if (g == Grade.Miss)
        {
            ShowJudge(label ?? "MISSED!", Color.white);
            Play(ref robberAnim, robber, Whiff());
            return;
        }

        bool perfect = g == Grade.Perfect;
        gusHp = Mathf.Max(0, gusHp - punchDamage * (perfect ? 1f : 0.5f));
        hits++;
        if (perfect) perfects++;

        ShowJudge(perfect ? "PERFECT!" : label, perfect ? new Color(1f, 0.85f, 0.2f) : Color.white);
        Play(ref robberAnim, robber, Lunge(perfect ? 110f : 70f));
        Play(ref gusAnim, gus, Reel(perfect ? 60f : 30f, perfect ? 14f : 6f));
        StartCoroutine(FlashRing(perfect ? new Color(1f, 0.85f, 0.2f) : Color.white));

        if (perfect) HitStop(0.07f, 16f);
    }

    void Dodge(Grade g, string label)
    {
        Play(ref gusAnim, gus, Swing());

        if (g == Grade.Perfect)
        {
            hits++;
            perfects++;
            ShowJudge("DODGED!", new Color(1f, 0.85f, 0.2f));
            Play(ref robberAnim, robber, Sidestep());
            StartCoroutine(FlashRing(new Color(1f, 0.85f, 0.2f)));
            HitStop(0.05f, 8f);
            return;
        }

        bool half = g == Grade.Half;
        playerHp = Mathf.Max(0, playerHp - playerHitDamage * (half ? 0.5f : 1f));
        if (half) hits++;
        ShowJudge(half ? label : (label == "MISSED!" ? "OOF!" : label), new Color(1f, 0.45f, 0.45f));
        Play(ref robberAnim, robber, half ? Graze() : Knockback());
        if (hurtFlash != null) hurtFlash.color = new Color(1, 0, 0, half ? 0.15f : 0.3f);
        Shake(half ? 6f : 14f, 0.18f);
    }

    void ShowJudge(string text, Color c)
    {
        judgeText.text = text;
        judgeText.color = c;
        judgeTimer = 0.5f;
    }

    // ---- character animations ----

    void Play(ref Coroutine slot, RectTransform who, IEnumerator anim)
    {
        if (slot != null) StopCoroutine(slot);
        ResetPose(who);
        slot = StartCoroutine(anim);
    }

    void ResetPose(RectTransform who)
    {
        who.anchoredPosition = who == robber ? robberHome : gusHome;
        who.localRotation = Quaternion.identity;
        who.localScale = Vector3.one;
        if (who == gus && gusImage != null) gusImage.color = Color.white;
        if (who == gus && gusAlert != null) gusAlert.text = "";
    }

    // smooth 0 -> 1 -> 0 curve, fast out and settle back
    static float Bump(float k) => Mathf.Sin(k * Mathf.PI);

    IEnumerator Lunge(float dist)
    {
        float t = 0, dur = 0.22f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float out1 = k < 0.35f ? Mathf.SmoothStep(0, 1, k / 0.35f) : 1f - Mathf.SmoothStep(0, 1, (k - 0.35f) / 0.65f);
            robber.anchoredPosition = robberHome + new Vector2(dist * out1, 8f * Bump(k));
            robber.localRotation = Quaternion.Euler(0, 0, -10f * out1);
            yield return null;
        }
        ResetPose(robber);
    }

    IEnumerator Whiff()
    {
        // swings at nothing, small stumble
        float t = 0, dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            robber.anchoredPosition = robberHome + new Vector2(35f * Bump(k), 0);
            robber.localRotation = Quaternion.Euler(0, 0, -6f * Bump(k));
            yield return null;
        }
        ResetPose(robber);
    }

    IEnumerator Reel(float dist, float tilt)
    {
        yield return new WaitForSeconds(0.06f); // land the punch first
        if (gusImage != null) gusImage.color = new Color(1f, 0.6f, 0.6f);
        float t = 0, dur = 0.3f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float b = k < 0.25f ? k / 0.25f : 1f - Mathf.SmoothStep(0, 1, (k - 0.25f) / 0.75f);
            gus.anchoredPosition = gusHome + new Vector2(dist * b, 0);
            gus.localRotation = Quaternion.Euler(0, 0, -tilt * b);
            if (gusImage != null && k > 0.3f) gusImage.color = Color.white;
            yield return null;
        }
        ResetPose(gus);
    }

    IEnumerator GusWindUp(float timeLeft)
    {
        if (gusAlert != null) gusAlert.text = "!";
        float t = 0;
        while (t < timeLeft)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, timeLeft));
            // leans back and gets redder the closer the swing is
            gus.anchoredPosition = gusHome + new Vector2(30f * k, 0);
            gus.localRotation = Quaternion.Euler(0, 0, -12f * k);
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 30f);
            if (gusImage != null) gusImage.color = Color.Lerp(Color.white, new Color(1f, 0.45f, 0.45f), k * pulse);
            if (gusAlert != null) gusAlert.rectTransform.localScale = Vector3.one * (1f + 0.25f * pulse);
            yield return null;
        }
        // holds the pose until the dodge gets judged
    }

    IEnumerator Swing()
    {
        float t = 0, dur = 0.26f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float out1 = k < 0.3f ? Mathf.SmoothStep(0, 1, k / 0.3f) : 1f - Mathf.SmoothStep(0, 1, (k - 0.3f) / 0.7f);
            gus.anchoredPosition = gusHome + new Vector2(-120f * out1, 6f * Bump(k));
            gus.localRotation = Quaternion.Euler(0, 0, 14f * out1);
            yield return null;
        }
        ResetPose(gus);
    }

    IEnumerator Sidestep()
    {
        // hops back and ducks out of the way
        float t = 0, dur = 0.32f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float b = Bump(k);
            robber.anchoredPosition = robberHome + new Vector2(-70f * b, 30f * Mathf.Sin(k * Mathf.PI * 2f) * (1 - k));
            robber.localScale = new Vector3(1f + 0.08f * b, 1f - 0.12f * b, 1);
            yield return null;
        }
        ResetPose(robber);
    }

    IEnumerator Graze()
    {
        yield return new WaitForSeconds(0.07f);
        float t = 0, dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            robber.anchoredPosition = robberHome + new Vector2(-30f * Bump(k), 0);
            robber.localRotation = Quaternion.Euler(0, 0, 8f * Bump(k));
            yield return null;
        }
        ResetPose(robber);
    }

    IEnumerator Knockback()
    {
        yield return new WaitForSeconds(0.07f);
        var img = robber.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 0.55f, 0.55f);
        float t = 0, dur = 0.4f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float b = k < 0.2f ? k / 0.2f : 1f - Mathf.SmoothStep(0, 1, (k - 0.2f) / 0.8f);
            robber.anchoredPosition = robberHome + new Vector2(-80f * b, 0);
            robber.localRotation = Quaternion.Euler(0, 0, 18f * b);
            if (img != null && k > 0.4f) img.color = Color.white;
            yield return null;
        }
        if (img != null) img.color = Color.white;
        ResetPose(robber);
    }

    // ---- juice ----

    void HitStop(float seconds, float shake)
    {
        StartCoroutine(Freeze(seconds));
        Shake(shake, 0.2f);
    }

    IEnumerator Freeze(float seconds)
    {
        // everything (including the arrow clock) pauses together so timing stays fair
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(seconds);
        Time.timeScale = 1f;
    }

    void Shake(float amount, float dur)
    {
        if (stage == null) return;
        if (shakeAnim != null) StopCoroutine(shakeAnim);
        shakeAnim = StartCoroutine(DoShake(amount, dur));
    }

    IEnumerator DoShake(float amount, float dur)
    {
        float t = 0;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = amount * (1f - t / dur);
            stage.anchoredPosition = new Vector2(Random.Range(-a, a), Random.Range(-a, a));
            yield return null;
        }
        stage.anchoredPosition = Vector2.zero;
    }

    IEnumerator ArrowFeedback(Note n, bool good)
    {
        var rt = n.rect;
        var img = rt.GetComponent<Image>();
        int i = (int)n.dir;
        var swap = good ? arrowsHit[i] : arrowsOff[i];
        if (swap != null) img.sprite = swap;

        float t = 0f, dur = good ? 0.15f : 0.3f;
        Vector3 baseScale = rt.localScale;
        Vector2 start = rt.anchoredPosition;
        while (t < dur && rt != null)
        {
            t += Time.deltaTime;
            float k = t / dur;
            if (good) rt.localScale = baseScale * (1f + 0.35f * k);
            else rt.anchoredPosition = start + Vector2.left * scrollSpeed * 0.3f * t;
            var c = img.color; c.a = 1f - k; img.color = c;
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    IEnumerator FlashRing(Color c)
    {
        if (hitInner == null) yield break;
        hitInner.color = c;
        yield return new WaitForSeconds(0.08f);
        hitInner.color = innerColor;
    }

    void UpdateBars()
    {
        if (playerHealth != null) playerHealth.localScale = new Vector3(playerHp, 1, 1);
        if (gusHealth != null) gusHealth.localScale = new Vector3(gusHp, 1, 1);
    }

    void EndFight()
    {
        if (finished) return;
        finished = true;
        running = false;
        Time.timeScale = 1f;

        foreach (var n in notes)
            if (n.rect != null && !n.judged) Destroy(n.rect.gameObject);

        LastFightWon = gusHp <= 0 || (playerHp > 0 && gusHp < playerHp);
        LastHits = hits;
        LastTotal = notes.Count;

        if (LastFightWon) Play(ref gusAnim, gus, KnockOver(gus, -90f));
        else Play(ref robberAnim, robber, KnockOver(robber, 90f));

        centerText.text = (LastFightWon ? "GUS IS DOWN!" : "YOU GOT KNOCKED OUT...")
            + "\n<size=40>" + hits + "/" + notes.Count + " landed  -  " + perfects + " perfect\n\npress enter</size>";
        judgeText.text = "";
    }

    IEnumerator KnockOver(RectTransform who, float angle)
    {
        float t = 0f;
        Vector2 home = who.anchoredPosition;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0, 1, t / 0.35f);
            who.localRotation = Quaternion.Euler(0, 0, angle * k);
            who.anchoredPosition = home + new Vector2(Mathf.Sign(-angle) * 40f * k, 0);
            yield return null;
        }
    }

    void OnDisable()
    {
        Time.timeScale = 1f;
    }

    void Leave()
    {
        if (string.IsNullOrEmpty(nextScene)) return;
        if (!string.IsNullOrEmpty(nextSpawn)) RoomTransition.Go(nextScene, nextSpawn);
        else SceneManager.LoadScene(nextScene);
    }

    RectTransform MakeArrow(Dir dir)
    {
        var go = new GameObject("Arrow " + dir, typeof(RectTransform));
        go.transform.SetParent(track, false);
        var img = go.AddComponent<Image>();
        img.sprite = arrows[(int)dir];
        img.preserveAspect = true;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.sizeDelta = new Vector2(arrowSize, arrowSize);
        if (dir == Dir.Right) rt.localScale = new Vector3(-1, 1, 1);
        return rt;
    }
}
