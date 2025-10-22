using Unity.VisualScripting;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Transform[] spawnPoints;
    public SpawnData[] spawnDatas;
    public float levelTime;

    int level;
    float timer;

    private void Awake()
    {
        spawnPoints = GetComponentsInChildren<Transform>();
        levelTime = GameManager.instance.maxGameTime / spawnDatas.Length;
    }

    private void Update()
    {
        if (!GameManager.instance.isLive)
            return;

        timer += Time.deltaTime;
        level = Mathf.Min(Mathf.FloorToInt(GameManager.instance.gameTime / levelTime), spawnDatas.Length - 1);

        if (timer > spawnDatas[level].spawnTime)
        {
            Spawn();
            timer = 0;
        }
    }

    void Spawn()
    {
        GameObject enemy = GameManager.instance.pool.Get(0);
        enemy.transform.position = spawnPoints[Random.Range(1, spawnPoints.Length)].position;
        enemy.GetComponent<Enemy>().Init(spawnDatas[level]);
    }
}

[System.Serializable]
public class SpawnData
{
    public int spriteType;
    public float spawnTime;
    public int health;
    public float speed;
}