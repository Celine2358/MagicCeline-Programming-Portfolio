using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockShardManager : MonoBehaviour
{
    public GameObject[] shardPrefabs;
    public List<GameObject> activeShards = new();
    private float lifetime = 30f;

    public int CurrentShardCount => activeShards.Count;

    // 바위 조각 생성
    public void SpawnShards(Vector2 canonPos, int num)
    {
        for (int s = 0; s < num; s++)
        {
            GameObject prefab = shardPrefabs[Random.Range(0, shardPrefabs.Length)];
            Vector2 spawnPos = canonPos + new Vector2(Random.Range(-2f, 2f), Random.Range(-0.9f, -0.8f));
            GameObject shard = Instantiate(prefab, spawnPos, Quaternion.identity);
            activeShards.Add(shard);

            StartCoroutine(AutoRemoveShard(shard, lifetime));
        }
    }

    // 바위 조각 자동 제거
    private IEnumerator AutoRemoveShard(GameObject shard, float time)
    {
        yield return new WaitForSeconds(time);
        RemoveShard(shard);
    }

    // 바위 조각 제거
    public void RemoveShard(GameObject shard)
    {
        if (activeShards.Contains(shard)) activeShards.Remove(shard);
        Destroy(shard);
    }

    // 주변 바위 조각 감지
    public List<GameObject> GetNearbyShards(Vector2 pos, float rangeX, float rangeY)
    {
        List<GameObject> nearby = new();
        foreach (var shard in activeShards)
        {
            Vector2 diff = (Vector2)shard.transform.position - pos;
            if (Mathf.Abs(diff.x) <= rangeX && Mathf.Abs(diff.y) <= rangeY)
                nearby.Add(shard);
        }
        return nearby;
    }

    // 바위 조각 소모
    public List<GameObject> ConsumeShards(List<GameObject> pool, int count)
    {
        var result = pool.GetRange(0, Mathf.Min(count, pool.Count));
        foreach (var shard in result) activeShards.Remove(shard);
        return result;
    }
}
