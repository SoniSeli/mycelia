using System;
using Mycelia.Game;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// Android's vibrator, driven through JNI (ADR-0016). Everything Java is looked up once, here: the vibrator, its
    /// <c>vibrate(VibrationEffect)</c> method and one effect per <see cref="Haptic"/>, each with its call arguments
    /// ready. Playing one is then a single JNI call that allocates nothing. Android 10+ plays its built-in effects;
    /// Android 8–9 a short one-shot with an amplitude. It never throws: if anything fails, haptics go quiet.
    /// Needs the VIBRATE permission, which the build adds (<c>AndroidVibratePermission</c>).
    /// </summary>
    public sealed class AndroidHaptics : IHaptics
    {
        /// <summary>How each haptic feels: a built-in effect (Android 10+), else a one-shot of so many ms at an amplitude (1–255).</summary>
        public readonly struct Strength
        {
            /// <summary>No built-in effect: always the one-shot.</summary>
            public const int OneShotOnly = -1;

            public readonly int Predefined;
            public readonly long Milliseconds;
            public readonly int Amplitude;

            public Strength(int predefined, long milliseconds, int amplitude)
            {
                Predefined = predefined;
                Milliseconds = milliseconds;
                Amplitude = amplitude;
            }
        }

        // android.os.VibrationEffect's public EFFECT_* constants.
        public const int EffectClick = 0;
        public const int EffectTick = 2;

        /// <summary>The strength of each haptic (tuned on the phone, ADR-0016).</summary>
        public static Strength For(Haptic haptic) => haptic switch
        {
            Haptic.Tick => new Strength(EffectTick, 10, 64),
            Haptic.Commit => new Strength(EffectClick, 20, 150),
            // Felt side by side on the phone, a long full buzz stood apart from the commit best (Soni, 2026-10-26).
            Haptic.Win => new Strength(Strength.OneShotOnly, 150, 255),
            // A soft, longer pulse: felt, but not a jolt (there is no public soft effect).
            _ => new Strength(Strength.OneShotOnly, 80, 70),
        };

#if UNITY_ANDROID && !UNITY_EDITOR
        readonly AndroidJavaObject vibrator;
        readonly AndroidJavaObject[] effects;
        readonly jvalue[][] arguments;
        readonly IntPtr vibrate;
        bool failed;

        AndroidHaptics(AndroidJavaObject vibrator, AndroidJavaObject[] effects, IntPtr vibrate)
        {
            this.vibrator = vibrator;
            this.effects = effects;
            this.vibrate = vibrate;
            arguments = new jvalue[effects.Length][];
            for (int i = 0; i < effects.Length; i++)
                arguments[i] = new[] { new jvalue { l = effects[i].GetRawObject() } };
        }

        public void Play(Haptic haptic)
        {
            if (failed)
                return;

            AndroidJNI.CallVoidMethod(vibrator.GetRawObject(), vibrate, arguments[(int)haptic]);
            IntPtr exception = AndroidJNI.ExceptionOccurred();
            if (exception == IntPtr.Zero)
                return;

            AndroidJNI.ExceptionClear();
            AndroidJNI.DeleteLocalRef(exception);
            failed = true;
            Debug.LogWarning("[Mycelia] Vibrating failed; haptics are off until the app restarts.");
        }

        /// <summary>The phone's vibrator, or <see cref="NoHaptics"/> if it has none or it can't be reached.</summary>
        public static IHaptics Create()
        {
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                    return new NoHaptics();

                int sdk;
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    sdk = version.GetStatic<int>("SDK_INT");

                int count = Enum.GetValues(typeof(Haptic)).Length;
                var effects = new AndroidJavaObject[count];
                using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                {
                    for (int i = 0; i < count; i++)
                    {
                        Strength strength = For((Haptic)i);
                        effects[i] = sdk >= 29 && strength.Predefined != Strength.OneShotOnly
                            ? effect.CallStatic<AndroidJavaObject>("createPredefined", strength.Predefined)
                            : effect.CallStatic<AndroidJavaObject>("createOneShot", strength.Milliseconds, strength.Amplitude);
                    }
                }

                IntPtr vibrate = AndroidJNI.GetMethodID(vibrator.GetRawClass(), "vibrate", "(Landroid/os/VibrationEffect;)V");
                if (vibrate == IntPtr.Zero)
                {
                    AndroidJNI.ExceptionClear();
                    Debug.LogWarning("[Mycelia] No haptics: the vibrator has no vibrate(VibrationEffect).");
                    return new NoHaptics();
                }
                return new AndroidHaptics(vibrator, effects, vibrate);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Mycelia] No haptics: {exception.Message}");
                return new NoHaptics();
            }
        }
#else
        AndroidHaptics() { }

        public void Play(Haptic haptic) { }

        /// <summary>Off Android there is no vibrator to drive.</summary>
        public static IHaptics Create() => new NoHaptics();
#endif
    }
}
