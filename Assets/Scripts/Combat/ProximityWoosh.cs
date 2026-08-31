using UnityEngine;

// Plays a "whoosh" sound the moment the player's ship passes through a
// WIDER trigger zone around an obstacle - even if the ship never actually
// hits the obstacle itself. This is what gives a close call that "something
// just screamed past me" feeling, the same way a car passing you at high
// speed on the highway sounds completely different from one that's still
// far off in the distance.
//
// SETUP: attach this to its OWN separate trigger collider, sized bigger
// than the obstacle's real hitbox - typically a new empty child object
// under the obstacle/enemy prefab, with its own Collider (Is Trigger
// checked) roughly 2-3x the size of the obstacle's actual collision shape.
// Keeping this on a SEPARATE child object (rather than reusing the
// obstacle's own collider) means it's completely isolated from
// DeathByCollision's collision/death logic sitting on the parent -
// clipping through the wider "near miss" zone can never accidentally cost
// you a life or count as a hit, since that's a totally different
// Collider/GameObject entirely.
[RequireComponent(typeof(Collider))]
public class ProximityWoosh : MonoBehaviour
{
    // CHANGED from a single AudioClip to an array: dragging more than one
    // whoosh variation in here means the same near-miss doesn't sound
    // identical every single time - with enemies and asteroids constantly
    // flying past you throughout a run, one lone sound repeating over and
    // over gets noticeably repetitive fast, while 2-3 slightly different
    // takes on the same "whoosh" makes each pass feel a little more organic,
    // the same way real passing-by sounds are never bit-for-bit identical.
    // You can still drop in just ONE clip here if that's all you want - it'll
    // just always pick that same one, same as before.
    [Tooltip("The whoosh sound(s) that can play on a pass. If you add more than one, a random one is picked each time - drag in as many variations as you'd like.")]
    public AudioClip[] wooshSounds;

    // CHANGED the Range from 0-1 to 0-3: the old ceiling of 1 turned out to
    // be part of why the whoosh sounded quiet even fully maxed out - see
    // the comment on PlaySoundWithControl() below for the real explanation
    // (it's actually mostly about spatialBlend, not this number), but
    // giving yourself genuine headroom ABOVE a clip's "normal" volume is
    // still useful on its own, since a source clip that was recorded/
    // generated a bit quiet has nowhere to go if 1 is treated as the
    // absolute max.
    [Range(0f, 3f)]
    [Tooltip("Playback volume multiplier - 1 is a clip's normal volume. This goes up to 3 so there's real headroom above 'normal' if a clip just sounds quiet. Pushing much past ~2 can start to sound a little crunchy/distorted, so nudge it up gradually rather than jumping straight to the max.")]
    public float volume = 1f;

    // This is very likely the BIGGER reason the whoosh sounded quiet, more
    // so than the volume number above: AudioSource.PlayClipAtPoint (what
    // this used to use) always plays sounds fully 3D-positional, using
    // Unity's default distance falloff curve. That means however far the
    // obstacle happened to be from the camera at the exact instant it
    // played was silently eating into the volume on top of whatever number
    // was typed into the field above - so the same whoosh could sound
    // noticeably louder or quieter purely based on where on screen it
    // happened to trigger, with no way to see or control that from the
    // Inspector. spatialBlend lets you dial in how much of that effect you
    // actually want: 0 completely turns it off (the sound plays at exactly
    // the volume above, every single time, regardless of distance) while 1
    // is fully realistic "gets quieter the farther away it is" 3D audio,
    // the same as footsteps or engine sounds fading out as something moves
    // off screen. Starting low (0.3) keeps a little bit of that natural
    // positional feel without letting distance silently undercut the
    // volume you actually asked for.
    [Range(0f, 1f)]
    [Tooltip("How 'positional' the whoosh sounds. 0 = always plays at exactly the volume above, no matter how far away it happens. 1 = fully realistic 3D audio that fades out the farther the obstacle is from the camera when it plays. If the whoosh still sounds inconsistently quiet depending on where it happens on screen, lower this toward 0.")]
    public float spatialBlend = 0.3f;

    // Stops this from playing more than once for a single pass through the
    // zone - without it, the ship lingering right at the trigger's edge
    // (clipping in and out repeatedly) could spam the same whoosh over and
    // over instead of playing it exactly once per genuine near-miss.
    private bool hasPlayedThisPass;

