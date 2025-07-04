using NAudio.Wave;
using System;
using System.IO;

public class LoopingSoundPlayer : IDisposable
{
	private IWavePlayer outputDevice;
	private LoopStream loopStream;
	private AudioFileReader audioReader;
	private string tempFilePath;
	private bool isLooping;

	// Nová vlastnost: nastavitelné volume (0.0f – 1.0f)
	public float Volume { get; set; } = 0.3f;

	public void StartLoop(byte[] soundData)
	{
		if (isLooping) return;

		tempFilePath = Path.GetTempFileName();
		File.WriteAllBytes(tempFilePath, soundData);

		audioReader = new AudioFileReader(tempFilePath)
		{
			Volume = Volume
		};

		loopStream = new LoopStream(audioReader);
		outputDevice = new WaveOutEvent();

		outputDevice.Init(loopStream);
		outputDevice.Play();

		isLooping = true;
	}

	public void StopLoopAfterCurrent()
	{
		if (!isLooping) return;
		if (loopStream != null)
		{
			loopStream.EnableLooping = false;
		}
		isLooping = false;
	}

	public void Dispose()
	{
		outputDevice?.Stop();
		outputDevice?.Dispose();
		outputDevice = null;

		loopStream?.Dispose();
		loopStream = null;

		audioReader?.Dispose();
		audioReader = null;

		if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
		{
			File.Delete(tempFilePath);
		}

		tempFilePath = null;
		isLooping = false;
	}

	private class LoopStream : WaveStream
	{
		private readonly WaveStream sourceStream;
		public bool EnableLooping = true;

		public LoopStream(WaveStream sourceStream)
		{
			this.sourceStream = sourceStream;
		}

		public override WaveFormat WaveFormat => sourceStream.WaveFormat;
		public override long Length => sourceStream.Length;

		public override long Position
		{
			get => sourceStream.Position;
			set => sourceStream.Position = value;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int totalBytesRead = 0;

			while (totalBytesRead < count)
			{
				int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
				if (bytesRead == 0)
				{
					if (EnableLooping)
					{
						sourceStream.Position = 0;
					}
					else
					{
						break;
					}
				}
				totalBytesRead += bytesRead;
			}

			return totalBytesRead;
		}
	}
}
public static class SoundHelper
{
	// Nový parametr volume s výchozí hodnotou 1.0f
	public static void PlayOnce(byte[] soundData, float volume = 1.0f)
	{
		if (soundData == null || soundData.Length == 0)
			return;

		string tempPath = Path.GetTempFileName();
		File.WriteAllBytes(tempPath, soundData);

		var audioReader = new AudioFileReader(tempPath)
		{
			Volume = volume
		};

		var outputDevice = new WaveOutEvent();
		outputDevice.Init(audioReader);
		outputDevice.Play();

		outputDevice.PlaybackStopped += (s, e) =>
		{
			outputDevice.Dispose();
			audioReader.Dispose();

			if (File.Exists(tempPath))
				File.Delete(tempPath);
		};
	}

}
