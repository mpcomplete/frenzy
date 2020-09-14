using UnityEngine;

namespace ECSFrenzy.MonoBehaviors {
  public class SystemConfig : MonoBehaviour {
    public static SystemConfig Instance;
    public RenderedPlayer RenderedPlayerPrefab;
    public RenderedFireball RenderedFireballPrefab;
    public float PlayerMoveSpeed = 5;
    public float ControllerDeadzone = .19f;
    public TeamConfig[] TeamConfigs;

    void Awake() {
      Instance = this;
    }
  }
}