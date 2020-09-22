using System;
using Unity.Entities;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct SharedCooldownStatus : ISharedComponentData {
    public enum Status { Elapsed, Active, JustElapsed, JustActive }

    public static SharedCooldownStatus Elapsed = new SharedCooldownStatus { Value = Status.Elapsed };
    public static SharedCooldownStatus Active = new SharedCooldownStatus { Value = Status.Active };
    public static SharedCooldownStatus JustElapsed = new SharedCooldownStatus { Value = Status.JustElapsed };
    public static SharedCooldownStatus JustActive = new SharedCooldownStatus { Value = Status.JustActive };

    public Status Value;
  }
}