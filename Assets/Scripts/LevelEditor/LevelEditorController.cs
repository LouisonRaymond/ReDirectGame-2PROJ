using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LevelEditorController : MonoBehaviour
{
    public static LevelEditorController Instance { get; private set; }
    
    public enum Tool { Place, Delete }
    public Tool currentTool = Tool.Place;

    [Header("Refs")]
    public Camera editorCamera;
    public float cellSize = 1f;
    public Transform elementsParent; 

    [Header("Prefabs")]
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

    [Header("Layers")]
    public LayerMask placeableMask;                 
    public string placeableLayerName = "Placeable"; 

    [Header("Data")]
    public LevelData currentLevelData = new LevelData();

    [Header("Playtest")]
    public GameObject ballRuntimePrefab;     
    public GameObject stopTestButton;        
    
    private Dictionary<string, GameObject> _prefabDict;
    private string _selectedKey;
    private GameObject _previewObj;

    private GameObject _runtimeBall;
    private int _starsNeeded;
    private int _starsCollected;
    private bool _isPlaytesting = false;
    
    private readonly List<Renderer> _hiddenSpawnRenderers = new();
    
    private readonly System.Collections.Generic.List<BreakableOnce> _brokenDuringTest = new();
    
    [Header("Rules")]
    public bool requireExactlyThreeStars = true; 
    
    
    private readonly System.Collections.Generic.HashSet<string> _nonRotatableKeys =
        new System.Collections.Generic.HashSet<string> { "EndPoint", "BallSpawn", "Teleporter", "Star" };

    private bool IsRotatableKey(string key) => !_nonRotatableKeys.Contains(key);
    
    [SerializeField] private NamePromptPanel namePrompt;
    
    

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        
        _prefabDict = new Dictionary<string, GameObject>()
        {
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

        if (currentLevelData == null) currentLevelData = new LevelData();
        if (string.IsNullOrEmpty(currentLevelData.levelName))
            currentLevelData.levelName = "NewLevel";

        if (stopTestButton) stopTestButton.SetActive(false);
        
        if (namePrompt == null)
            namePrompt = Object.FindFirstObjectByType<NamePromptPanel>();
        
        Debug.Log("[LevelEditor] NamePromptPanel found? " + (namePrompt != null));
    }

    void Update()
    {
        if (_isPlaytesting && Input.GetKeyDown(KeyCode.Escape))
        {
            OnStopTestClicked();
            return;
        }
        
        if (_isPlaytesting) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (currentTool == Tool.Delete)
        {
            HandleDelete();
            return;
        }
        
        
        if (_previewObj == null && Input.GetMouseButtonDown(1) && currentTool != Tool.Delete)
        {
            TryRotateExistingAtCursor();
            return; 
        }

        
        if (!string.IsNullOrEmpty(_selectedKey))
        {
            Vector3 worldPos = editorCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 snapped = new Vector3(
                Mathf.Round(worldPos.x / cellSize) * cellSize,
                Mathf.Round(worldPos.y / cellSize) * cellSize,
                0f
            );

            
            if (_previewObj == null)
            {
                _previewObj = Instantiate(_prefabDict[_selectedKey], elementsParent);
                SetPreviewAlpha(_previewObj, 0.5f); 
            }
            
            _previewObj.transform.position = snapped;

            
            if (IsRotatableKey(_selectedKey) && (Input.GetMouseButtonDown(1) || Mathf.Abs(Input.mouseScrollDelta.y) > 0f))
            {
                _previewObj.transform.Rotate(0, 0, 90);
            }

            
            if (Input.GetMouseButtonDown(0))
            {
                PlaceSelected(snapped, _previewObj.transform.rotation);
            }
        }
    }
    
    void Start()
    {
        
        if (!string.IsNullOrEmpty(LevelBridge.pathToLoad))
        {
            var data = LevelIO.Load(LevelBridge.pathToLoad);
            if (data != null)
            {
                LoadLevelData(data);
                currentLevelData.levelName = data.levelName;
                LevelBridge.currentPath = LevelBridge.pathToLoad; 
            }
            else
            {
                PopupService.Instance?.Error("Impossible Loading", "File Unreadable.");
            }
            LevelBridge.pathToLoad = null; 
        }
    }

    

    
    public void SelectPrefab(string key)
    {
        ClearPreview();
        currentTool = Tool.Place;
        _selectedKey = key;
    }

    
    public void ToggleDeleteMode()
    {
        if (currentTool != Tool.Delete)
        {
            currentTool = Tool.Delete;
            ClearPreview();
        }
        else
        {
            currentTool = Tool.Place;
        }
    }

    void HandleDelete()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 p = editorCamera.ScreenToWorldPoint(Input.mousePosition);
            
            Collider2D[] hits = Physics2D.OverlapPointAll(p, placeableMask);
            if (hits == null || hits.Length == 0)
                hits = Physics2D.OverlapCircleAll(p, cellSize * 0.35f, placeableMask);

            if (hits != null && hits.Length > 0)
            {
                
                Placeable target = null;
                foreach (var h in hits)
                {
                    target = h.GetComponentInParent<Placeable>();
                    if (target != null) break;
                }

                if (target != null)
                {
                    var tel = target.GetComponent<TeleporterElement>();
                    if (tel != null)
                    {
                        
                        var toDelete = new System.Collections.Generic.List<Placeable>();
                        toDelete.Add(target);
                        
                        TeleporterElement otherTel = null;

                        if (!string.IsNullOrEmpty(tel.pairId))
                        {
                            foreach (var t in elementsParent.GetComponentsInChildren<TeleporterElement>(true))
                            {
                                if (t == tel) continue;
                                if (t.pairId == tel.pairId) { otherTel = t; break; }
                            }
                        }
                        if (otherTel == null && tel.paired != null)
                        {
                            otherTel = tel.paired;
                        }

                        if (otherTel != null)
                        {
                            var otherPlc = otherTel.GetComponent<Placeable>();
                            if (otherPlc != null) toDelete.Add(otherPlc);
                        }
                        
                        foreach (var plc in toDelete)
                        {
                            if (plc == null) continue;
                            if (plc.data != null)
                                currentLevelData.elements.RemoveAll(e => e.guid == plc.data.guid);
                            Destroy(plc.gameObject);
                        }
                    }
                    else
                    {
                        if (target.data != null)
                            currentLevelData.elements.RemoveAll(e => e.guid == target.data.guid);
                        
                        
                        Destroy(target.gameObject);
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            currentTool = Tool.Place;
    }
    
    private void PlaceSelected(Vector3 pos, Quaternion rot)
    {
        var cell = new Vector2Int(
            Mathf.RoundToInt(pos.x / cellSize),
            Mathf.RoundToInt(pos.y / cellSize)
        );
        
        var go = Instantiate(_prefabDict[_selectedKey], pos, rot, elementsParent);
        go.name = _selectedKey;
        
        
        int placeableLayer = LayerMask.NameToLayer(placeableLayerName);
        if (placeableLayer >= 0)
            SetLayerRecursively(go, placeableLayer);

        
        var col = go.GetComponentInChildren<Collider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true; 

        
        var plc = go.GetComponent<Placeable>();
        if (plc == null) plc = go.AddComponent<Placeable>();

        var data = new ElementData
        {
            guid = System.Guid.NewGuid().ToString(),
            prefabKey = _selectedKey,
            gridPos = new Vector2Int(
                Mathf.RoundToInt(pos.x / cellSize),
                Mathf.RoundToInt(pos.y / cellSize)
            ),
            rotationSteps = ((int)Mathf.Round(go.transform.eulerAngles.z / 90f)) & 3,
            isBreakable = false,
            isActivable = false
        };
        
        
        if (IsRotatableKey(_selectedKey))
        {
            if (go.GetComponent<Rotatable>() == null) go.AddComponent<Rotatable>();
        }
        
        
        if (_selectedKey == "Teleporter")
        {
            
            
            var tel = go.GetComponent<TeleporterElement>();
            if (tel == null) tel = go.AddComponent<TeleporterElement>();

            if (string.IsNullOrEmpty(_pendingTelePairId))
            {
                
                _pendingTelePairId = System.Guid.NewGuid().ToString();
                data.pairId = _pendingTelePairId;
                tel.pairId = _pendingTelePairId;
                tel.SetTint(ColorForPair(_pendingTelePairId));

                _pendingTeleFirst = plc;
                
                currentLevelData.elements.Add(data);
                
                return;
            }
            else
            {
                
                data.pairId = _pendingTelePairId;
                tel.pairId = _pendingTelePairId;
                tel.SetTint(ColorForPair(_pendingTelePairId));

                
                var firstTel = _pendingTeleFirst != null
                    ? _pendingTeleFirst.GetComponent<TeleporterElement>()
                    : null;

                if (firstTel != null)
                {
                    firstTel.paired = tel;
                    tel.paired = firstTel;
                }

                
                currentLevelData.elements.Add(data);
                
                
                _pendingTelePairId = null;
                _pendingTeleFirst = null;

                
                ClearPreview();
                _selectedKey = null;
                currentTool = Tool.Place;
                return;
            }
        }

        plc.data = data;
        currentLevelData.elements.Add(data);
        
        ClearPreview();
        _selectedKey = null;
        currentTool = Tool.Place;
    }

    private void ClearPreview()
    {
        if (_previewObj != null) Destroy(_previewObj);
        _previewObj = null;
        _selectedKey = null;
    }

    private void SetPreviewAlpha(GameObject root, float a)
    {
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>())
        {
            var c = sr.color; c.a = a; sr.color = c;
        }
    }

    
    public void OnSaveClicked()
    {
        Debug.Log("[LevelEditor] OnSaveClicked");

        
        if (!string.IsNullOrEmpty(LevelBridge.currentPath) && System.IO.File.Exists(LevelBridge.currentPath))
        {
            LevelIO.SaveOverwrite(currentLevelData, LevelBridge.currentPath);
            PopupService.Instance?.Success("Save", "Level updated.", 1.2f);
            return;
        }

        
        if (namePrompt != null)
        {
            string suggested = string.IsNullOrWhiteSpace(currentLevelData.levelName)
                ? "MonNiveau"
                : currentLevelData.levelName;

            namePrompt.Show("Nom du niveau", suggested,
                onOk: (givenName) =>
                {
                    currentLevelData.levelName = givenName;
                    LevelIO.Save(currentLevelData);
                    PopupService.Instance?.Success("Save", $"« {givenName} » Saved.", 1.2f);
                },
                onCancel: () => {  }
            );
        }
        else
        {
            // Fallback si le panel n'est pas dans la scène : nom auto au lieu d'un crash
            string autoName = "Level_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            currentLevelData.levelName = string.IsNullOrWhiteSpace(currentLevelData.levelName)
                ? autoName : currentLevelData.levelName;

            Debug.LogWarning("[LevelEditor] namePrompt est null -> sauvegarde avec nom auto: " + currentLevelData.levelName);
            LevelIO.Save(currentLevelData);
            PopupService.Instance?.Success("Saved", $"« {currentLevelData.levelName} » Saved.", 1.2f);
        }
    }

    public void OnBackToMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
    
    public void OnTestClicked()
    {
        if (_isPlaytesting) return;

        Vector3? spawn = null;
        EndPointGoal endGoal = null;

        _starsCollected = 0;
        _starsNeeded = 0;

        foreach (Transform child in elementsParent)
        {
            var plc = child.GetComponent<Placeable>();
            if (plc != null && plc.data != null && plc.data.prefabKey == "BallSpawn")
                spawn = child.position;

            var eg = child.GetComponentInChildren<EndPointGoal>();
            if (eg != null) endGoal = eg;
        }

        if (spawn == null) { Debug.LogWarning("No Ball found."); return; }
        if (endGoal == null) { Debug.LogWarning("No EndPoint found."); return; }
        
        int starsPresent = elementsParent.GetComponentsInChildren<StarPickup>(true).Length;
        if (requireExactlyThreeStars && starsPresent != 3)
        {
            PopupService.Instance?.Warn("Invalid configuration","There must be exactly 3 stars on the field.");
            return;
        }

        _isPlaytesting = true;
        ClearPreview();
        currentTool = Tool.Place;

        if (stopTestButton) stopTestButton.SetActive(true);

        _runtimeBall = Instantiate(ballRuntimePrefab, spawn.Value, Quaternion.identity);
        var runner = _runtimeBall.GetComponent<BallRunner>();
        if (runner == null)
        {
            Debug.LogError("BallRuntimePrefab need a BallRunner.");
            return;
        }
        
        _hiddenSpawnRenderers.Clear();
        foreach (var marker in elementsParent.GetComponentsInChildren<SpawnMarker>(true))
        {
            foreach (var r in marker.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue;
                r.enabled = false;
                _hiddenSpawnRenderers.Add(r);
            }
        }
        
        if (requireExactlyThreeStars)
        {
            if (starsPresent != 3)
            {
                Debug.LogWarning($"The level must contain exactly 3 stars (currently {starsPresent}).");
                return; 
            }
            _starsNeeded = 3;
        }
        else
        {
            
            _starsNeeded = starsPresent;
        }
        _starsCollected = 0;
        
        ResolveTeleporters();
        
        runner.StopRun();   
        runner.StartRun();  
    }

    public void OnStopTestClicked()
    {
        if (!_isPlaytesting) return;
        EndPlaytestCommon();

        if (_runtimeBall)
        {
            var r = _runtimeBall.GetComponent<BallRunner>();
            if (r) r.StopRun();
            Destroy(_runtimeBall);
        }
        
        foreach (var r in _hiddenSpawnRenderers)
            if (r) r.enabled = true;
        _hiddenSpawnRenderers.Clear();

        _isPlaytesting = false;
        if (stopTestButton) stopTestButton.SetActive(false);

        
        foreach (var star in elementsParent.GetComponentsInChildren<StarPickup>(true))
            star.gameObject.SetActive(true);

        
        foreach (var b in _brokenDuringTest)
            if (b) b.Restore();
        _brokenDuringTest.Clear();
        
        
        foreach (var act in elementsParent.GetComponentsInChildren<Activable>(true))
            act.ResetRuntimeState();

        
        foreach (var r in _hiddenSpawnRenderers)
            if (r) r.enabled = true;
        _hiddenSpawnRenderers.Clear();
    }

   
    public void PlaytestCollectStar(StarPickup s)
    {
        _starsCollected++;
    }

    public void TryFinishPlaytest()
    {
        if (_starsCollected >= _starsNeeded) OnPlaytestSuccess();
        else OnPlaytestFailed("There are stars missing.");
    }

    public void OnPlaytestSuccess()
    {
        Debug.Log("Victoire ! Niveau validé.");
        PopupService.Instance?.Success("Level completed!", "You have collected all the stars");
    }

    public void OnPlaytestFailed(string reason)
    {
        Debug.LogWarning("Test failed: " + reason);
        PopupService.Instance?.Error("Test failed", reason);
    }
    
    private void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    public void RegisterTempDisabled(BreakableOnce b)
    {
        if (b != null && !_brokenDuringTest.Contains(b))
            _brokenDuringTest.Add(b);
    }
    
    
    private string _pendingTelePairId = null;
    private Placeable _pendingTeleFirst = null;

    
    private Color ColorForPair(string id)
    {
        if (string.IsNullOrEmpty(id)) return Color.white;
        unchecked {
            int h = id.GetHashCode();
            float hue = ( (h & 0x7fffffff) % 360 ) / 360f; // 0..1
            return Color.HSVToRGB(hue, 0.7f, 1f);
        }
    }
    
    private void ResolveTeleporters()
    {
        var map = new Dictionary<string, System.Collections.Generic.List<TeleporterElement>>();

        foreach (var tel in elementsParent.GetComponentsInChildren<TeleporterElement>(true))
        {
            var plc = tel.GetComponent<Placeable>();
            
            string pid = !string.IsNullOrEmpty(tel.pairId) ? tel.pairId
                : (plc != null && plc.data != null ? plc.data.pairId : null);

            if (string.IsNullOrEmpty(pid))
            {
                tel.paired = null;
                tel.ResetTint();
                continue;
            }
            
            tel.pairId = pid;
            if (plc != null && plc.data != null) plc.data.pairId = pid;

            tel.SetTint(ColorForPair(pid));

            if (!map.TryGetValue(pid, out var list))
            {
                list = new System.Collections.Generic.List<TeleporterElement>();
                map[pid] = list;
            }
            list.Add(tel);
        }
        
        foreach (var kv in map)
        {
            var list = kv.Value;
            if (list.Count == 2)
            {
                list[0].paired = list[1];
                list[1].paired = list[0];
            }
            else
            {
                foreach (var t in list) t.paired = null;
                Debug.LogWarning($"PairId {kv.Key}: {list.Count} téléporteur (attendu: 2).");
            }
        }
    }
    
    private void DeleteTeleporterPairAndData(Placeable targetPlc)
    {
        if (targetPlc == null) return;

        var tel = targetPlc.GetComponent<TeleporterElement>();
        if (tel == null)
        {
            
            if (targetPlc.data != null)
                currentLevelData.elements.RemoveAll(e => e.guid == targetPlc.data.guid);
            Destroy(targetPlc.gameObject);
            return;
        }
        
        string pid = targetPlc.data != null && !string.IsNullOrEmpty(targetPlc.data.pairId)
            ? targetPlc.data.pairId
            : tel.pairId;

      
        var toDelete = new List<Placeable> { targetPlc };

        if (!string.IsNullOrEmpty(pid))
        {
            
            foreach (var p in elementsParent.GetComponentsInChildren<Placeable>(true))
            {
                if (p == targetPlc) continue;
                var t = p.GetComponent<TeleporterElement>();
                if (t == null) continue;

                string otherPid = p.data != null && !string.IsNullOrEmpty(p.data.pairId)
                    ? p.data.pairId
                    : t.pairId;

                if (otherPid == pid)
                {
                    toDelete.Add(p);
                    break;
                }
            }
        }
        else if (tel.paired != null)
        {
            
            var otherPlc = tel.paired.GetComponent<Placeable>();
            if (otherPlc) toDelete.Add(otherPlc);
        }

        
        if (_pendingTeleFirst != null && (toDelete.Contains(_pendingTeleFirst) || targetPlc == _pendingTeleFirst))
        {
            _pendingTeleFirst = null;
            _pendingTelePairId = null;
        }

        
        foreach (var plc in toDelete)
        {
            if (plc == null) continue;
            if (plc.data != null)
                currentLevelData.elements.RemoveAll(e => e.guid == plc.data.guid);
            Destroy(plc.gameObject);
        }
    }
    
    private void TryRotateExistingAtCursor()
    {
        Vector2 p = editorCamera.ScreenToWorldPoint(Input.mousePosition);

        var hits = Physics2D.OverlapPointAll(p, placeableMask);
        if (hits == null || hits.Length == 0)
            hits = Physics2D.OverlapCircleAll(p, cellSize * 0.35f, placeableMask);

        if (hits == null || hits.Length == 0) return;

        Placeable target = null;
        float best = float.MaxValue;

        foreach (var h in hits)
        {
            var plc = h.GetComponentInParent<Placeable>();
            if (plc == null) continue;
            float d = Vector2.Distance((Vector2)plc.transform.position, p);
            if (d < best) { best = d; target = plc; }
        }
        if (target == null) return;
        
        
        if (IsRotatableKey(target.data?.prefabKey ?? target.name) &&
            target.GetComponent<Rotatable>() == null)
        {
            target.gameObject.AddComponent<Rotatable>();
        }

        var rot = target.GetComponent<Rotatable>();
        if (rot != null)
        {
            rot.RotateStep();
            
        }
    }
    
    public void OnReachedEndpoint(BallRunner ball)
    {
        bool win = (_starsCollected >= _starsNeeded);

        
        if (win) OnPlaytestSuccess();
        else     OnPlaytestFailed("There are stars missing.");

        
        if (ball != null)
            FXManager.Instance?.PlayGoal(ball.transform.position);
        Shake(0.25f, 0.35f); 
        
        AudioManager.Instance?.PlayGoal();

        
        if (ball != null)
        {
            ball.StopRun();
            Destroy(ball.gameObject);
            if (_runtimeBall == ball.gameObject) _runtimeBall = null;
        }

        
        EndPlaytestCommon();
    }
    
    [Header("FX / Camera")]
    public CameraShake2D shaker;
    private void Shake(float dur, float mag)
    {
        if (!shaker) shaker = Camera.main ? Camera.main.GetComponent<CameraShake2D>() : null;
        shaker?.Shake(dur, mag);
    }
    
    private void EndPlaytestCommon()
    {
        // détruire la balle si encore là
        if (_runtimeBall) { Destroy(_runtimeBall); _runtimeBall = null; }

        // réactiver étoiles
        foreach (var star in elementsParent.GetComponentsInChildren<StarPickup>(true))
            star.gameObject.SetActive(true);

        // restaurer éléments cassés temporairement
        foreach (var b in _brokenDuringTest) if (b) b.Restore();
        _brokenDuringTest.Clear();

        // remettre les Activable à l'état initial
        foreach (var act in elementsParent.GetComponentsInChildren<Activable>(true))
            act.ResetRuntimeState();

        // réafficher le spawn
        foreach (var r in _hiddenSpawnRenderers) if (r) r.enabled = true;
        _hiddenSpawnRenderers.Clear();

        _isPlaytesting = false;
        if (stopTestButton) stopTestButton.SetActive(false);
    }
    
    public void OnBallOutOfBounds(BallRunner ball)
    {
        OnPlaytestFailed("The ball went out of the screen.");
        
        AudioManager.Instance?.PlayFail();
        
        Shake(0.25f, 0.35f);

        if (ball) { ball.StopRun(); Destroy(ball.gameObject); _runtimeBall = null; }
        EndPlaytestCommon();
    }
    
    public void LoadLevelData(LevelData data)
    {
        
        foreach (Transform c in elementsParent) Destroy(c.gameObject);
        currentLevelData = new LevelData { levelName = data.levelName, elements = new System.Collections.Generic.List<ElementData>() };

        
        foreach (var e in data.elements)
        {
            if (!_prefabDict.TryGetValue(e.prefabKey, out var prefab) || prefab == null) {
                Debug.LogWarning($"Prefab introuvable pour key {e.prefabKey}");
                continue;
            }

            Vector3 pos = new Vector3(e.gridPos.x * cellSize, e.gridPos.y * cellSize, 0f);
            var go = Instantiate(prefab, pos, Quaternion.Euler(0,0, e.rotationSteps * 90f), elementsParent);
            go.name = e.prefabKey;

            int placeableLayer = LayerMask.NameToLayer(placeableLayerName);
            if (placeableLayer >= 0) SetLayerRecursively(go, placeableLayer);

            
            if (!go.GetComponentInChildren<Collider2D>()) go.AddComponent<BoxCollider2D>();

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
            
            if (e.prefabKey == "Teleporter")
            {
                var tel = go.GetComponent<TeleporterElement>() ?? go.AddComponent<TeleporterElement>();
                tel.pairId = e.pairId;              
                
                if (!string.IsNullOrEmpty(e.pairId))
                    tel.SetTint(ColorForPair(e.pairId));
            }

            currentLevelData.elements.Add(plc.data);
        }

        
        ResolveTeleporters();
    }

}


