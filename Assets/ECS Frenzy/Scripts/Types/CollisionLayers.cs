namespace ECSFrenzy {
  // Keep in sync with the "Physics Category Names" in the Editor.
  public static class CollisionLayer {
    public const uint Team1 = 1<<1;
    public const uint Team2 = 1<<2;
    public const uint Player = 1<<2;
    public const uint Minion = 1<<3;
    public const uint Stanchion = 1<<4;
    public const uint Base = 1<<5;
  }
}