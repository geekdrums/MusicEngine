using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class MusicMeter
{
	public int StartBar = 0;
	public double Tempo = 120.0f;
	public int UnitPerBeat = 4;
	public int UnitPerBar = 16;
	public int Numerator = 4;
	public int Denominator = 4;

	public int StartSamples { get; private set; }
	public int SamplesPerUnit { get; private set; }
	public int SamplesPerBeat { get; private set; }
	public int SamplesPerBar { get; private set; }
	public double SecPerBar { get; private set; }
	public double SecPerBeat { get; private set; }
	public double SecPerUnit { get; private set; }

	public MusicMeter(int startBar, int unitPerBeat = 4, int unitPerBar = 16, double tempo = 120)
	{
		StartBar = startBar;
		Tempo = tempo;

		UnitPerBeat = unitPerBeat;
		UnitPerBar = unitPerBar;

		CalcMeterByUnits(UnitPerBeat, UnitPerBar, out Numerator, out Denominator);
	}

	public static void CalcMeterByUnits(int unitPerBeat, int unitPerBar, out int numerator, out int denominator)
	{
		if( unitPerBar % unitPerBeat == 0 )
		{
			denominator = unitPerBeat == 2 ? 8 : 4;
			numerator = unitPerBar / unitPerBeat;
		}
		else
		{
			int commonDividor = Euclid(unitPerBar, unitPerBeat * 4);
			numerator = unitPerBar / commonDividor;
			denominator = unitPerBeat * 4 / commonDividor;
		}
	}

	public static void CalcMeterByFraction(int numerator, int denominator, out int unitPerBeat, out int unitPerBar)
	{
		unitPerBeat = 4 * Mathf.Max(1, (denominator / 16));
		unitPerBar = numerator * ((unitPerBeat * 4) / denominator);
	}

	public static int Euclid(int small, int big)
	{
		if( big % small == 0 )
			return small;
		return Euclid(big % small, small);
	}

	public void OnValidate(int samplingRate = 44100, int startTimeSample = 0)
	{
		StartSamples = startTimeSample;

		double beatSec = (60.0 / Tempo);
		SamplesPerUnit = (int)(samplingRate * (beatSec / UnitPerBeat));
		SamplesPerBeat = (int)(samplingRate * beatSec);
		SamplesPerBar = (int)(samplingRate * UnitPerBar * (beatSec / UnitPerBeat));
		SecPerUnit = (double)SamplesPerUnit / (double)samplingRate;
		SecPerBeat = (double)SamplesPerBeat / (double)samplingRate;
		SecPerBar = (double)SamplesPerBar / (double)samplingRate;
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

	public Timing GetTimingFromSample(int sample)
	{
		if( sample < StartSamples )
		{
			return new Timing(StartBar);
		}
		else
		{
			int meterSamples = sample - StartSamples;
			int bar = (int)(meterSamples / SamplesPerBar);
			int beat = (int)((meterSamples - bar * SamplesPerBar) / SamplesPerBeat);
			int unit = (int)(((meterSamples - bar * SamplesPerBar) - beat * SamplesPerBeat) / SamplesPerUnit);
			return new Timing(bar + StartBar, beat, unit);
		}
	}

	
	public override string ToString()
	{
		return string.Format("({0}/{1}, {2:F2})", Numerator, Denominator, Tempo);
	}
}
