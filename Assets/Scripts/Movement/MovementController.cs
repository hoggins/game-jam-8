using System;
using UnityEngine;

namespace Movement
{
  [Flags]
  public enum MovementLayer
  {
    None = 0,
    Player = 1 << 0,
    Mob = 1 << 1,
    All = ~0,
  }

  public interface IMovementController
  {
    float Speed { get; }
    float Radius { get; }
    float AvoidancePower { get; }
    float VelocitySmoothing { get; }
    float RotationSpeed { get; }
    MovementLayer Layer { get; }
    MovementLayer CollidesWith { get; }

    Vector3 GetDesiredVelocity(in MovementContext context);
  }

  public readonly struct MovementContext
  {
    private readonly FlowMap _flowMap;

    public readonly MovementAgent Agent;
    public readonly Vector3 PlayerPosition;
    public readonly bool HasPlayer;

    internal MovementContext(
      MovementAgent agent,
      FlowMap flowMap,
      Vector3 playerPosition,
      bool hasPlayer)
    {
      Agent = agent;
      PlayerPosition = playerPosition;
      HasPlayer = hasPlayer;
      _flowMap = flowMap;
    }

    public Vector3 GetFlowDirection() =>
      HasPlayer
        ? _flowMap.GetDirection(Agent.Position, PlayerPosition)
        : Vector3.zero;
  }
}
