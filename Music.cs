//Copyright (c) 2019 geekdrums
//Released under the MIT license
//http://opensource.org/licenses/mit-license.php
//Feel free to use this for your lovely musical games :)

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public abstract class Music : MonoBehaviour
{
	#region static params

	protected static Music Current_;
	protected static List<Music> MusicList_ = new List<Music>();

	public delegate void HorizontalSequenceEvent(string name);
	protected static HorizontalSequenceEvent OnTransitionedEvent;
	protected static HorizontalSequenceEvent OnRepeatedEvent;
	protected static readonly float PITCH_UNIT = Mathf.Pow(2.0f, 1.0f / 12.0f);

	public enum SyncType
	{
		Immediate,
		Unit,
		Beat,
		Bar,
		Marker,
		ExitPoint,
	};

	public enum TimeUnitType
	{
		Sec,
		MSec,
		Bar,
		Beat,
		Unit,
		Sample,
	};
	
	public static class TimeUtility
	{
		public static float DefaultBPM = 120;

		public static float ConvertTime(float time, TimeUnitType from, TimeUnitType to = TimeUnitType.Sec)
		{
			if( from == to ) return time;

			float sec = time;
			if( from == TimeUnitType.Sec )
			{
				sec = time;
			}
			else if( from == TimeUnitType.MSec )
			{
				sec = time / 1000.0f;
			}
			else if( from == TimeUnitType.Sample )
			{
				sec = (float)time / Music.CurrentSampleRate;
			}
			else
			{
				if( Music.HasValidMeter )
				{
					switch( from )
					{
						case TimeUnitType.Bar:
							sec = time * (float)Music.CurrentMeter.SecPerBar;
							break;
						case TimeUnitType.Beat:
							sec = time * (float)Music.CurrentMeter.SecPerBeat;
							break;
						case TimeUnitType.Unit:
							sec = time * (float)Music.CurrentMeter.SecPerUnit;
							break;
					}
				}
				else
				{
					switch( from )
					{
						case TimeUnitType.Bar:
							sec = time * (60.0f * 4.0f / DefaultBPM);
							break;
						case TimeUnitType.Beat:
							sec = time * (60.0f / DefaultBPM);
							break;
						case TimeUnitType.Unit:
							sec = time * (60.0f / 4.0f / DefaultBPM);
							break;
					}
				}
			}

			if( to == TimeUnitType.Sec )
			{
				return sec;
			}
			else if( to == TimeUnitType.MSec )
			{
				return sec * 1000.0f;
			}
			else if( to == TimeUnitType.Sample )
			{
				return sec * Music.CurrentSampleRate;
			}
			else
			{
				if( Music.HasValidMeter )
				{
					switch( to )
					{
						case TimeUnitType.Bar:
							return sec / (float)Music.CurrentMeter.SecPerBar;
						case TimeUnitType.Beat:
							return sec / (float)Music.CurrentMeter.SecPerBeat;
						case TimeUnitType.Unit:
							return sec / (float)Music.CurrentMeter.SecPerUnit;
					}
				}
				else
				{
					switch( to )
					{
						case TimeUnitType.Bar:
							return sec / (60.0f * 4.0f / DefaultBPM);
						case TimeUnitType.Beat:
							return sec / (60.0f / DefaultBPM);
						case TimeUnitType.Unit:
							return sec / (60.0f / 4.0f / DefaultBPM);
					}
				}
			}

			return sec;
		}
	}

	#endregion


	#region public static properties

	public static Music Current { get { return Current_; } }

	public static bool IsPlaying { get { return Current_.IsPlaying_; } }
	/// <summary>
	/// means last timing.
	/// </summary>
	public static Timing Just { get { return Current_.just_; } }
	/// <summary>
	/// means nearest timing.
	/// </summary>
	public static Timing Near { get { return Current_.near_; } }
	/// <summary>
	/// is Just changed in this frame or not.
	/// </summary>
	public static bool IsJustChanged { get { return Current_.isJustChanged_; } }
	/// <summary>
	/// is Near changed in this frame or not.
	/// </summary>
	public static bool IsNearChanged { get { return Current_.isNearChanged_; } }
	/// <summary>
	/// is currently former half in a MusicTimeUnit, or last half.
	/// </summary>
	public static bool IsFormerHalf { get { return Current_.isFormerHalf_; } }
	/// <summary>
	/// how many times you repeat current music/block.
	/// </summary>
	public static int NumRepeat { get { return Current_.numRepeat_; } }

	/// <summary>
	/// returns how long from nearest Just timing with sign.
	/// </summary>
	public static double SecFromJust { get { return Current_.SecFromJust_; } }
	/// <summary>
	/// returns how long from nearest Just timing absolutely.
	/// </summary>
	public static double SecFromJustAbs { get { return Current_.SecFromJustAbs_; } }
	/// <summary>
	/// returns normalized lag.
	/// </summary>
	public static double UnitFromJust { get { return Current_.UnitFromJust_; } }

	/// <summary>
	/// returns samples per sec
	/// </summary>
	public static int CurrentSampleRate { get { return Current_.sampleRate_; } }

	public static bool HasValidMeter { get { return Current_ != null && Current_.currentMeter_ != null; } }
	/// <summary>
	/// current musical meter
	/// </summary>
	public static MusicMeter CurrentMeter { get { return Current_.currentMeter_; } }
	public static int CurrentUnitPerBar { get { return Current_.currentMeter_.UnitPerBar; } }
	public static int CurrentUnitPerBeat { get { return Current_.currentMeter_.UnitPerBeat; } }
	public static double CurrentTempo { get { return Current_.currentMeter_.Tempo; } }

	public static string CurrentMusicName { get { return Current_.name; } }
	public static string CurrentSequenceName { get { return Current_.SequenceName; } }
	public static int CurrentSequenceIndex { get { return Current_.SequenceIndex; } }

	/// <summary>
	/// current musical time in units
	/// </summary>
	public static int JustTotalUnits { get { return Current_.JustTotalUnits_; } }
	/// <summary>
	/// current musical time in units
	/// </summary>
	public static int NearTotalUnits { get { return Current_.NearTotalUnits_; } }
	/// <summary>
	/// current musical time in bars
	/// </summary>
	public static float MusicalTime { get { return Current_.MusicalTime_; } }

	/// <summary>
	/// returns musically synced cos wave.
	/// if default( MusicalCos(16,0,0,1),
	/// starts from max=1,
	/// reaches min=0 on MusicalTime = cycle/2 = 8,
	/// back to max=1 on MusicalTIme = cycle = 16.
	/// </summary>
	/// <param name="cycle">wave cycle in musical unit</param>
	/// <param name="offset">wave offset in musical unit</param>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	public static float MusicalCos(float cycle = 16, float offset = 0, float min = 0, float max = 1)
	{
		return Mathf.Lerp(min, max, ((float)Math.Cos(Math.PI * 2 * (CurrentUnitPerBar * MusicalTime + offset) / cycle) + 1.0f) / 2.0f);
	}

	#endregion


	#region public static predicates

	public static bool IsJustChangedWhen(Predicate<Timing> pred)
	{
		return Current_.IsJustChangedWhen_(pred);
	}
	public static bool IsJustChangedBar()
	{
		return Current_.IsJustChangedBar_();
	}
	public static bool IsJustChangedBeat()
	{
		return Current_.IsJustChangedBeat_();
	}
	public static bool IsJustChangedAt(int bar = 0, int beat = 0, int unit = 0)
	{
		return Current_.IsJustChangedAt_(bar, beat, unit);
	}
	public static bool IsJustChangedAt(Timing t)
	{
		return Current_.IsJustChangedAt_(t.Bar, t.Beat, t.Unit);
	}

	public static bool IsNearChangedWhen(Predicate<Timing> pred)
	{
		return Current_.IsNearChangedWhen_(pred);
	}
	public static bool IsNearChangedBar()
	{
		return Current_.IsNearChangedBar_();
	}
	public static bool IsNearChangedBeat()
	{
		return Current_.IsNearChangedBeat_();
	}
	public static bool IsNearChangedAt(int bar, int beat = 0, int unit = 0)
	{
		return Current_.IsNearChangedAt_(bar, beat, unit);
	}
	public static bool IsNearChangedAt(Timing t)
	{
		return Current_.IsNearChangedAt_(t.Bar, t.Beat, t.Unit);
	}

	#endregion


	#region public static functions

	/// <summary>
	/// Change Current Music.
	/// </summary>
	/// <param name="musicName">name of the GameObject that include Music</param>
	public static void Play(string musicName)
	{
		Music music = MusicList_.Find((Music m) => m != null && m.name == musicName);
		if( music != null )
		{
			Play(music);
		}
		else
		{
			Debug.Log("Can't find music: " + musicName);
		}
	}

	public static void Play(Music music)
	{
		if( Current_ != null && Current_.IsPlaying_ )
		{
			Current_.Stop_();
		}

		music.Play_();
	}

	public static void PlayFrom(string musicName, int sequenceIndex, Timing seekTiming)
	{
		Music music = MusicList_.Find((Music m) => m != null && m.name == musicName);
		if( music != null )
		{
			music.PlayFrom_(sequenceIndex, seekTiming);
		}
		else
		{
			Debug.Log("Can't find music: " + musicName);
		}
	}

	public static void PlayFrom(Music music, int sequenceIndex, Timing seekTiming)
	{
		music.PlayFrom_(sequenceIndex, seekTiming);
	}

	public static void Suspend() { Current_.Suspend_(); }
	public static void Resume() { Current_.Resume_(); }
	public static void Stop() { Current_.Stop_(); }

	#endregion


	#region interface

	protected abstract void Initialize();

	protected virtual void Ready()
	{
		currentSample_ = 0;
		isJustChanged_ = false;
		isNearChanged_ = false;
		isJustLooped_ = false;
		isNearLooped_ = false;
		near_.Set(-1, 0, 0);
		just_.Set(-1, 0, 0);
		oldNear_.Set(near_);
		oldJust_.Set(just_);
		samplesFromJust_ = 0;
		isFormerHalf_ = true;
		numRepeat_ = 0;
	}

	public virtual void Play_()
	{
		Current_ = this;
		Ready();
	}

	public virtual void PlayFrom_(int sequenceIndex, Timing seekTiming)
	{
		Seek(sequenceIndex, seekTiming);
		Play_();
	}

	public abstract void Stop_();

	public abstract void Suspend_();

	public abstract void Resume_();

	public abstract bool PlayOnStart { get; set; }

	public abstract float Volume { get; set; }

	public abstract string SequenceName { get; }

	public abstract int SequenceIndex { get; }

	public abstract bool IsPlaying_ { get; }

	public abstract void Seek(int sequenceIndex, Timing seekTiming);

	public abstract void SetHorizontalSequence(string name);

	public abstract void SetHorizontalSequenceByIndex(int index);

	public abstract void SetVerticalMixByIndex(int index);

	public abstract void SetVerticalMix(string name);

	protected abstract int GetCurrentSample();

	protected abstract int GetSampleRate();

	protected abstract MusicMeter GetMeterFromSample(int currentSample);

	protected abstract Timing GetSequenceEndTiming();

	protected abstract void UpdateHorizontalState();

	protected abstract void UpdateVerticalState();

	#endregion


	#region protected params

	// 現在再生中の箇所のメーター情報。
	protected MusicMeter currentMeter_;
	// 最新のJustタイミング。(タイミングちょうどになってから切り替わる）
	protected Timing just_ = new Timing(-1, 0, 0);
	// 最新のNearタイミング。（最も近いタイミングが変わった地点、つまり2つのタイミングの中間で切り替わる）
	protected Timing near_ = new Timing(-1, 0, 0);
	// 1フレーム前のJustタイミング。
	protected Timing oldJust_ = new Timing(-1, 0, 0);
	// 1フレーム前のNearタイミング。
	protected Timing oldNear_ = new Timing(-1, 0, 0);

	// 今のフレームでjust_が変化したフラグ。
	protected bool isJustChanged_ = false;
	// 今のフレームでnear_が変化したフラグ。
	protected bool isNearChanged_ = false;
	// 今のフレームでjust_がループして戻ったフラグ。
	protected bool isJustLooped_ = false;
	// 今のフレームでnear_がループして戻ったフラグ。
	protected bool isNearLooped_ = false;
	// 今がunit内の前半かどうか。 true なら just_ == near_, false なら ++just == near。
	protected bool isFormerHalf_;

	// 現在の曲のサンプルレート。
	protected int sampleRate_ = 44100;
	// 現在の再生サンプル数。
	protected int currentSample_;
	// Justのタイミングから何サンプル過ぎているか。
	protected int samplesFromJust_;
	// 現在のループカウント。
	protected int numRepeat_;
	// 現在のシーケンス（横の遷移の単位）の小節数。
	protected Timing sequenceEndTiming_ = null;
	
	#endregion


	#region protected properties

	protected double SecFromJust_
	{
		get
		{
			if( isFormerHalf_ )
				return samplesFromJust_ / sampleRate_;
			else
				return samplesFromJust_ / sampleRate_ - currentMeter_.SecPerUnit;
		}
	}
	protected double SecFromJustAbs_ { get { return Math.Abs(SecFromJust_); } }
	protected double UnitFromJust_ { get { return SecFromJust_ / currentMeter_.SecPerUnit; } }
	protected double UnitFromJustAbs_ { get { return Math.Abs(UnitFromJust_); } }

	protected int JustTotalUnits_ { get { return just_.GetTotalUnits(currentMeter_); } }
	protected int NearTotalUnits_ { get { return near_.GetTotalUnits(currentMeter_); } }
	protected float MusicalTime_ { get { return currentMeter_ != null ? currentMeter_.GetMusicalTime(just_, samplesFromJust_) : -1.0f; } }
	
	#endregion


	#region protected predicates

	protected bool IsNearChangedWhen_(Predicate<Timing> pred)
	{
		if( isNearChanged_ )
		{
			if( pred(near_) ) return true;
		}
		return false;
	}
	protected bool IsNearChangedBar_()
	{
		return isNearChanged_ && (oldNear_.Bar != near_.Bar);
	}
	protected bool IsNearChangedBeat_()
	{
		return isNearChanged_ && (oldNear_.Beat != near_.Beat);
	}
	protected bool IsNearChangedAt_(int bar = 0, int beat = 0, int unit = 0)
	{
		return IsNearChangedAt_(new Timing(bar, beat, unit));
	}
	protected bool IsNearChangedAt_(Timing t)
	{
		return (isNearChanged_ && (oldNear_ < t && t <= near_)) || (isNearLooped_ && (oldNear_ < t || t <= near_));
	}
	protected bool IsJustChangedWhen_(Predicate<Timing> pred)
	{
		if( isJustChanged_ )
		{
			if( pred(just_) ) return true;
		}
		return false;
	}
	protected bool IsJustChangedBar_()
	{
		return isJustChanged_ && (oldJust_.Bar != just_.Bar);
	}
	protected bool IsJustChangedBeat_()
	{
		return isJustChanged_ && (oldJust_.Beat != just_.Beat);
	}
	protected bool IsJustChangedAt_(int bar = 0, int beat = 0, int unit = 0)
	{
		return IsJustChangedAt_(new Timing(bar, beat, unit));
	}
	protected bool IsJustChangedAt_(Timing t)
	{
		return (isJustChanged_ && (oldJust_ < t && t <= just_)) || (isJustLooped_ && (oldJust_ < t || t <= just_));
	}

	#endregion


	#region unity functions

	void Awake()
	{
		MusicList_.Add(this);
		if( Current_ == null || PlayOnStart )
		{
			Current_ = this;
		}
		Initialize();
		Ready();
		currentMeter_ = GetMeterFromSample(0);
		sampleRate_ = GetSampleRate();
	}

	void Start()
	{
#if UNITY_EDITOR
		 UnityEditor.EditorApplication.pauseStateChanged += OnPlaymodeStateChanged;
#endif
		if( PlayOnStart )
		{
			Music.Play(this);
		}
	}

	protected virtual void Update()
	{
		if( IsPlaying == false )
		{
			return;
		}

		UpdateTiming();
	}

	protected void UpdateTiming()
	{
		int oldSample = currentSample_;
		oldNear_.Set(near_);
		oldJust_.Set(just_);
		isNearChanged_ = false;
		isJustChanged_ = false;
		isJustLooped_ = false;
		isNearLooped_ = false;

		int oldSequenceIndex = SequenceIndex;
		UpdateHorizontalState();
		UpdateVerticalState();

		currentSample_ = GetCurrentSample();
		if( currentSample_ < 0 )
		{
			return;
		}

		currentMeter_ = GetMeterFromSample(currentSample_);
		if( currentMeter_ == null )
		{
			just_.Set(-1, 0, 0);
		}
		else
		{
			int meterSample = currentSample_ - currentMeter_.StartSamples;
			int bar = (int)(meterSample / currentMeter_.SamplesPerBar);
			int beat = (int)((meterSample - bar * currentMeter_.SamplesPerBar) / currentMeter_.SamplesPerBeat);
			int unit = (int)(((meterSample - bar * currentMeter_.SamplesPerBar) - beat * currentMeter_.SamplesPerBeat) / currentMeter_.SamplesPerUnit);
			just_.Set(bar + currentMeter_.StartBar, beat, unit);
			just_.Fix(currentMeter_);

			samplesFromJust_ = currentSample_ - currentMeter_.GetSampleFromTiming(just_);
			isFormerHalf_ = samplesFromJust_ < currentMeter_.SamplesPerUnit / 2;

			near_.Set(just_);
			if( !isFormerHalf_ )
			{
				near_.Increment(currentMeter_);
			}


			if( sequenceEndTiming_ != null )
			{
				while( just_ >= sequenceEndTiming_ )
				{
					just_.Decrement(currentMeter_);
				}
				if( near_ >= sequenceEndTiming_ )
				{
					near_.Reset();
				}
			}

			isJustChanged_ = (just_.Equals(oldJust_) == false);
			isNearChanged_ = (near_.Equals(oldNear_) == false);
			isJustLooped_ = isJustChanged_ && just_ < oldJust_;
			isNearLooped_ = isNearChanged_ && near_ < oldNear_;

			if( isJustLooped_ )
			{
				if( oldSequenceIndex != SequenceIndex )
				{
					OnHorizontalSequenceChanged();
				}
				else
				{
					OnRepeated();
				}
			}
		}
	}

#if UNITY_EDITOR
	void OnPlaymodeStateChanged(UnityEditor.PauseState state)
	{
		if( Current_ != null )
		{
			if ( state == UnityEditor.PauseState.Paused )
			{
				Current_.Suspend_();
			}
			else
			{
				Current_.Resume_();
			}
		}
	}
#endif

	#endregion
	

	#region Events

	protected virtual void OnRepeated()
	{
		++numRepeat_;
	}

	protected virtual void OnHorizontalSequenceChanged()
	{
		numRepeat_ = 0;
		sequenceEndTiming_ = GetSequenceEndTiming();
		if( OnTransitionedEvent != null )
		{
			OnTransitionedEvent(SequenceName);
			OnTransitionedEvent = null;
		}
	}

	#endregion



	public override string ToString()
	{
		return String.Format("{0}", just_.ToString());
	}
}
