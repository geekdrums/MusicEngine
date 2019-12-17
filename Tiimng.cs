using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class Timing : IComparable<Timing>, IEquatable<Timing>
{
	public int Bar, Beat, Unit;

	public Timing(int bar = 0, int beat = 0, int unit = 0)
	{
		Bar = bar;
		Beat = beat;
		Unit = unit;
	}
	public Timing() { this.Reset(); }
	public Timing(Timing t) { Set(t); }

	public bool IsZero()
	{
		return Bar == 0 && Beat == 0 && Unit == 0;
	}
	public int GetTotalUnits(Meter meter)
	{
		return Bar * meter.UnitPerBar + Beat * meter.UnitPerBeat + Unit;
	}

	public void SetBar(int bar) { Bar = bar; }
	public void SetBeat(int beat) { Beat = beat; }
	public void SetUnit(int unit) { Unit = unit; }
	public void Set(int bar, int beat = 0, int unit = 0) { Bar = bar; Beat = beat; Unit = unit; }
	public void Set(Timing t) { Bar = t.Bar; Beat = t.Beat; Unit = t.Unit; }
	public void Reset() { Bar = 0; Beat = 0; Unit = 0; }

	public void Fix(Meter meter)
	{
		int totalUnit = Bar * meter.UnitPerBar + Beat * meter.UnitPerBeat + Unit;
		Bar = totalUnit / meter.UnitPerBar;
		Beat = (totalUnit - Bar * meter.UnitPerBar) / meter.UnitPerBeat;
		Unit = (totalUnit - Bar * meter.UnitPerBar - Beat * meter.UnitPerBeat);
	}
	public void FixToCeil()
	{
		if( Beat > 0 || Unit > 0 )
		{
			++Bar;
			Beat = Unit = 0;
		}
	}
	public void LoopBack(int loopBar, Meter meter)
	{
		if( loopBar > 0 )
		{
			Bar += loopBar;
			Fix(meter);
			Bar %= loopBar;
		}
	}

	public void Increment(Meter meter)
	{
		++Unit;
		Fix(meter);
	}
	public void Decrement(Meter meter)
	{
		--Unit;
		Fix(meter);
	}
	public void Add(int bar, int beat, int unit, Meter meter)
	{
		Bar += bar; Beat += beat; Unit += unit;
		Fix(meter);
	}
	public void Add(Timing t, Meter meter)
	{
		Add(t.Bar, t.Beat, t.Unit, meter);
	}
	public void Subtract(int bar, int beat, int unit, Meter meter)
	{
		Bar -= bar; Beat -= beat; Unit -= unit;
		Fix(meter);
	}
	public void Subtract(Timing t, Meter meter)
	{
		Subtract(t.Bar, t.Beat, t.Unit, meter);
	}

	public static bool operator ==(Timing t, Timing t2) { return (t.Bar == t2.Bar && t.Beat == t2.Beat && t.Unit == t2.Unit ); }
	public static bool operator !=(Timing t, Timing t2) { return !(t == t2); }
	public static bool operator >(Timing t, Timing t2) { return t.Bar > t2.Bar || (t.Bar == t2.Bar && t.Beat > t2.Beat) || (t.Bar == t2.Bar && t.Beat == t2.Beat && t.Unit > t2.Unit); }
	public static bool operator <(Timing t, Timing t2) { return !(t > t2) && !(t.Equals(t2)); }
	public static bool operator <=(Timing t, Timing t2) { return !(t > t2); }
	public static bool operator >=(Timing t, Timing t2) { return !(t < t2); }

	public override bool Equals(object obj)
	{
		if( object.ReferenceEquals(obj, null) )
		{
			return false;
		}
		if( object.ReferenceEquals(obj, this) )
		{
			return true;
		}
		if( this.GetType() != obj.GetType() )
		{
			return false;
		}
		return this.Equals(obj as Timing);
	}
	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
	public bool Equals(Timing other)
	{
		return (this.Bar == other.Bar && this.Beat == other.Beat && this.Unit == other.Unit);
	}
	public int CompareTo(Timing tother)
	{
		if( this.Equals(tother) ) return 0;
		else if( this > tother ) return 1;
		else return -1;
	}

	public override string ToString()
	{
		return Bar + " " + Beat + " " + Unit;
	}
}