    // Resets on every reuse from the pool - this object's parent obstacle
    // is itself a pooled prefab (see ObjectPoolManager.cs) - so a recycled
    // obstacle correctly plays its own fresh whoosh the next time
    // something passes it, rather than staying permanently "used up" after
    // whichever pass happened to be its very first one ever.
    private void OnEnable()
    {
        hasPlayedThisPass = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // (The temporary diagnostic Debug.Log that was here has been
        // removed now that we've confirmed the trigger itself was working
        // correctly all along - the quiet sound was a volume/spatial-audio
        // issue, not a "this never fires" issue.)
        if (hasPlayedThisPass || !IsPlayer(other))
        {
            return;
        }

        hasPlayedThisPass = true;

        PlayRandomWoosh();
    }

    // Picks one random clip out of wooshSounds and plays it. Pulled out into
    // its own method (rather than living inline in OnTriggerEnter) mainly
    // for the empty-array guard below - without it, an obstacle prefab that
    // hasn't had any clips dragged into the Inspector yet would throw a
    // division-by-zero-style error the moment the player flew past it,
    // instead of just quietly doing nothing.
    private void PlayRandomWoosh()
    {
        if (wooshSounds == null || wooshSounds.Length == 0)
        {
            Debug.LogWarning("[ProximityWoosh] No woosh sounds assigned on " + name + " - drag at least one AudioClip into the Woosh Sounds list in the Inspector.", this);
            return;
        }

        // Random.Range(min, max) with an int max is EXCLUSIVE of that max -
        // so this correctly lands anywhere from index 0 up to (but never
        // past) the last valid index in the array, rather than occasionally
        // reaching one slot too far and throwing an IndexOutOfRange error.
        AudioClip chosenClip = wooshSounds[Random.Range(0, wooshSounds.Length)];

        PlaySoundWithControl(chosenClip, transform.position);
    }

    // CHANGED from AudioSource.PlayClipAtPoint to this custom version.
    // PlayClipAtPoint is convenient (LaserSpawner.cs still uses it, and
    // that's fine to leave as-is) but it gives zero control over HOW 3D the
    // sound is - it always plays fully 3D-positional using Unity's default
    // distance falloff, with no Inspector-exposed way to change that. This
    // does the same basic trick PlayClipAtPoint does internally - spin up a
    // temporary GameObject with its own AudioSource, play the clip, then
    // clean itself up automatically once done - but lets volume AND
    // spatialBlend (see the tooltip above) actually be tuned from the
    // Inspector instead of being locked to Unity's defaults.
    private void PlaySoundWithControl(AudioClip clip, Vector3 position)
    {
        GameObject tempAudioObject = new GameObject("OneShotAudio_" + clip.name);
        tempAudioObject.transform.position = position;

        AudioSource tempSource = tempAudioObject.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = volume;
        tempSource.spatialBlend = spatialBlend;

        // Doppler Level defaults to 1 on a fresh AudioSource, which
        // pitch-shifts the sound based on how fast the camera (the
        // AudioListener) is moving relative to it - completely harmless
        // normally, but with the new explosion screen shake violently
        // moving the camera around, that shows up as a horrible "sped up"
        // pitch warble on anything playing nearby right as a shake
        // happens. Zeroing it out here means the whoosh's pitch stays
        // locked to normal no matter how hard the camera is shaking at
        // that exact moment.
        tempSource.dopplerLevel = 0f;

        tempSource.Play();

        // Destroys the temporary GameObject once the clip has finished
        // playing (clip.length seconds from now, timed from right now) -
        // nothing gets left behind cluttering up the Hierarchy once the
        // sound is done.
        Destroy(tempAudioObject, clip.length);
    }

    private void OnTriggerExit(Collider other)
    {
        // Allows another whoosh the NEXT time the ship passes through -
        // actually exiting the zone means this was a genuinely separate
        // pass, not just lingering right at the edge of it.
        if (IsPlayer(other))
        {
            hasPlayedThisPass = false;
        }
    }

    // Checks other.transform.root rather than just other itself, in case
    // the ship's own collider happens to live on a child object rather
    // than its top-level GameObject - the exact same kind of
    // collider-vs-root mismatch that turned out to matter for
    // EnemyDestroyer.cs during the object pooling work.
    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }
}