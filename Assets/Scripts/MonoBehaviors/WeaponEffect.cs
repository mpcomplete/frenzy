using System.Collections;
using UnityEngine;

public abstract class WeaponEffect : MonoBehaviour {
  public delegate void DamageDelegate();

  public float Time { get; private set; }
  public bool IsPlaying { get; private set; }
  public void Play(Unit target) {
    IsPlaying = true;
    Time = 0;
    DoPlay(target);
  }
  public void Stop() {
    IsPlaying = false;
    DoStop();
  }

  protected abstract void DoPlay(Unit target);
  protected abstract void DoStop();
}