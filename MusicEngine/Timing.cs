using System;

public class Timing : IComparable<Timing>, IEquatable<Timing>
{
	public Timing( int b = 0, int be = 0, int u = 0 )
	{
		bar = b;
		beat = be;
		unit = u;
	}

	public Timing( Timing copy )
	{
		Copy( copy );
	}
	public Timing() { this.Init(); }
	public void Init() { bar = 0; beat = 0; unit = 0; }
	public void Copy( Timing copy )
	{
		bar = copy.bar;
		beat = copy.beat;
		unit = copy.unit;
	}

	public int bar, beat, unit;

	public int totalUnit { get { return Music.mtBar * bar + Music.mtBeat * beat + unit; } }
	public int totalBeat { get { return totalUnit/Music.mtBeat; } }
	public int barUnit { get { return ( unit < 0 ? Music.mtBar - unit : Music.mtBeat * beat + unit )%Music.mtBar; } }
	public void Increment()
	{
		unit++;
		if ( Music.mtBeat * beat + unit >= Music.mtBar )
		{
			unit = 0;
			beat = 0;
			bar += 1;
		}
		else if ( unit >= Music.mtBeat )
		{
			unit = 0;
			beat += 1;
		}
	}

	public void IncrementBeat()
	{
		beat++;
		if ( Music.mtBeat * beat + unit >= Music.mtBar )
		{
			beat = 0;
			bar += 1;
		}
	}


	public static bool operator >( Timing t, Timing t2 ) { return t.totalUnit > t2.totalUnit; }
	public static bool operator <( Timing t, Timing t2 ) { return !( t > t2 ) && !( t.Equals( t2 ) ); }
	public static bool operator <=( Timing t, Timing t2 ) { return !( t > t2 ); }
	public static bool operator >=( Timing t, Timing t2 ) { return t > t2 || t.Equals( t2 ); }
	public static int operator -( Timing t, Timing t2 ) { return t.totalUnit - t2.totalUnit; }
	public static Timing operator +( Timing t, Timing t2 )
	{
		return new Timing( t.bar + t2.bar + ( t.beat + t2.beat ) / Music.mtBar,
			( t.beat + t2.beat ) % Music.mtBeat + ( t.unit + t2.unit ) / Music.mtBeat,
			( t.unit + t2.unit ) % Music.mtBeat );
	}


	public bool IsEqualTo( int bar, int beat = 0, int unit = 0 )
	{
		return this.totalUnit == Music.mtBar * bar + Music.mtBeat * beat + unit;
	}

	public override bool Equals( object obj )
	{
		if ( object.ReferenceEquals( obj, null ) )
		{
			return false;
		}
		if ( object.ReferenceEquals( obj, this ) )
		{
			return true;
		}
		if ( this.GetType() != obj.GetType() )
		{
			return false;
		}
		return this.Equals( obj as Timing );
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public bool Equals( Timing other )
	{
		return this.totalUnit == other.totalUnit;
	}

	public int CompareTo( Timing tother )
	{
		if ( this.Equals( tother ) ) return 0;
		else if ( this > tother ) return 1;
		else return -1;
	}

	public override string ToString()
	{
		return bar + " " + beat + " " + unit;
	}
}
