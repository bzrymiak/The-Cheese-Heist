using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// vent escape (tony bailed on you). top down maze like joann's mock: in at the top, EXIT at the bottom,
// django hunting you through the ducts. red buttons flip the vent gates: open your way through,
// or slam one shut behind you to cut django off. he catches you = caught screen, reach EXIT = solo ending.
public class VentMaze : MonoBehaviour
{
    // # wall  . duct  S start  E exit  D django
    // A/B/C buttons. a/b/c gates that start shut, x/y/z gates that start open (A flips a+x, B flips b+y, C flips c+z)
    static readonly string[] Layout =
    {
        "###########S###########",
        "#.........#.#.........#",
        "#.#######.#.#.#######.#",
        "#.#A....#...#....B..#.#",
        "#.#.###.#####.###.#.#.#",
        "#...#...x...y...#...#.#",
        "###.#.#####.#####.###.#",
        "#...#.....#D#.........#",
        "#.#####.#.#.#c#######.#",
        "#.....#.#...#.#..C..#.#",
        "#.###.#.#####.#.###.#.#",
        "#...#...a.......#...b.#",
        "###########E###########",
    };

    [Header("UI")]
    public RectTransform board;
    public Text statusText;
    public Text titleText;
    public Image flash;
    public Sprite robberSprite, djangoSprite;

    [Header("Tuning")]
    public float tile = 64f;
    public float playerStep = 0.13f;   // seconds per tile while a key is held
    public float djangoStep = 0.2f;    // a bit slower than you, but he never stops
    public float djangoDelay = 2.5f;   // head start
    public float flipCooldown = 0.4f;

    int W, H;
    char[,] map;
    bool[,] gateOpen;
    Image[,] cells;
    Vector2Int player, django, exit;
    RectTransform playerRt, djangoRt;
    float moveTimer, djangoTimer, flipTimer, startTime;
    bool over;

    static readonly Color Wall = new Color32(70, 86, 98, 255);
    static readonly Color WallEdge = new Color32(98, 118, 132, 255);
    static readonly Color Duct = new Color32(20, 22, 26, 255);
    static readonly Color GateShut = new Color32(150, 160, 170, 255);
    static readonly Color GateOpen = new Color32(40, 46, 52, 255);
    static readonly Color Button = new Color32(200, 40, 40, 255);

    void Start()
    {
        Time.timeScale = 1f;
        PauseController.SetPause(false);
        H = Layout.Length;
        W = Layout[0].Length;
        map = new char[W, H];
        gateOpen = new bool[W, H];
        cells = new Image[W, H];

        board.sizeDelta = new Vector2(W * tile, H * tile);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                char c = Layout[y][x];
                map[x, y] = c;
                gateOpen[x, y] = c == 'x' || c == 'y' || c == 'z';
                if (c == 'S') player = new Vector2Int(x, y);
                if (c == 'D') django = new Vector2Int(x, y);
                if (c == 'E') exit = new Vector2Int(x, y);
                cells[x, y] = MakeCell(x, y, c);
            }

        playerRt = MakeSprite("Robber", robberSprite, Color.white);
        djangoRt = MakeSprite("Django", djangoSprite, Color.white);
        Place(playerRt, player);
        Place(djangoRt, django);

