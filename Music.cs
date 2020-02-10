//Copyright (c) 2020 geekdrums
//Released under the MIT license
//http://opensource.org/licenses/mit-license.php
//Feel free to use this for your lovely musical games :)

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Static access to currently playing music.
/// Music.Play will set and change current instance.
/// </summary>
public static class Music
{
	#region properties
	
	// current music & state

	public static MusicBase Current { get { return current_; } }
	public static string CurrentMusicName { get { return current_ != null ? current_.name : ""; } }
	public static bool IsPlaying { get { return current_ != null ? current_.IsPlaying : false; } }
	public static bool IsPlayingOrSuspended { get { return current_ != null ? current_.IsPlayingOrSuspended : false; } }
	public static PlayState State { get { return current_ != null ? current_.State : PlayState.Invalid; } }
	
	// current timing

	/// <summary>
	/// Last timing.
	/// </summary>
	public static Timing Just { get { return current_ != null ? current_.Just : null; } }
	/// <summary>
	/// Nearest timing.
	/// </summary>
	public static Timing Near { get { return current_ != null ? current_.Near : null; } }
	/// <summary>
	/// is Just changed in this frame or not.
	/// </summary>
	public static bool IsJustChanged { get { return current_ != null ? current_.IsJustChanged : false; } }
	/// <summary>
	/// is Near changed in this frame or not.
	/// </summary>
	public static bool IsNearChanged { get { return current_ != null ? current_.IsNearChanged : false; } }
	/// <summary>
	/// is currently former half in a MusicTimeUnit, or last half.
	/// </summary>
	public static bool IsFormerHalf { get { return current_ != null ? current_.IsFormerHalf : false; } }
	/// <summary>
	/// returns sec from last Just timing.
	/// </summary>
	public static double SecFromJust { get { return current_ != null ? current_.SecFromJust : 0; } }
	/// <summary>
	/// returns normalized time (0 to 1) from last Just timing.
	/// </summary>
	public static double UnitFromJust { get { return current_ != null ? current_.UnitFromJust : 0; } }
	
	// current meter

	public static bool HasValidMeter { get { return current_ != null && current_.CurrentMeter != null; } }
	public static MusicMeter CurrentMeter { get { return current_ != null ? current_.CurrentMeter : null; } }
	public static double CurrentTempo { get { return current_ != null ? current_.CurrentMeter.Tempo : 0; } }
	public static int CurrentUnitPerBar { get { return current_ != null ? current_.CurrentMeter.UnitPerBar : 0; } }
	public static int CurrentUnitPerBeat { get { return current_ != null ? current_.CurrentMeter.UnitPerBeat : 0; } }
	
	// current sequence

	public static string CurrentSequenceName { get { return current_ != null ? current_.SequenceName : ""; } }
	public static int CurrentSequenceIndex { get { return current_ != null ? current_.SequenceIndex : 0; } }
	public static int NumRepeat { get { return current_ != null ? current_.NumRepeat : 0; } }
	
	// musical time

	/// <summary>
	/// current musical time in bars
	/// </summary>
	public static float MusicalTime { get { return current_ != null ? current_.MusicalTime : 0 ; } }
	/// <summary>
	/// current musical time in units
	/// </summary>
	public static int JustTotalUnits { get { return current_ != null ? current_.JustTotalUnits : 0; } }
	/// <summary>
	/// current musical time in units
	/// </summary>
	public static int NearTotalUnits { get { return current_ != null ? current_.NearTotalUnits : 0; } }
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


	#region just / near changed predicates

	// just changed

	public static bool IsJustChangedWhen(Predicate<Timing> pred)
	{
		return current_ != null ? current_.IsJustChangedWhen(pred) : false;
	}
	public static bool IsJustChangedBar()
	{
		return current_ != null ? current_.IsJustChangedBar() : false;
	}
	public static bool IsJustChangedBeat()
	{
		return current_ != null ? current_.IsJustChangedBeat() : false;
	}
	public static bool IsJustChangedAt(int bar = 0, int beat = 0, int unit = 0)
	{
		return current_ != null ? current_.IsJustChangedAt(bar, beat, unit) : false;
	}
	public static bool IsJustChangedAt(Timing t)
	{
		return current_ != null ? current_.IsJustChangedAt(t.Bar, t.Beat, t.Unit) : false;
	}

	// near changed

