using UnityEngine;
using UnityEngine.InputSystem;

// On a tap, raycasts from the camera through the tapped screen point and
// randomizes the color of whatever's hit. (This looks like a leftover
// tech-demo/test script from earlier in the project rather than something
// tied to core gameplay - flagging that in case you want to remove it
// later, but converting it as-is for now rather than guessing at cutting
// it.)
//
// Touchscreen.current is the new Input System's device object for touch
// input - same idea as Gyroscope.current in GyroMovement.cs, just for
// touches instead of the gyro sensor. Unlike the gyro, touch doesn't need
// an explicit EnableDevice() call - it's considered a normal input device
// rather than a power-hungry sensor, so it's live by default.
public class TouchInputManager : MonoBehaviour
{
    private void Update()
    {
        // Touchscreen.current is null on a device with no touchscreen at
        // all (like a desktop PC), so this check doubles as both "is touch
        // available" and a safe way to bail out before touching anything else.
        if (Touchscreen.current == null)
        {
            return;
        }

        // wasPressedThisFrame is true for exactly one frame - the frame the
        // touch first made contact - which is the direct equivalent of the
        // old Input.touches[0].phase == TouchPhase.Began check.
        if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(touchPosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider != null)
                {
                    Color color = new Color(Random.value, Random.value, Random.value);
                    hit.collider.gameObject.GetComponent<MeshRenderer>().material.color = color;
                }
            }
        }
    }
}