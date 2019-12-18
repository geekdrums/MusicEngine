using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MusicSourceUnity : MonoBehaviour, IMusicSource
{
	[Range(0,1)]
	public float Volume = 1.0f;
	public AudioMixerGroup OutputMixerGroup;
	public bool _PlayOnStart;

	private MusicSection[] sections_;
	private AudioSource[] musicSources_;
	private AudioSource[] transitionMusicSources_;

	private int sectionIndex_ = 0;
	private int numTracks_ = 0;
	private bool isValid_ = true;

	// 僅かに遅らせることでPlayとPlayScheduleのズレを回避 https://qiita.com/tatmos/items/4c78c127291a0c3b74ed
	private static double SCHEDULE_DELAY = 0.1;
	private static double ScheduleDSPTime { get { return AudioSettings.dspTime + SCHEDULE_DELAY; } }

	#region properties

	public MusicSection CurrentSection { get { return sections_[sectionIndex_]; } }

	public MusicSection this[int index]
	{
		get
		{
			if( 0 <= index && index < sections_.Length )
			{
				return sections_[index];
			}
			else
			{
				Debug.LogWarning("Section index out of range! index = " + index + ", SectionCount = " + sections_.Length);
				return null;
			}
		}
	}

	#endregion

	#region unity functions

	void Awake()
	{
		sections_ = GetComponentsInChildren<MusicSection>();
		if( sections_.Length == 0 )
		{
			isValid_ = false;
		}
		foreach( MusicSection section in sections_ )
		{
			if( section.Clips.Length == 0 )
			{
				isValid_ = false;
				break;
			}
			numTracks_ = Math.Max(numTracks_, section.Clips.Length);
		}


		// AudioSourceを最大トラック数*2（遷移時に重なる分）まで生成。
		if( isValid_ )
		{
			musicSources_ = new AudioSource[numTracks_];
			transitionMusicSources_ = new AudioSource[numTracks_];
			for( int i = 0; i < numTracks_; ++i )
			{
				musicSources_[i] = new GameObject("MusicTrack" + i.ToString(), typeof(AudioSource)).GetComponent<AudioSource>();
				musicSources_[i].transform.parent = this.transform;
				musicSources_[i].outputAudioMixerGroup = OutputMixerGroup;
				musicSources_[i].volume = Volume;
				musicSources_[i].playOnAwake = false;
				musicSources_[i].loop = false;

				if( i < CurrentSection.Clips.Length )
				{
					musicSources_[i].clip = CurrentSection.Clips[i];
				}
			}
			for( int i = 0; i < numTracks_; ++i )
			{
				transitionMusicSources_[i] = new GameObject("TransitionMusicTrack" + i.ToString(), typeof(AudioSource)).GetComponent<AudioSource>();
				transitionMusicSources_[i].transform.parent = this.transform;
				transitionMusicSources_[i].outputAudioMixerGroup = OutputMixerGroup;
				transitionMusicSources_[i].volume = Volume;
				transitionMusicSources_[i].playOnAwake = false;
				musicSources_[i].loop = false;
			}
		}
	}

	void Start()
	{
		if( PlayOnStart )
		{
			Play();
		}
	}

	#endregion


	#region IMusicSource
	
	public bool PlayOnStart { get { return _PlayOnStart; } }

	public bool IsPlaying { get { return isValid_ && musicSources_[0].isPlaying; } }

	public bool IsValid { get { return isValid_; } }

	public string SequenceName { get { return sections_[sectionIndex_].name; } }

	public int SequenceIndex { get { return sectionIndex_; } }


	public void Play()
	{
		if( isValid_ == false )
		{
			return;
		}

		for( int i = 0; i < numTracks_; ++i )
		{
			if( musicSources_[i].clip != null )
			{
				musicSources_[i].PlayScheduled(MusicSourceUnity.ScheduleDSPTime);
			}
		}

		for( int i = 0; i < numTracks_; ++i )
		{
			if( CurrentSection.TransitionType == MusicSection.ETransitionType.Loop )
			{
				if( CurrentSection.Clips.Length <= i )
				{
					break;
				}
				// ループ波形予約
				transitionMusicSources_[i].clip = CurrentSection.Clips[i];
				transitionMusicSources_[i].timeSamples = CurrentSection.EntryPointSample;
				transitionMusicSources_[i].PlayScheduled(MusicSourceUnity.ScheduleDSPTime + CurrentSection.ExitPointSample);
			}
			if( CurrentSection.TransitionType == MusicSection.ETransitionType.Transition )
			{
				MusicSection transitionSection = this[CurrentSection.TransitionDestinationIndex];
				if( transitionSection == null || transitionSection.Clips.Length <= i )
				{
					break;
				}
				// 遷移波形予約
				transitionMusicSources_[i].clip = transitionSection.Clips[i];
				transitionMusicSources_[i].timeSamples = 0;
				transitionMusicSources_[i].PlayScheduled(MusicSourceUnity.ScheduleDSPTime + CurrentSection.ExitPointSample);
			}
		}
	}

	public void Stop()
	{
		if( isValid_ == false )
		{
			return;
		}

		for( int i = 0; i < numTracks_; ++i )
		{
			if( musicSources_[i].clip != null )
			{
				musicSources_[i].Stop();
			}
			if( transitionMusicSources_[i].clip != null )
			{
				transitionMusicSources_[i].Stop();
			}
		}
	}

	public void Suspend()
	{
		if( isValid_ == false )
		{
			return;
		}

		for( int i = 0; i < numTracks_; ++i )
		{
			if( musicSources_[i].clip != null )
			{
				musicSources_[i].Pause();
			}
			if( transitionMusicSources_[i].clip != null )
			{
				transitionMusicSources_[i].Pause();
			}
		}
	}

	public void Resume()
	{
		if( isValid_ == false )
		{
			return;
		}

		for( int i = 0; i < numTracks_; ++i )
		{
			if( musicSources_[i].clip != null )
			{
				musicSources_[i].UnPause();
			}
			if( transitionMusicSources_[i].clip != null )
			{
				transitionMusicSources_[i].UnPause();
			}
		}
	}

	public int GetCurrentSample()
	{
		return musicSources_[0].timeSamples;
	}

	public int GetSampleRate()
	{
		return musicSources_[0].clip.frequency;
	}

	public MusicMeter GetCurrentMeter(int currentSample)
	{
		MusicMeter res = null;
		foreach( MusicMeter meter in CurrentSection.Meters )
		{
			if( currentSample < meter.StartSamples )
			{
				return res;
			}
			res = meter;
		}
		return res;
	}



	public int GetSequenceEndBar()
	{
		return 0;
	}

	public bool CheckHorizontalSequenceChanged() { return false; }

	public void OnRepeated()
	{
		// 遷移予約
	}

	public void OnHorizontalSequenceChanged()
	{
		// 遷移予約
	}



	public void SetHorizontalSequence(string name) { }

	public void SetHorizontalSequenceByIndex(int index) { }

	public void SetVerticalMix(float param) { }

	public void SetVerticalMixByName(string name) { }

	#endregion
}
