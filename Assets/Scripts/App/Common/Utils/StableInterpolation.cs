using Unity.Mathematics;

namespace App.Common.Utils
{
  public static class StableInterpolation
  {
    public static float Lerp(float a, float b, float dt) =>
      b + (a - b) * math.exp(-dt);
  }
}
