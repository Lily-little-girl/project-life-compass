using UnityEngine;

public class TimedKidSpawner : MonoBehaviour
{
    public Kid runnerKid;
    public float[] spawnAtRemainingTime = { 20f, 10f };   // โผล่เมื่อเวลาที่เหลือถึงค่าเหล่านี้

    private bool[] spawned;

    void Start()
    {
        spawned = new bool[spawnAtRemainingTime.Length];
    }

    void Update()
    {
        if (runnerKid == null || GameOverManager.IsGameOver) return;

        float remain = GameTimer.RemainingTime;

        for (int i = 0; i < spawnAtRemainingTime.Length; i++)
        {
            if (!spawned[i] && remain <= spawnAtRemainingTime[i])
            {
                spawned[i] = true;
                runnerKid.Spawn();
            }
        }
    }
}