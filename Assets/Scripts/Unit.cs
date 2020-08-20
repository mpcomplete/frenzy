using UnityEngine;

public class Unit : MonoBehaviour {
  public float Health = 100f;
  public float MaxHealth = 100f;

  public bool Alive { get => Health > 0f; }
}
