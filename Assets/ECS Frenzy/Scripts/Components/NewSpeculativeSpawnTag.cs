using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable] 
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType=GhostPrefabType.PredictedClient)]
  public struct NewSpeculativeSpawnTag : IComponentData {}
}