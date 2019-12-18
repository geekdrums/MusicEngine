using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicSection : MonoBehaviour
{
	public AudioClip[] Clips = new AudioClip[1];

	public MusicMeter[] Meters = new MusicMeter[1] { new MusicMeter(0) };

	public Timing EntryPointTiming = new Timing();
	public Timing ExitPointTiming = new Timing();
	public Timing LoopStartTiming = new Timing();
	public Timing LoopEndTiming = new Timing();

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

	
	void OnValidate()
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

		// ExitPointが未割り当てだったら、波形終わりのタイミングを参考に設定する
		if( EntryPointTiming >= ExitPointTiming )
		{
			ExitPointTiming = lastMeter.GetTimingFromSample(Clips[0].samples);
			ExitPointTiming.FixToCeil();
		}

		if( TransitionType == ETransitionType.Loop )
		{
			// LoopStartはEntryPointより後じゃないとダメ
			if( LoopStartTiming < EntryPointTiming )
			{
				LoopStartTiming.Set(EntryPointTiming);
			}
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

	int GetSampleFromTiming(Timing timing)
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
