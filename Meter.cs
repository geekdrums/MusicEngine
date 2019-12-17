using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class Meter
{
	public int UnitPerBeat;
	public int UnitPerBar;
	public double Tempo;
	public int StartBar;

	public int StartSamples { get; private set; }

	public int SamplesPerUnit { get; private set; }
	public int SamplesPerBeat { get; private set; }
	public int SamplesPerBar { get; private set; }
	public double SecPerUnit { get; private set; }

	public Meter(int startBar, int unitPerBeat = 4, int unitPerBar = 16, double tempo = 120)
	{
		StartBar = startBar;
		UnitPerBeat = unitPerBeat;
		UnitPerBar = unitPerBar;
		Tempo = tempo;
	}

	public void OnValidate(int samplingRate = 44100, int startTimeSample = 0)
	{
		StartSamples = startTimeSample;

		double beatSec = (60.0 / Tempo);
		SamplesPerUnit = (int)(samplingRate * (beatSec / UnitPerBeat));
		SamplesPerBeat = (int)(samplingRate * beatSec);
		SamplesPerBar = (int)(samplingRate * UnitPerBar * (beatSec / UnitPerBeat));
		SecPerUnit = (double)SamplesPerUnit / (double)samplingRate;
	}

	public float GetMusicalTime(Timing just, int samplesFromJust)
	{
		float barUnit = (float)(just.Beat * UnitPerBeat + just.Unit + (float)samplesFromJust / SamplesPerUnit);
		return (float)just.Bar + Mathf.Min(1.0f, barUnit / UnitPerBar);
	}

	public int GetSampleFromTiming(Timing timing)
	{
		return StartSamples + (timing.Bar - StartBar) * SamplesPerBar + timing.Beat * SamplesPerBeat + timing.Unit * SamplesPerUnit;
	}
	
	public override string ToString()
	{
		return string.Format("StartBar:{0}, Tempo:{1}", StartBar, Tempo);
	}
}
