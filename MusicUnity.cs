using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicUnity : MusicBase
{
	#region editor params

	[Range(0,1)]
	public float Volume = 1.0f;
	public AudioMixerGroup OutputMixerGroup;

	public MusicSection[] Sections = new MusicSection[1] { new MusicSection() };
	public MusicMode[] Modes = new MusicMode[1] { new MusicMode() };
	public MusicMode.TransitionParams ModeTransitionParam;

	[Serializable]
	public class SectionTransitionOverride
	{
		public int FromSectionIndex = -1;
		public int ToSectionIndex = -1;
		public Music.SyncType SyncType = Music.SyncType.Bar;
		public int SyncFactor = 1;
		public MusicSection.TransitionParams Transition;
	}
	public List<SectionTransitionOverride> SectionTransitionOverrides;

	[Serializable]
	public class ModeTransitionOverride
	{
		public int FromModeIndex = -1;
		public int ToModeIndex = -1;
		public MusicMode.TransitionParams Transition;
	}
	public List<ModeTransitionOverride> ModeTransitionOverrides;
	public int NumTracks = 1;

	#if UNITY_EDITOR
	public int SeekBar = -1;
	public int PreviewSectionIndex = 0;
	public int PreviewModeIndex = 0;
	#endif

	#endregion


	#region properties

	// states
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
	public enum EModeTransitionState
	{
		Invalid,
		Ready,
		Sync,        //次のモードへのFading開始前
		Fading,      //次のモードへフェードイン中
	};
	public EModeTransitionState ModeTransitionState { get; private set; } = EModeTransitionState.Invalid;

	// section property
	public int SectionIndex { get { return sectionIndex_; } }
	public int NextSectionIndex { get { return nextSectionIndex_; } }
	public override int SequenceIndex { get { return sectionIndex_; } }
	public override string SequenceName { get { return Sections[sectionIndex_].Name; } }
	public MusicSection CurrentSection { get { return Sections[sectionIndex_]; } }
	public MusicSection NextSection { get { return Sections[nextSectionIndex_]; } }
	public MusicSection PrevSection { get { return Sections[prevSectionIndex_]; } }
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

	// mode property
	public int ModeIndex { get { return modeIndex_; } }
	public int NextModeIndex { get { return nextModeIndex_; } }
	public MusicMode CurrentMode { get { return Modes[modeIndex_]; } }
	public MusicMode NextMode { get { return Modes[nextModeIndex_]; } }
	public MusicMode GetModeAt(int index)
	{
		if( 0 <= index && index < Modes.Length )
		{
			return Modes[index];
		}
		else
		{
			Debug.LogWarning("Mode index out of range! index = " + index + ", ModeCount = " + Modes.Length);
			return null;
		}
	}

	#endregion


	#region params

	// samples
	protected int sampleRate_ = 44100;
	protected int currentSample_;

	// sources
	private AudioSource[] musicSources_;
	private AudioSource[] transitionMusicSources_;

	// dspTime
	private double playedDSPTime_;
	private double syncScheduledDSPTime_ = 0.0;
	private double endScheduledDSPTime_ = 0.0;
	private double suspendedDSPTime_ = 0.0;

	// section
	private int sectionIndex_ = 0;
	private int nextSectionIndex_ = -1;
	private int prevSectionIndex_ = -1;

	// section transition fade
	public class Fade
	{
		public Fade(bool isFadeOut)
		{
			isFadeOut_ = isFadeOut;
		}

		public int FadeStartSample { get { return fadeStartSample_; } }
		public int FadeEndSample { get { return fadeStartSample_ + fadeLengthSample_; } }

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
		public void OnEntryPoint(int newStart)
		{
			fadeStartSample_ = newStart;
		}

		bool isFadeOut_;
		int fadeStartSample_;
		int fadeLengthSample_;
	}
	private Fade transitionFadeIn_ = new Fade(false);
	private Fade transitionFadeOut_ = new Fade(true);
	private float transitionFadeInVolume_ = 1.0f;
	private float transitionFadeOutVolume_ = 1.0f;

	// section transition param
	[Serializable]
	public class SectionTransitionParam
	{
		public SectionTransitionParam(int targetIndex, int currentIndex, MusicUnity music)
		{
			SectionIndex = targetIndex;

			SectionTransitionOverride overrideParam = music.SectionTransitionOverrides.Find(
				(SectionTransitionOverride ov) =>
				{
					return (ov.FromSectionIndex == currentIndex || ov.FromSectionIndex == -1)
					 && (ov.ToSectionIndex == targetIndex || ov.ToSectionIndex == -1);
				});

			if( overrideParam !=null )
			{
				SyncType = overrideParam.SyncType;
				SyncFactor = overrideParam.SyncFactor;
				Transition = new MusicSection.TransitionParams(overrideParam.Transition);
			}
			else
			{
				SyncType = music.Sections[currentIndex].SyncType;
				SyncFactor = music.Sections[currentIndex].SyncFactor;
				Transition = new MusicSection.TransitionParams(music.Sections[targetIndex].Transition);
			}

			if( SyncType == Music.SyncType.ExitPoint )
			{
				Transition.UseFadeOut = false;
			}
		}
		public SectionTransitionParam(int index, MusicUnity music, Music.SyncType syncType, int syncFactor = 1)
		{
			SectionIndex = index;
			SyncType = syncType;
			SyncFactor = syncFactor;
			Transition = new MusicSection.TransitionParams(music.Sections[index].Transition);
			if( syncType == Music.SyncType.ExitPoint )
			{
				Transition.UseFadeOut = false;
			}
		}

		public int SectionIndex;
		public Music.SyncType SyncType = Music.SyncType.Bar;
		public int SyncFactor = 1;
		public MusicSection.TransitionParams Transition;
	}
	private SectionTransitionParam requenstedTransition_;
	private SectionTransitionParam executedTransition_;

	// mode
	private int modeIndex_ = 0;
	private int nextModeIndex_ = 0;
	private int requestedModeIndex_ = -1;

	// mode transition fade
	private Fade modeFade_ = new Fade(false);
	private List<float> modeLayerVolumes_ = new List<float>();
	private List<float> modeLayerBaseVolumes_ = new List<float>();
	private float modeVolume_ = 1.0f;
	private float modeBaseVolume_ = 1.0f;

	// 僅かに遅らせることでPlayとPlayScheduleのズレを回避 https://qiita.com/tatmos/items/4c78c127291a0c3b74ed
	private static double SCHEDULE_DELAY = 0.1;
	private static double ScheduleDSPTime { get { return AudioSettings.dspTime + SCHEDULE_DELAY; } }

	#endregion


	#region override functions

	// internal

	protected override bool ReadyInternal()
	{
		if( Validate() )
		{
			InstantiateAudioSourceObjects();
			ResetSectionClips();
			ResetModeLayerVolumes();
			UpdateVolumes();
			return true;
		}
		return false;
	}

	protected override void SeekInternal(Timing seekTiming, int sequenceIndex = 0)
	{
		sectionIndex_ = sequenceIndex;
		int seekSample = CurrentSection.GetSampleFromTiming(seekTiming);
		for( int i = 0; i < NumTracks; ++i )
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

	protected override bool PlayInternal()
	{
		#if UNITY_EDITOR
		if( PreviewSectionIndex != sectionIndex_ || SeekBar >= 0 )
		{
			SeekInternal(new Timing(SeekBar), PreviewSectionIndex);
		}
		if( PreviewModeIndex != modeIndex_ )
		{
			modeIndex_ = nextModeIndex_ = PreviewModeIndex;
			ResetModeLayerVolumes();
		}
		#endif
		
		TransitionState = ETransitionState.Intro;
		ModeTransitionState = EModeTransitionState.Ready;
		UpdateVolumes();

		playedDSPTime_ = MusicUnity.ScheduleDSPTime;
		for( int i = 0; i < NumTracks; ++i )
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

		return true;
	}

	protected override bool SuspendInternal()
	{
		for( int i = 0; i < NumTracks; ++i )
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

		suspendedDSPTime_ = AudioSettings.dspTime;
		return true;
	}

	protected override bool ResumeInternal()
	{
		double suspendedDuration = AudioSettings.dspTime - suspendedDSPTime_;
		if( TransitionState == ETransitionState.Synced && syncScheduledDSPTime_ > 0.0 )
		{
			syncScheduledDSPTime_ += suspendedDuration;
			for( int i = 0; i < NumTracks; ++i )
			{
				if( transitionMusicSources_[i].clip != null )
				{
					transitionMusicSources_[i].SetScheduledStartTime(syncScheduledDSPTime_);
				}
			}
		}
		if( endScheduledDSPTime_ > 0.0 )
		{
			endScheduledDSPTime_ += suspendedDuration;
			for( int i = 0; i < NumTracks; ++i )
			{
				if( musicSources_[i].clip != null )
				{
					musicSources_[i].SetScheduledEndTime(endScheduledDSPTime_);
				}
			}
		}

		for( int i = 0; i < NumTracks; ++i )
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

		if( requenstedTransition_ != null )
		{
			SetNextSection(requenstedTransition_);
		}
		if( requestedModeIndex_ >= 0 )
		{
			SetMode(requestedModeIndex_);
		}
		return true;
	}

	protected override bool StopInternal()
	{
		for( int i = 0; i < NumTracks; ++i )
		{
			if( musicSources_[i].clip != null )
			{
				musicSources_[i].Stop();
				musicSources_[i].clip = null;
			}
			if( transitionMusicSources_[i].clip != null )
			{
				transitionMusicSources_[i].Stop();
				transitionMusicSources_[i].clip = null;
			}
		}

		ResetSectionClips();
		ResetModeLayerVolumes();

		return true;
	}

	protected override void ResetParamsInternal()
	{
		currentSample_ = 0;
		sectionIndex_ = 0;
		nextSectionIndex_ = -1;
		prevSectionIndex_ = -1;
		requenstedTransition_ = null;
		modeIndex_ = 0;
		nextModeIndex_ = 0;
		requestedModeIndex_ = -1;
		endScheduledDSPTime_ = 0.0;
		syncScheduledDSPTime_ = 0.0;
		ModeTransitionState = EModeTransitionState.Invalid;
		TransitionState = ETransitionState.Invalid;
	}

	// timing

	protected override void UpdateTimingInternal()
	{
		currentSample_ = musicSources_[0].timeSamples;
		currentMeter_ = GetMeterFromSample(currentSample_);

		UpdateHorizontalState();
		UpdateVerticalState();
	}
	
	protected override void CalcTimingAndFraction(ref Timing just, out float fraction)
	{
		if( currentMeter_ == null )
		{
			just.Set(-1, 0, 0);
			fraction = 0;
		}
		else
		{
			MusicMeterBySample meter = currentMeter_ as MusicMeterBySample;
			just.Set(meter.GetTimingFromSample(currentSample_));
			fraction = (float)(currentSample_ - meter.GetSampleFromTiming(just)) / meter.SamplesPerUnit;
		}
	}

	protected override Timing GetSequenceEndTiming()
	{
		return CurrentSection.ExitPointTiming;
	}
	
	// update
	
	protected override bool CheckFinishPlaying()
	{
		if( nextSectionIndex_ == -1 ) // will end
		{
			bool isSomeSourcePlaying = false;
			foreach( AudioSource source in musicSources_ )
			{
				if( source.isPlaying )
				{
					isSomeSourcePlaying = true;
					break;
				}
			}

			if( isSomeSourcePlaying == false )
			{
				return true;
			}
		}
		return false;
	}

	protected override void UpdateInternal()
	{
		#if UNITY_EDITOR
		UpdateGameObjectNames();
		#endif
	}

	protected void UpdateHorizontalState()
	{
		int transitionCurrentSample;
		
		switch( TransitionState )
		{
			case ETransitionState.Intro:
				if( AudioSettings.dspTime >= playedDSPTime_ && currentSample_ > CurrentSection.EntryPointSample )
				{
					TransitionState = ETransitionState.Ready;
					OnTransitionReady();
				}
				break;
			case ETransitionState.Ready:
				if( nextSectionIndex_ == -1 && currentSample_ > CurrentSection.ExitPointSample )
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
				if( executedTransition_.Transition.UseFadeIn )
				{
					transitionFadeInVolume_ = transitionFadeIn_.GetVolume(transitionCurrentSample);
				}
				if( executedTransition_.Transition.UseFadeOut )
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
				if( executedTransition_.Transition.UseFadeIn )
				{
					transitionFadeInVolume_ = transitionFadeIn_.GetVolume(transitionCurrentSample);
					isFadeInFinished = transitionFadeInVolume_ >= 1.0f;
				}
				else
				{
					isFadeInFinished = true;
				}
				bool isFadeOutFinished = false;
				if( executedTransition_.Transition.UseFadeOut )
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

	protected void UpdateVerticalState()
	{
		int currentSample = currentSample_;

		if( ModeTransitionState == EModeTransitionState.Sync )
		{
			if( modeFade_.FadeStartSample <= currentSample )
			{
				ModeTransitionState = EModeTransitionState.Fading;
			}
		}

		if( ModeTransitionState == EModeTransitionState.Fading )
		{
			float fade = modeFade_.GetVolume(currentSample);
			modeVolume_ = modeBaseVolume_ + (NextMode.TotalVolume - modeBaseVolume_) * fade;
			for( int i = 0; i < NumTracks; ++i )
			{
				modeLayerVolumes_[i] = modeLayerBaseVolumes_[i] + (NextMode.LayerVolumes[i] - modeLayerBaseVolumes_[i]) * fade;
			}
			UpdateVolumes();
			if( fade >= 1.0f )
			{
				ModeTransitionState = EModeTransitionState.Ready;
				modeIndex_ = nextModeIndex_;
			}
		}
	}
	
	// interactive music

	public override void SetHorizontalSequence(string name)
	{
		for( int i = 0; i < Sections.Length; ++i )
		{
			if( Sections[i].Name == name )
			{
				SetHorizontalSequenceByIndex(i);
				return;
			}
		}
		print("Couldn't find section name = " + name);
	}

	public override void SetHorizontalSequenceByIndex(int index)
	{
		if( index < 0 || Sections.Length <= index
			// インデックス範囲外
			|| index == nextSectionIndex_
			// 既に遷移確定済み
			|| (requenstedTransition_ != null && requenstedTransition_.SectionIndex == index) )
			// 既にリクエスト済み
		{
			return;
		}

		// 再生中以外は設定だけ
		switch( State )
		{
			case Music.PlayState.Invalid:
				return;
			case Music.PlayState.Ready:
			case Music.PlayState.Finished:
				sectionIndex_ = index;
				ResetSectionClips();
				return;
			case Music.PlayState.Suspended:
				requenstedTransition_ = new SectionTransitionParam(index, sectionIndex_, this);
				return;
			case Music.PlayState.Playing:
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
				requenstedTransition_ = new SectionTransitionParam(index, sectionIndex_, this);
			}
			return;
		}
		
		switch( TransitionState )
		{
			case ETransitionState.Invalid:
			case ETransitionState.Outro:
				return;
			case ETransitionState.Intro:
			case ETransitionState.PreEntry:
			case ETransitionState.PostEntry:
				requenstedTransition_ = new SectionTransitionParam(index, sectionIndex_, this);
				return;
			default:
			//case ETransitionState.Ready:
			//case ETransitionState.Synced:
				break;
		}

		SetNextSection(new SectionTransitionParam(index, sectionIndex_, this));
	}

	public override void SetVerticalMix(string name)
	{
		for( int i = 0; i < Modes.Length; ++i )
		{
			if( Modes[i].Name == name )
			{
				SetVerticalMixByIndex(i);
				return;
			}
		}
		print("Couldn't find mode name = " + name);
	}

	public override void SetVerticalMixByIndex(int index)
	{
		if( index < 0 || Modes.Length <= index
			// インデックス範囲外
			|| index == nextModeIndex_
			// 既に遷移確定済み
			|| (requestedModeIndex_ >= 0 && requestedModeIndex_ == index) )
			// 既にリクエスト済み
		{
			return;
		}

		// 再生中以外は設定だけ
		switch( State )
		{
			case Music.PlayState.Invalid:
				return;
			case Music.PlayState.Ready:
			case Music.PlayState.Finished:
				modeIndex_ = index;
				nextModeIndex_ = index;
				ResetModeLayerVolumes();
				return;
			case Music.PlayState.Suspended:
				requestedModeIndex_ = index;
				return;
			case Music.PlayState.Playing:
				break;
		}

		// 今と同じモードが指定されたら
		if( index == modeIndex_ && ModeTransitionState != EModeTransitionState.Fading )
		{
			// 予約中のはキャンセル
			requestedModeIndex_ = -1;
			// 遷移前のもキャンセル
			if( ModeTransitionState == EModeTransitionState.Sync )
			{
				ModeTransitionState = EModeTransitionState.Ready;
				nextModeIndex_ = modeIndex_;
			}
			return;
		}

		SetMode(index);
	}

	#endregion


	#region private functions

	// initialize

	void OnValidate()
	{
		Validate();
	}

	bool Validate()
	{
		if( Sections.Length == 0 )
		{
			return false;
		}
		if( NumTracks < 1 )
		{
			return false;
		}
		if( Sections[0].Clips.Length == 0 )
		{
			return false;
		}

		sampleRate_ = Sections[0].Clips[0].frequency;

		foreach( MusicSection section in Sections )
		{
			section.Validate(sampleRate_);
			if( section.IsValid == false )
			{
				return false;
			}
		}

		if( Modes.Length == 0 )
		{
			Modes = new MusicMode[1] { new MusicMode() };
		}
		foreach( MusicMode mode in Modes )
		{
			for( int i = mode.LayerVolumes.Count; i < NumTracks; ++i )
			{
				mode.LayerVolumes.Add(1.0f);
			}
			for( int i = NumTracks; i < mode.LayerVolumes.Count; ++i )
			{
				mode.LayerVolumes.RemoveAt(i);
			}
		}

		return true;
	}

	void InstantiateAudioSourceObjects()
	{
		// AudioSourceを最大トラック数*2（遷移時に重なる分）まで生成。
		musicSources_ = new AudioSource[NumTracks];
		transitionMusicSources_ = new AudioSource[NumTracks];
		for( int i = 0; i < NumTracks; ++i )
		{
			musicSources_[i] = new GameObject("audioSource_" + i.ToString(), typeof(AudioSource)).GetComponent<AudioSource>();
			musicSources_[i].transform.parent = this.transform;
			musicSources_[i].outputAudioMixerGroup = OutputMixerGroup;
			musicSources_[i].playOnAwake = false;
			musicSources_[i].loop = false;
		}
		for( int i = 0; i < NumTracks; ++i )
		{
			transitionMusicSources_[i] = new GameObject("audioSource_t_" + i.ToString(), typeof(AudioSource)).GetComponent<AudioSource>();
			transitionMusicSources_[i].transform.parent = this.transform;
			transitionMusicSources_[i].outputAudioMixerGroup = OutputMixerGroup;
			transitionMusicSources_[i].playOnAwake = false;
			musicSources_[i].loop = false;
		}
	}

	void ResetSectionClips()
	{
		for( int i = 0; i < NumTracks; ++i )
		{
			if( i < CurrentSection.Clips.Length )
			{
				musicSources_[i].clip = CurrentSection.Clips[i];
				musicSources_[i].timeSamples = 0;
			}
			else
			{
				musicSources_[i].clip = null;
			}
			transitionMusicSources_[i].clip = null;
		}
	}

	void ResetModeLayerVolumes()
	{
		modeLayerVolumes_.Clear();
		modeLayerBaseVolumes_.Clear();
		for( int i = 0; i < NumTracks; ++i )
		{
			modeLayerVolumes_.Add(CurrentMode.LayerVolumes[i]);
			modeLayerBaseVolumes_.Add(CurrentMode.LayerVolumes[i]);
		}
		modeVolume_ = CurrentMode.TotalVolume;
	}
	
	// update

	void UpdateVolumes()
	{
		float mainVolume = Volume * modeVolume_;
		float transitionVolume = Volume * modeVolume_;
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

		for( int i = 0; i < musicSources_.Length; ++i )
		{
			AudioSource source = musicSources_[i];
			if( source.clip != null )
			{
				source.volume = mainVolume * modeLayerVolumes_[i];
			}
			else
			{
				break;
			}
		}
		for( int i = 0; i < transitionMusicSources_.Length; ++i )
		{
			AudioSource transitionSource = transitionMusicSources_[i];
			if( transitionSource.clip != null )
			{
				transitionSource.volume = transitionVolume * modeLayerVolumes_[i];
			}
			else
			{
				break;
			}
		}
	}

	void UpdateGameObjectNames()
	{
		for( int i = 0; i < musicSources_.Length; ++i )
		{
			AudioSource source = musicSources_[i];
			if( source.clip != null )
			{
				source.gameObject.name = String.Format("{0}[{1}] Playing ({2:F2})", CurrentSection.Name, i, modeLayerVolumes_[i]);
			}
			else
			{
				source.gameObject.name = "_no track_";
			}
		}

		MusicSection transitionSection = null;
		switch(TransitionState )
		{
			case ETransitionState.Synced:
			case ETransitionState.PreEntry:
				transitionSection = NextSection;
				break;
			case ETransitionState.PostEntry:
				transitionSection = PrevSection;
				break;
		}
		
		for( int i = 0; i < transitionMusicSources_.Length; ++i )
		{
			AudioSource transitionSource = transitionMusicSources_[i];
			if( transitionSection != null && transitionSource.clip != null )
			{
				transitionSource.gameObject.name = String.Format("{0}[{1}] {2}", transitionSection.Name, i, TransitionState);
			}
			else
			{
				transitionSource.gameObject.name = "_no track_";
			}
		}
	}
	
	// transition event

	void OnEntryPoint()
	{
		// ソースの切り替え
		AudioSource[] oldTracks = musicSources_;
		musicSources_ = transitionMusicSources_;
		transitionMusicSources_ = oldTracks;
		// インデックス切り替え
		prevSectionIndex_ = sectionIndex_;
		sectionIndex_ = nextSectionIndex_;
		switch( CurrentSection.TransitionType )
		{
			case MusicSection.AutoTransitionType.Loop:
				nextSectionIndex_ = sectionIndex_;
				break;
			case MusicSection.AutoTransitionType.Transition:
				nextSectionIndex_ = CurrentSection.TransitionDestinationIndex;
				break;
			case MusicSection.AutoTransitionType.End:
				nextSectionIndex_ = -1;
				break;
		}

		if( requestedModeIndex_ >= 0 )
		{
			SetMode(requestedModeIndex_);
		}

		if( ModeTransitionState == EModeTransitionState.Fading )
		{
			modeFade_.OnEntryPoint(musicSources_[0].timeSamples + modeFade_.FadeStartSample - transitionMusicSources_[0].timeSamples);
		}
	}

	void OnTransitionReady()
	{
		executedTransition_ = null;
		if( requenstedTransition_ != null )
		{
			SetNextSection(requenstedTransition_);
		}
		else
		{
			switch( CurrentSection.TransitionType )
			{
				case MusicSection.AutoTransitionType.Loop:
					SetNextLoop();
					break;
				case MusicSection.AutoTransitionType.Transition:
					SetNextSection(new SectionTransitionParam(CurrentSection.TransitionDestinationIndex, this, Music.SyncType.ExitPoint));
					break;
				case MusicSection.AutoTransitionType.End:
					nextSectionIndex_ = -1;
					endScheduledDSPTime_ = 0.0;
					break;
			}
		}
	}

	// sync
	
	MusicMeterBySample GetMeterFromSample(int sample)
	{
		MusicMeterBySample res = null;
		foreach( MusicMeterBySample meter in CurrentSection.Meters )
		{
			if( sample < meter.StartSamples )
			{
				return res;
			}
			res = meter;
		}
		return res;
	}

	MusicMeterBySample GetMeterFromTiming(Timing timing)
	{
		MusicMeterBySample res = null;
		foreach( MusicMeterBySample meter in CurrentSection.Meters )
		{
			if( timing.Bar < meter.StartBar )
			{
				return res;
			}
			res = meter;
		}
		return res;
	}

	bool FindSyncPoint(Music.SyncType syncType, int syncFactor, int currentSample, int entryPointSample, out int syncPointSample)
	{
		syncPointSample = currentSample;

		MusicMeterBySample currentMeter = GetMeterFromSample(currentSample);
		Timing currentTiming = currentMeter != null ? currentMeter.GetTimingFromSample(currentSample) : new Timing();
		Timing syncPointCandidateTiming = new Timing(currentTiming);

		switch( syncType )
		{
			case Music.SyncType.Immediate:
				syncPointSample = currentSample + entryPointSample;
				break;
			case Music.SyncType.ExitPoint:
				syncPointSample = CurrentSection.ExitPointSample;
				if( syncPointSample <= currentSample + entryPointSample )
				{
					return false;
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
						return false;
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
						return false;
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
						return false;
					}
					currentMeter = GetMeterFromTiming(syncPointCandidateTiming);
					syncPointSample = CurrentSection.GetSampleFromTiming(syncPointCandidateTiming);
				}
				break;
			case Music.SyncType.Marker:
				if( 0 <= syncFactor && syncFactor < CurrentSection.Markers.Length && CurrentSection.Markers[syncFactor].Timings.Length > 0 )
				{
					MusicSection.MusicMarker marker = CurrentSection.Markers[syncFactor];
					int markerIndex = 0;
					syncPointCandidateTiming.Set(marker.Timings[markerIndex]);

					syncPointSample = CurrentSection.GetSampleFromTiming(syncPointCandidateTiming);
					while( syncPointSample <= currentSample + entryPointSample )
					{
						++markerIndex;
						if( marker.Timings.Length <= markerIndex )
						{
							return false;
						}
						syncPointCandidateTiming.Set(marker.Timings[markerIndex]);
						syncPointSample = CurrentSection.GetSampleFromTiming(syncPointCandidateTiming);
					}
				}
				else
				{
					print(String.Format("Failed to SetNextSection. {0} section doesn't have Marker[{1}].", CurrentSection.Name, syncFactor));
					return false;
				}
				break;
		}
		return true;
	}
	
	// transition

	void SetNextLoop()
	{
		syncScheduledDSPTime_ = AudioSettings.dspTime + (double)(CurrentSection.LoopEndSample - currentSample_) / sampleRate_;
		endScheduledDSPTime_ = syncScheduledDSPTime_;
		for( int i = 0; i < NumTracks; ++i )
		{
			if( i < CurrentSection.Clips.Length )
			{
				// ループ波形予約
				transitionMusicSources_[i].clip = CurrentSection.Clips[i];
				transitionMusicSources_[i].timeSamples = CurrentSection.LoopStartSample;
				transitionMusicSources_[i].PlayScheduled(syncScheduledDSPTime_);
				musicSources_[i].SetScheduledEndTime(endScheduledDSPTime_);
			}
			else
			{
				musicSources_[i].clip = null;
				transitionMusicSources_[i].clip = null;
			}
		}

		UpdateVolumes();

		TransitionState = ETransitionState.Synced;
		nextSectionIndex_ = sectionIndex_;
	}

	void SetNextSection(SectionTransitionParam param)
	{
		// 遷移タイミング計算
		MusicSection requestedSection = Sections[param.SectionIndex];
		int entryPointSample = requestedSection.EntryPointSample;
		int currentSample = currentSample_;
		int syncPointSample = 0;
		if( FindSyncPoint(param.SyncType, param.SyncFactor, currentSample, entryPointSample, out syncPointSample) == false )
		{
			requenstedTransition_ = param;
			return;
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

		// 波形終了予約
		endScheduledDSPTime_ = 0.0;
		if( param.Transition.UseFadeOut == false )
		{
			if( param.SyncType == Music.SyncType.ExitPoint )
			{
				endScheduledDSPTime_ = AudioSettings.dspTime + (double)(musicSources_[0].clip.samples - currentSample) / sampleRate_; ;
			}
			else
			{
				endScheduledDSPTime_ = AudioSettings.dspTime + (double)(syncPointSample - currentSample) / sampleRate_;
			}
		}

		// 遷移波形予約
		syncScheduledDSPTime_ = AudioSettings.dspTime + (double)(syncPointSample - currentSample - entryPointSample) / sampleRate_;
		for( int i = 0; i < NumTracks; ++i )
		{
			if( i < requestedSection.Clips.Length )
			{
				transitionMusicSources_[i].clip = requestedSection.Clips[i];
				transitionMusicSources_[i].timeSamples = 0;
				transitionMusicSources_[i].PlayScheduled(syncScheduledDSPTime_);
			}
			else
			{
				transitionMusicSources_[i].clip = null;
			}

			if( i < CurrentSection.Clips.Length && endScheduledDSPTime_ > 0 )
			{
				musicSources_[i].SetScheduledEndTime(endScheduledDSPTime_);
			}
		}

		// 状態遷移
		TransitionState = ETransitionState.Synced;
		nextSectionIndex_ = param.SectionIndex;
		requenstedTransition_ = null;
		executedTransition_ = param;

		// フェード時間計算
		transitionFadeOutVolume_ = 1.0f;
		transitionFadeInVolume_ = 1.0f;
		if( param.Transition.UseFadeOut )
		{
			transitionFadeOut_.SetFade(
				(int)(param.Transition.FadeOutOffset * sampleRate_) + NextSection.EntryPointSample,
				(int)(param.Transition.FadeOutTime * sampleRate_));
		}
		if( param.Transition.UseFadeIn )
		{
			transitionFadeInVolume_ = 0.0f;
			transitionFadeIn_.SetFade(
				(int)(param.Transition.FadeInOffset * sampleRate_),
				(int)(param.Transition.FadeInTime * sampleRate_));
		}

		UpdateVolumes();
	}

	void CancelSyncedTransition()
	{
		if( endScheduledDSPTime_ > 0.0 )
		{
			endScheduledDSPTime_ = AudioSettings.dspTime + (double)(musicSources_[0].clip.samples - currentSample_) / sampleRate_;
			for( int i = 0; i < NumTracks; ++i )
			{
				if( musicSources_[i].clip != null )
				{
					musicSources_[i].SetScheduledEndTime(endScheduledDSPTime_);
				}
			}
		}

		for( int i = 0; i < NumTracks; ++i )
		{
			if( transitionMusicSources_[i].clip != null )
			{
				transitionMusicSources_[i].Stop();
			}
		}
	}

	void SetMode(int index)
	{
		MusicMode.TransitionParams param;

		int fromModeIndex = modeIndex_;
		if( index == modeIndex_ && ModeTransitionState == EModeTransitionState.Fading )
		{
			fromModeIndex = nextModeIndex_;
		}

		ModeTransitionOverride overrideParam = ModeTransitionOverrides.Find(
			(ModeTransitionOverride ov) =>
			{
				return (ov.FromModeIndex == fromModeIndex || ov.FromModeIndex == -1)
				 && (ov.ToModeIndex == index || ov.ToModeIndex == -1);
			});
		if( overrideParam != null )
		{
			param = overrideParam.Transition;
		}
		else
		{
			param = ModeTransitionParam;
		}

		// 遷移タイミング計算
		int currentSample = currentSample_;
		int fadeOffsetSample = param.SyncType == Music.SyncType.Immediate ? 0 : (int)(param.FadeOffsetSec * sampleRate_);
		int entryPointSample = Math.Max(-fadeOffsetSample, 0);
		int syncPointSample = 0;
		if( FindSyncPoint(param.SyncType, param.SyncFactor, currentSample, entryPointSample, out syncPointSample) == false )
		{
			requestedModeIndex_ = index;
			return;
		}

		// BaseVolume計算
		modeBaseVolume_ = modeVolume_;
		for( int i = 0; i < NumTracks; ++i )
		{
			modeLayerBaseVolumes_[i] = modeLayerVolumes_[i];
		}

		// フェード時間計算
		modeFade_.SetFade(
			Math.Max(currentSample, syncPointSample + fadeOffsetSample),
			(int)(param.FadeTimeSec * sampleRate_));

		// 状態遷移
		nextModeIndex_ = index;
		requestedModeIndex_ = -1;
		if( modeFade_.FadeStartSample <= currentSample )
		{
			ModeTransitionState = EModeTransitionState.Fading;
		}
		else
		{
			ModeTransitionState = EModeTransitionState.Sync;
		}

		UpdateVolumes();
	}
	
	#endregion

}
