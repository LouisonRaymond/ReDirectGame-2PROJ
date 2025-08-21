using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ElementData {
    public string guid;
    public string prefabKey;
    public Vector2Int gridPos;
    public int rotationSteps;   // 0..3
    public bool isBreakable;
    public bool isActivable;
    
    public string pairId;   //teleporter par id data
}

[Serializable]
public class LevelData {
    public string levelName = "NewLevel";
    public List<ElementData> elements = new List<ElementData>();
}