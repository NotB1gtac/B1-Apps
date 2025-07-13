using System;

namespace B1_Apps.Apps.AudioPlayer
{
	public class FftEventArgs : EventArgs
	{
		public float[] Result { get; }

		public FftEventArgs(float[] result)
		{
			Result = result ?? throw new ArgumentNullException(nameof(result));
		}
	}
}