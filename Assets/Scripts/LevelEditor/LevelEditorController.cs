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

    [Header("Raycast / Layers")]
    public LayerMask placeableMask;            // Assigne le layer "Placeable" ici
    public string placeableLayerName = "Placeable"; // Optionnel: appliqué aux instances posées

    [Header("Données du niveau")]
    public LevelData currentLevelData = new LevelData();

    // Mapping clé → prefab
    private Dictionary<string, GameObject> _prefabDict;
    private string _selectedKey;
    private GameObject _previewObj;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Initialisation du dictionnaire
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
    }

    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (currentTool == Tool.Delete)
        {
            HandleDelete();
            return;
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
                // Optionnel: rendre le preview semi-transparent
                SetPreviewAlpha(_previewObj, 0.5f);
            }

            _previewObj.transform.position = snapped;

            // Rotation à la molette ou clic droit
            if (Input.GetMouseButtonDown(1) || Mathf.Abs(Input.mouseScrollDelta.y) > 0f)
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

    // --- Appelé par tes PaletteButtons ---
    public void SelectPrefab(string key)
    {
        ClearPreview();
        currentTool = Tool.Place;
        _selectedKey = key;
    }

    // --- Bouton Delete ---
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

    // --- Placement effectif ---
    private void PlaceSelected(Vector3 pos, Quaternion rot)
    {
        var go = Instantiate(_prefabDict[_selectedKey], pos, rot, elementsParent);
        go.name = _selectedKey;

        // Layer pour le raycast delete
        if (!string.IsNullOrEmpty(placeableLayerName))
            go.layer = LayerMask.NameToLayer(placeableLayerName);

        // S’assure qu’un collider existe (OverlapPoint en a besoin)
        if (go.GetComponentInChildren<Collider2D>() == null)
            go.AddComponent<BoxCollider2D>();

        // Composant Placeable pour relier scène <-> données
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

        plc.data = data;
        currentLevelData.elements.Add(data);

        ClearPreview();
        _selectedKey = null;
        currentTool = Tool.Place;
    }

    private void HandleDelete()
    {
        // Clic gauche = supprimer l’objet sous la souris
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 world = editorCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 p = world;

            var hit = Physics2D.OverlapPoint(p, placeableMask);
            if (hit == null) return;

            var placeable = hit.GetComponentInParent<Placeable>();
            if (placeable == null || placeable.data == null) return;

            // 1) enlever des données
            currentLevelData.elements.RemoveAll(e => e.guid == placeable.data.guid);

            // 2) détruire l'objet
            Destroy(placeable.gameObject);
        }

        // Echap pour quitter le mode delete
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            currentTool = Tool.Place;
        }
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

    // --- Bouton Save ---
    public void OnSaveClicked()
    {
        // (Tu peux remplacer levelName par un champ InputField TMP si tu veux)
        if (string.IsNullOrWhiteSpace(currentLevelData.levelName))
            currentLevelData.levelName = "Level_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string folder = Path.Combine(Application.persistentDataPath, "Levels");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string path = Path.Combine(folder, currentLevelData.levelName + ".json");
        string json = JsonUtility.ToJson(currentLevelData, true);
        File.WriteAllText(path, json);
        Debug.Log($"Level saved to: {path}");
    }

    public void OnBackToMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
