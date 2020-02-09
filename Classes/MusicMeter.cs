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


	#region properties

	public double StartSec { get; protected set; }
	public double SecPerBar { get; protected set; }
	public double SecPerBeat { get; protected set; }
	public double SecPerUnit { get; protected set; }

	public double StartMSec { get; protected set; }
	public double MSecPerBar { get; protected set; }
	public double MSecPerBeat { get; protected set; }
	public double MSecPerUnit { get; protected set; }

	#endregion


	public MusicMeter(int startBar, int unitPerBeat = 4, int unitPerBar = 16, double tempo = 120)
	{
		StartBar = startBar;
		Tempo = tempo;

		UnitPerBeat = unitPerBeat;
		UnitPerBar = unitPerBar;

		CalcMeterByUnits(UnitPerBeat, UnitPerBar, out Numerator, out Denominator);
	}
	
	public override string ToString()
	{
		return string.Format("({0}/{1}, {2:F2})", Numerator, Denominator, Tempo);
	}


	#region validate

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
	
	public void Validate(double startSec)
	{
		StartSec = startSec;
		SecPerBeat = (60.0 / Tempo);
		SecPerUnit = SecPerBeat / UnitPerBeat;
		SecPerBar = UnitPerBar * (SecPerBeat / UnitPerBeat);

		StartMSec = StartSec * 1000.0;
		MSecPerBeat = SecPerBeat * 1000.0;
		MSecPerUnit = SecPerUnit * 1000.0;
		MSecPerBar = SecPerBar * 1000.0;
	}

	#endregion


	#region convert

	public float GetMusicalTime(Timing just, float fractionFromJust)
	{
		return (just.GetTotalUnits(this) + fractionFromJust) / UnitPerBar;
	}
	
	public double GetSecondsFromTiming(Timing timing)
	{
		return StartSec + (timing.Bar - StartBar) * SecPerBar + timing.Beat * SecPerBeat + timing.Unit * SecPerUnit;
	}

	public double GetMilliSecondsFromTiming(Timing timing)
	{
		return StartMSec + (timing.Bar - StartBar) * MSecPerBar + timing.Beat * MSecPerBeat + timing.Unit * MSecPerUnit;
	}

	public Timing GetTimingFromSeconds(double sec)
	{
		if( sec < StartSec )
		{
			return new Timing(StartBar);
		}
		else
		{
			double meterSec = sec - StartSec;
			int bar = (int)(meterSec / SecPerBar);
			int beat = (int)((meterSec - bar * SecPerBar) / SecPerBeat);
			int unit = (int)(((meterSec - bar * SecPerBar) - beat * SecPerBeat) / SecPerUnit);
			return new Timing(bar + StartBar, beat, unit);
		}
	}

	public Timing GetTimingFromMilliSeconds(double msec)
	{
		return GetTimingFromSeconds(msec/1000.0);
	}

	#endregion

}

[Serializable]
public class MusicMeterBySample : MusicMeter
{
	public MusicMeterBySample(int startBar, int unitPerBeat = 4, int unitPerBar = 16, double tempo = 120)
		: base(startBar, unitPerBeat, unitPerBar, tempo)
	{

	}

	public int SampleRate { get; protected set; }
	public int StartSamples { get; protected set; }
	public int SamplesPerUnit { get; protected set; }
	public int SamplesPerBeat { get; protected set; }
	public int SamplesPerBar { get; protected set; }

	public void OnValidate(int sampleRate = 44100, int startTimeSample = 0)
	{
		SampleRate = sampleRate;

		Validate((double)startTimeSample / SampleRate);

		StartSamples = startTimeSample;
		SamplesPerBeat = (int)(SampleRate * SecPerBeat);
		SamplesPerUnit = (int)(SampleRate * (SecPerBeat / UnitPerBeat));
		SamplesPerBar = (int)(SampleRate * UnitPerBar * (SecPerBeat / UnitPerBeat));
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
}
