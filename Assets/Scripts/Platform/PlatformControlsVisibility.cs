using UnityEngine;

// IMPORTANT ALIAS: this swaps out the normal UnityEngine.Application for
// UnityEngine.Device.Application everywhere in this file. They look and
// behave identically almost all the time, but there's one specific case
// where they don't: Unity's Device Simulator window (the one that lets you
// preview the game as if it were running on a specific phone, without
// actually building for Android). The PLAIN UnityEngine.Application always
// reports the truth about whatever computer is ACTUALLY running the code -
// so isMobilePlatform stays false in the Simulator, because you're still
// technically running on your Windows/Mac Editor no matter which phone
// you've picked from the dropdown. UnityEngine.Device.Application is a
// Simulator-aware version Unity ships specifically to fix this - it
// correctly reports true for isMobilePlatform while previewing a mobile
// device in the Simulator, and behaves completely normally (falls through
// to the exact same real values) in the Editor without the Simulator open,
// and in actual PC or Android builds. Aliasing it this way means every
// other line below can just keep saying "Application" like normal, rather
// than needing the fully spelled-out UnityEngine.Device.Application
// everywhere it's used.
using Application = UnityEngine.Device.Application;

// Shows your on-screen touch controls when the game is actually running on
// a phone or tablet (or being PREVIEWED as one via Unity's Device
// Simulator - see the alias above), and hides them everywhere else (PC,
// Mac, a normal Editor Play session) - completely automatically, with
// nothing manual to remember to toggle before a build.
//
// Application.isMobilePlatform is the check doing all the work here - it's
// a simple built-in flag that's true specifically on Android and iOS
// builds (and now, correctly, while previewing either of those in the
// Device Simulator), and false on every other platform (Windows, Mac,
// Linux, WebGL, and a plain Editor session). This is a good fit for "phone
// vs PC" specifically because it's answering "what platform is this build
// actually running on," which is exactly the question you're asking - as
// opposed to something like checking whether a touchscreen is currently
// plugged in (which some Windows laptops/monitors have too, and would give
// the wrong answer here even though they're not phones).
//
// SETUP: drag your on-screen touch controls panel (whatever parent
// GameObject holds your virtual joystick/buttons) into touchControlsUI
// below. If you also have something that should ONLY show on PC - like a
// "use WASD to move" hint text - drag that into desktopOnlyUI too, though
// that one's entirely optional; leave it empty if you don't have anything
// like that.
public class PlatformControlsVisibility : MonoBehaviour
{
    [Tooltip("The on-screen touch controls (virtual joystick/buttons, etc.) - shown ONLY when running on a phone or tablet, hidden everywhere else.")]
    public GameObject touchControlsUI;

    [Tooltip("Optional - anything that should show ONLY on PC/desktop (like a keyboard control hint) and be hidden on mobile. Leave empty if you don't have anything like this.")]
    public GameObject desktopOnlyUI;

    // Awake (not Start) specifically so this resolves BEFORE the very
    // first frame ever gets rendered - using Start here could let one
    // frame slip by showing the wrong controls first, which would show up
    // as a visible flicker right as the game loads, especially on a
    // slower phone.
    private void Awake()
    {
        bool isMobile = Application.isMobilePlatform;
        //isMobile = true;
        // Each of these is checked individually (rather than assuming
        // both fields are always filled in) so that leaving
        // desktopOnlyUI empty - which is expected, since it's optional -
        // doesn't cause a null-reference error that would also stop
        // touchControlsUI from getting set correctly.
        if (touchControlsUI != null)
        {
            touchControlsUI.SetActive(isMobile);
        }
        else
        {
            Debug.LogWarning("[PlatformControlsVisibility] 'touchControlsUI' isn't assigned - nothing will be shown or hidden. Drag your touch controls panel into this field in the Inspector.", this);
        }

        if (desktopOnlyUI != null)
        {
            desktopOnlyUI.SetActive(!isMobile);
        }
    }
}