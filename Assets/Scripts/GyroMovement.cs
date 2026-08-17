using UnityEngine;
using UnityEngine.InputSystem;

// Alias needed here specifically: both UnityEngine (the old, legacy
// namespace that Input.gyro used to live under) and UnityEngine.InputSystem
// (the new one we're using now) each define their own type named
// "Gyroscope." Having both namespaces in scope via the two usings above
// means the bare name "Gyroscope" is genuinely ambiguous to the compiler -
// it has no way to know which one you mean, hence the CS0104 error. This
// alias pins "Gyroscope" to specifically mean the new Input System's
// version everywhere in this file, so the rest of the code below can just
// use the short name without spelling out the full
// UnityEngine.InputSystem.Gyroscope path every time.
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

// Translates this object based on device gyroscope rotation, when a
// gyroscope is actually available (i.e. on a phone, not a desktop).
//
// This one's a bit different from the other Input System conversions in
// this project: gyroscope/accelerometer/etc. are "sensors" in the new
// Input System, and they're read by polling a device directly
// (Gyroscope.current) rather than through an Action binding in the
// PlayerControls asset - that's the normal pattern for sensors
// specifically, as opposed to buttons/sticks which do go through actions.
// One important difference from the old API: sensors aren't automatically
// turned on just because a device supports them - you have to explicitly
// call InputSystem.EnableDevice() before a sensor starts producing
// readings, otherwise it just sits there disabled to save battery.
// Forgetting that step is a common "why is my gyro reading always zero"
// gotcha.
public class GyroMovement : MonoBehaviour
{
    private bool gyroEnabled;

    private void OnEnable()
    {
        gyroEnabled = EnableGyro();
    }

    private void OnDisable()
    {
        // Explicitly turn the sensor back off when this object is disabled,
        // rather than leaving it running in the background for no reason -
        // sensors have a real battery/performance cost on mobile.
        if (Gyroscope.current != null)
        {
            InputSystem.DisableDevice(Gyroscope.current);
        }
    }

    private void Update()
    {
        if (gyroEnabled)
        {
            // angularVelocity is the new Input System's equivalent of the
            // old Input.gyro.rotationRateUnbiased - how fast the device is
            // currently rotating around each axis.
            Vector3 rate = Gyroscope.current.angularVelocity.ReadValue();
            transform.Translate(-rate.x, -rate.y, 0);
        }
    }

    private bool EnableGyro()
    {
        // Gyroscope.current is null on any device that doesn't have one
        // (like your PC while testing in the Editor) - this is the new
        // Input System's replacement for the old SystemInfo.supportsGyroscope
        // check.
        if (Gyroscope.current == null)
        {
            return false;
        }

        InputSystem.EnableDevice(Gyroscope.current);
        return true;
    }
}