	public static bool IsNearChangedWhen(Predicate<Timing> pred)
	{
		return current_ != null ? current_.IsNearChangedWhen(pred) : false;
	}
	public static bool IsNearChangedBar()
	{
		return current_ != null ? current_.IsNearChangedBar() : false;
	}
	public static bool IsNearChangedBeat()
	{
		return current_ != null ? current_.IsNearChangedBeat() : false;
	}
	public static bool IsNearChangedAt(int bar, int beat = 0, int unit = 0)
	{
		return current_ != null ? current_.IsNearChangedAt(bar, beat, unit) : false;
	}
	public static bool IsNearChangedAt(Timing t)
	{
		return current_ != null ? current_.IsNearChangedAt(t.Bar, t.Beat, t.Unit) : false;
	}

	#endregion


	#region play / stop / suspend / resume

	/// <summary>
	/// Change current music and play.
	/// </summary>
	/// <param name="musicName">name of the GameObject that include Music</param>
	public static void Play(string musicName)
	{
		MusicBase music = musicList_.Find((MusicBase m) => m != null && m.name == musicName);
		if( music != null )
		{
			music.Play();
		}
		else
		{
			Debug.Log("Can't find music: " + musicName);
		}
	}
	public static void PlayFrom(string musicName, Timing seekTiming, int sequenceIndex = 0)
	{
		MusicBase music = musicList_.Find((MusicBase m) => m != null && m.name == musicName);
		if( music != null )
		{
			music.Seek(seekTiming, sequenceIndex);
			music.Play();
		}
		else
		{
			Debug.Log("Can't find music: " + musicName);
		}
	}
	public static void Suspend() { current_?.Suspend(); }
	public static void Resume() { current_?.Resume(); }
	public static void Stop() { current_?.Stop(); }

	#endregion


	#region interactive music

	public static void SetHorizontalSequence(string name) { current_?.SetHorizontalSequence(name); }

	public static void SetHorizontalSequenceByIndex(int index) { current_?.SetHorizontalSequenceByIndex(index); }

	public static void SetVerticalMix(string name) { current_?.SetVerticalMix(name); }

	public static void SetVerticalMixByIndex(int index) { current_?.SetVerticalMixByIndex(index); }

	#endregion


	#region params

	static MusicBase current_;
	static List<MusicBase> musicList_ = new List<MusicBase>();

	public static void RegisterMusic(MusicBase music)
	{
		if( musicList_.Contains(music) == false )
		{
			musicList_.Add(music);
		}
	}

	public static void OnPlay(MusicBase music)
	{
		if( current_ != null && current_ != music && current_.IsPlaying )
		{
			current_.Stop();
		}

		current_ = music;
	}

	public static void OnFinish(MusicBase music)
	{
		if( current_ == music )
		{
			current_ = null;
		}
	}

	#endregion


	#region enum / delegate

