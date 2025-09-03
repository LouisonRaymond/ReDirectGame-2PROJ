using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FXManager : MonoBehaviour
{
    public static FXManager Instance { get; private set; }

    [Header("Prefabs")]
    public ParticleSystem hitFXPrefab;      
    public ParticleSystem teleportFXPrefab; 
    public ParticleSystem goalFXPrefab;     

    [Header("Sound Pool")]
    public int initialPoolPerPrefab = 6;

    private readonly Dictionary<ParticleSystem, Queue<ParticleSystem>> _pools = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        
        Prewarm(hitFXPrefab);
        if (teleportFXPrefab) Prewarm(teleportFXPrefab);
        if (goalFXPrefab) Prewarm(goalFXPrefab);
    }

    void Prewarm(ParticleSystem prefab)
    {
        if (prefab == null) return;
        if (!_pools.ContainsKey(prefab)) _pools[prefab] = new Queue<ParticleSystem>();
        for (int i = 0; i < initialPoolPerPrefab; i++)
        {
            var ps = Instantiate(prefab, transform);
            ps.gameObject.SetActive(false);
            _pools[prefab].Enqueue(ps);
        }
    }

    ParticleSystem Spawn(ParticleSystem prefab, Vector3 pos, float zRotDeg = 0f)
    {
        if (prefab == null) return null;
        if (!_pools.ContainsKey(prefab)) _pools[prefab] = new Queue<ParticleSystem>();

        ParticleSystem ps;
        if (_pools[prefab].Count > 0)
        {
            ps = _pools[prefab].Dequeue();
        }
        else
        {
            ps = Instantiate(prefab, transform);
        }

        var go = ps.gameObject;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, 0, zRotDeg);
        go.SetActive(true);
        ps.Clear(true);
        ps.Play(true);
        
        StartCoroutine(ReturnAfter(ps, TotalDuration(ps)));
        return ps;
    }

    IEnumerator ReturnAfter(ParticleSystem ps, float t)
    {
        yield return new WaitForSeconds(t);
        if (ps != null)
        {
            ps.gameObject.SetActive(false);
            
            foreach (var kv in _pools)
            {
                if (kv.Key != null && ps.main.startSpeed.mode == kv.Key.main.startSpeed.mode) { }
            }
            
            var tag = ps.GetComponent<PoolTag>();
            var prefabRef = tag ? tag.prefabRef : hitFXPrefab;
            if (!_pools.ContainsKey(prefabRef)) _pools[prefabRef] = new Queue<ParticleSystem>();
            _pools[prefabRef].Enqueue(ps);
        }
    }

    float TotalDuration(ParticleSystem ps)
    {
        var m = ps.main;
        float life = 0f;
        if (m.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            life = m.startLifetime.constantMax;
        else if (m.startLifetime.mode == ParticleSystemCurveMode.Constant)
            life = m.startLifetime.constant;
        else life = m.duration; 

        return m.duration + life + 0.1f;
    }
    
    public void PlayHit(Vector3 pos, Vector2 dir)
    {
        float angle = DirToAngle(dir);
        var ps = Spawn(hitFXPrefab, pos, angle);
        if (ps != null) EnsurePoolTag(ps, hitFXPrefab);
    }

    public void PlayTeleport(Vector3 pos, Vector2 dir)
    {
        var pf = teleportFXPrefab ? teleportFXPrefab : hitFXPrefab;
        float angle = DirToAngle(dir);
        var ps = Spawn(pf, pos, angle);
        if (ps != null) EnsurePoolTag(ps, pf);
    }

    public void PlayGoal(Vector3 pos)
    {
        var pf = goalFXPrefab ? goalFXPrefab : hitFXPrefab;
        var ps = Spawn(pf, pos, 0f);
        if (ps != null) EnsurePoolTag(ps, pf);
    }

    void EnsurePoolTag(ParticleSystem ps, ParticleSystem prefabRef)
    {
        var tag = ps.GetComponent<PoolTag>();
        if (!tag) tag = ps.gameObject.AddComponent<PoolTag>();
        tag.prefabRef = prefabRef;
    }

    float DirToAngle(Vector2 d)
    {
        d = d.normalized;
        if (d == Vector2.right) return 0f;
        if (d == Vector2.up)    return 90f;
        if (d == Vector2.left)  return 180f;
        if (d == Vector2.down)  return 270f;
        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
    }
}

public class PoolTag : MonoBehaviour
{
    public ParticleSystem prefabRef;
}
