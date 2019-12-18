using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicSection : MonoBehaviour
{
	public AudioClip[] Clips;

	public MusicMeter[] Meters;

	public enum ETransitionType
	{
		Loop,
		Transition,
		End,
	}
	public ETransitionType TransitionType;
	public int TransitionDestinationIndex;

	public int LoopStartSample { get; private set; }
	public int LoopLengthSample { get; private set; }
	public int EntryPointSample { get; private set; }
	public int ExitPointSample { get; private set; }



	void OnValidate()
	{
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
	}
}
