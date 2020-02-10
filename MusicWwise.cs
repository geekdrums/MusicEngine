//#define Wwise
#if Wwise

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class MusicWwise : MusicBase
{
	#region editor params

	public AK.Wwise.Event Event;

	public string HorizontalStateGroup;
	public string InitialHorizontalState;
	public string VerticalStateGroup;
	public string InitialVerticalState;

	// SwitchContainerでは、波形の再生が終わってもセグメント「Nothing」を再生している、という状態になって
	// State変更でいつでも他のセグメント再生に戻れる状態がキープされます。このフラグをTrueにすることで、
	// Nothingに移行した事を検知して明示的にStopを呼び出し、MuiscインスタンスをFinish状態に移行させることができます。
	[Tooltip("SwitchContainer再生時にループしないセグメントが終わった時、自動的にStopを呼び出すことができます")]
	public bool StopWhenPostExitOfLastSegment = true;

	[Tooltip("Seek時に利用される。通常再生時はMeter情報は再生中に自動取得されるので不要です。")]
	public MusicMeter DefaultMeter = new MusicMeter(0);

	#endregion


	#region params

	private int currentMSec_;
	private int sequenceEndBar_;
	private bool endOfEvent_;
	private Timing seekTiming_;

	private uint playingId_;
	private AkSegmentInfo segmentInfo_;

	public override int SequenceIndex { get { return -1; } }
	public override string SequenceName { get { return ""; } }

	public enum ETransitionState
	{
		Invalid,
		Intro,
		Active,
		Outro,
	}
	public ETransitionState TransitionState { get; private set; } = ETransitionState.Invalid;

	#endregion


	#region override functions

	// internal

	protected override bool ReadyInternal()
	{
		DefaultMeter.Validate(0);
		if( Event != null && Event.Id != AkSoundEngine.AK_INVALID_UNIQUE_ID )
		{
			return true;
		}
		return false;
	}

	protected override void SeekInternal(Timing seekTiming, int sequenceIndex = 0)
	{
		seekTiming_ = seekTiming;
	}

	protected override bool PlayInternal()
	{
		uint callbackFlags = (uint)(AkCallbackType.AK_EnableGetMusicPlayPosition | AkCallbackType.AK_MusicSyncEntry | AkCallbackType.AK_EndOfEvent);
		playingId_ = Event.Post(this.gameObject,callbackFlags, AkEventCallback);
		TransitionState = ETransitionState.Intro;

		if( seekTiming_ != null && seekTiming_ >= Timing.Zero )
		{
			AkSoundEngine.SeekOnEvent(Event.Id, gameObject, (int)DefaultMeter.GetMilliSecondsFromTiming(seekTiming_));
		}

		return (playingId_ != AkSoundEngine.AK_INVALID_PLAYING_ID);
	}

	protected override bool SuspendInternal()
	{
		Event.ExecuteAction(gameObject, AkActionOnEventType.AkActionOnEventType_Pause, 0, AkCurveInterpolation.AkCurveInterpolation_Linear);
		return true;
	}

	protected override bool ResumeInternal()
	{
		Event.ExecuteAction(gameObject, AkActionOnEventType.AkActionOnEventType_Resume, 0, AkCurveInterpolation.AkCurveInterpolation_Linear);
		return true;
	}

	protected override bool StopInternal()
	{
		AkSoundEngine.StopPlayingID(playingId_);
		return true;
	}

	protected override void ResetParamsInternal()
	{
		seekTiming_ = null;
		currentMSec_ = 0;
		sequenceEndBar_ = 0;
		endOfEvent_ = false;
		playingId_ = AkSoundEngine.AK_INVALID_PLAYING_ID;
		TransitionState = ETransitionState.Invalid;
		segmentInfo_ = new AkSegmentInfo();
		if( string.IsNullOrEmpty(HorizontalStateGroup) == false && string.IsNullOrEmpty(InitialHorizontalState) == false )
		{
			AkSoundEngine.SetState(HorizontalStateGroup, InitialHorizontalState);
		}
		if( string.IsNullOrEmpty(VerticalStateGroup) == false && string.IsNullOrEmpty(InitialVerticalState) == false )
		{
			AkSoundEngine.SetState(VerticalStateGroup, InitialVerticalState);
		}
	}

	// timing

	protected override void UpdateTimingInternal()
	{
		AkSoundEngine.GetPlayingSegmentInfo(playingId_, segmentInfo_);
		currentMSec_ = segmentInfo_.iCurrentPosition;

		bool isActiveSegmentIsNothing = segmentInfo_.iPreEntryDuration == 0 && segmentInfo_.iPostExitDuration == 0 && segmentInfo_.iActiveDuration == 0;
		if( TransitionState == ETransitionState.Intro && isActiveSegmentIsNothing == false )
		{
			TransitionState = ETransitionState.Active;
		}
		else if( TransitionState == ETransitionState.Active )
		{
			if( isActiveSegmentIsNothing )
			{
				if( StopWhenPostExitOfLastSegment )
				{
					endOfEvent_ = true;
				}
				else
				{
					TransitionState = ETransitionState.Outro;
				}
			}
			else if( currentMeter_ == null )
			{
				CalcMeter(segmentInfo_.fBeatDuration, segmentInfo_.fBarDuration, segmentInfo_.iActiveDuration);
			}
		}
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
			just.Set(currentMeter_.GetTimingFromMilliSeconds(currentMSec_));
			fraction = (float)((currentMSec_ - currentMeter_.GetMilliSecondsFromTiming(just)) / currentMeter_.MSecPerUnit);
		}
	}

	protected override Timing GetSequenceEndTiming()
	{
		return sequenceEndBar_ > 0 ? new Timing(sequenceEndBar_) : null;
	}

	// update

	protected override bool CheckFinishPlaying()
	{
		if( endOfEvent_ )
		{
			endOfEvent_ = false;
			return true;
		}
		return false;
	}

	protected override void UpdateInternal()
	{

	}

	// interactive music

	public override void SetHorizontalSequence(string name)
	{
		if( string.IsNullOrEmpty(HorizontalStateGroup) == false )
		{
			AkSoundEngine.SetState(HorizontalStateGroup, name);
		}
	}

	public override void SetHorizontalSequenceByIndex(int index)
	{
		print("SetHorizontalSequenceByIndex is not implemented in MusicWwise");
	}

	public override void SetVerticalMix(string name)
	{
		if( string.IsNullOrEmpty(VerticalStateGroup) == false )
		{
			AkSoundEngine.SetState(VerticalStateGroup, name);
		}
	}

	public override void SetVerticalMixByIndex(int index)
	{
		print("SetVerticalMixByIndex is not implemented in MusicWwise");
	}

	#endregion


	#region callback / utils

	void AkEventCallback(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
	{
		switch( in_type )
		{
			case AkCallbackType.AK_MusicSyncEntry:
				AkMusicSyncCallbackInfo musicSyncCallbackInfo = in_info as AkMusicSyncCallbackInfo;
				CalcMeter(musicSyncCallbackInfo.segmentInfo_fBeatDuration,
						  musicSyncCallbackInfo.segmentInfo_fBarDuration,
						  musicSyncCallbackInfo.segmentInfo_iActiveDuration);
				break;
			case AkCallbackType.AK_EndOfEvent:
				AkEventCallbackInfo endInfo = in_info as AkEventCallbackInfo;
				if( endInfo.playingID == playingId_ )
				{
					endOfEvent_ = true;
				}
				break;
		}
	}

	void CalcMeter(float fBeatDuration, float fBarDuration, int iActiveDuration)
	{
		double tempo = 60.0 / fBeatDuration;
		int unitPerBeat = 4;
		int unitPerBar = Mathf.RoundToInt(unitPerBeat * (fBarDuration / fBeatDuration));
		currentMeter_ = new MusicMeter(0, unitPerBeat, unitPerBar, tempo);
		sequenceEndBar_ = Mathf.RoundToInt((iActiveDuration / 1000.0f) / fBarDuration);
		currentMeter_.Validate(0);
	}

	#endregion
}

#endif