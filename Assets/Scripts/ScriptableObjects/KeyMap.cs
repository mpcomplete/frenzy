using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu]
public class KeyMap : ScriptableObject {
  public InputActionReference Move;
  public InputActionReference PlaceStanchion;
  public InputActionReference Attack;
  public InputActionReference Ability1;
  public InputActionReference Ability2;
  public InputActionReference Ability3;
  public InputActionReference Ability4;
}