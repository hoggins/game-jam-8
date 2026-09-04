using UnityEngine;

namespace Movement
{
  [CreateAssetMenu(fileName = "MovementSettings", menuName = "Game/Movement Settings")]
  public sealed class MovementSettings : ScriptableObject
  {
    [Header("Spatial Map")]
    [SerializeField, Min(0.01f)] private float _spatialCellSize = 2f;

    [Header("Flow Map")]
    [SerializeField, Min(0.01f)] private float _flowCellSize = 1f;
    [SerializeField, Min(0f)] private float _flowPadding = 15f;
    [SerializeField, Min(0)] private int _flowTargetCellDeviation = 2;
    [SerializeField, Min(1)] private int _maxFlowCellCount = 262144;

    [Header("Wall Collision")]
    [SerializeField, Min(0f)] private float _wallSkin = 0.02f;
    [SerializeField, Range(1, 4)] private int _wallSlideIterations = 3;

    [Header("Flocking")]
    [SerializeField, Min(0f)] private float _selfRadiusModifier = 3f;
    [SerializeField, Min(0f)] private float _avoidanceSpread = 2f;
    [SerializeField, Min(0.01f)] private float _avoidanceTargetDistance = 3f;

    internal float SpatialCellSize => _spatialCellSize;
    internal float FlowCellSize => _flowCellSize;
    internal float FlowPadding => _flowPadding;
    internal int FlowTargetCellDeviation => _flowTargetCellDeviation;
    internal int MaxFlowCellCount => _maxFlowCellCount;
    internal float WallSkin => _wallSkin;
    internal int WallSlideIterations => _wallSlideIterations;
    internal float SelfRadiusModifier => _selfRadiusModifier;
    internal float AvoidanceSpread => _avoidanceSpread;
    internal float AvoidanceTargetDistance => _avoidanceTargetDistance;

    private void OnValidate()
    {
      _spatialCellSize = Mathf.Max(0.01f, _spatialCellSize);
      _flowCellSize = Mathf.Max(0.01f, _flowCellSize);
      _flowPadding = Mathf.Max(0f, _flowPadding);
      _flowTargetCellDeviation = Mathf.Max(0, _flowTargetCellDeviation);
      _maxFlowCellCount = Mathf.Max(1, _maxFlowCellCount);
      _wallSkin = Mathf.Max(0f, _wallSkin);
      _wallSlideIterations = Mathf.Clamp(_wallSlideIterations, 1, 4);
      _selfRadiusModifier = Mathf.Max(0f, _selfRadiusModifier);
      _avoidanceSpread = Mathf.Max(0f, _avoidanceSpread);
      _avoidanceTargetDistance = Mathf.Max(0.01f, _avoidanceTargetDistance);
    }
  }
}