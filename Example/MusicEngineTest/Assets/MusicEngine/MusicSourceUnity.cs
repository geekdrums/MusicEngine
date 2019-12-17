using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicSourceUnity : MonoBehaviour, IMusicSource
{
	public Section[] sections_;
	private AudioSource[] musicSources_;
	private AudioSource[] transitionMusicSources_;

	private int sectionIndex_;


	#region properties

	public Section this[int index]
	{
		get
		{
			if( 0 <= index && index < sections_.Length )
			{
				return sections_[index];
			}
			else
			{
				Debug.LogWarning("Section index out of range! index = " + index + ", SectionCount = " + sections_.Count);
				return null;
			}
		}
	}

	#endregion

	#region unity functions

	void Awake()
	{

	}


	#endregion


	#region IMusicSource

	public bool PlayOnStart { get; private set; }

	public bool IsPlaying { get; private set; }

	public string SequenceName { get; }

	public int SequenceIndex { get; }


	public void Play();

	public void Stop();

	public void Suspend();

	public void Resume();

	public int GetCurrentSample();

	public int GetSampleRate();

	public MusicMeter GetCurrentMeter(int currentSample);



	public int GetSequenceEndBar();

	public bool CheckHorizontalSequenceChanged();

	public void OnRepeated();

	public void OnHorizontalSequenceChanged();



	public void SetHorizontalSequence(string name);

	public void SetHorizontalSequenceByIndex(int index);

	public void SetVerticalMix(float param);

	public void SetVerticalMixByName(string name);

	#endregion
}
