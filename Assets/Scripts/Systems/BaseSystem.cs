using UnityEngine;

[System.Serializable]
public class BaseSystem {
  public int MAX_MINIONS_PER_TEAM;
  public Minion MeleeMinionPrefab = null;
  public Minion RangedMinionPrefab = null;

  public void Update(Team team, float dt) {
    MaybeSpawnMinions(team, team.Base);
    MaybeSpawnMinions(team, team.SpawnerTop);
    MaybeSpawnMinions(team, team.SpawnerBot);
  }

  void MaybeSpawnMinions(Team team, Base spawner) {
    if (!spawner.gameObject.activeSelf)
      return;
    while (Time.time >= spawner.NextSpawnTime && team.Minions.Count < MAX_MINIONS_PER_TEAM) {
      Minion prefab = Random.value < .7f ? MeleeMinionPrefab : RangedMinionPrefab;  // TODO
      Minion minion = Minion.Instantiate(prefab, spawner.SpawnLocation.position, spawner.SpawnLocation.rotation);
      minion.AssignTeam(team);
      team.Minions.Add(minion);
      spawner.NextSpawnTime += spawner.SpawnCooldown;
    }
  }
}