using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlaySceneController : MonoBehaviour
{
    [Header("Refs")]
    public Transform elementsParent;
    public float cellSize = 1f;
    public string mainMenuSceneName = "MainMenu";
    public GameObject startButton;  // Run
    public GameObject stopButton;   // Stop
    public Camera playCamera;       // Caméra de la scène (sinon Camera.main)

    [Header("Prefabs (mêmes que l’éditeur)")]
    public GameObject LinePrefab;
    public GameObject LineOneUsePrefab;
    public GameObject LineSpawnablePrefab;
    public GameObject ArrowPrefab;
    public GameObject TrianglePrefab;
    public GameObject TriangleOneUsePrefab;
    public GameObject TriangleSpawnablePrefab;
    public GameObject StarPrefab;
    public GameObject TeleporterPrefab;
    public GameObject EndPointPrefab;
    public GameObject BallSpawnPrefab;
    public GameObject ballRuntimePrefab;

    [Header("Masks")]
    public LayerMask interactableMask;   // Placeable

    LevelData _data;
    GameObject _runtimeBall;
    int _starsNeeded, _starsCollected;
    bool _running;

    Dictionary<string, GameObject> _dict;
    
    public static PlaySceneController Instance { get; private set; }
    
    readonly System.Collections.Generic.List<Renderer> _spawnRenderers = new();
    
    // étoiles masquées pendant la run
    private readonly System.Collections.Generic.List<GameObject> _hiddenStars = new();

    const string PROG_KEY = "play_progress";

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
        _dict = new Dictionary<string, GameObject> {
            {"Line", LinePrefab},
            {"LineOneUse", LineOneUsePrefab},
            {"LineSpawnable", LineSpawnablePrefab},
            {"Arrow", ArrowPrefab},
            {"Triangle", TrianglePrefab},
            {"TriangleOneUse", TriangleOneUsePrefab},
            {"TriangleSpawnable", TriangleSpawnablePrefab},
            {"Star", StarPrefab},
            {"Teleporter", TeleporterPrefab},
            {"EndPoint", EndPointPrefab},
            {"BallSpawn", BallSpawnPrefab},
        };

        if (!playCamera) playCamera = Camera.main;
        if (startButton) startButton.SetActive(true);
        if (stopButton)  stopButton.SetActive(false);
        
    }

    void Start()
    {
        // --- MODE DISQUE (depuis LevelBrowser) ---
        if (!string.IsNullOrEmpty(PlayBridge.diskPathToLoad))
        {
            var data = LevelIO.Load(PlayBridge.diskPathToLoad);
            if (data == null)
            {
                PopupService.Instance?.Error("Illisible", "Fichier de niveau introuvable/corrompu.");
                return;
            }

            _data = data;
            BuildFromData(_data);
            RandomizeRotatables();
            ResolveTeleporters();

            _starsNeeded = elementsParent.GetComponentsInChildren<StarPickup>(true).Length;
            _starsCollected = 0;

            // En mode “disque”, pas de progression campagne :
            startButton?.SetActive(true);
            stopButton?.SetActive(false);
            return;
        }
        
        // Charger la data depuis Resources/PlayLevels/<PlayBridge.levelResourceName>.json
        if (string.IsNullOrEmpty(PlayBridge.levelResourceName))
        {
            PopupService.Instance?.Error("Aucun niveau", "Reviens au menu.");
            return;
        }

        var ta = Resources.Load<TextAsset>("PlayLevels/" + PlayBridge.levelResourceName);
        if (!ta) { PopupService.Instance?.Error("Introuvable", PlayBridge.levelResourceName); return; }

        _data = JsonUtility.FromJson<LevelData>(ta.text);
        if (_data == null) { PopupService.Instance?.Error("Illisible", PlayBridge.levelResourceName); return; }

        BuildFromData(_data);
        RandomizeRotatables();
        ResolveTeleporters();

        _starsNeeded = elementsParent.GetComponentsInChildren<StarPickup>(true).Length;
        _starsCollected = 0;
    }

    void Update()
    {
        // rotation clic droit uniquement avant de lancer
        if (_running) return;
        if (Input.GetMouseButtonDown(1))
            TryRotateExistingAtCursor();
    }

    // ---------------- Scene build ----------------
    void BuildFromData(LevelData data)
    {
        foreach (Transform c in elementsParent) Destroy(c.gameObject);

        int placeableLayer = LayerMask.NameToLayer("Placeable");

        foreach (var e in data.elements)
        {
            if (!_dict.TryGetValue(e.prefabKey, out var prefab) || !prefab) continue;

            Vector3 pos = new Vector3(e.gridPos.x * cellSize, e.gridPos.y * cellSize, 0f);
            var go = Instantiate(prefab, pos, Quaternion.Euler(0, 0, e.rotationSteps * 90f), elementsParent);
            go.name = e.prefabKey;

            // sécurité collider
            var col = go.GetComponentInChildren<Collider2D>();
            if (!col) { col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; }

            // layer pour raycasts gameplay
            if (placeableLayer >= 0) SetLayerRecursively(go, placeableLayer);

            // placeable + data
            var plc = go.GetComponent<Placeable>() ?? go.AddComponent<Placeable>();
            plc.data = new ElementData {
                guid = string.IsNullOrEmpty(e.guid) ? System.Guid.NewGuid().ToString() : e.guid,
                prefabKey = e.prefabKey,
                gridPos = e.gridPos,
                rotationSteps = e.rotationSteps,
                isBreakable = e.isBreakable,
                isActivable = e.isActivable,
                pairId = e.pairId
            };

            // rotatable seulement si autorisé
            if (IsRotatableKey(e.prefabKey) && go.GetComponent<Rotatable>() == null)
                go.AddComponent<Rotatable>();
        }
    }

    void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    bool IsRotatableKey(string key)
        => key != "EndPoint" && key != "BallSpawn" && key != "Teleporter" && key != "Star";

    bool _randomizedLogged = false;

    void RandomizeRotatables()
    {
        var rots = elementsParent.GetComponentsInChildren<Rotatable>(true);
        int count = 0;
        foreach (var r in rots)
        {
            // 0..3 quarts de tour
            int steps = Random.Range(0, 4);
            if (steps > 0) count++;
            for (int i = 0; i < steps; i++) r.RotateStep();

            // sync LevelData pour que d’autres scripts ne remettent pas la valeur d’origine
            var plc = r.GetComponent<Placeable>();
            if (plc && plc.data != null)
                plc.data.rotationSteps = ((int)Mathf.Round(r.transform.eulerAngles.z / 90f)) & 3;
        }
        if (!_randomizedLogged) { Debug.Log($"[Play] Rotatables: {rots.Length} | Rotated: {count}"); _randomizedLogged = true; }
    }


    void ResolveTeleporters()
    {
        var map = new Dictionary<string, List<TeleporterElement>>();

        foreach (var tel in elementsParent.GetComponentsInChildren<TeleporterElement>(true))
        {
            var plc = tel.GetComponent<Placeable>();
            string pid = !string.IsNullOrEmpty(tel.pairId) ? tel.pairId
                        : (plc && plc.data != null ? plc.data.pairId : null);

            if (string.IsNullOrEmpty(pid)) { tel.paired = null; tel.ResetTint(); continue; }

            tel.pairId = pid;
            tel.SetTint(ColorForPair(pid));

            if (!map.TryGetValue(pid, out var list)) { list = new List<TeleporterElement>(); map[pid] = list; }
            list.Add(tel);
        }

        foreach (var kv in map)
        {
            var list = kv.Value;
            if (list.Count == 2) { list[0].paired = list[1]; list[1].paired = list[0]; }
            else foreach (var t in list) t.paired = null;
        }
    }

    Color ColorForPair(string id)
    {
        unchecked { int h = id.GetHashCode(); float hue = ((h & 0x7fffffff) % 360) / 360f; return Color.HSVToRGB(hue, 0.7f, 1f); }
    }

    // ---------------- Rotation pré-run ----------------
    void TryRotateExistingAtCursor()
    {
        if (!playCamera) return;

        Vector2 p = playCamera.ScreenToWorldPoint(Input.mousePosition);

        var hits = Physics2D.OverlapPointAll(p, interactableMask);
        if (hits == null || hits.Length == 0)
            hits = Physics2D.OverlapCircleAll(p, cellSize * 0.35f, interactableMask);
        if (hits == null || hits.Length == 0) return;

        Placeable target = null;
        float best = float.MaxValue;
        foreach (var h in hits)
        {
            var plc = h.GetComponentInParent<Placeable>();
            if (!plc) continue;
            float d = Vector2.Distance(plc.transform.position, p);
            if (d < best) { best = d; target = plc; }
        }
        if (!target) return;

        var rot = target.GetComponent<Rotatable>();
        if (!rot) return; // non rotatable (tp, star, spawn, endpoint)

        rot.RotateStep();

        if (target.data != null)
            target.data.rotationSteps = ((int)Mathf.Round(target.transform.eulerAngles.z / 90f)) & 3;

        AudioManager.Instance?.PlayHit(0.6f);
    }

    // ---------------- Run / Stop ----------------
    public void OnStartClicked()
    {
        if (_running) return;
        _running = true;
        if (startButton) startButton.SetActive(false);
        if (stopButton)  stopButton.SetActive(true);
        
        // reset collecte et ré-affiche TOUTES les étoiles
        RestoreStars();
        foreach (var st in elementsParent.GetComponentsInChildren<StarPickup>(true))
            st.gameObject.SetActive(true);
        
        HideSpawnRenderers();

        // reset runtime…
        _starsCollected = 0;
        foreach (var s in elementsParent.GetComponentsInChildren<StarPickup>(true)) s.gameObject.SetActive(true);
        foreach (var b in elementsParent.GetComponentsInChildren<BreakableOnce>(true)) b.Restore();
        foreach (var a in elementsParent.GetComponentsInChildren<Activable>(true)) a.ResetRuntimeState();

        // spawn
        Vector3? spawn = null;
        foreach (Transform t in elementsParent)
        {
            var plc = t.GetComponent<Placeable>();
            if (plc && plc.data != null && plc.data.prefabKey == "BallSpawn") { spawn = t.position; break; }
        }
        if (spawn == null) { PopupService.Instance?.Error("Niveau invalide", "Pas de BallSpawn"); OnStopClicked(); return; }

        _runtimeBall = Instantiate(ballRuntimePrefab, spawn.Value, Quaternion.identity);
        var runner = _runtimeBall.GetComponent<BallRunner>();
        //runner.interactableMask = interactableMask;
        runner.dir = Vector2.down;

        // ⬇️ important
        runner.StopRun();   // sécurité si le prefab avait auto-start
        runner.StartRun();  // lance vraiment la simulation
    }

    public void OnStopClicked()
    {
        if (_runtimeBall) { var r = _runtimeBall.GetComponent<BallRunner>(); if (r) r.StopRun(); Destroy(_runtimeBall); _runtimeBall = null; }
        _running = false;
        
        ShowSpawnRenderers();

        foreach (var s in elementsParent.GetComponentsInChildren<StarPickup>(true)) s.gameObject.SetActive(true);
        foreach (var b in elementsParent.GetComponentsInChildren<BreakableOnce>(true)) b.Restore();
        foreach (var a in elementsParent.GetComponentsInChildren<Activable>(true)) a.ResetRuntimeState();
        
        foreach (var r in _spawnRenderers) if (r) r.enabled = true;
        _spawnRenderers.Clear();

        if (startButton) startButton.SetActive(true);
        if (stopButton)  stopButton.SetActive(false);
    }

    // ---------------- Callbacks des éléments ----------------
    public void OnCollectStar() { _starsCollected++; }

    public void OnReachedEndpoint(BallRunner ball)
    {
        bool win = (_starsCollected >= _starsNeeded);
        if (ball) { ball.StopRun(); Destroy(ball.gameObject); _runtimeBall = null; }
        _running = false;

        if (win)
        {   
            
            // Mode disque (depuis LevelBrowser) => pas de progression campagne
            if (!string.IsNullOrEmpty(PlayBridge.diskPathToLoad) || PlayBridge.levelIndex < 0)
            {
                PopupService.Instance?.ShowWithAction(
                    "Niveau terminé",
                    "Bravo !",
                    "Retour menu",
                    () => SceneManager.LoadScene(mainMenuSceneName),
                    PopupType.Success
                );
                return;
            }
            AudioManager.Instance?.PlayGoal();

            // On regarde s'il y a un prochain niveau
            int cur = PlayBridge.levelIndex;
            SaveProgress(cur);
            var nextTa = Resources.Load<TextAsset>("PlayLevels/Level" + (cur + 2));

            if (nextTa)
            {
                PopupService.Instance?.ShowWithAction(
                    "Niveau terminé",
                    "Bravo !",
                    "Niveau suivant",
                    () => {
                        PlayBridge.levelIndex = cur + 1;
                        PlayBridge.levelResourceName = nextTa.name;
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    },
                    PopupType.Success
                );
            }
            else
            {
                PopupService.Instance?.ShowWithAction(
                    "GG !",
                    "Tu as fini tous les niveaux",
                    "Retour menu",
                    () => SceneManager.LoadScene(mainMenuSceneName),
                    PopupType.Success
                );
            }
        }
        else
        {
            RestoreStars();
            ShowSpawnRenderers(); 
            AudioManager.Instance?.PlayFail();
            PopupService.Instance?.Error("Raté", "Il faut toutes les étoiles.");
            if (startButton) startButton.SetActive(true);
            if (stopButton)  stopButton.SetActive(false);
        }
    }

    public void OnBallOutOfBounds(BallRunner ball)
    {
        if (ball) { ball.StopRun(); Destroy(ball.gameObject); _runtimeBall = null; }
        _running = false;
        
        RestoreStars();
        ShowSpawnRenderers();           // ← ré-affiche le BallSpawn
        AudioManager.Instance?.PlayFail();
        PopupService.Instance?.Error("Hors écran", "Réessaie !");
        Shake(0.25f, 0.35f);

        if (startButton) startButton.SetActive(true);
        if (stopButton)  stopButton.SetActive(false);
    }

    // ---------------- Progression ----------------
    void UnlockNextAndOffer()
    {
        const string PROG_KEY = "play_progress";
        int cur = PlayBridge.levelIndex;
        int best = PlayerPrefs.GetInt(PROG_KEY, -1);
        if (cur > best) { PlayerPrefs.SetInt(PROG_KEY, cur); PlayerPrefs.Save(); }

        var nextTa = Resources.Load<TextAsset>("PlayLevels/Level" + (cur + 2));
        if (!nextTa)
        {
            PopupService.Instance?.Info("GG !", "Tu as fini tous les niveaux ⚡");
            if (startButton) startButton.SetActive(true);
            if (stopButton)  stopButton.SetActive(false);
            return;
        }

        PlayBridge.levelIndex = cur + 1;
        PlayBridge.levelResourceName = nextTa.name;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnBackToMenu() => SceneManager.LoadScene(mainMenuSceneName);
    
    public void RegisterCollectedStar(GameObject starGO)
    {
        if (starGO && !_hiddenStars.Contains(starGO))
            _hiddenStars.Add(starGO);
        _starsCollected++;
    }

    void RestoreStars()
    {
        foreach (var s in _hiddenStars) if (s) s.SetActive(true);
        _hiddenStars.Clear();
        _starsCollected = 0;
    }
    
    [Header("Camera FX")]
    public CameraShake2D shaker;

    void Shake(float dur, float mag)
    {
        if (!shaker) shaker = (playCamera ? playCamera.GetComponent<CameraShake2D>() : null);
        if (!shaker && Camera.main) shaker = Camera.main.GetComponent<CameraShake2D>();
        shaker?.Shake(dur, mag);
    }

    void SaveProgress(int levelIndex)
    {
        int best = PlayerPrefs.GetInt(PROG_KEY, -1);
        if (levelIndex > best)
        {
            PlayerPrefs.SetInt(PROG_KEY, levelIndex);
            PlayerPrefs.Save();
        }
    }
    
    // Cache/affiche tous les renderers marqués SpawnMarker pendant la run
    void HideSpawnRenderers()
    {
        _spawnRenderers.Clear();
        foreach (var marker in elementsParent.GetComponentsInChildren<SpawnMarker>(true))
        {
            foreach (var r in marker.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue;
                r.enabled = false;
                _spawnRenderers.Add(r);
            }
        }
    }

    void ShowSpawnRenderers()
    {
        foreach (var r in _spawnRenderers) if (r) r.enabled = true;
        _spawnRenderers.Clear();
    }
}
