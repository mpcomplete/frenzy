using UnityEngine;

[CreateAssetMenu]
public class TeamConfiguration : ScriptableObject {
  public Minion MinionPrefab;
  public Material Material;
  public LayerMask MinionLayerMask;
  public LayerMask PlayerLayerMask;
  public LayerMask AttackableMinionLayerMask;
  public LayerMask AttackablePlayerLayerMask;
}