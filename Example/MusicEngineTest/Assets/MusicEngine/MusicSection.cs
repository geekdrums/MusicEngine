using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Section : MonoBehaviour
{
	public AudioClip Clip;

	public enum ETransitionType
	{
		Loop,
		Transition,
		End,
	}
	public ETransitionType TransitionType;
	public int TransitionDestinationIndex;
	
	public MusicMeter[] Meters;

	public int EntryPointSample { get; private set; }
	public int ExitPointSample { get; private set; }
}
