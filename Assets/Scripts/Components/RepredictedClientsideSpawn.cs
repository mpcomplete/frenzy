using System;
using Unity.Entities;
using Unity.NetCode;

[Serializable]
[GenerateAuthoringComponent]
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct RepredictedClientsideSpawn : IComponentData {
  public bool Value;
}
