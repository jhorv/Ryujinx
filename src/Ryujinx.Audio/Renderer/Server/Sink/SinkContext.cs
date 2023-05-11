using System;
using System.Diagnostics;

namespace Ryujinx.Audio.Renderer.Server.Sink
{
    /// <summary>
    /// Sink context.
    /// </summary>
    public class SinkContext
    {
        /// <summary>
        /// Storage for <see cref="BaseSink"/>.
        /// </summary>
        private BaseSink[] _sinks;

        /// <summary>
        /// Initialize the <see cref="SinkContext"/>.
        /// </summary>
        /// <param name="sinksCount">The total sink count.</param>
        public void Initialize(uint sinksCount)
        {
            int length = checked((int)sinksCount);
            _sinks = length == 0 ? Array.Empty<BaseSink>() : new BaseSink[length];

            for (int i = 0; i < _sinks.Length; i++)
            {
                _sinks[i] = new BaseSink();
            }
        }

        /// <summary>
        /// Get the total sink count.
        /// </summary>
        /// <returns>The total sink count.</returns>
        public uint GetCount()
        {
            return (uint)_sinks.Length;
        }

        /// <summary>
        /// Get a reference to a <see cref="BaseSink"/> at the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The index to use.</param>
        /// <returns>A reference to a <see cref="BaseSink"/> at the given <paramref name="id"/>.</returns>
        public ref BaseSink GetSink(int id)
        {
            Debug.Assert(id >= 0 && id < _sinks.Length);

            return ref _sinks[id];
        }
    }
}