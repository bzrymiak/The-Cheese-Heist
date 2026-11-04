using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// laser vault run. side view, the robber creeps down the vault corridor towards the safe while
// laser turrets at the end of the hall fire at him in time with the arrows.
// up = jump a low beam, down = duck a high beam, long arrow = hold down and slide under a long beam.
// zaps fill the alarm meter, fill it and you're caught (restart). reach the safe and you're through.
public class LaserDodge : MonoBehaviour
{
    public enum Dir { Up, Down }

    class Note
    {
        public Dir dir;
        public float time;
        public float hold;          // 0 = tap
        public RectTransform rect, tail;
        public int state;           // 0 waiting, 1 being held, 2 done
        public bool zapped;
        // the laser shot coming at him
        public RectTransform bolt;
        public Image core, glow, halo;
        public bool fired;
    }

    [Header("UI")]
    public RectTransform stage;
    public RectTransform world;        // the corridor, slides left as you run
    public RectTransform boltLayer;    // shots fly in here (doesn't scroll)
    public RectTransform turretLow, turretHigh;
    public Image ledLow, ledHigh, muzzleLow, muzzleHigh;
    public RectTransform safe;         // inside world, sits at the end
    public RectTransform track;
    public RectTransform hitCircle;
    public Image hitRing;
    public Image guide;
    public Text judgeText;
    public Text centerText;
    public RectTransform progressFill;
    public Text progressText;
    public RectTransform alarmFill;
    public RectTransform robber;
    public Image redPulse;
    public Image siren;
    public GameObject introCard;

    [Header("Arrow art (up, down)")]
    public Sprite arrowUp, arrowDown, arrowUpHit, arrowDownHit, arrowUpOff, arrowDownOff;
    public Sprite beamSprite;   // pixel laser strip, same look as the beams in the figma vault
    public Sprite sparkSprite;  // pixel spark for hits + muzzle flash
    public float arrowSize = 110f;

    [Header("Timing")]
    public float scrollSpeed = 600f;   // arrow track
    public float runSpeed = 300f;      // how fast the corridor goes by
    public float perfectWindow = 0.07f;
    public float okWindow = 0.17f;
    public float ignoreWindow = 0.3f;
    public float chargeTime = 0.45f;   // how long a turret charges before it shoots
    public float boltSpeed = 1500f;    // how fast the shots travel
    public float countdown = 3f;

    [Header("Pattern")]
    public int noteCount = 24;
    public float gap = 0.75f;
    public bool endWithBursts = false; // second pass on the way out ends with quick hop-hop-hops
    public int seed = 12;
    public int lives = 3;              // zaps before the alarm goes off for real

    [Header("Layout")]
    public float robberX = -420f;
    public float floorY = -360f;
    public float lowBeamY = -318f, highBeamY = -165f;

    [Header("After")]
    public string nextScene = "LaserVault";
    public string nextSpawn = "past_lasers";

    public static bool Passed;

    readonly List<Note> notes = new List<Note>();
    float startTime, runTime, endTime;
    bool started, running, finished, won, ducking, animating;
    int cleared, zaps;
    float judgeTimer, sirenTimer, timeScaleBase = 1f;
    Coroutine robberAnim, shakeAnim, knockAnim;
    Image robberImg;
    Color ringColor;

    void Start()
    {
        Time.timeScale = 1f;
        BuildPattern();

        robberImg = robber.GetComponent<Image>();
        if (hitRing != null) ringColor = hitRing.color;
        if (redPulse != null) redPulse.color = new Color(1, 0, 0, 0);
        if (siren != null) siren.color = new Color(1, 0.15f, 0.15f, 0);
        judgeText.text = "";
        centerText.text = "";

        var last = notes[notes.Count - 1];
        endTime = last.time + last.hold;
        if (safe != null) safe.anchoredPosition = new Vector2(robberX + (endTime + 1.6f) * runSpeed, safe.anchoredPosition.y);

        robber.anchoredPosition = new Vector2(robberX, floorY);
        UpdateHud();
        if (introCard != null) introCard.SetActive(true);
        else Begin();
    }

