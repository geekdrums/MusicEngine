using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

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
	public bool IsJustLooped()
	{
		return isJustLooped_;
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
	// 以前のシーケンスインデックス
	private int oldSequenceIndex_ = 0;
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
			isJustLooped_ = isJustChanged_ && (just_ < oldJust_ || (oldJust_.Bar < 0 && just_.Bar >= 0));
			isNearLooped_ = isNearChanged_ && (near_ < oldNear_ || (oldNear_.Bar < 0 && near_.Bar >= 0));

			if( isJustLooped_ )
			{
				if( oldSequenceIndex_ != SequenceIndex )
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
		oldSequenceIndex_ = SequenceIndex;
		numRepeat_ = 0;
		sequenceEndTiming_ = GetSequenceEndTiming();
	}

	#endregion
	
}
