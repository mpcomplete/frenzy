using Unity.Entities;
using Unity.NetCode;

[GenerateAuthoringComponent]
[GhostComponent(PrefabType = GhostPrefabType.All)]
public struct CooldownStatus : IComponentData {
  public enum Status { Elapsed, Active, JustElapsed, JustActive }

  public static CooldownStatus Elapsed = new CooldownStatus { Value = Status.Elapsed };
  public static CooldownStatus Active = new CooldownStatus { Value = Status.Active };
  public static CooldownStatus JustElapsed = new CooldownStatus { Value = Status.JustElapsed };
  public static CooldownStatus JustActive = new CooldownStatus { Value = Status.JustActive };

  [GhostField] public Status Value;
}