    void Begin()
    {
        if (introCard != null) introCard.SetActive(false);
        startTime = Time.time + countdown;
        started = running = true;
    }

    void BuildPattern()
    {
        var rng = new System.Random(seed);
        float t = 0.8f;
        for (int i = 0; i < noteCount; i++)
        {
            float g = Mathf.Lerp(gap * 1.3f, gap * 0.75f, i / (float)noteCount);

            if (i < 4)
            {
                notes.Add(new Note { dir = i % 2 == 0 ? Dir.Up : Dir.Down, time = t });
                t += g;
                continue;
            }

            if (rng.NextDouble() < 0.22)
            {
                float hold = 0.6f + (float)rng.NextDouble() * 0.6f;
                notes.Add(new Note { dir = Dir.Down, time = t, hold = hold });
                t += hold + g;
                continue;
            }

            notes.Add(new Note { dir = rng.NextDouble() < 0.5 ? Dir.Up : Dir.Down, time = t });
            t += g;
        }

        if (endWithBursts)
        {
            for (int b = 0; b < 2; b++)
            {
                for (int k = 0; k < 3; k++) { notes.Add(new Note { dir = Dir.Up, time = t }); t += 0.2f; }
                t += gap;
            }
        }
    }

    void Update()
    {
        var kb = Keyboard.current;

        if (!started)
        {
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) Begin();
            return;
        }

        if (finished)
        {
            if (won) ScrollToSafe();
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            {
                if (won) Leave();
                else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            UpdateSiren();
            return;
        }
        if (!running) return;

        float t = Time.time - startTime;
        centerText.text = t < 0 ? Mathf.CeilToInt(-t).ToString() : "";
        runTime = Mathf.Max(0, t);

        world.anchoredPosition = new Vector2(-runTime * runSpeed, 0);
        MoveNotes(t);
        UpdateTurrets(t);
        UpdateBolts(t);
        UpdateGuide();
        if (t >= 0) ReadInput(t);
        UpdateHolds(t);
        CheckMissed(t);
        SlowMoOnLast(t);
        WalkBob(t);
        UpdateSiren();

        if (judgeTimer > 0 && (judgeTimer -= Time.unscaledDeltaTime) <= 0) judgeText.text = "";
        if (redPulse != null && redPulse.color.a > 0)
            redPulse.color = new Color(1, 0, 0, Mathf.MoveTowards(redPulse.color.a, 0, Time.unscaledDeltaTime * 1.2f));

        if (notes.TrueForAll(n => n.state == 2)) End(true);
    }

    // ---- arrows ----

    void MoveNotes(float t)
    {
        float hitX = hitCircle.anchoredPosition.x;
        float rightEdge = track.rect.width / 2f + arrowSize;

        foreach (var n in notes)
        {
            if (n.state == 2) continue;
            float x = n.state == 1 ? hitX : hitX + (n.time - t) * scrollSpeed;
            if (n.rect == null)
            {
                if (x > rightEdge) continue;
                MakeArrow(n);
            }
            n.rect.anchoredPosition = new Vector2(x, hitCircle.anchoredPosition.y);

            if (n.tail != null)
            {
                float len = n.state == 1 ? (n.time + n.hold - t) * scrollSpeed : n.hold * scrollSpeed;
                n.tail.sizeDelta = new Vector2(Mathf.Max(0, len), arrowSize * 0.38f);
            }
        }
    }

    void UpdateGuide()
    {
        if (guide == null) return;
        Note next = NextNote();
        guide.enabled = next != null;
        if (next == null) return;
        var off = next.dir == Dir.Up ? arrowUpOff : arrowDownOff;
        guide.sprite = off != null ? off : (next.dir == Dir.Up ? arrowUp : arrowDown);
    }

    Note NextNote()
    {
        Note next = null;
        foreach (var n in notes)
            if (n.state != 2 && (next == null || n.time < next.time)) next = n;
        return next;
    }

