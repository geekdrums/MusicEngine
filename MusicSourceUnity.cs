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
	private int nextSectionIndex_ = -1;
	private int numTracks_ = 0;

	// 僅かに遅らせることでPlayとPlayScheduleのズレを回避 https://qiita.com/tatmos/items/4c78c127291a0c3b74ed
	private static double SCHEDULE_DELAY = 0.1;
	private static double ScheduleDSPTime { get { return AudioSettings.dspTime + SCHEDULE_DELAY; } }

	public enum EState
	{
		Invalid,
		Ready,
		Playing,
		Suspended,
		Finished
	}
	public EState State { get; private set; }

	#region properties

	public MusicSection CurrentSection { get { return sections_[sectionIndex_]; } }

	public MusicSection NextSection { get { return sections_[nextSectionIndex_]; } }

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
		State = EState.Ready;

		sections_ = GetComponentsInChildren<MusicSection>();
		if( sections_.Length == 0 )
		{
			State = EState.Invalid;
		}
		foreach( MusicSection section in sections_ )
		{
			if( section.IsValid == false )
			{
				State = EState.Invalid;
				break;
			}
			numTracks_ = Math.Max(numTracks_, section.Clips.Length);
		}

		// AudioSourceを最大トラック数*2（遷移時に重なる分）まで生成。
		if( State != EState.Invalid )
		{
			GameObject trackParent = new GameObject("Tracks");
			trackParent.transform.parent = this.transform;
			musicSources_ = new AudioSource[numTracks_];
			transitionMusicSources_ = new AudioSource[numTracks_];
			for( int i = 0; i < numTracks_; ++i )
			{
				musicSources_[i] = new GameObject("MusicTrack" + i.ToString(), typeof(AudioSource)).GetComponent<AudioSource>();
				musicSources_[i].transform.parent = trackParent.transform;
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
				transitionMusicSources_[i].transform.parent = trackParent.transform;
				transitionMusicSources_[i].outputAudioMixerGroup = OutputMixerGroup;
				transitionMusicSources_[i].volume = Volume;
				transitionMusicSources_[i].playOnAwake = false;
				musicSources_[i].loop = false;
			}
		}
	}

	#endregion


	#region IMusicSource
	
	public bool PlayOnStart { get { return _PlayOnStart; } }

	public bool IsPlaying { get { return State == EState.Playing; } }

	public bool IsValid { get { return State != EState.Invalid; } }

	public string SequenceName { get { return sections_[sectionIndex_].name; } }

	public int SequenceIndex { get { return sectionIndex_; } }


	public void Play()
	{
		if( State == EState.Invalid )
		{
			return;
		}

		State = EState.Playing;

		for( int i = 0; i < numTracks_; ++i )
		{
			if( musicSources_[i].clip != null )
			{
				musicSources_[i].PlayScheduled(MusicSourceUnity.ScheduleDSPTime);
			}
		}

		SetTransitionSchedule(MusicSourceUnity.ScheduleDSPTime);
	}

	void SetTransitionSchedule(double startedDSPTime)
	{
		if( CurrentSection.TransitionType == MusicSection.ETransitionType.Loop )
		{
			nextSectionIndex_ = sectionIndex_;
			for( int i = 0; i < numTracks_; ++i )
			{
				if( CurrentSection.Clips.Length <= i )
				{
					break;
				}
				// ループ波形予約
				double loopEndTime = (double)CurrentSection.LoopEndSample / GetSampleRate();
				transitionMusicSources_[i].clip = CurrentSection.Clips[i];
				transitionMusicSources_[i].timeSamples = CurrentSection.LoopStartSample;
				transitionMusicSources_[i].PlayScheduled(startedDSPTime + loopEndTime);
				musicSources_[i].SetScheduledEndTime(startedDSPTime + loopEndTime);
			}
		}
		else if( CurrentSection.TransitionType == MusicSection.ETransitionType.Transition )
		{
			nextSectionIndex_ = CurrentSection.TransitionDestinationIndex;
			for( int i = 0; i < numTracks_; ++i )
			{
				if( NextSection != null && i < NextSection.Clips.Length )
				{
					// 遷移波形予約
					double syncPointTime = (double)(CurrentSection.ExitPointSample - NextSection.EntryPointSample - GetCurrentSample()) / GetSampleRate();
					transitionMusicSources_[i].clip = NextSection.Clips[i];
					transitionMusicSources_[i].timeSamples = 0;
					transitionMusicSources_[i].PlayScheduled(startedDSPTime + syncPointTime);
				}

				if( i < CurrentSection.Clips.Length )
				{
					// todo: fade
					double exitPointTime = (double)CurrentSection.ExitPointSample / GetSampleRate();
					musicSources_[i].SetScheduledEndTime(startedDSPTime + exitPointTime);
				}
			}
		}
	}

	public void Stop()
	{
		if( State == EState.Invalid )
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

		State = EState.Finished;
	}

	public void Suspend()
	{
		if( State == EState.Invalid )
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

		State = EState.Suspended;
	}

	public void Resume()
	{
		if( State == EState.Invalid )
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

		State = EState.Playing;
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

	public bool CheckHorizontalSequenceChanged()
	{
		if( nextSectionIndex_ >= 0 )
		{
			if( (nextSectionIndex_ == sectionIndex_ && transitionMusicSources_[0].timeSamples > CurrentSection.LoopStartSample)
				|| (nextSectionIndex_ != sectionIndex_ && transitionMusicSources_[0].timeSamples > NextSection.EntryPointSample) )
			{
				AudioSource[] oldTracks = musicSources_;
				musicSources_ = transitionMusicSources_;
				transitionMusicSources_ = oldTracks;
				sectionIndex_ = nextSectionIndex_;
				
				SetTransitionSchedule(AudioSettings.dspTime - (double)musicSources_[0].timeSamples / GetSampleRate());
			}
		}

		return false;
	}

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
