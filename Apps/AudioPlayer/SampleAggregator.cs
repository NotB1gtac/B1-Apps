using NAudio.Wave;
using System;

namespace B1_Apps.Apps.AudioPlayer
{
	public class SampleAggregator : ISampleProvider
	{
		private readonly ISampleProvider source;
		private readonly int fftLength;
		private readonly float[] buffer;
		private int bufferPosition;

		public event EventHandler<FftEventArgs> FftCalculated;

		public SampleAggregator(ISampleProvider source, int fftLength)
		{
			this.source = source ?? throw new ArgumentNullException(nameof(source));
			this.fftLength = fftLength;
			this.buffer = new float[fftLength * 2]; // Double buffer for overlap
			this.bufferPosition = 0;
		}

		public WaveFormat WaveFormat => source.WaveFormat;

		public int Read(float[] buffer, int offset, int count)
		{
			int samplesRead = source.Read(buffer, offset, count);

			for (int n = 0; n < samplesRead; n++)
			{
				// Mono conversion if needed
				float sample = buffer[offset + n];
				if (WaveFormat.Channels > 1)
				{
					sample = (buffer[offset + n] + buffer[offset + n + 1]) / 2f;
					n++; // Skip next channel
				}

				this.buffer[bufferPosition++] = sample;

				// When buffer is full, calculate FFT
				if (bufferPosition >= this.buffer.Length)
				{
					bufferPosition = fftLength; // Keep half for overlap

					// Prepare FFT input (apply window function)
					var fftInput = new float[fftLength];
					Array.Copy(this.buffer, 0, fftInput, 0, fftLength);
					ApplyHannWindow(fftInput);

					// Raise event with FFT data
					FftCalculated?.Invoke(this, new FftEventArgs(fftInput));

					// Shift buffer for overlap
					Array.Copy(this.buffer, fftLength, this.buffer, 0, fftLength);
				}
			}

			return samplesRead;
		}

		private void ApplyHannWindow(float[] buffer)
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				// Hann window reduces spectral leakage
				buffer[i] *= (float)(0.5 * (1 - Math.Cos(2 * Math.PI * i / (buffer.Length - 1))));
			}
		}
	}
}