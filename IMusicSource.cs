using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IMusicSource
{
	bool PlayOnStart { get; }

	bool IsPlaying { get; }

	bool IsValid { get; }

	string SequenceName { get; }

	int SequenceIndex { get; }

	void Initialize();

	void Play();

	void Stop();

	void Suspend();

	void Resume();

	void Seek(int sequenceIndex, Timing seekTiming);

	int GetCurrentSample();

	int GetSampleRate();

	MusicMeter GetMeterFromSample(int currentSample);



	Timing GetSequenceEndTiming();

	void UpdateSequenceState();

	void OnRepeated();

	void OnHorizontalSequenceChanged();



	void SetHorizontalSequence(string name);

	void SetHorizontalSequenceByIndex(int index);

	void SetVerticalMix(float param);

	void SetVerticalMixByName(string name);

}
