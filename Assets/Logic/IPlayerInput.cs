// ===========================
// IPlayerInput.cs
// ===========================
using UnityEngine;

public interface IPlayerInput
{
    Vector2 MoveAxis { get; }
    bool JumpDown { get; }
    bool SprintHeld { get; }
    bool CrouchHeld { get; }
    bool SlowWalkHeld { get; }

    bool Fire1Down { get; }
    bool Fire1Hold { get; }
    bool Fire1Up { get; }

    bool Fire2Down { get; }
    bool Fire2Hold { get; }
    bool Fire2Up { get; }

    bool UtilityDown { get; }
    bool UtilityUp { get; }

    bool ReloadDown { get; }

    bool InteractDown { get; }

    float LookAxisX { get; }
    float LookAxisY { get; }

    int ScrollDelta { get; }
    int NumberKeyPressed { get; }
}
