using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class MusicSection
{
	public string Name = "Section";
	public AudioClip[] Clips = new AudioClip[1];
	public MusicMeter[] Meters = new MusicMeter[1] { new MusicMeter(0) };

	public Timing EntryPointTiming = new Timing();
	public Timing ExitPointTiming = new Timing();
	public Timing LoopStartTiming = new Timing();
	public Timing LoopEndTiming = new Timing();

	public TransitionParams Transition;

	[Serializable]
	public class TransitionParams
	{
		public TransitionParams() { }
		public TransitionParams(TransitionParams other)
		{
			SyncType = other.SyncType;
			SyncFactor = other.SyncFactor;
			UseFadeOut = other.UseFadeOut;
			FadeOutTime = other.FadeOutTime;
			FadeOutOffset = other.FadeOutOffset;
			UseFadeIn = other.UseFadeIn;
			FadeInTime = other.FadeInTime;
			FadeInOffset = other.FadeInOffset;
		}

		public Music.SyncType SyncType = Music.SyncType.Bar;
		public int SyncFactor = 1;

		public bool UseFadeOut = false;
		public float FadeOutTime;
		public float FadeOutOffset;

		public bool UseFadeIn = false;
		public float FadeInTime;
		public float FadeInOffset;
	}

	public enum ETransitionType
	{
		Loop,
		Transition,
		End,
	}
	public ETransitionType TransitionType;
	public int TransitionDestinationIndex;

	public bool IsValid { get; private set; }
	public int LoopStartSample { get; private set; }
	public int LoopEndSample { get; private set; }
	public int EntryPointSample { get; private set; }
	public int ExitPointSample { get; private set; }

	public void Initialize()
	{
		IsValid = false;
		if( Clips.Length == 0 || Clips[0] == null || Meters.Length == 0 )
		{
			return;
		}

		// 最初のメーターをValidateして、EntryPointのサンプル数を計算
		Meters[0].OnValidate(Clips[0].frequency, 0);
		EntryPointSample = Meters[0].GetSampleFromTiming(EntryPointTiming);

		// 後続のメーターすべてをValidate
		int startSample = EntryPointSample;
		MusicMeter lastMeter = null;
		foreach( MusicMeter meter in Meters )
		{
			if( lastMeter != null )
			{
				startSample += lastMeter.SamplesPerBar * meter.StartBar;
			}
			meter.OnValidate(Clips[0].frequency, startSample);
			lastMeter = meter;
		}

		Timing clipEndTiming = lastMeter.GetTimingFromSample(Clips[0].samples);
		// 波形終わりのタイミングを参考にExitPointを設定する
		if( ExitPointTiming <= EntryPointTiming || clipEndTiming < ExitPointTiming )
		{
			ExitPointTiming = new Timing(clipEndTiming);
			ExitPointTiming.FixToFloor();
		}
		if( clipEndTiming < LoopEndTiming )
		{
			LoopEndTiming = new Timing(clipEndTiming);
			LoopEndTiming.FixToFloor();
		}

		if( TransitionType == ETransitionType.Loop )
		{
			// LoopEndはLoopStartより後じゃないとダメ
			if( LoopStartTiming >= LoopEndTiming )
			{
				LoopEndTiming.Set(ExitPointTiming);
			}
			// ExitPointはLoopEndと同一
			ExitPointTiming = LoopEndTiming;

			LoopStartSample = GetSampleFromTiming(LoopStartTiming);
			LoopEndSample = GetSampleFromTiming(LoopEndTiming);
		}

		ExitPointSample = GetSampleFromTiming(ExitPointTiming);

		IsValid = true;
	}

	public int GetSampleFromTiming(Timing timing)
	{
		Timing nextMeterTiming = new Timing();
		if( timing < nextMeterTiming )
		{
			return 0;
		}

		MusicMeter meter = null;
		for( int i = 0; i < Meters.Length; ++i )
		{
			if( i + 1 < Meters.Length )
			{
				nextMeterTiming.Set(Meters[i + 1].StartBar);
				if( timing < nextMeterTiming )
				{
					meter = Meters[i];
					break;
				}
			}
			else // 最後のメーター
			{
				meter = Meters[i];
				break;
			}
		}

		return meter.GetSampleFromTiming(timing);
	}
}
