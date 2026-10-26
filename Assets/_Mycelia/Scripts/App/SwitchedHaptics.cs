using System;
using Mycelia.Game;

namespace Mycelia.App
{
    /// <summary>
    /// The haptics every scene gets (ADR-0016): the device's, played only while the saved haptics switch is on. The
    /// switch is read on every call, so a change in the settings modal applies at once. Allocation-free.
    /// </summary>
    public sealed class SwitchedHaptics : IHaptics
    {
        readonly PlayerProgress progress;
        readonly IHaptics device;

        public SwitchedHaptics(PlayerProgress progress, IHaptics device)
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.device = device ?? throw new ArgumentNullException(nameof(device));
        }

        public void Play(Haptic haptic)
        {
            if (progress.Settings.Haptics)
                device.Play(haptic);
        }
    }
}
