using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class WeaponInfo : ScriptableObject {
  public bool Targeted;
  public float Range;
  public float Damage;
  public float AnimationPre;  // TODO: replace with AnimationCurve
  public float AnimationPost;
}