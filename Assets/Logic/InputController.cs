// ===========================
// InputController.cs
// ===========================
using UnityEngine;

public class InputController : MonoBehaviour, IPlayerInput
{
    public Vector2 MoveAxis => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    public bool JumpDown => Input.GetButtonDown("Jump");
    public bool SprintHeld => Input.GetKey(KeyCode.LeftShift);
    public bool CrouchHeld => Input.GetKey(KeyCode.C);
    public bool SlowWalkHeld => Input.GetKey(KeyCode.LeftControl);

    public bool Fire1Down => Input.GetButtonDown("Fire1");
    public bool Fire1Hold => Input.GetButton("Fire1");
    public bool Fire1Up => Input.GetButtonUp("Fire1");

    public bool Fire2Down => Input.GetButtonDown("Fire2");
    public bool Fire2Hold => Input.GetButton("Fire2");
    public bool Fire2Up => Input.GetButtonUp("Fire2");

    public bool UtilityDown => Input.GetKeyDown(KeyCode.G);
    public bool UtilityUp => Input.GetKeyUp(KeyCode.G);

    public bool ReloadDown => Input.GetKeyDown(KeyCode.R);

    public bool InteractDown => Input.GetKeyDown(KeyCode.E);
    
    public float LookAxisX => Input.GetAxis("Mouse X");
    public float LookAxisY => Input.GetAxis("Mouse Y");

    public int ScrollDelta => Mathf.RoundToInt(Input.GetAxis("Mouse ScrollWheel") * 10f);

    public int NumberKeyPressed
    {
        get
        {
            for (int i = 1; i <= 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                    return i;
            return 0;
        }
    }
}