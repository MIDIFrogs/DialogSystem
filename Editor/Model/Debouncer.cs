using System;

namespace MIDIFrogs.DialogSystem.Editor.Model
{
    /// <summary>Becomes due once, <c>delay</c> seconds after the last <see cref="Poke"/>. Drives autosave.</summary>
    public sealed class Debouncer
    {
        private readonly double delay;
        private readonly Func<double> clock;
        private double lastPoke;
        private bool pending;

        public Debouncer(double delaySeconds, Func<double> clock)
        {
            delay = delaySeconds;
            this.clock = clock;
        }

        public bool Pending => pending;

        public void Poke()
        {
            lastPoke = clock();
            pending = true;
        }

        public bool Due()
        {
            if (!pending || clock() - lastPoke < delay)
            {
                return false;
            }

            pending = false;
            return true;
        }

        /// <summary>Forgets a pending poke (after an immediate save).</summary>
        public void Cancel() => pending = false;
    }
}
