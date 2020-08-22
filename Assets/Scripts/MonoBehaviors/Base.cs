using UnityEngine;

public class Base : MonoBehaviour {
  public float SpawnCooldown = 1f;
  public float NextSpawnTime = 0f;
  public Transform SpawnLocation;
  public Minion MinionPrefab;
}