using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Scale
{
	public Scale( params int[] tones )
	{
		foreach ( int t in tones )
		{
			if ( 0 <= t && t < Tone.OCTAVE )
			{
				scales[t] = true;
			}
		}
	}
	public Scale( string s )
	{
		tones = new Chord( s );
		foreach ( int t in tones )
		{
			if ( 0 <= t && t < Tone.OCTAVE )
			{
				scales[t] = true;
			}
		}
	}

	private bool[] scales = new bool[Tone.OCTAVE];
	public Chord tones { get; private set; }
	public int numTones { get { return tones.numTones; } }
	public int this[int i]
	{
		get
		{
			//何オクターブ目の音か
			int octave = ( i + ( i < 0 ? 1 - numTones : 0 ) ) / numTones;
			i -= octave * numTones;
			return tones[i] + Tone.OCTAVE * octave;
		}
	}

	private static bool plusminus = true;
	public int Fix( int tone )
	{
		int i = tone % Tone.OCTAVE;
		if ( i < 0 ) i += Tone.OCTAVE;
		if ( scales[i] )
		{
			plusminus = !plusminus;
			return tone;
		}
		else if ( plusminus )
			return Fix( tone + 1 );
		else
			return Fix( tone - 1 );
	}
	public int Fix( int tone, bool pm )
	{
		int i = tone % Tone.OCTAVE;
		if ( i < 0 ) i += Tone.OCTAVE;
		if ( scales[i] )
		{
			return tone;
		}
		else if ( pm )
			return Fix( tone + 1 );
		else
			return Fix( tone - 1 );
	}
}