        startTime = Time.time;
        djangoTimer = djangoDelay;
        StartCoroutine(Banner());
    }

    IEnumerator Banner()
    {
        statusText.text = "DJANGO: \"You'll never get past me!\"";
        yield return new WaitForSeconds(2.2f);
        statusText.text = "";
    }

    Image MakeCell(int x, int y, char c)
    {
        var go = new GameObject("Cell", typeof(RectTransform));
        go.transform.SetParent(board, false);
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.sizeDelta = new Vector2(tile, tile);
        rt.anchoredPosition = CellPos(new Vector2Int(x, y));

        if (c == '#')
        {
            img.color = Wall;
            // little panel seam so it reads as sheet metal
            var seam = new GameObject("Seam", typeof(RectTransform)).AddComponent<Image>();
            seam.transform.SetParent(go.transform, false);
            seam.color = WallEdge;
            seam.raycastTarget = false;
            seam.rectTransform.sizeDelta = new Vector2(tile, 4);
            seam.rectTransform.anchoredPosition = new Vector2(0, tile / 2f - 2);
        }
        else img.color = Duct;

        if (IsButton(c))
        {
            var b = new GameObject("Button", typeof(RectTransform)).AddComponent<Image>();
            b.transform.SetParent(go.transform, false);
            b.color = Button;
            b.raycastTarget = false;
            b.rectTransform.sizeDelta = new Vector2(tile * 0.55f, tile * 0.35f);
        }
        if (c == 'E' || c == 'S')
        {
            var t = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(go.transform, false);
            t.font = titleText.font;
            t.text = c == 'E' ? "EXIT" : "ENTER";
            t.fontSize = 22;
            t.color = new Color32(240, 180, 60, 255);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.rectTransform.sizeDelta = new Vector2(tile * 2, tile);
            t.rectTransform.anchoredPosition = new Vector2(0, c == 'E' ? -tile * 0.75f : tile * 0.75f);
        }
        if (IsGate(c)) PaintGate(img, x, y);
        return img;
    }

    void PaintGate(Image img, int x, int y)
    {
        img.color = gateOpen[x, y] ? GateOpen : GateShut;
    }

    RectTransform MakeSprite(string name, Sprite s, Color c)
    {
        var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(board, false);
        img.sprite = s;
        img.color = c;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.rectTransform.sizeDelta = new Vector2(tile * 0.95f, tile * 0.95f);
        return img.rectTransform;
    }

    Vector2 CellPos(Vector2Int p) => new Vector2((p.x - (W - 1) / 2f) * tile, ((H - 1) / 2f - p.y) * tile);

    void Place(RectTransform rt, Vector2Int p) => rt.anchoredPosition = CellPos(p);

    static bool IsButton(char c) => c == 'A' || c == 'B' || c == 'C';
    static bool IsGate(char c) => "abcxyz".IndexOf(c) >= 0;

    bool Walkable(Vector2Int p)
    {
        if (p.x < 0 || p.y < 0 || p.x >= W || p.y >= H) return false;
        char c = map[p.x, p.y];
        if (c == '#') return false;
        if (IsGate(c)) return gateOpen[p.x, p.y];
        return true;
    }

    void Update()
    {
        if (over) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        // you
        moveTimer -= Time.deltaTime;
        flipTimer -= Time.deltaTime;
        Vector2Int dir = Vector2Int.zero;
        if (kb.upArrowKey.isPressed || kb.wKey.isPressed) dir = Vector2Int.down;      // rows count downward
        else if (kb.downArrowKey.isPressed || kb.sKey.isPressed) dir = Vector2Int.up;
        else if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir = Vector2Int.left;
        else if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir = Vector2Int.right;

        if (dir != Vector2Int.zero && moveTimer <= 0f)
        {
            var next = player + dir;
            if (Walkable(next)) { player = next; moveTimer = playerStep; }
        }
        if (dir == Vector2Int.zero) moveTimer = 0f;

        if (kb.eKey.wasPressedThisFrame && flipTimer <= 0f && IsButton(map[player.x, player.y]))
        {
            Flip(map[player.x, player.y]);
            flipTimer = flipCooldown;
        }
        else if (IsButton(map[player.x, player.y]) && statusText.text == "")
        {
            statusText.text = "press E to flip the vent gates";
        }
        else if (!IsButton(map[player.x, player.y]) && statusText.text.StartsWith("press E")) statusText.text = "";

        // smooth slide toward the tile you're on
        playerRt.anchoredPosition = Vector2.MoveTowards(playerRt.anchoredPosition, CellPos(player), tile / playerStep * Time.deltaTime);

        // django
        djangoTimer -= Time.deltaTime;
        if (djangoTimer <= 0f)
        {
            djangoTimer = djangoStep;
            var step = NextStepTowards(django, player);
            if (step.HasValue) django = step.Value;
        }
        djangoRt.anchoredPosition = Vector2.MoveTowards(djangoRt.anchoredPosition, CellPos(django), tile / djangoStep * Time.deltaTime);
        // creepy little twitch
        djangoRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 25f) * 6f);

        if (flash != null && flash.color.a > 0)
            flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, Mathf.MoveTowards(flash.color.a, 0, Time.deltaTime * 2f));

        if (django == player) StartCoroutine(Caught());
        else if (player == exit) StartCoroutine(Escaped());
    }

    void Flip(char button)
    {
        char shut = char.ToLower(button);                 // a b c
        char open = (char)('x' + (button - 'A'));         // x y z
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                char c = map[x, y];
                if (c != shut && c != open) continue;
                // can't close a gate on top of someone, that'd be mean
                var p = new Vector2Int(x, y);
                if (gateOpen[x, y] && (p == player || p == django)) continue;
                gateOpen[x, y] = !gateOpen[x, y];
                PaintGate(cells[x, y], x, y);
            }
        if (flash != null) flash.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        statusText.text = "*CLUNK*";
        StartCoroutine(ClearStatus(0.6f));
    }

    IEnumerator ClearStatus(float t)
    {
        yield return new WaitForSeconds(t);
        if (!over) statusText.text = "";
    }

    // shortest path through the ducts (respecting gates). if you've sealed him off he just paces
    Vector2Int? NextStepTowards(Vector2Int from, Vector2Int to)
    {
        var prev = new Dictionary<Vector2Int, Vector2Int>();
        var q = new Queue<Vector2Int>();
        q.Enqueue(from);
        prev[from] = from;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            if (c == to) break;
            foreach (var d in dirs)
            {
                var n = c + d;
                if (prev.ContainsKey(n) || !Walkable(n)) continue;
                prev[n] = c;
                q.Enqueue(n);
            }
        }
        if (!prev.ContainsKey(to))
        {
            // no way through, wander a step
            foreach (var d in dirs) if (Walkable(from + d) && Random.value < 0.3f) return from + d;
            return null;
        }
        var step = to;
        while (prev[step] != from) step = prev[step];
        return step;
    }

    IEnumerator Caught()
    {
        over = true;
        statusText.text = "DJANGO: \"GOTCHA!\"";
        if (flash != null) flash.color = new Color(1f, 0f, 0f, 0.5f);
        yield return new WaitForSeconds(1.1f);
        GameOver.Show(GameOver.Kind.Vent, SceneManager.GetActiveScene().name);
    }

    IEnumerator Escaped()
    {
        over = true;
        statusText.text = "You squeeze out of the vent... fresh air!";
        yield return new WaitForSeconds(1.4f);
        Ending.Show(false);
    }
}
