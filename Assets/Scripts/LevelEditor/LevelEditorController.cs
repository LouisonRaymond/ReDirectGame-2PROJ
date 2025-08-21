using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LevelEditorController : MonoBehaviour
{
    public static LevelEditorController Instance { get; private set; }

    // --- Outils ---
    public enum Tool { Place, Delete }
    public Tool currentTool = Tool.Place;

    [Header("Références")]
    public Camera editorCamera;
    public float cellSize = 1f;
    public Transform elementsParent; // Empty sous LevelEditorRoot pour organiser les objets

    [Header("Tous les Prefabs")]
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

    [Header("Layers / Delete")]
    public LayerMask placeableMask;                 // Layer "Placeable" pour la suppression
    public string placeableLayerName = "Placeable"; // Appliqué aux instances posées

    [Header("Données du niveau")]
    public LevelData currentLevelData = new LevelData();

    [Header("Playtest")]
    public GameObject ballRuntimePrefab;     // Prefab de la balle (BallRunner + Collider2D isTrigger)
    public GameObject stopTestButton;        // Bouton UI "Stop" activé pendant le test

    // internes
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
    public bool requireExactlyThreeStars = true; // par défaut: 3 étoiles obligatoires
    
    // Éléments qui ne doivent JAMAIS tourner
    private readonly System.Collections.Generic.HashSet<string> _nonRotatableKeys =
        new System.Collections.Generic.HashSet<string> { "EndPoint", "BallSpawn", "Teleporter", "Star" };

    private bool IsRotatableKey(string key) => !_nonRotatableKeys.Contains(key);
    
    [SerializeField] private NamePromptPanel namePrompt;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Dico clés -> prefabs
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
        // Autoriser Escape pour stopper même en test
        if (_isPlaytesting && Input.GetKeyDown(KeyCode.Escape))
        {
            OnStopTestClicked();
            return;
        }
        // En playtest : on bloque l'édition
        if (_isPlaytesting) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (currentTool == Tool.Delete)
        {
            HandleDelete();
            return;
        }
        
        // clic droit : si pas de preview en cours -> rotation de l'objet sous le curseur
        if (_previewObj == null && Input.GetMouseButtonDown(1) && currentTool != Tool.Delete)
        {
            TryRotateExistingAtCursor();
            return; // évite d'autres traitements ce frame
        }

        // --- Tool.Place ---
        if (!string.IsNullOrEmpty(_selectedKey))
        {
            Vector3 worldPos = editorCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 snapped = new Vector3(
                Mathf.Round(worldPos.x / cellSize) * cellSize,
                Mathf.Round(worldPos.y / cellSize) * cellSize,
                0f
            );

            // Crée le preview si besoin
            if (_previewObj == null)
            {
                _previewObj = Instantiate(_prefabDict[_selectedKey], elementsParent);
                SetPreviewAlpha(_previewObj, 0.5f); // preview semi-transparent
            }
            
            _previewObj.transform.position = snapped;

            // Rotation à la molette ou clic droit (UNIQUEMENT si l'élément est autorisé)
            if (IsRotatableKey(_selectedKey) && (Input.GetMouseButtonDown(1) || Mathf.Abs(Input.mouseScrollDelta.y) > 0f))
            {
                _previewObj.transform.Rotate(0, 0, 90);
            }

            // Placement au clic gauche
            if (Input.GetMouseButtonDown(0))
            {
                PlaceSelected(snapped, _previewObj.transform.rotation);
            }
        }
    }
    
    void Start()
    {
        // si on vient du browser
        if (!string.IsNullOrEmpty(LevelBridge.pathToLoad))
        {
            var data = LevelIO.Load(LevelBridge.pathToLoad);
            if (data != null)
            {
                LoadLevelData(data);
                currentLevelData.levelName = data.levelName;
                LevelBridge.currentPath = LevelBridge.pathToLoad; // pour SaveOverwrite
            }
            else
            {
                PopupService.Instance?.Error("Impossible Loading", "File Unreadable.");
            }
            LevelBridge.pathToLoad = null; // consommer
        }
    }

    

    // --- Palette ---
    public void SelectPrefab(string key)
    {
        ClearPreview();
        currentTool = Tool.Place;
        _selectedKey = key;
    }

    // --- Delete ---
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

            // Tolérant : d'abord au point, puis petit rayon
            Collider2D[] hits = Physics2D.OverlapPointAll(p, placeableMask);
            if (hits == null || hits.Length == 0)
                hits = Physics2D.OverlapCircleAll(p, cellSize * 0.35f, placeableMask);

            if (hits != null && hits.Length > 0)
            {
                // Remonte au Placeable le plus proche
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
                        // --- SUPPRESSION D'UNE PAIRE DE TÉLÉPORTEURS ---

                        // On prépare la liste des deux extrémités
                        var toDelete = new System.Collections.Generic.List<Placeable>();
                        toDelete.Add(target);

                        // Trouve l'autre extrémité : d'abord via pairId, sinon via le lien runtime 'paired'
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

                        // Supprime des données + détruit les deux objets
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
                        // --- SUPPRESSION CLASSIQUE ---
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

    // --- Placement effectif ---
    private void PlaceSelected(Vector3 pos, Quaternion rot)
    {
        var go = Instantiate(_prefabDict[_selectedKey], pos, rot, elementsParent);
        go.name = _selectedKey;

        // Layer pour la suppression (root + enfants)
        int placeableLayer = LayerMask.NameToLayer(placeableLayerName);
        if (placeableLayer >= 0)
            SetLayerRecursively(go, placeableLayer);

        // S’assure qu’un collider existe pour la suppression ET le playtest
        var col = go.GetComponentInChildren<Collider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true; // le playtest se fait par OnTriggerEnter2D

        // Lien scène <-> données
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
        
        // Rendre l'objet rotatable uniquement si autorisé
        if (IsRotatableKey(_selectedKey))
        {
            if (go.GetComponent<Rotatable>() == null) go.AddComponent<Rotatable>();
        }
        
        
        // --- Téléporteur : pairing en 2 poses ---
        if (_selectedKey == "Teleporter")
        {
            var tel = go.GetComponent<TeleporterElement>();
            if (tel == null) tel = go.AddComponent<TeleporterElement>();

            if (string.IsNullOrEmpty(_pendingTelePairId))
            {
                // 1er téléporteur : crée ID, teinte, reste en mode placement Teleporter
                _pendingTelePairId = System.Guid.NewGuid().ToString();
                data.pairId = _pendingTelePairId;
                tel.pairId = _pendingTelePairId;
                tel.SetTint(ColorForPair(_pendingTelePairId));

                _pendingTeleFirst = plc;

                // ne ferme PAS la preview : oblige à poser le 2e
                currentLevelData.elements.Add(data);
                return;
            }
            else
            {
                // 2e téléporteur : même ID, teinte, relie les 2
                data.pairId = _pendingTelePairId;
                tel.pairId = _pendingTelePairId;
                tel.SetTint(ColorForPair(_pendingTelePairId));

                // relier en scène
                var firstTel = _pendingTeleFirst != null
                    ? _pendingTeleFirst.GetComponent<TeleporterElement>()
                    : null;

                if (firstTel != null)
                {
                    firstTel.paired = tel;
                    tel.paired = firstTel;
                }

                // ajouter les données du 2e
                currentLevelData.elements.Add(data);

                // reset état de pairing
                _pendingTelePairId = null;
                _pendingTeleFirst = null;

                // cette fois, on peut fermer la preview
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

    // --- Save ---
    public void OnSaveClicked()
    {
        Debug.Log("[LevelEditor] OnSaveClicked");

        // Écrasement si on édite un fichier existant
        if (!string.IsNullOrEmpty(LevelBridge.currentPath) && System.IO.File.Exists(LevelBridge.currentPath))
        {
            LevelIO.SaveOverwrite(currentLevelData, LevelBridge.currentPath);
            PopupService.Instance?.Success("Save", "Level updated.", 1.2f);
            return;
        }

        // Nouveau fichier : si panel présent -> demander un nom
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
                    PopupService.Instance?.Success("Sauvegardé", $"« {givenName} » enregistré.", 1.2f);
                },
                onCancel: () => { /* rien */ }
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
            PopupService.Instance?.Success("Sauvegardé", $"« {currentLevelData.levelName} » enregistré.", 1.2f);
        }
    }

    public void OnBackToMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // --- Playtest ---
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
            Debug.LogError("BallRuntimePrefab doit avoir un BallRunner.");
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
        
        // --- compter les étoiles présentes ---
        //int starsPresent = elementsParent.GetComponentsInChildren<StarPickup>(true).Length;

        // règle: exactement 3 étoiles ?
        if (requireExactlyThreeStars)
        {
            if (starsPresent != 3)
            {
                Debug.LogWarning($"The level must contain exactly 3 stars (currently {starsPresent}).");
                return; // on empêche le test si la règle n'est pas respectée
            }
            _starsNeeded = 3;
        }
        else
        {
            // sinon on exige simplement toutes celles placées
            _starsNeeded = starsPresent;
        }
        _starsCollected = 0;
        
        ResolveTeleporters();
        
        runner.StopRun();   // sécurité
        runner.StartRun();  // ⇦ démarre VRAIMENT ici seulement
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

        // Réactiver les étoiles (si tu avais déjà ce bloc, garde-le)
        foreach (var star in elementsParent.GetComponentsInChildren<StarPickup>(true))
            star.gameObject.SetActive(true);

        // Ré-activer les éléments cassés temporairement
        foreach (var b in _brokenDuringTest)
            if (b) b.Restore();
        _brokenDuringTest.Clear();
        
        // Remettre les Activable à l'état initial
        foreach (var act in elementsParent.GetComponentsInChildren<Activable>(true))
            act.ResetRuntimeState();

        // Ré-afficher les spawns si tu caches leurs renderers (comme on l'a fait avant)
        foreach (var r in _hiddenSpawnRenderers)
            if (r) r.enabled = true;
        _hiddenSpawnRenderers.Clear();
    }

    // Appelés par les éléments
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
    
    // état de pairing en cours
    private string _pendingTelePairId = null;
    private Placeable _pendingTeleFirst = null;

    // couleur des spawns déjà stockée
    

    // couleur stable par ID (HSV)
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
            // Priorité: composant → sinon données
            string pid = !string.IsNullOrEmpty(tel.pairId) ? tel.pairId
                : (plc != null && plc.data != null ? plc.data.pairId : null);

            if (string.IsNullOrEmpty(pid))
            {
                tel.paired = null;
                tel.ResetTint();
                continue;
            }

            // garde sync compo <-> data
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

        // faire les couples
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
                Debug.LogWarning($"PairId {kv.Key}: {list.Count} téléporteur(s) (attendu: 2).");
            }
        }
    }
    
    // Supprime un téléporteur ET sa paire (données + objets)
    // targetPlc = Placeable du téléporteur cliqué
    private void DeleteTeleporterPairAndData(Placeable targetPlc)
    {
        if (targetPlc == null) return;

        var tel = targetPlc.GetComponent<TeleporterElement>();
        if (tel == null)
        {
            // Ce n'est pas un téléporteur → suppression simple
            if (targetPlc.data != null)
                currentLevelData.elements.RemoveAll(e => e.guid == targetPlc.data.guid);
            Destroy(targetPlc.gameObject);
            return;
        }

        // 1) Trouver l'ID de paire (priorité aux données)
        string pid = targetPlc.data != null && !string.IsNullOrEmpty(targetPlc.data.pairId)
            ? targetPlc.data.pairId
            : tel.pairId;

        // 2) Construire la liste des deux extrémités à supprimer
        var toDelete = new List<Placeable> { targetPlc };

        if (!string.IsNullOrEmpty(pid))
        {
            // Cherche l'autre extrémité par pairId
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
            // Fallback : pas d'ID mais lien runtime
            var otherPlc = tel.paired.GetComponent<Placeable>();
            if (otherPlc) toDelete.Add(otherPlc);
        }

        // 3) Nettoie l'état de pairing en cours si besoin
        if (_pendingTeleFirst != null && (toDelete.Contains(_pendingTeleFirst) || targetPlc == _pendingTeleFirst))
        {
            _pendingTeleFirst = null;
            _pendingTelePairId = null;
        }

        // 4) Retire des données + détruit les objets
        foreach (var plc in toDelete)
        {
            if (plc == null) continue;
            if (plc.data != null)
                currentLevelData.elements.RemoveAll(e => e.guid == plc.data.guid);
            Destroy(plc.gameObject);
        }
    }
    
    // Rotation 90° de l'objet sous la souris (si aucun preview en cours)
    // Rotation 90° de l'objet sous la souris (si aucun preview en cours)
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

        var rot = target.GetComponent<Rotatable>();
        if (rot != null)
        {
            rot.RotateStep();
            // rien si pas de Rotatable => l’objet est non-rotatable
        }
    }
    
    public void OnReachedEndpoint(BallRunner ball)
    {
        bool win = (_starsCollected >= _starsNeeded);

        // Feedback (logs/panels)
        if (win) OnPlaytestSuccess();
        else     OnPlaytestFailed("There are stars missing.");

        // FX + shake
        if (ball != null)
            FXManager.Instance?.PlayGoal(ball.transform.position);
        Shake(0.25f, 0.35f); // petit tremblement
        
        AudioManager.Instance?.PlayGoal();

        // Retire la balle
        if (ball != null)
        {
            ball.StopRun();
            Destroy(ball.gameObject);
            if (_runtimeBall == ball.gameObject) _runtimeBall = null;
        }

        // Quitte proprement le mode test (restaure étoiles, éléments cassés/activables, UI, etc.)
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

        // réafficher les spawns
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
        // 1) clear scène et data
        foreach (Transform c in elementsParent) Destroy(c.gameObject);
        currentLevelData = new LevelData { levelName = data.levelName, elements = new System.Collections.Generic.List<ElementData>() };

        // 2) réinstancie
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

            // collider de secours si besoin
            if (!go.GetComponentInChildren<Collider2D>()) go.AddComponent<BoxCollider2D>();

            var plc = go.GetComponent<Placeable>() ?? go.AddComponent<Placeable>();
            // très important : assigner la même data (pour suppression/rotation/Save)
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
                tel.pairId = e.pairId;              // << sync depuis les données
                // Optionnel: teinte immédiate (ResolveTeleporters le fera aussi)
                if (!string.IsNullOrEmpty(e.pairId))
                    tel.SetTint(ColorForPair(e.pairId));
            }

            currentLevelData.elements.Add(plc.data);
        }

        // 3) résoudre les téléporteurs (paire + couleur)
        ResolveTeleporters();
    }

}


