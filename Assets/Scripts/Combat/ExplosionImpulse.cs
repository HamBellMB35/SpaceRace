using Cinemachine;
using UnityEngine;

// Makes the screen/phone shake automatically whenever this explosion
// spawns - and the closer the explosion is to the camera, the stronger the
// shake feels. That "closer = stronger" falloff isn't something this
// script calculates itself - it comes for free from Cinemachine's own
// Impulse system, which is built exactly for this: GenerateImpulse() below
// sends out a "shockwave" from this object's current position, and
// whichever CinemachineImpulseListener is listening (should be on your
// Virtual Camera) automatically weakens the shake the farther away it is
// from wherever the impulse actually happened. That's a much better fit
// here than hand-rolling distance math ourselves - Cinemachine already
// knows exactly where the camera is and how it's currently behaving, and
// blends shake smoothly with whatever else the camera's doing (like the
// existing chase-cam lag) instead of just yanking the raw camera Transform
// around, which would fight with Cinemachine and look janky.
//
// REQUIRES a CinemachineImpulseListener component somewhere your camera
// setup can see - normally added directly to the Virtual Camera. Without
// one, GenerateImpulse() below has nothing listening, and nothing will
// visibly happen even though this script is running correctly - if you
// add this and see no shake at all, that listener is the first thing to
// check.
// ALSO now handles the explosion's boom sound (see the Audio section below)
// - it lives here, sharing the same OnEnable() as the screen shake, rather
// than in its own separate script, specifically because this script
// already solved the "fire correctly every time this pooled object gets
// reused, not just on its very first spawn ever" problem for the shake -
// piggybacking on that same OnEnable() means the sound automatically gets
// that exact same correctness for free, instead of needing to work out
// pooling timing all over again in a second script.
[RequireComponent(typeof(CinemachineImpulseSource))]
[RequireComponent(typeof(AudioSource))]
public class ExplosionImpulse : MonoBehaviour
{
    private CinemachineImpulseSource impulseSource;
    private AudioSource audioSource;

    // CHANGED from a single AudioClip to an array, same idea as
    // ProximityWoosh.cs's wooshSounds: dragging in a few different boom
    // takes means enemies dying constantly throughout a run doesn't
    // produce the exact same identical explosion sound every single time,
    // which gets noticeably repetitive fast. Leave just one clip in here
    // if that's all you want - it'll simply always pick that one.
    [Header("Audio")]
    [Tooltip("The boom sound(s) that can play when this explosion appears. If you add more than one, a random one is picked each time - drag in as many variations as you'd like. If left empty, this script just quietly skips playing anything (the screen shake still works fine on its own) - it doesn't throw an error.")]
    public AudioClip[] explosionSounds;

    [Range(0f, 3f)]
    [Tooltip("Playback volume multiplier - 1 is the clip's normal volume. This goes up to 3 for real headroom above 'normal', but see Spatial Blend below first - that's the far more likely fix if explosions sound quiet.")]
    public float volume = 1f;

    // This is very likely the BIGGER reason explosions sound quiet, more so
    // than the volume number above - exact same story as
    // ProximityWoosh.cs's spatialBlend field: a freshly-added AudioSource
    // (the one RequireComponent above adds automatically) plays as fully
    // 3D-positional audio by default, so however far the explosion happens
    // to be from the camera at that instant silently eats into the volume
    // on top of whatever's typed into the field above - meaning the exact
    // same explosion could sound louder or quieter purely based on where
    // on screen it happened. 0 turns that off entirely (always plays at
    // exactly the volume above, no matter the distance); 1 is fully
    // realistic 3D falloff. Starting low keeps a little natural
    // positional feel without letting distance silently undercut the
    // volume you actually asked for.
    [Range(0f, 1f)]
    [Tooltip("How 'positional' the boom sounds. 0 = always plays at exactly the volume above, no matter how far the explosion is from the camera. 1 = fully realistic 3D audio that fades out with distance. If explosions still sound inconsistently quiet depending on where they happen, lower this toward 0.")]
    public float spatialBlend = 0.3f;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();

        audioSource = GetComponent<AudioSource>();

        // Play On Awake defaults to ON for a freshly-added AudioSource,
        // which would make this explosion sound play immediately the very
        // first time it's ever instantiated into the pool (before it's
        // even been positioned anywhere sensible) - this script is the one
        // deciding exactly when the sound plays (down in OnEnable below),
        // so that default needs to be turned off.
        audioSource.playOnAwake = false;

        // Doppler Level defaults to 1 on a fresh AudioSource, which
        // pitch-shifts a sound based on how fast the camera (the
        // AudioListener) is moving relative to it - and since THIS exact
        // sound plays at the very same instant this same object is
        // triggering a big Cinemachine Impulse camera shake, it would be
        // guaranteed to hit that same "sounds sped up" Doppler bug we just
        // tracked down and fixed on the laser and whoosh sounds. Turning
        // it off here from the start means the explosion sound never has
        // that problem in the first place.
        audioSource.dopplerLevel = 0f;

        // See the comment on the spatialBlend field above - this is what
        // actually applies that setting to the real AudioSource component,
        // since spatialBlend up there is just a public Inspector field
        // until something copies its value onto the AudioSource itself.
        audioSource.spatialBlend = spatialBlend;
    }

    // OnEnable (not Start) is what makes this work correctly with object
    // pooling: DeathByCollision spawns this explosion through
    // ObjectPoolManager, which always finishes positioning a pooled object
    // BEFORE activating it (see the comment on ObjectPoolManager.Spawn()
    // for why that ordering matters) - so by the time OnEnable() fires
    // here, transform.position is already the correct, current explosion
    // location every single time this object gets reused, not just on its
    // very first spawn ever.
    private void OnEnable()
    {
        impulseSource.GenerateImpulse();
        PlayExplosionSound();
    }

    // Pulled out into its own method mainly for the empty-array guard
    // below - without it, an explosion prefab variant that hasn't had any
    // clips dragged into explosionSounds yet would throw an error the
    // instant anything died, instead of just quietly playing no sound.
    private void PlayExplosionSound()
    {
        if (explosionSounds == null || explosionSounds.Length == 0)
        {
            // CHANGED to actually log a warning here (this used to fail
            // silently) - since there are multiple different explosion
            // prefab variants in this project (RiftExplosionBlue,
            // RiftExplosionYellow, YellowFireImpactV2), it was way too
            // easy to miss dragging clips into one of them and have
            // literally no way to tell that was the problem versus some
            // deeper audio bug. Same pattern ProximityWoosh.cs already
            // uses for its own empty-array case.
            Debug.LogWarning("[ExplosionImpulse] No explosion sounds assigned on " + name + " - drag at least one AudioClip into the Explosion Sounds list in the Inspector.", this);
            return;
        }

        // Random.Range(min, max) with an int max is EXCLUSIVE of that max
        // - so this correctly lands anywhere from index 0 up to (but never
        // past) the last valid index in the array.
        AudioClip chosenClip = explosionSounds[Random.Range(0, explosionSounds.Length)];

        // PlayOneShot (rather than setting .clip and calling .Play())
        // means if this same pooled explosion object somehow gets reused
        // again before the previous boom has fully finished playing, the
        // new sound plays on TOP of the tail end of the old one instead of
        // cutting it off - which sounds much more natural for something
        // as short and punchy as an explosion.
        audioSource.PlayOneShot(chosenClip, volume);
    }
}