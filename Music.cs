//Copyright (c) 2019 geekdrums
//Released under the MIT license
//http://opensource.org/licenses/mit-license.php
//Feel free to use this for your lovely musical games :)

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public static class Music
{
	#region static properties

	public static MusicBase Current { get { return Current_; } }

	public static bool IsPlaying { get { return Current_.IsPlaying; } }
	/// <summary>
	/// means last timing.
	/// </summary>
	public static Timing Just { get { return Current_.Just; } }
	/// <summary>
	/// means nearest timing.
	/// </summary>
	public static Timing Near { get { return Current_.Near; } }
	/// <summary>
	/// is Just changed in this frame or not.
	/// </summary>
	public static bool IsJustChanged { get { return Current_.IsJustChanged; } }
	/// <summary>
	/// is Near changed in this frame or not.
	/// </summary>
	public static bool IsNearChanged { get { return Current_.IsNearChanged; } }
	/// <summary>
	/// is currently former half in a MusicTimeUnit, or last half.
	/// </summary>
	public static bool IsFormerHalf { get { return Current_.IsFormerHalf; } }
	/// <summary>
	/// how many times you repeat current music/block.
	/// </summary>
	public static int NumRepeat { get { return Current_.NumRepeat; } }

	/// <summary>
	/// returns how long from nearest Just timing with sign.
	/// </summary>
	public static double SecFromJust { get { return Current_.SecFromJust; } }
	/// <summary>
	/// returns how long from nearest Just timing absolutely.
	/// </summary>
	public static double SecFromJustAbs { get { return Current_.SecFromJustAbs; } }
	/// <summary>
	/// returns normalized lag.
	/// </summary>
	public static double UnitFromJust { get { return Current_.UnitFromJust; } }
	
	public static bool HasValidMeter { get { return Current_ != null && Current_.CurrentMeter != null; } }
	public static MusicMeter CurrentMeter { get { return Current_.CurrentMeter; } }
	public static int CurrentUnitPerBar { get { return Current_.CurrentMeter.UnitPerBar; } }
	public static int CurrentUnitPerBeat { get { return Current_.CurrentMeter.UnitPerBeat; } }
	public static double CurrentTempo { get { return Current_.CurrentMeter.Tempo; } }

	public static string CurrentMusicName { get { return Current_.name; } }
	public static string CurrentSequenceName { get { return Current_.SequenceName; } }
	public static int CurrentSequenceIndex { get { return Current_.SequenceIndex; } }

	/// <summary>
	/// current musical time in units
	/// </summary>
	public static int JustTotalUnits { get { return Current_.JustTotalUnits; } }
	/// <summary>
	/// current musical time in units
	/// </summary>
	public static int NearTotalUnits { get { return Current_.NearTotalUnits; } }
	/// <summary>
	/// current musical time in bars
	/// </summary>
	public static float MusicalTime { get { return Current_.MusicalTime; } }

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


	#region static predicates

	public static bool IsJustChangedWhen(Predicate<Timing> pred)
	{
		return Current_.IsJustChangedWhen(pred);
	}
	public static bool IsJustChangedBar()
	{
		return Current_.IsJustChangedBar();
	}
	public static bool IsJustChangedBeat()
	{
		return Current_.IsJustChangedBeat();
	}
	public static bool IsJustChangedAt(int bar = 0, int beat = 0, int unit = 0)
	{
		return Current_.IsJustChangedAt(bar, beat, unit);
	}
	public static bool IsJustChangedAt(Timing t)
	{
		return Current_.IsJustChangedAt(t.Bar, t.Beat, t.Unit);
	}

	public static bool IsNearChangedWhen(Predicate<Timing> pred)
	{
		return Current_.IsNearChangedWhen(pred);
	}
	public static bool IsNearChangedBar()
	{
		return Current_.IsNearChangedBar();
	}
	public static bool IsNearChangedBeat()
	{
		return Current_.IsNearChangedBeat();
	}
	public static bool IsNearChangedAt(int bar, int beat = 0, int unit = 0)
	{
		return Current_.IsNearChangedAt(bar, beat, unit);
	}
	public static bool IsNearChangedAt(Timing t)
	{
		return Current_.IsNearChangedAt(t.Bar, t.Beat, t.Unit);
	}

	#endregion


	#region static functions

	/// <summary>
	/// Change Current Music.
	/// </summary>
	/// <param name="musicName">name of the GameObject that include Music</param>
	public static void Play(string musicName)
	{
		MusicBase music = MusicList_.Find((MusicBase m) => m != null && m.name == musicName);
		if( music != null )
		{
			Play(music);
		}
		else
		{
			Debug.Log("Can't find music: " + musicName);
		}
	}

	public static void Play(MusicBase music)
	{
		if( Current_ != null && Current_.IsPlaying )
		{
			Current_.Stop();
		}

		music.Play();
	}

	public static void PlayFrom(string musicName, int sequenceIndex, Timing seekTiming)
	{
		MusicBase music = MusicList_.Find((MusicBase m) => m != null && m.name == musicName);
		if( music != null )
		{
			music.PlayFrom(sequenceIndex, seekTiming);
		}
		else
		{
			Debug.Log("Can't find music: " + musicName);
		}
	}

	public static void PlayFrom(MusicBase music, int sequenceIndex, Timing seekTiming)
	{
		music.PlayFrom(sequenceIndex, seekTiming);
	}

	public static void Suspend() { Current_.Suspend(); }
	public static void Resume() { Current_.Resume(); }
	public static void Stop() { Current_.Stop(); }

	#endregion


	#region static params

	static MusicBase Current_;
	static List<MusicBase> MusicList_ = new List<MusicBase>();

	public static void OnPlay(MusicBase music)
	{
		Current_ = music;
	}

	public static void OnFinish(MusicBase music)
	{
		if( Current_ == music )
		{
			Current_ = null;
		}
	}

	public static void RegisterMusic(MusicBase music)
	{
		if( MusicList_.Contains(music) == false )
		{
			MusicList_.Add(music);
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
				sec = (float)time / Current_.SampleRate;
			}
			else
			{
				if( HasValidMeter )
				{
					switch( from )
					{
						case TimeUnitType.Bar:
							sec = time * (float)Current_.CurrentMeter.SecPerBar;
							break;
						case TimeUnitType.Beat:
							sec = time * (float)Current_.CurrentMeter.SecPerBeat;
							break;
						case TimeUnitType.Unit:
							sec = time * (float)Current_.CurrentMeter.SecPerUnit;
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
				return sec * Current_.SampleRate;
			}
			else
			{
				if( HasValidMeter )
				{
					switch( to )
					{
						case TimeUnitType.Bar:
							return sec / (float)Current_.CurrentMeter.SecPerBar;
						case TimeUnitType.Beat:
							return sec / (float)Current_.CurrentMeter.SecPerBeat;
						case TimeUnitType.Unit:
							return sec / (float)Current_.CurrentMeter.SecPerUnit;
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

public abstract class MusicBase : MonoBehaviour
{
	#region properties

	// editor params
	public bool PlayOnStart;

	// state
	public Music.PlayState State { get; private set; } = Music.PlayState.Invalid;
	public bool IsPlaying { get { return State == Music.PlayState.Playing; } }

	// timing
	public Timing Just { get { return just_; } }
	public Timing Near { get { return near_; } }
	public bool IsJustChanged { get { return isJustChanged_; } }
	public bool IsNearChanged { get { return isNearChanged_; } }
	public bool IsFormerHalf { get { return isFormerHalf_; } }

	// meter
	public bool HasValidMeter { get { return currentMeter_ != null; } }
	public MusicMeter CurrentMeter { get { return currentMeter_; } }
	public int UnitPerBar { get { return currentMeter_.UnitPerBar; } }
	public int UnitPerBeat { get { return currentMeter_.UnitPerBeat; } }
	public double Tempo { get { return currentMeter_.Tempo; } }
	public int SampleRate { get { return sampleRate_; } }

	// from just
	public double SecFromJust
	{
		get
		{
			if( isFormerHalf_ )
				return samplesFromJust_ / sampleRate_;
			else
				return samplesFromJust_ / sampleRate_ - currentMeter_.SecPerUnit;
		}
	}
	public double SecFromJustAbs { get { return Math.Abs(SecFromJust); } }
	public double UnitFromJust { get { return SecFromJust / currentMeter_.SecPerUnit; } }
	public double UnitFromJustAbs { get { return Math.Abs(UnitFromJust); } }

	// total time / units
	public float MusicalTime { get { return currentMeter_ != null ? currentMeter_.GetMusicalTime(just_, samplesFromJust_) : -1.0f; } }
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

	public void PlayFrom(int sequenceIndex, Timing seekTiming)
	{
		SeekInternal(sequenceIndex, seekTiming);
		Play();
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

	protected abstract void SeekInternal(int sequenceIndex, Timing seekTiming);

	protected abstract bool PlayInternal();

	protected abstract bool SuspendInternal();

	protected abstract bool ResumeInternal();

	protected abstract bool StopInternal();

	protected abstract void ResetParamsInternal();
	
	// update

	protected abstract void UpdateInternal();

	protected abstract bool CheckFinishPlaying();

	protected abstract void UpdateHorizontalState();

	protected abstract void UpdateVerticalState();
	
	//timing

	protected abstract int GetCurrentSample();

	protected abstract int GetSampleRate();

	protected abstract MusicMeter GetMeterFromSample(int currentSample);

	protected abstract Timing GetSequenceEndTiming();

	#endregion


	#region params

	// 現在の曲のサンプルレート。
	protected int sampleRate_ = 44100;
	// 現在の再生サンプル数。
	protected int currentSample_;

	// 現在再生中の箇所のメーター情報。
	private MusicMeter currentMeter_;
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
	// Justのタイミングから何サンプル過ぎているか。
	private int samplesFromJust_;
	// 現在のループカウント。
	private int numRepeat_;
	// 現在のシーケンス（横の遷移の単位）の小節数。
	private Timing sequenceEndTiming_ = null;

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
		if( IsPlaying )
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
			sampleRate_ = GetSampleRate();
			currentMeter_ = GetMeterFromSample(0);
			ResetParams();
		}
	}

	void ResetParams()
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
		sequenceEndTiming_ = null;

		ResetParamsInternal();
	}

	void UpdateTiming()
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

			while( sequenceEndTiming_ != null && just_ >= sequenceEndTiming_ )
			{
				just_.Decrement(currentMeter_);
			}

			samplesFromJust_ = currentSample_ - currentMeter_.GetSampleFromTiming(just_);
			isFormerHalf_ = samplesFromJust_ < currentMeter_.SamplesPerUnit / 2;

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
				else
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