    void ReadInput(float t)
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Press(Dir.Up, t);
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Press(Dir.Down, t);
    }

    bool Held(Dir d)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        return d == Dir.Up ? kb.upArrowKey.isPressed || kb.wKey.isPressed : kb.downArrowKey.isPressed || kb.sKey.isPressed;
    }

    void Press(Dir dir, float t)
    {
        Note target = null;
        foreach (var n in notes)
        {
            if (n.state != 0) continue;
            if (target == null || Mathf.Abs(n.time - t) < Mathf.Abs(target.time - t)) target = n;
        }
        if (target == null) return;

        float diff = target.time - t;
        if (Mathf.Abs(diff) > ignoreWindow) return;

        if (target.dir != dir) { Zap(target, "WRONG WAY!"); return; }
        if (Mathf.Abs(diff) > okWindow) { Zap(target, diff > 0 ? "TOO EARLY!" : "TOO SLOW!"); return; }

        bool perfect = Mathf.Abs(diff) <= perfectWindow;

        if (target.hold > 0)
        {
            target.state = 1;
            ducking = true;
            Play(Slide());
            ShowJudge("HOLD IT!", Color.white, 99f);
            SwapArrow(target, true);
            return;
        }

        Clear(target, perfect ? "PERFECT!" : "CLOSE ONE!", perfect);
    }

    void UpdateHolds(float t)
    {
        foreach (var n in notes)
        {
            if (n.state != 1) continue;
            if (t >= n.time + n.hold) { ducking = false; Clear(n, "PERFECT!", true); }
            else if (!Held(n.dir)) { ducking = false; Zap(n, "LET GO TOO SOON!"); }
        }
    }

    void CheckMissed(float t)
    {
        foreach (var n in notes)
            if (n.state == 0 && t - n.time > okWindow) Zap(n, "MISSED!");
    }

    void Clear(Note n, string label, bool perfect)
    {
        n.state = 2;
        cleared++;
        Color gold = new Color(1f, 0.85f, 0.2f);
        ShowJudge(label, perfect ? gold : Color.white, 0.5f);
        StartCoroutine(Pop(n, true));
        if (n.hold <= 0) Play(n.dir == Dir.Up ? Jump() : Duck());
        else Play(StandUp());
        StartCoroutine(FlashRing(perfect ? gold : Color.white));
        if (perfect) HitStop(0.06f, 9f);
        UpdateHud();
    }

    void Zap(Note n, string label)
    {
        n.state = 2;
        n.zapped = true;
        StartCoroutine(Burst(n));
        cleared++;
        zaps++;
        ShowJudge(label, new Color(1f, 0.45f, 0.45f), 0.6f);
        StartCoroutine(Pop(n, false));
        Play(Zapped());
        if (knockAnim != null) StopCoroutine(knockAnim);
        knockAnim = StartCoroutine(KnockBack());
        if (redPulse != null) redPulse.color = new Color(1, 0, 0, 0.35f);
        sirenTimer = 1.4f;
        Shake(16f, 0.25f);
        UpdateHud();
        if (zaps >= lives) End(false);
    }

    void ShowJudge(string text, Color c, float time)
    {
        judgeText.text = text;
        judgeText.color = c;
        judgeTimer = time;
    }

    void UpdateHud()
    {
        float p = notes.Count == 0 ? 1 : cleared / (float)notes.Count;
        if (progressFill != null) progressFill.localScale = new Vector3(p, 1, 1);
        if (progressText != null) progressText.text = Mathf.RoundToInt(p * 100) + "%";
        if (alarmFill != null) alarmFill.localScale = new Vector3(Mathf.Clamp01(zaps / (float)lives), 1, 1);
    }

    // last beam goes into slow motion for a sec
    void SlowMoOnLast(float t)
    {
        var last = notes[notes.Count - 1];
        bool slow = last.state == 0 && last.time - t < 0.35f && last.time - t > -okWindow;
        timeScaleBase = slow ? 0.45f : 1f;
        if (Time.timeScale > 0) Time.timeScale = timeScaleBase;
    }

    // ---- the traps in the corridor ----

    // when a shot has to leave the turret so it reaches him right on the beat
    float FireTime(Note n, RectTransform turret)
    {
        return n.time - (turret.anchoredPosition.x - robberX) / boltSpeed;
    }

    RectTransform TurretFor(Note n) => n.dir == Dir.Up ? turretLow : turretHigh;

    // turrets blink red while idle, then flash green and glow at the muzzle right before they fire
    void UpdateTurrets(float t)
    {
        ChargeLane(t, Dir.Up, turretLow, ledLow, muzzleLow);
        ChargeLane(t, Dir.Down, turretHigh, ledHigh, muzzleHigh);
    }

    void ChargeLane(float t, Dir lane, RectTransform turret, Image led, Image muzzle)
    {
        if (turret == null) return;

        Note next = null;
        foreach (var n in notes)
            if (!n.fired && n.dir == lane && (next == null || n.time < next.time)) next = n;

        float k = 0f;
        if (next != null)
        {
            float until = FireTime(next, turret) - t;
            if (until < chargeTime) k = Mathf.Clamp01(1f - until / chargeTime);
        }

        if (led != null)
        {
            bool blink = k > 0 ? Mathf.Sin(Time.time * Mathf.Lerp(20f, 70f, k)) > 0 : Mathf.PingPong(Time.time * 1.5f, 1f) > 0.5f;
            led.color = k > 0 ? new Color(0.4f, 1f, 0.5f, blink ? 1f : 0.3f) : new Color(1f, 0.25f, 0.25f, blink ? 1f : 0.35f);
        }
        if (muzzle != null)
        {
            muzzle.color = new Color(1f, 1f, 1f, k);
            muzzle.rectTransform.localScale = Vector3.one * (0.4f + 0.8f * k);
        }
    }

    void UpdateBolts(float t)
    {
        foreach (var n in notes)
        {
            var turret = TurretFor(n);
            if (turret == null) continue;

            if (!n.fired && t >= FireTime(n, turret))
            {
                n.fired = true;
                MakeBolt(n, turret);
                StartCoroutine(Recoil(turret));
                Shake(3f, 0.08f);
            }
            if (n.bolt == null) continue;

            // front of the shot reaches the robber exactly at n.time, long beams trail behind it
            float front = robberX + (n.time - t) * boltSpeed;
            n.bolt.anchoredPosition = new Vector2(front, n.bolt.anchoredPosition.y);

            // a long beam keeps pouring out of the turret until its tail has left
            if (n.hold > 0)
            {
                float tailX = robberX + (n.time + n.hold - t) * boltSpeed;
                float maxLen = turret.anchoredPosition.x - front;
                n.bolt.sizeDelta = new Vector2(Mathf.Clamp(tailX - front, 0, maxLen), n.bolt.sizeDelta.y);
            }

            // buzzes: thickness jitters, brightness strobes, halo pulses. goes red once it hits him
            bool strobe = Mathf.FloorToInt(Time.time * 24f + n.time * 7f) % 2 == 0;
            float thick = strobe ? 1.12f : 0.92f;
            n.core.rectTransform.localScale = new Vector3(1f, thick, 1f);
            Color hot = n.zapped ? new Color(1f, 0.4f, 0.4f) : (strobe ? Color.white : new Color(0.8f, 1f, 0.8f));
            n.core.color = hot;
            n.glow.color = strobe ? Color.white : (n.zapped ? new Color(1f, 0.6f, 0.6f) : new Color(0.75f, 1f, 0.75f));
            Color haloCol = n.zapped ? new Color(1f, 0.2f, 0.2f) : new Color(0.3f, 1f, 0.35f);
            haloCol.a = 0.18f + 0.12f * Mathf.Sin(Time.time * 30f);
            n.halo.color = haloCol;

            // gone off the left side
            if (front + n.bolt.sizeDelta.x < -1100f)
            {
                Destroy(n.bolt.gameObject);
                n.bolt = null;
            }
        }
    }

    void MakeBolt(Note n, RectTransform turret)
    {
        float y = turret.anchoredPosition.y;
        float len = n.hold > 0 ? 10f : 190f;

        n.bolt = new GameObject("Shot", typeof(RectTransform)).GetComponent<RectTransform>();
        n.bolt.SetParent(boltLayer, false);
        n.bolt.pivot = new Vector2(0, 0.5f);
        n.bolt.sizeDelta = new Vector2(len, 56f);
        n.bolt.anchoredPosition = new Vector2(turret.anchoredPosition.x, y);

        // faint wider band behind it so it reads as a hot beam, then the thick crisp beam on top,
        // then a white hot square head at the front
        n.halo = MakeImg(n.bolt, "Halo", null, Color.clear, Vector2.zero, Vector2.zero);
        Fill(n.halo.rectTransform, new Vector2(-6, 0), new Vector2(0, 0));
        n.core = MakeImg(n.bolt, "Beam", beamSprite, Color.white, Vector2.zero, Vector2.zero);
        Fill(n.core.rectTransform, new Vector2(0, 10), new Vector2(0, -10));
        n.glow = MakeImg(n.bolt, "Head", null, Color.white, Vector2.zero, new Vector2(26, 30));
        n.glow.rectTransform.anchorMin = n.glow.rectTransform.anchorMax = new Vector2(0, 0.5f);
        n.glow.rectTransform.anchoredPosition = new Vector2(4, 0);
    }

    static void Fill(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = min; rt.offsetMax = max;
    }

    IEnumerator Recoil(RectTransform turret)
    {
        Vector2 home = turret.anchoredPosition;
        float t = 0;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float k = t / 0.15f;
            turret.anchoredPosition = home + Vector2.right * 16f * (1f - k);
            yield return null;
        }
        turret.anchoredPosition = home;
    }

    // shot hits him, little burst and the rest of the shot is gone
    IEnumerator Burst(Note n)
    {
        float y = n.dir == Dir.Up ? lowBeamY : highBeamY;
        if (n.bolt != null) { Destroy(n.bolt.gameObject); n.bolt = null; }

        Vector2 at = new Vector2(robberX + 40f, y);
        var flash = MakeImg(boltLayer, "Spark", sparkSprite, Color.white, at, new Vector2(48, 48));

        // a few square bits fly out, pixel style
        var bits = new Image[6];
        var vel = new Vector2[6];
        for (int i = 0; i < bits.Length; i++)
        {
            bits[i] = MakeImg(boltLayer, "Bit", null, new Color(0.6f, 1f, 0.6f), at, new Vector2(10, 10));
            float a = (i / (float)bits.Length) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(250f, 450f);
        }

        float t = 0;
        while (t < 0.35f)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float k = t / 0.35f;
            flash.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(48f, 140f, k);
            flash.color = new Color(1f, 1f, 1f, 1f - k);
            for (int i = 0; i < bits.Length; i++)
            {
                bits[i].rectTransform.anchoredPosition += vel[i] * dt;
                vel[i] += Vector2.down * 900f * dt;
                bits[i].color = new Color(0.6f, 1f, 0.6f, 1f - k);
            }
            yield return null;
        }
        Destroy(flash.gameObject);
        foreach (var b in bits) Destroy(b.gameObject);
    }

    Image MakeImg(RectTransform parent, string name, Sprite s, Color c, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = s;
        img.color = c;
        img.raycastTarget = false;
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = size;
        return img;
    }

    void ScrollToSafe()
    {
        // keep running until the safe is right in front of him
        if (safe == null) return;
        float safeScreenX = safe.anchoredPosition.x + world.anchoredPosition.x;
        if (safeScreenX > robberX + 230f)
        {
            world.anchoredPosition += Vector2.left * runSpeed * Time.deltaTime;
            WalkBob(Time.time);
        }
    }

    // ---- robber ----

    void WalkBob(float t)
    {
        if (animating) return;
        if (t < 0) t = 0; // stand still during the countdown
        robber.anchoredPosition = new Vector2(robber.anchoredPosition.x, floorY + Mathf.Abs(Mathf.Sin(t * 9f)) * 8f);
        robber.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 9f) * 2.5f);
    }

    void Play(IEnumerator anim)
    {
        if (robberAnim != null) StopCoroutine(robberAnim);
        robber.localScale = Vector3.one;
        robber.localRotation = Quaternion.identity;
        robber.anchoredPosition = new Vector2(robber.anchoredPosition.x, floorY);
        if (robberImg != null) robberImg.color = Color.white;
        robberAnim = StartCoroutine(Wrap(anim));
    }

    IEnumerator Wrap(IEnumerator anim)
    {
        animating = true;
        yield return anim;
        animating = false;
    }

    IEnumerator Jump()
    {
        float t = 0, dur = 0.42f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float h = Mathf.Sin(k * Mathf.PI);
            robber.anchoredPosition = new Vector2(robber.anchoredPosition.x, floorY + 175f * h);
            // tuck the knees up at the top of the jump
            float tuck = Mathf.Sin(k * Mathf.PI);
            robber.localScale = k < 0.1f || k > 0.9f ? new Vector3(1.12f, 0.86f, 1) : new Vector3(1f - 0.06f * tuck, 1f - 0.14f * tuck, 1);
            robber.localRotation = Quaternion.Euler(0, 0, -12f * tuck);
            yield return null;
        }
        robber.localScale = Vector3.one;
        robber.localRotation = Quaternion.identity;
        robber.anchoredPosition = new Vector2(robber.anchoredPosition.x, floorY);
    }

    IEnumerator Duck()
    {
        float t = 0;
        while (t < 0.07f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.07f);
            robber.localScale = new Vector3(Mathf.Lerp(1f, 1.2f, k), Mathf.Lerp(1f, 0.5f, k), 1);
            yield return null;
        }
        yield return new WaitForSeconds(0.24f);
        yield return StandUp();
    }

    // long beam: low and leaning back, slides under it for as long as you hold
    IEnumerator Slide()
    {
        float t = 0;
        while (t < 0.08f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.08f);
            robber.localScale = new Vector3(Mathf.Lerp(1f, 1.25f, k), Mathf.Lerp(1f, 0.45f, k), 1);
            robber.localRotation = Quaternion.Euler(0, 0, 14f * k);
            yield return null;
        }
        while (ducking)
        {
            // little shuffle so it doesn't look frozen
            robber.anchoredPosition = new Vector2(robber.anchoredPosition.x, floorY + Mathf.Sin(Time.time * 30f) * 2f);
            yield return null;
        }
        yield return StandUp();
    }

    IEnumerator StandUp()
    {
        float t = 0;
        Vector3 from = robber.localScale;
        Quaternion fromRot = robber.localRotation;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float k = t / 0.12f;
            robber.localScale = Vector3.Lerp(from, Vector3.one, k);
            robber.localRotation = Quaternion.Slerp(fromRot, Quaternion.identity, k);
            yield return null;
        }
        robber.localScale = Vector3.one;
        robber.localRotation = Quaternion.identity;
    }

    IEnumerator Zapped()
    {
        float t = 0, dur = 0.5f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            bool on = Mathf.FloorToInt(t * 30f) % 2 == 0;
            if (robberImg != null) robberImg.color = on ? new Color(0.55f, 1f, 0.65f) : Color.white;
            robber.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 60f) * 9f * (1 - k));
            yield return null;
        }
        if (robberImg != null) robberImg.color = Color.white;
        robber.localRotation = Quaternion.identity;
    }

    IEnumerator KnockBack()
    {
        float t = 0, dur = 0.5f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            float b = k < 0.2f ? k / 0.2f : 1f - Mathf.SmoothStep(0, 1, (k - 0.2f) / 0.8f);
            robber.anchoredPosition = new Vector2(robberX - 70f * b, robber.anchoredPosition.y);
            yield return null;
        }
        robber.anchoredPosition = new Vector2(robberX, robber.anchoredPosition.y);
    }

    // ---- juice ----

    void UpdateSiren()
    {
        if (siren == null) return;
        if (sirenTimer > 0 || (finished && !won))
        {
            sirenTimer -= Time.unscaledDeltaTime;
            siren.rectTransform.Rotate(0, 0, -720f * Time.unscaledDeltaTime);
            siren.color = new Color(1f, 0.15f, 0.15f, 0.55f + 0.25f * Mathf.Sin(Time.unscaledTime * 20f));
        }
        else siren.color = new Color(1f, 0.15f, 0.15f, Mathf.MoveTowards(siren.color.a, 0, Time.unscaledDeltaTime * 2f));
    }

    void HitStop(float seconds, float shake)
    {
        StartCoroutine(Freeze(seconds));
        Shake(shake, 0.15f);
    }

    IEnumerator Freeze(float seconds)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(seconds);
        Time.timeScale = timeScaleBase;
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

    IEnumerator FlashRing(Color c)
    {
        if (hitRing == null) yield break;
        hitRing.color = c;
        yield return new WaitForSecondsRealtime(0.08f);
        hitRing.color = ringColor;
    }

    void SwapArrow(Note n, bool good)
    {
        var img = n.rect != null ? n.rect.GetComponent<Image>() : null;
        if (img == null) return;
        var s = n.dir == Dir.Up ? (good ? arrowUpHit : arrowUpOff) : (good ? arrowDownHit : arrowDownOff);
        if (s != null) img.sprite = s;
    }

    IEnumerator Pop(Note n, bool good)
    {
        if (n.rect == null) yield break;
        SwapArrow(n, good);
        var rt = n.rect;
        var img = rt.GetComponent<Image>();
        if (n.tail != null) Destroy(n.tail.gameObject);
        float t = 0, dur = good ? 0.15f : 0.3f;
        Vector2 start = rt.anchoredPosition;
        while (t < dur && rt != null)
        {
            t += Time.deltaTime;
            float k = t / dur;
            if (good) rt.localScale = Vector3.one * (1f + 0.35f * k);
            else rt.anchoredPosition = start + Vector2.left * scrollSpeed * 0.3f * t;
            var c = img.color; c.a = 1f - k; img.color = c;
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    void MakeArrow(Note n)
    {
        var go = new GameObject("Arrow " + n.dir, typeof(RectTransform));
        go.transform.SetParent(track, false);
        var img = go.AddComponent<Image>();
        img.sprite = n.dir == Dir.Up ? arrowUp : arrowDown;
        img.preserveAspect = true;
        img.raycastTarget = false;
        n.rect = img.rectTransform;
        n.rect.sizeDelta = new Vector2(arrowSize, arrowSize);

        if (n.hold > 0)
        {
            var tailGo = new GameObject("Tail", typeof(RectTransform));
            tailGo.transform.SetParent(n.rect, false);
            tailGo.transform.SetAsFirstSibling();
            var tail = tailGo.AddComponent<Image>();
            tail.raycastTarget = false;
            tail.color = new Color(0.7f, 0.45f, 1f, 0.75f);
            n.tail = tail.rectTransform;
            n.tail.pivot = new Vector2(0, 0.5f);
            n.tail.anchoredPosition = Vector2.zero;
        }
    }

    // ---- end ----

    void End(bool success)
    {
        if (finished) return;
        finished = true;
        running = false;
        won = success;
        Passed = success;
        ducking = false;
        Time.timeScale = 1f;
        timeScaleBase = 1f;

        foreach (var n in notes)
            if (n.rect != null && n.state != 2) Destroy(n.rect.gameObject);

        judgeText.text = "";
        if (success)
        {
            centerText.text = "MADE IT TO THE SAFE!\n<size=40>" + zaps + " zap" + (zaps == 1 ? "" : "s") + "\n\npress enter</size>";
        }
        else
        {
            Play(Zapped());
            if (redPulse != null) redPulse.color = new Color(1, 0, 0, 0.45f);
            centerText.text = "ALARM TRIPPED!\n<size=40>the lasers got you\n\npress enter to try again</size>";
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
}
