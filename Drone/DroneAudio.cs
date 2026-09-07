using System.Reflection;
using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Motor noise for the drone. The game's AudioManager only fires one-shot clips at a position
    /// and ships no drone-motor sound, so this synthesises a looping quad whine at runtime (a few
    /// detuned saw partials for the motors plus filtered noise for prop wash) and plays it from a
    /// 3D AudioSource on the craft itself. Pitch and volume track throttle, so it spools up and
    /// down as you fly.
    /// </summary>
    internal class DroneAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const float ClipSeconds = 1f;

        private AudioSource _source;
        private float _currentThrottle;

        public static DroneAudio Attach(GameObject droneObject)
        {
            DroneAudio audio = droneObject.GetComponent<DroneAudio>();
            if ((bool)audio)
            {
                return audio;
            }
            return droneObject.AddComponent<DroneAudio>();
        }

        /// <summary>Called from the flight loop each tick with the current throttle (0-1).</summary>
        public void SetThrottle(float throttle01)
        {
            _currentThrottle = Mathf.Clamp01(throttle01);
        }

        private void Start()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.clip = BuildMotorClip();
            _source.loop = true;
            // The AudioListener stays on the player's body (the game never moves it), so a fully
            // 3D source would fade out as the drone flies away - exactly wrong when you're the
            // one wearing the goggles. Mostly-2D keeps the motors in your ears while still giving
            // a bit of directionality.
            _source.spatialBlend = 0.25f;
            _source.rolloffMode = AudioRolloffMode.Linear;
            _source.minDistance = 5f;
            _source.maxDistance = 200f;
            _source.volume = 0.35f;
            _source.Play();
        }

        private void Update()
        {
            if (!_source)
            {
                return;
            }
            // Idle whine at low stick, screaming at full throttle.
            _source.pitch = Mathf.Lerp(0.75f, 1.7f, _currentThrottle);
            _source.volume = Mathf.Lerp(0.18f, 0.5f, _currentThrottle);
        }

        private void OnDestroy()
        {
            if ((bool)_source)
            {
                _source.Stop();
            }
        }

        private static AudioClip BuildMotorClip()
        {
            int sampleCount = (int)(SampleRate * ClipSeconds);
            float[] samples = new float[sampleCount];

            // Four slightly detuned motors, so they beat against each other the way real quads do
            // instead of sounding like one clean tone.
            float[] motorHz = { 118f, 124f, 131f, 137f };
            float noiseState = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float value = 0f;

                foreach (float hz in motorHz)
                {
                    // Saw wave - buzzy, like a brushless motor, richer than a sine.
                    float phase = (t * hz) % 1f;
                    value += (phase * 2f - 1f) * 0.16f;
                    // A little second harmonic for bite.
                    float phase2 = (t * hz * 2f) % 1f;
                    value += (phase2 * 2f - 1f) * 0.05f;
                }

                // Low-passed white noise for prop wash / air.
                float white = Random.value * 2f - 1f;
                noiseState = Mathf.Lerp(noiseState, white, 0.08f);
                value += noiseState * 0.22f;

                samples[i] = Mathf.Clamp(value * 0.7f, -1f, 1f);
            }

            // Cross-fade the tail into the head so the loop point isn't an audible click.
            int fade = SampleRate / 100;
            for (int i = 0; i < fade; i++)
            {
                float blend = (float)i / fade;
                samples[i] = Mathf.Lerp(samples[sampleCount - fade + i], samples[i], blend);
            }

            AudioClip clip = AudioClip.Create("DroneMotor", sampleCount, 1, SampleRate, stream: false);

            // AudioClip.SetData has both float[] and ReadOnlySpan<float> overloads. Calling it
            // directly makes the compiler consider the Span one, which doesn't exist on net48
            // (CS7069), so bind to the float[] overload by reflection instead.
            MethodInfo setData = typeof(AudioClip).GetMethod(
                nameof(AudioClip.SetData),
                new[] { typeof(float[]), typeof(int) });
            setData.Invoke(clip, new object[] { samples, 0 });

            return clip;
        }
    }
}
