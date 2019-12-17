using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IMusicSource
{
	bool PlayOnStart { get; }

	bool IsPlaying { get; }

	string PlayingSequenceName { get; }


	void Play();

	void Stop();

	void Suspend();

	void Resume();

	int GetCurrentSample();

	int GetSampleRate();

	Meter GetCurrentMeter(int currentSample);



	int GetSequenceEndBar();

	bool CheckHorizontalSequenceChanged();

	void OnRepeated();

	void OnHorizontalSequenceChanged();



	void SetHorizontalSequence(string name);

	void SetHorizontalSequenceByIndex(int index);

	void SetVerticalMix(float param);

	void SetVerticalMixByName(string name);

}
