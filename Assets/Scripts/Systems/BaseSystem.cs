using UnityEngine;

[System.Serializable]
public class BaseSystem {
  public int MAX_MINIONS_PER_TEAM;

  public void Update(Team team, float dt) {
    MaybeSpawnMinions(team, team.Base);
    MaybeSpawnMinions(team, team.SpawnerTop);
    MaybeSpawnMinions(team, team.SpawnerBot);
  }

  void MaybeSpawnMinions(Team team, Base spawner) {
    if (!spawner.gameObject.activeSelf)
      return;
    if (Time.time >= spawner.NextSpawnTime && team.Minions.Count < MAX_MINIONS_PER_TEAM) {
      Minion minion = Minion.Instantiate(spawner.MinionPrefab, spawner.SpawnLocation.position, spawner.SpawnLocation.rotation);
      team.Minions.Add(minion);
      minion.AssignTeam(team);
      spawner.NextSpawnTime = Time.time + spawner.SpawnCooldown / team.MinionSpawnRateMultiplier;
    }
  }
}