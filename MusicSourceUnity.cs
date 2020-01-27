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
	public bool _PlayOnStart;
	public AudioMixerGroup OutputMixerGroup;

	public MusicSection[] Sections = new MusicSection[1] { new MusicSection() };

	public enum EState
	{
		Invalid,
		Ready,
		Playing,
		Suspended,
		Finished
	}
	public EState State { get; private set; } = EState.Invalid;

	public enum ETransitionState
	{
		Invalid = 0,
		Intro,
		Ready,
		Synced,		//再生予約完了後～再生開始前
		PreEntry,	//再生開始〜EntryPointまで
		PostEntry,	//EntryPoint〜フェード完了まで
		Outro,
	};
	public ETransitionState TransitionState { get; private set; } = ETransitionState.Invalid;

	private AudioSource[] musicSources_;
	private AudioSource[] transitionMusicSources_;

	private int sectionIndex_ = 0;
	private int nextSectionIndex_ = -1;
	private int numTracks_ = 0;

	private double playedDSPTime_;

	// transition
	public class Fade
	{
		public Fade(bool isFadeOut)
		{
			isFadeOut_ = isFadeOut;
		}

		public float GetVolume(int currentSample)
		{
			if( isFadeOut_ )
			{
				return 1.0f - Mathf.Clamp01((float)(currentSample - fadeStartSample_) / fadeLengthSample_);
			}
			else
			{
				return Mathf.Clamp01((float)(currentSample - fadeStartSample_) / fadeLengthSample_);
			}
		}

		public void SetFade(int start, int length)
		{
			fadeStartSample_ = start;
			fadeLengthSample_ = length;
		}
		public void SetFadeIn(int start, int length)
		{
			fadeStartSample_ = start;
			fadeLengthSample_ = length;
			isFadeOut_ = false;
		}
		public void SetFadeOut(int start, int length)
		{
			fadeStartSample_ = start;
			fadeLengthSample_ = length;
			isFadeOut_ = true;
		}

		bool isFadeOut_;
		int fadeStartSample_;
		int fadeLengthSample_;
	}

	private Fade transitionFadeIn_ = new Fade(false);
	private Fade transitionFadeOut_ = new Fade(true);
	private float transitionFadeInVolume_ = 1.0f;
	private float transitionFadeOutVolume_ = 1.0f;

	public class TransitionRequestParam
	{
		public TransitionRequestParam(int index, MusicSourceUnity music)
		{
			SectionIndex = index;
			Transition = new MusicSection.TransitionParams(music.Sections[index].Transition);
		}
		public TransitionRequestParam(int index, MusicSourceUnity music, Music.SyncType syncType, int syncFactor = 1)
		{
			SectionIndex = index;
			Transition = new MusicSection.TransitionParams(music.Sections[index].Transition);
			Transition.SyncType = syncType;
			Transition.SyncFactor = syncFactor;
			if( syncType == Music.SyncType.ExitPoint )
			{
				Transition.UseFadeOut = false;
			}
		}

		public int SectionIndex;
		public MusicSection.TransitionParams Transition;
	}

	private TransitionRequestParam requenstedTransition_;

	// 僅かに遅らせることでPlayとPlayScheduleのズレを回避 https://qiita.com/tatmos/items/4c78c127291a0c3b74ed
	private static double SCHEDULE_DELAY = 0.1;
	private static double ScheduleDSPTime { get { return AudioSettings.dspTime + SCHEDULE_DELAY; } }


	#region properties

	public MusicSection CurrentSection { get { return Sections[sectionIndex_]; } }

	public MusicSection NextSection { get { return Sections[nextSectionIndex_]; } }

	public MusicSection this[int index]
	{
		get
		{
			if( 0 <= index && index < Sections.Length )
			{
				return Sections[index];
			}
			else
			{
				Debug.LogWarning("Section index out of range! index = " + index + ", SectionCount = " + Sections.Length);
				return null;
			}
		}
	}

	#endregion


	#region unity functions
	
	void OnValidate()
	{
		foreach( MusicSection section in Sections )
		{
			section.Initialize();
		}
	}

	void Awake()
	{
		if( State == EState.Invalid )
		{
			Initialize();
		}
	}

	void Update()
	{
	}

	#endregion



	#region IMusicSource

	public bool PlayOnStart { get { return _PlayOnStart; } }

	public bool IsPlaying { get { return State == EState.Playing; } }

	public bool IsValid { get { return State != EState.Invalid; } }

	public string SequenceName { get { return Sections[sectionIndex_].Name; } }

	public int SequenceIndex { get { return sectionIndex_; } }


	public void Initialize()
	{
		if( State == EState.Ready )
		{
			return;
		}

		State = EState.Ready;

		if( Sections.Length == 0 )
		{
			State = EState.Invalid;
		}
		foreach( MusicSection section in Sections )
		{
			section.Initialize();
			if( section.IsValid == false )
			{
				State = EState.Invalid;
				break;
			}
			numTracks_ = Math.Max(numTracks_, section.Clips.Length);
		}

		if( State != EState.Invalid )
		{
			CreateAudioTrackObjects();
			ResetAudioClips();
		}
	}

	void CreateAudioTrackObjects()
	{
		// AudioSourceを最大トラック数*2（遷移時に重なる分）まで生成。
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

	void ResetAudioClips()
	{
		for( int i = 0; i < numTracks_; ++i )
		{
			if( i < CurrentSection.Clips.Length )
			{
				musicSources_[i].clip = CurrentSection.Clips[i];
			}
			else
			{
				musicSources_[i].clip = null;
			}
			transitionMusicSources_[i].clip = null;
		}
	}

	public void Play()
	{
		if( State == EState.Invalid )
		{
			return;
		}

		State = EState.Playing;
		TransitionState = ETransitionState.Intro;
		
		playedDSPTime_ = MusicSourceUnity.ScheduleDSPTime;
		for( int i = 0; i < numTracks_; ++i )
		{
			if( musicSources_[i].clip != null )
			{
				musicSources_[i].PlayScheduled(playedDSPTime_);
			}
			else
			{
				break;
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
				musicSources_[i].clip = null;
			}
			if( transitionMusicSources_[i].clip != null )
			{
				transitionMusicSources_[i].Stop();
			}
		}

		sectionIndex_ = 0;
		nextSectionIndex_ = -1;
		requenstedTransition_ = null;
		ResetAudioClips();

		TransitionState = ETransitionState.Invalid;
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

	public void Seek(int sequenceIndex, Timing seekTiming)
	{
		sectionIndex_ = sequenceIndex;
		int seekSample = CurrentSection.GetSampleFromTiming(seekTiming);
		for( int i = 0; i < numTracks_; ++i )
		{
			if( i < CurrentSection.Clips.Length )
			{
				musicSources_[i].clip = CurrentSection.Clips[i];
				musicSources_[i].timeSamples = seekSample;
			}
			else
			{
				musicSources_[i].clip = null;
			}
		}
	}

	public double GetCurrentTimeSec()
	{
		return (double)musicSources_[0].timeSamples / GetSampleRate();
	}

	public int GetCurrentSample()
	{
		return musicSources_[0].timeSamples;
	}

	public int GetSampleRate()
	{
		return musicSources_[0].clip.frequency;
	}

	public MusicMeter GetMeterFromSample(int currentSample)
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

	public MusicMeter GetMeterFromTiming(Timing timing)
	{
		MusicMeter res = null;
		foreach( MusicMeter meter in CurrentSection.Meters )
		{
			if( timing.Bar < meter.StartBar )
			{
				return res;
			}
			res = meter;
		}
		return res;
	}



	public Timing GetSequenceEndTiming()
	{
		return CurrentSection.ExitPointTiming;
	}

	public void UpdateSequenceState()
	{
		int transitionCurrentSample;

		// TransitionState遷移処理
		switch( TransitionState )
		{
			case ETransitionState.Intro:
				if( AudioSettings.dspTime >= playedDSPTime_ && GetCurrentSample() > CurrentSection.EntryPointSample )
				{
					TransitionState = ETransitionState.Ready;
					OnTransitionReady();
				}
				break;
			case ETransitionState.Ready:
				if( nextSectionIndex_ == -1 && GetCurrentSample() > CurrentSection.ExitPointSample )
				{
					TransitionState = ETransitionState.Outro;
				}
				break;
			case ETransitionState.Outro:
				break;
			case ETransitionState.Synced:
				transitionCurrentSample = transitionMusicSources_[0].timeSamples;
				if( nextSectionIndex_ == sectionIndex_ )
				{
					// ループ処理
					if( transitionCurrentSample > CurrentSection.LoopStartSample )
					{
						OnEntryPoint();
						TransitionState = ETransitionState.Ready;
						OnTransitionReady();
					}
				}
				else
				{
					// 遷移処理
					if( transitionCurrentSample > 0 )
					{
						TransitionState = ETransitionState.PreEntry;
						if( NextSection.EntryPointSample == 0 )
						{
							OnEntryPoint();
							TransitionState = ETransitionState.PostEntry;
						}
					}
				}
				break;
			case ETransitionState.PreEntry:
				transitionCurrentSample = transitionMusicSources_[0].timeSamples;
				if( NextSection.Transition.UseFadeIn )
				{
					transitionFadeInVolume_ = transitionFadeIn_.GetVolume(transitionCurrentSample);
				}
				if( NextSection.Transition.UseFadeOut )
				{
					transitionFadeOutVolume_ = transitionFadeOut_.GetVolume(transitionCurrentSample);
				}
				UpdateVolumes();
				if( transitionCurrentSample > NextSection.EntryPointSample )
				{
					OnEntryPoint();
					TransitionState = ETransitionState.PostEntry;
				}
				break;
			case ETransitionState.PostEntry:
				transitionCurrentSample = musicSources_[0].timeSamples;
				bool isFadeInFinished = false;
				if( CurrentSection.Transition.UseFadeIn )
				{
					transitionFadeInVolume_ = transitionFadeIn_.GetVolume(transitionCurrentSample);
					isFadeInFinished = transitionFadeInVolume_ >= 1.0f;
				}
				else
				{
					isFadeInFinished = true;
				}
				bool isFadeOutFinished = false;
				if( CurrentSection.Transition.UseFadeOut )
				{
					transitionFadeOutVolume_ = transitionFadeOut_.GetVolume(transitionCurrentSample);
					if( transitionFadeOutVolume_ <= 0 )
					{
						foreach( AudioSource prevSectionSource in transitionMusicSources_ )
						{
							if( prevSectionSource.clip != null )
							{
								prevSectionSource.Stop();
							}
							else
							{
								break;
							}
						}
						isFadeOutFinished = true;
					}
				}
				else
				{
					if( transitionMusicSources_[0].isPlaying == false )
					{
						isFadeOutFinished = true;
					}
				}
				UpdateVolumes();

				if( isFadeOutFinished && isFadeInFinished )
				{
					TransitionState = ETransitionState.Ready;
					OnTransitionReady();
				}
				break;
		}
	}

	void UpdateVolumes()
	{
		float mainVolume = Volume;
		float transitionVolume = Volume;
		switch( TransitionState )
		{
			case ETransitionState.Synced:
				transitionVolume *= transitionFadeInVolume_;
				break;
			case ETransitionState.PreEntry:
				transitionVolume *= transitionFadeInVolume_;
				mainVolume *= transitionFadeOutVolume_;
				break;
			case ETransitionState.PostEntry:
				mainVolume *= transitionFadeInVolume_;
				transitionVolume *= transitionFadeOutVolume_;
				break;
			default:
				break;
		}

		foreach( AudioSource source in musicSources_ )
		{
			if( source.clip != null )
			{
				source.volume = mainVolume;
			}
			else
			{
				break;
			}
		}
		foreach( AudioSource transitionSource in transitionMusicSources_ )
		{
			if( transitionSource.clip != null )
			{
				transitionSource.volume = transitionVolume;
			}
			else
			{
				break;
			}
		}
	}

	void OnEntryPoint()
	{
		// ソースの切り替え
		AudioSource[] oldTracks = musicSources_;
		musicSources_ = transitionMusicSources_;
		transitionMusicSources_ = oldTracks;
		// インデックス切り替え
		sectionIndex_ = nextSectionIndex_;
		nextSectionIndex_ = -1;
	}

	void OnTransitionReady()
	{
		if( requenstedTransition_ != null )
		{
			SetNextSection(requenstedTransition_);
			requenstedTransition_ = null;
		}
		else
		{
			switch( CurrentSection.TransitionType )
			{
				case MusicSection.ETransitionType.Loop:
					SetNextLoop();
					break;
				case MusicSection.ETransitionType.Transition:
					SetNextSection(new TransitionRequestParam(CurrentSection.TransitionDestinationIndex, this, Music.SyncType.ExitPoint));
					break;
				case MusicSection.ETransitionType.End:
					nextSectionIndex_ = -1;
					break;
			}
		}
	}

	void SetNextLoop()
	{
		double loopEndTime = AudioSettings.dspTime + (double)(CurrentSection.LoopEndSample - GetCurrentSample()) / GetSampleRate();
		for( int i = 0; i < numTracks_; ++i )
		{
			if( i < CurrentSection.Clips.Length )
			{
				// ループ波形予約
				transitionMusicSources_[i].clip = CurrentSection.Clips[i];
				transitionMusicSources_[i].timeSamples = CurrentSection.LoopStartSample;
				transitionMusicSources_[i].PlayScheduled(loopEndTime);
				musicSources_[i].SetScheduledEndTime(loopEndTime);
			}
			else
			{
				musicSources_[i].clip = null;
				transitionMusicSources_[i].clip = null;
			}
		}

		TransitionState = ETransitionState.Synced;
		nextSectionIndex_ = sectionIndex_;
	}

	void SetNextSection(TransitionRequestParam param)
	{
		// 遷移タイミング計算
		MusicSection requestedSection = Sections[param.SectionIndex];
		int syncPointSample = 0;

		int currentSample = GetCurrentSample();
		MusicMeter currentMeter = GetMeterFromSample(currentSample);
		Timing currentTiming = currentMeter.GetTimingFromSample(currentSample);
		int entryPointSample = requestedSection.EntryPointSample;
		int syncFactor = param.Transition.SyncFactor;
		Timing syncPointCandidateTiming = new Timing(currentTiming);
		
		switch( param.Transition.SyncType )
		{
			case Music.SyncType.Immediate:
				syncPointSample = currentSample + entryPointSample;
				break;
			case Music.SyncType.ExitPoint:
				syncPointSample = CurrentSection.ExitPointSample;
				if( syncPointSample <= currentSample + entryPointSample )
				{
					print("request and try later");
					requenstedTransition_ = param;
					return;
				}
				break;
			case Music.SyncType.Bar:
				syncPointCandidateTiming.Set(currentTiming.Bar - (currentTiming.Bar - currentMeter.StartBar) % syncFactor + syncFactor, 0, 0);
				syncPointSample = CurrentSection.GetSampleFromTiming(syncPointCandidateTiming);
				while( syncPointSample <= currentSample + entryPointSample )
				{
					syncPointCandidateTiming.Add(syncFactor);
					if( syncPointCandidateTiming > CurrentSection.ExitPointTiming )
					{
						print("request and try later");
						requenstedTransition_ = param;
						return;
					}
					syncPointSample = CurrentSection.GetSampleFromTiming(syncPointCandidateTiming);
				}
				break;
			case Music.SyncType.Beat:
				syncPointCandidateTiming.Set(currentTiming.Bar, currentTiming.Beat - (currentTiming.Beat % syncFactor) + syncFactor, 0);
				syncPointCandidateTiming.Fix(currentMeter);
				syncPointSample = CurrentSection.GetSampleFromTiming(syncPointCandidateTiming);
				while( syncPointSample <= currentSample + entryPointSample )
				{
					syncPointCandidateTiming.Add(0, syncFactor, 0, currentMeter);
					syncPointCandidateTiming.Fix(currentMeter);
					if( syncPointCandidateTiming > CurrentSection.ExitPointTiming )
					{
						print("request and try later");
						requenstedTransition_ = param;
						return;
					}
					currentMeter = GetMeterFromTiming(syncPointCandidateTiming);
					syncPointSample = CurrentSection.GetSampleFromTiming(syncPointCandidateTiming);
				}
				break;
			case Music.SyncType.Unit:
				syncPointCandidateTiming.Set(currentTiming.Bar, currentTiming.Beat, currentTiming.Unit - (currentTiming.Unit % syncFactor) + syncFactor);
				syncPointCandidateTiming.Fix(currentMeter);
				syncPointSample = CurrentSection.GetSampleFromTiming(syncPointCandidateTiming);
				while( syncPointSample <= currentSample + entryPointSample )
				{
					syncPointCandidateTiming.Add(0, 0, syncFactor, currentMeter);
					syncPointCandidateTiming.Fix(currentMeter);
					if( syncPointCandidateTiming > CurrentSection.ExitPointTiming )
					{
						print("request and try later");
						requenstedTransition_ = param;
						return;
					}
					currentMeter = GetMeterFromTiming(syncPointCandidateTiming);
					syncPointSample = CurrentSection.GetSampleFromTiming(syncPointCandidateTiming);
				}
				break;
			case Music.SyncType.Marker:
				// not impl
				return;
				//break;
		}

		// 遷移状態によって前のをキャンセルしたり
		switch( TransitionState )
		{
			case ETransitionState.Ready:
				// 準備OK
				break;
			case ETransitionState.Synced:
				// 前のをキャンセル
				CancelSyncedTransition();
				break;
			case ETransitionState.Intro:
			case ETransitionState.PreEntry:
			case ETransitionState.PostEntry:
			case ETransitionState.Outro:
				// 遷移できないはずなんですが……
				print("invalid transition state " + TransitionState);
				return;
		}

		// バツ切りの場合はその時間を計算しておく
		double scheduleEndTime = 0.0f;
		if( param.Transition.UseFadeOut == false )
		{
			if( param.Transition.SyncType == Music.SyncType.ExitPoint )
			{
				scheduleEndTime = AudioSettings.dspTime + (double)(musicSources_[0].clip.samples - currentSample) / GetSampleRate();
			}
			else
			{
				scheduleEndTime = AudioSettings.dspTime + (double)(syncPointSample - currentSample) / GetSampleRate(); ;
			}
		}

		// 遷移波形予約
		double transitionStartTime = AudioSettings.dspTime + (double)(syncPointSample - currentSample - entryPointSample) / GetSampleRate();
		for( int i = 0; i < numTracks_; ++i )
		{
			if( i < requestedSection.Clips.Length )
			{
				transitionMusicSources_[i].clip = requestedSection.Clips[i];
				transitionMusicSources_[i].timeSamples = 0;
				transitionMusicSources_[i].PlayScheduled(transitionStartTime);
			}
			else
			{
				transitionMusicSources_[i].clip = null;
			}

			if( i < CurrentSection.Clips.Length && scheduleEndTime > 0.0 )
			{
				musicSources_[i].SetScheduledEndTime(scheduleEndTime);
			}
		}

		TransitionState = ETransitionState.Synced;
		nextSectionIndex_ = param.SectionIndex;

		// フェード時間計算
		transitionFadeOutVolume_ = 1.0f;
		transitionFadeInVolume_ = 1.0f;
		if( param.Transition.UseFadeOut )
		{
			transitionFadeOut_.SetFade(
				(int)(param.Transition.FadeOutOffset * GetSampleRate()) + NextSection.EntryPointSample,
				(int)(param.Transition.FadeOutTime * GetSampleRate()));
		}
		if( param.Transition.UseFadeIn )
		{
			transitionFadeInVolume_ = 0.0f;
			transitionFadeIn_.SetFade(
				(int)(param.Transition.FadeInOffset * GetSampleRate()), 
				(int)(param.Transition.FadeInTime * GetSampleRate()));
		}

		UpdateVolumes();
	}

	void CancelSyncedTransition()
	{
		for( int i = 0; i < numTracks_; ++i )
		{
			if( transitionMusicSources_[i].clip != null )
			{
				transitionMusicSources_[i].Stop();
			}
			if( musicSources_[i].clip != null )
			{
				musicSources_[i].SetScheduledEndTime(AudioSettings.dspTime + (double)(musicSources_[i].clip.samples - GetCurrentSample()) / GetSampleRate());
			}
		}
	}

	public void OnRepeated()
	{
	}

	public void OnHorizontalSequenceChanged()
	{
	}



	public void SetHorizontalSequence(string name)
	{
		for( int i = 0; i < Sections.Length; ++i )
		{
			if( Sections[i].Name == name )
			{
				SetHorizontalSequenceByIndex(i);
				break;
			}
		}
	}

	public void SetHorizontalSequenceByIndex(int index)
	{
		if( index < 0 || Sections.Length <= index || index == nextSectionIndex_ )
		{
			// インデックス範囲外、もしくは既に遷移確定済み
			return;
		}

		if( requenstedTransition_ != null && requenstedTransition_.SectionIndex == index )
		{
			// 既にリクエスト済み
			return;
		}

		switch( State )
		{
			case EState.Invalid:
			case EState.Finished:
				return;
			case EState.Ready:
				sectionIndex_ = index;
				ResetAudioClips();
				return;
			case EState.Suspended:
				requenstedTransition_ = new TransitionRequestParam(index, this);
				return;
			case EState.Playing:
				break;
		}

		// 今と同じセクションが指定されたら
		if( index == sectionIndex_ )
		{
			// 予約中のはキャンセル
			requenstedTransition_ = null;
			// 遷移中のはキャンセルできるならキャンセル
			if( TransitionState == ETransitionState.Synced )
			{
				CancelSyncedTransition();
				OnTransitionReady();
			}
			else if( TransitionState == ETransitionState.PreEntry )
			{
				requenstedTransition_ = new TransitionRequestParam(index, this);
			}
			return;
		}
		
		switch( TransitionState )
		{
			case ETransitionState.Invalid:
				return;
			case ETransitionState.Intro:
			case ETransitionState.Outro:
			case ETransitionState.PreEntry:
			case ETransitionState.PostEntry:
				requenstedTransition_ = new TransitionRequestParam(index, this);
				return;
			default:
			//case ETransitionState.Ready:
			//case ETransitionState.Synced:
				break;
		}

		SetNextSection(new TransitionRequestParam(index, this));
	}

	public void SetVerticalMix(float param) { }

	public void SetVerticalMixByName(string name) { }

	#endregion
}