	public enum PlayState
	{
		Invalid,
		Ready,
		Playing,
		Suspended,
		Finished
	};

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
			else
			{
				if( HasValidMeter )
				{
					switch( from )
					{
						case TimeUnitType.Bar:
							sec = time * (float)current_.CurrentMeter.SecPerBar;
							break;
						case TimeUnitType.Beat:
							sec = time * (float)current_.CurrentMeter.SecPerBeat;
							break;
						case TimeUnitType.Unit:
							sec = time * (float)current_.CurrentMeter.SecPerUnit;
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
			else
			{
				if( HasValidMeter )
				{
					switch( to )
					{
						case TimeUnitType.Bar:
							return sec / (float)current_.CurrentMeter.SecPerBar;
						case TimeUnitType.Beat:
							return sec / (float)current_.CurrentMeter.SecPerBeat;
						case TimeUnitType.Unit:
							return sec / (float)current_.CurrentMeter.SecPerUnit;
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
	static readonly float PITCH_UNIT = Mathf.Pow(2.0f, 1.0f / 12.0f);

	#endregion
}

/// <summary>
/// base class for MusicUnity / MusicADX2 / MusicWwise
/// </summary>
public abstract class MusicBase : MonoBehaviour
{
	#region properties

	// editor params
	public bool PlayOnStart;

	// state
	public Music.PlayState State { get; private set; } = Music.PlayState.Invalid;
	public bool IsPlaying { get { return State == Music.PlayState.Playing; } }
	public bool IsPlayingOrSuspended { get { return State == Music.PlayState.Playing || State == Music.PlayState.Suspended; } }

	// timing
	public Timing Just { get { return just_; } }
	public Timing Near { get { return near_; } }
	public bool IsJustChanged { get { return isJustChanged_; } }
	public bool IsNearChanged { get { return isNearChanged_; } }
	public bool IsFormerHalf { get { return isFormerHalf_; } }
	public double SecFromJust { get { return fractionFromJust_ * currentMeter_.SecPerUnit; } }
	public double UnitFromJust { get { return SecFromJust / currentMeter_.SecPerUnit; } }

	// meter
	public bool HasValidMeter { get { return currentMeter_ != null; } }
	public MusicMeter CurrentMeter { get { return currentMeter_; } }
	public double Tempo { get { return currentMeter_.Tempo; } }
	public int UnitPerBar { get { return currentMeter_.UnitPerBar; } }
	public int UnitPerBeat { get { return currentMeter_.UnitPerBeat; } }
	
	// musical time
	public float MusicalTime { get { return currentMeter_ != null ? currentMeter_.GetMusicalTime(just_, fractionFromJust_) : 0.0f; } }
	public int JustTotalUnits { get { return just_.GetTotalUnits(currentMeter_); } }
	public int NearTotalUnits { get { return near_.GetTotalUnits(currentMeter_); } }
	
	// sequence
	public abstract string SequenceName { get; }
	public abstract int SequenceIndex { get; }
	public int NumRepeat { get { return numRepeat_; } }

	#endregion


	#region predicates

	public bool IsNearChangedWhen(Predicate<Timing> pred)
	{
		if( isNearChanged_ )
		{
			if( pred(near_) ) return true;
		}
		return false;
	}
	public bool IsNearChangedBar()
	{
		return isNearChanged_ && (oldNear_.Bar != near_.Bar);
	}
	public bool IsNearChangedBeat()
	{
		return isNearChanged_ && (oldNear_.Beat != near_.Beat);
	}
	public bool IsNearChangedAt(int bar = 0, int beat = 0, int unit = 0)
	{
		return IsNearChangedAt(new Timing(bar, beat, unit));
	}
	public bool IsNearChangedAt(Timing t)
	{
		return (isNearChanged_ && (oldNear_ < t && t <= near_)) || (isNearLooped_ && (oldNear_ < t || t <= near_));
	}
	public bool IsJustChangedWhen(Predicate<Timing> pred)
	{
		if( isJustChanged_ )
		{
			if( pred(just_) ) return true;
		}
		return false;
	}
	public bool IsJustChangedBar()
	{
		return isJustChanged_ && (oldJust_.Bar != just_.Bar);
	}
	public bool IsJustChangedBeat()
	{
		return isJustChanged_ && (oldJust_.Beat != just_.Beat);
	}
	public bool IsJustChangedAt(int bar = 0, int beat = 0, int unit = 0)
	{
		return IsJustChangedAt(new Timing(bar, beat, unit));
	}
	public bool IsJustChangedAt(Timing t)
	{
		return (isJustChanged_ && (oldJust_ < t && t <= just_)) || (isJustLooped_ && (oldJust_ < t || t <= just_));
	}

	#endregion


	#region public functions

	public void Play()
	{
		if( State == Music.PlayState.Playing || State == Music.PlayState.Suspended || State == Music.PlayState.Invalid )
		{
			return;
		}

		if( PlayInternal() )
		{
			Music.OnPlay(this);
			State = Music.PlayState.Playing;
			OnHorizontalSequenceChanged();
		}
	}

	public void Seek(Timing seekTiming, int sequenceIndex = 0)
	{
		SeekInternal(seekTiming, sequenceIndex);
	}

	public void Stop()
	{
		if( State == Music.PlayState.Invalid )
		{
			return;
		}

		if( StopInternal() )
		{
			State = Music.PlayState.Finished;
			ResetParams();
			Music.OnFinish(this);
		}
	}

	public void Suspend()
	{
		if( State != Music.PlayState.Playing )
		{
			return;
		}

		if( SuspendInternal() )
		{
			State = Music.PlayState.Suspended;
		}
	}

	public void Resume()
	{
		if( State != Music.PlayState.Suspended )
		{
			return;
		}

		if( ResumeInternal() )
		{
			State = Music.PlayState.Playing;
		}
	}

	public abstract void SetHorizontalSequence(string name);

	public abstract void SetHorizontalSequenceByIndex(int index);

	public abstract void SetVerticalMix(string name);

	public abstract void SetVerticalMixByIndex(int index);

	#endregion
	

	#region protected functions

	// internal

	protected abstract bool ReadyInternal();

	protected abstract void SeekInternal(Timing seekTiming, int sequenceIndex = 0);

	protected abstract bool PlayInternal();

	protected abstract bool SuspendInternal();

	protected abstract bool ResumeInternal();

	protected abstract bool StopInternal();

	protected abstract void ResetParamsInternal();
	
	// update

	protected abstract void UpdateInternal();

	protected abstract bool CheckFinishPlaying();

	//timing

	protected abstract void UpdateTimingInternal();

	protected abstract void CalcTimingAndFraction(ref Timing just, out float fraction);

	protected abstract Timing GetSequenceEndTiming();

	#endregion


	#region params

	// 現在再生中の箇所のメーター情報。
	protected MusicMeter currentMeter_;
	// 現在のシーケンス（横の遷移の単位）の小節数。
	protected Timing sequenceEndTiming_ = null;

	// 最新のJustタイミング。(タイミングちょうどになってから切り替わる）
	private Timing just_ = new Timing(-1, 0, 0);
	// 最新のNearタイミング。（最も近いタイミングが変わった地点、つまり2つのタイミングの中間で切り替わる）
	private Timing near_ = new Timing(-1, 0, 0);
	// 1フレーム前のJustタイミング。
	private Timing oldJust_ = new Timing(-1, 0, 0);
	// 1フレーム前のNearタイミング。
	private Timing oldNear_ = new Timing(-1, 0, 0);
	// 今のフレームでjust_が変化したフラグ。
	private bool isJustChanged_ = false;
	// 今のフレームでnear_が変化したフラグ。
	private bool isNearChanged_ = false;
	// 今のフレームでjust_がループして戻ったフラグ。
	private bool isJustLooped_ = false;
	// 今のフレームでnear_がループして戻ったフラグ。
	private bool isNearLooped_ = false;
	// 今がunit内の前半かどうか。 true なら just_ == near_, false なら ++just == near。
	private bool isFormerHalf_;
	// Justのタイミングから次のタイミングまでを0～1で表した小数。
	private float fractionFromJust_;
	// 現在のループカウント。
	private int numRepeat_;

	#endregion

	
	#region functions

	void Awake()
	{
		Music.RegisterMusic(this);
		Ready();
	}

	void Start()
	{
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.pauseStateChanged += OnPlaymodeStateChanged;
		#endif
		if( PlayOnStart )
		{
			Play();
		}
	}

	#if UNITY_EDITOR
	void OnPlaymodeStateChanged(UnityEditor.PauseState state)
	{
		if( State == Music.PlayState.Playing || State == Music.PlayState.Suspended )
		{
			if( state == UnityEditor.PauseState.Paused )
			{
				Suspend();
			}
			else
			{
				Resume();
			}
		}
	}
	#endif
	
	void Update()
	{
		if( IsPlaying )
		{
			if( CheckFinishPlaying() )
			{
				Stop();
			}
			else
			{
				UpdateTiming();
				UpdateInternal();
			}
		}
	}
	
	void Ready()
	{
		if( State != Music.PlayState.Invalid )
		{
			return;
		}
		
		if( ReadyInternal() )
		{
			State = Music.PlayState.Ready;
			ResetParams();
		}
	}

	void ResetParams()
	{
		isJustChanged_ = false;
		isNearChanged_ = false;
		isJustLooped_ = false;
		isNearLooped_ = false;
		near_.Set(-1, 0, 0);
		just_.Set(-1, 0, 0);
		oldNear_.Set(near_);
		oldJust_.Set(just_);
		fractionFromJust_ = 0.0f;
		isFormerHalf_ = true;
		numRepeat_ = 0;
		sequenceEndTiming_ = null;
		currentMeter_ = null;

		ResetParamsInternal();
	}

	void UpdateTiming()
	{
		oldNear_.Set(near_);
		oldJust_.Set(just_);
		isNearChanged_ = false;
		isJustChanged_ = false;
		isJustLooped_ = false;
		isNearLooped_ = false;

		int oldSequenceIndex = SequenceIndex;
		UpdateTimingInternal();
		CalcTimingAndFraction(ref just_, out fractionFromJust_);

		if( currentMeter_ != null )
		{
			while( sequenceEndTiming_ != null && just_ >= sequenceEndTiming_ )
			{
				just_.Decrement(currentMeter_);
				fractionFromJust_ = 1.0f;
			}

			isFormerHalf_ = fractionFromJust_ < 0.5f;

			near_.Set(just_);
			if( !isFormerHalf_ )
			{
				near_.Increment(currentMeter_);
			}
			if( sequenceEndTiming_ != null && near_ >= sequenceEndTiming_ )
			{
				near_.Reset();
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
				else if( SequenceIndex != -1 )
				{
					OnRepeated();
				}
			}
		}
	}
	
	public override string ToString()
	{
		return String.Format("{0}", just_.ToString());
	}

	#endregion


	#region events
	
	protected virtual void OnRepeated()
	{
		++numRepeat_;
	}

	protected virtual void OnHorizontalSequenceChanged()
	{
		numRepeat_ = 0;
		sequenceEndTiming_ = GetSequenceEndTiming();
	}

	#endregion
	
}
