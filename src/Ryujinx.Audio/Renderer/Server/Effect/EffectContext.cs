using Ryujinx.Audio.Renderer.Parameter;
using Ryujinx.Audio.Renderer.Utils;
using System;
using System.Diagnostics;

namespace Ryujinx.Audio.Renderer.Server.Effect
{
    /// <summary>
    /// Effect context.
    /// </summary>
    public class EffectContext
    {
        /// <summary>
        /// Storage for <see cref="BaseEffect"/>.
        /// </summary>
        private BaseEffect[] _effects;

        private EffectResultState[] _resultStatesCpu;
        private EffectResultState[] _resultStatesDsp;

        /// <summary>
        /// Create a new <see cref="EffectContext"/>.
        /// </summary>
        public EffectContext()
        {
            _effects = Array.Empty<BaseEffect>();
        }

        /// <summary>
        /// Initialize the <see cref="EffectContext"/>.
        /// </summary>
        /// <param name="effectCount">The total effect count.</param>
        /// <param name="resultStateCount">The total result state count.</param>
        public void Initialize(uint effectCount, uint resultStateCount)
        {
            _effects = new BaseEffect[effectCount];

            for (int i = 0; i < _effects.Length; i++)
            {
                _effects[i] = new BaseEffect();
            }

            _resultStatesCpu = new EffectResultState[resultStateCount];
            _resultStatesDsp = new EffectResultState[resultStateCount];
        }

        /// <summary>
        /// Get the total effect count.
        /// </summary>
        /// <returns>The total effect count.</returns>
        public uint GetCount()
        {
            return (uint)_effects.Length;
        }

        /// <summary>
        /// Get a reference to a <see cref="BaseEffect"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>A reference to a <see cref="BaseEffect"/> at the given <paramref name="index"/>.</returns>
        public ref BaseEffect GetEffect(int index)
        {
            Debug.Assert(index >= 0 && index < _effects.Length);

            return ref _effects[index];
        }

        /// <summary>
        /// Get a reference to a <see cref="EffectResultState"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>A reference to a <see cref="EffectResultState"/> at the given <paramref name="index"/>.</returns>
        /// <remarks>The returned <see cref="EffectResultState"/> should only be used when updating the server state.</remarks>
        public ref EffectResultState GetState(int index)
        {
            Debug.Assert(index >= 0 && index < _resultStatesCpu.Length);

            return ref _resultStatesCpu[index];
        }

        /// <summary>
        /// Get a reference to a <see cref="EffectResultState"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>A reference to a <see cref="EffectResultState"/> at the given <paramref name="index"/>.</returns>
        /// <remarks>The returned <see cref="EffectResultState"/> should only be used in the context of processing on the <see cref="Dsp.AudioProcessor"/>.</remarks>
        public ref EffectResultState GetDspState(int index)
        {
            Debug.Assert(index >= 0 && index < _resultStatesDsp.Length);

            return ref _resultStatesDsp[index];
        }

        /// <summary>
        /// Get a memory instance to a <see cref="EffectResultState"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>A memory instance to a <see cref="EffectResultState"/> at the given <paramref name="index"/>.</returns>
        /// <remarks>The returned <see cref="Memory{EffectResultState}"/> should only be used in the context of processing on the <see cref="Dsp.AudioProcessor"/>.</remarks>
        public Memory<EffectResultState> GetDspStateMemory(int index)
        {
            return SpanIOHelper.GetMemory(_resultStatesDsp.AsMemory(), index, (uint)_resultStatesDsp.Length);
        }

        /// <summary>
        /// Update internal state during command generation.
        /// </summary>
        public void UpdateResultStateForCommandGeneration()
        {
            for (int index = 0; index < _resultStatesCpu.Length; index++)
            {
                _effects[index].UpdateResultState(ref _resultStatesCpu[index], ref _resultStatesDsp[index]);
            }
        }
    }
}