//Copyright (c) 2019 geekdrums
//Released under the MIT license
//http://opensource.org/licenses/mit-license.php
//Feel free to use this for your lovely musical games :)

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class Music : MonoBehaviour
{
	public TextMesh DebugText;

	#region static params

	static Music Current_;
	static List<Music> MusicList = new List<Music>();

	public delegate void HorizontalSequenceEvent(string name);
	static HorizontalSequenceEvent OnTransitionedEvent;
	static HorizontalSequenceEvent OnRepeatedEvent;
	static readonly float PITCH_UNIT = Mathf.Pow(2.0f, 1.0f / 12.0f);

	public enum SyncType
	{
		Immediate,
		Unit,
		Beat,
		Bar,
		Marker,
		ExitPoint,
	};

	#endregion


	#region public static properties

	public static bool IsPlaying { get { return Current_.musicSource_.IsPlaying; } }
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


	public static bool HasValidMeter { get { return Current_ != null && Current_.currentMeter_ != null; } }
	/// <summary>
	/// current musical meter
	/// </summary>
	public static MusicMeter Meter { get { return Current_.currentMeter_; } }
	public static int CurrentUnitPerBar { get { return Current_.currentMeter_.UnitPerBar; } }
	public static int CurrentUnitPerBeat { get { return Current_.currentMeter_.UnitPerBeat; } }
	public static double CurrentTempo { get { return Current_.currentMeter_.Tempo; } }

	public static string CurrentMusicName { get { return Current_.name; } }
	public static string CurrentSequenceName { get { return Current_.musicSource_.SequenceName; } }
	public static int CurrentSequenceIndex { get { return Current_.musicSource_.SequenceIndex; } }

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
		Music music = MusicList.Find((Music m) => m != null && m.name == musicName);
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
		if( Current_ != null && IsPlaying )
		{
			Stop();
		}

		Current_ = music;

		music.Play_();
	}

	public static void PlayFrom(string musicName, int sequenceIndex, Timing seekTiming)
	{
		Music music = MusicList.Find((Music m) => m != null && m.name == musicName);
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

	public static void Suspend() { Current_.musicSource_.Suspend(); }
	public static void Resume() { Current_.musicSource_.Resume(); }
	public static void Stop() { Current_.musicSource_.Stop(); }

	#endregion


	#region interactive music functions

	public void SetHorizontalSequence(string name)
	{
		Current_.musicSource_.SetHorizontalSequence(name);
	}

	public void SetHorizontalSequenceByIndex(int index)
	{
		Current_.musicSource_.SetHorizontalSequenceByIndex(index);
	}

	public void SetVerticalMix(float param)
	{
		Current_.musicSource_.SetVerticalMix(param);
	}

	public void SetVerticalMixByName(string name)
	{
		Current_.musicSource_.SetVerticalMixByName(name);
	}

	#endregion


	#region private params

	private IMusicSource musicSource_;

	// 現在再生中の箇所のメーター情報。
	private MusicMeter currentMeter_;
	// 最新のJustタイミング。(タイミングちょうどになってから切り替わる）
	private Timing just_;
	// 最新のNearタイミング。（最も近いタイミングが変わった地点、つまり2つのタイミングの中間で切り替わる）
	private Timing near_;
	// 1フレーム前のJustタイミング。
	private Timing oldJust_;
	// 1フレーム前のNearタイミング。
	private Timing oldNear_;

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

	// 現在の曲のサンプルレート。
	private int sampleRate_ = 44100;
	// 現在の再生サンプル数。
	private int currentSample_;
	// Justのタイミングから何サンプル過ぎているか。
	private int samplesFromJust_;
	// 現在のループカウント。
	private int numRepeat_;
	// 現在のシーケンス（横の遷移の単位）の小節数。
	private Timing sequenceEndTiming_ = null;
	
	#endregion


	#region private properties

	private double SecFromJust_
	{
		get
		{
			if( isFormerHalf_ )
				return samplesFromJust_ / sampleRate_;
			else
				return samplesFromJust_ / sampleRate_ - currentMeter_.SecPerUnit;
		}
	}
	private double SecFromJustAbs_ { get { return Math.Abs(SecFromJust_); } }
	private double UnitFromJust_ { get { return SecFromJust_ / currentMeter_.SecPerUnit; } }
	private double UnitFromJustAbs_ { get { return Math.Abs(UnitFromJust_); } }

	private int JustTotalUnits_ { get { return just_.GetTotalUnits(currentMeter_); } }
	private int NearTotalUnits_ { get { return near_.GetTotalUnits(currentMeter_); } }
	private float MusicalTime_ { get { return currentMeter_ != null ? currentMeter_.GetMusicalTime(just_, samplesFromJust_) : -1.0f; } }
	
	#endregion


	#region private predicates

	private bool IsNearChangedWhen_(Predicate<Timing> pred)
	{
		if( isNearChanged_ )
		{
			if( pred(near_) ) return true;
		}
		return false;
	}
	private bool IsNearChangedBar_()
	{
		return isNearChanged_ && (oldNear_.Bar != near_.Bar);
	}
	private bool IsNearChangedBeat_()
	{
		return isNearChanged_ && (oldNear_.Beat != near_.Beat);
	}
	private bool IsNearChangedAt_(int bar = 0, int beat = 0, int unit = 0)
	{
		return IsNearChangedAt_(new Timing(bar, beat, unit));
	}
	private bool IsNearChangedAt_(Timing t)
	{
		return (isNearChanged_ && (oldNear_ < t && t <= near_)) || (isNearLooped_ && (oldNear_ < t || t <= near_));
	}
	private bool IsJustChangedWhen_(Predicate<Timing> pred)
	{
		if( isJustChanged_ )
		{
			if( pred(just_) ) return true;
		}
		return false;
	}
	private bool IsJustChangedBar_()
	{
		return isJustChanged_ && (oldJust_.Bar != just_.Bar);
	}
	private bool IsJustChangedBeat_()
	{
		return isJustChanged_ && (oldJust_.Beat != just_.Beat);
	}
	private bool IsJustChangedAt_(int bar = 0, int beat = 0, int unit = 0)
	{
		return IsJustChangedAt_(new Timing(bar, beat, unit));
	}
	private bool IsJustChangedAt_(Timing t)
	{
		return (isJustChanged_ && (oldJust_ < t && t <= just_)) || (isJustLooped_ && (oldJust_ < t || t <= just_));
	}

	#endregion


	#region unity functions

	void Awake()
	{
		MusicList.Add(this);
		musicSource_ = GetComponent<IMusicSource>();
		if( Current_ == null || musicSource_.PlayOnStart )
		{
			Current_ = this;
		}
		musicSource_.Initialize();
		currentMeter_ = musicSource_.GetMeterFromSample(0);
		sampleRate_ = musicSource_.GetSampleRate();

		Initialize();
	}
	
	void Start()
	{
#if UNITY_EDITOR
		 UnityEditor.EditorApplication.pauseStateChanged += OnPlaymodeStateChanged;
#endif
		if( musicSource_.PlayOnStart )
		{
			Music.Play(this);
		}
	}
	
	void Update()
	{
		if( IsPlaying )
		{
			UpdateTiming();
		}
	}

#if UNITY_EDITOR
	void OnPlaymodeStateChanged(UnityEditor.PauseState state)
	{
		if( Current_.musicSource_ != null )
		{
			if ( state == UnityEditor.PauseState.Paused )
			{
				Current_.musicSource_.Suspend();
			}
			else
			{
				Current_.musicSource_.Resume();
			}
		}
	}
#endif

	#endregion


	#region initialize, play, stop

	void Initialize()
	{
		currentSample_ = 0;
		isJustChanged_ = false;
		isNearChanged_ = false;
		isJustLooped_ = false;
		isNearLooped_ = false;
		near_ = new Timing(-1, 0, 0);
		just_ = new Timing(-1, 0, 0);
		oldNear_ = new Timing(near_);
		oldJust_ = new Timing(just_);
		samplesFromJust_ = 0;
		isFormerHalf_ = true;
		numRepeat_ = 0;
	}

	public void Play_()
	{
		Initialize();

		musicSource_.Play();
	}

	public void PlayFrom_(int sequenceIndex, Timing seekTiming)
	{
		Seek_(sequenceIndex, seekTiming);
		Play_();
	}

	public void Stop_()
	{
		musicSource_.Stop();
	}

	public void Seek_(int sequenceIndex, Timing seekTiming)
	{
		musicSource_.Seek(sequenceIndex, seekTiming);
	}

	#endregion


	#region update functions

	void UpdateTiming()
	{
		int oldSample = currentSample_;
		oldNear_.Set(near_);
		oldJust_.Set(just_);
		isNearChanged_ = false;
		isJustChanged_ = false;
		isJustLooped_ = false;
		isNearLooped_ = false;

		int oldSequenceIndex = musicSource_.SequenceIndex;
		musicSource_.UpdateSequenceState();
		
		currentSample_ = musicSource_.GetCurrentSample();
		if( currentSample_ < 0 )
		{
			return;
		}

		currentMeter_ = musicSource_.GetMeterFromSample(currentSample_);
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
				if( oldSequenceIndex != musicSource_.SequenceIndex )
				{
					OnHorizontalSequenceChanged();
				}
				else
				{
					OnRepeated();
				}
			}
		}


		if( DebugText != null )
		{
			DebugText.text = String.Format("Just = {0}, MusicalTime = {1}", Just.ToString(), MusicalTime);
		}
	}

	#endregion


	#region Events

	void OnRepeated()
	{
		++numRepeat_;
		musicSource_.OnRepeated();
	}

	void OnHorizontalSequenceChanged()
	{
		numRepeat_ = 0;
		musicSource_.OnHorizontalSequenceChanged();
		sequenceEndTiming_ = musicSource_.GetSequenceEndTiming();
		if( OnTransitionedEvent != null )
		{
			OnTransitionedEvent(musicSource_.SequenceName);
			OnTransitionedEvent = null;
		}
	}

	#endregion
}
