//Copyright (c) 2014 geekdrums
//Released under the MIT license
//http://opensource.org/licenses/mit-license.php
//Feel free to use this for your lovely musical games :)

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(AudioSource))]
public class Music : MonoBehaviour
{
	#region Section class
	[Serializable]
	public class Section
	{
		public string Name;
		public int UnitPerBeat;
		public int UnitPerBar;
		public double Tempo;
		public Timing StartTiming;
		public int StartTimeSamples;

		public Section(Timing startTiming, int mtBeat = 4, int mtBar = 16, double tempo = 120)
		{
			StartTiming = startTiming;
			UnitPerBeat = mtBeat;
			UnitPerBar = mtBar;
			Tempo = tempo;
		}

		public void OnValidate(int startTimeSample)
		{
			StartTimeSamples = startTimeSample;
		}

		public override string ToString()
		{
			return string.Format("\"{0}\" startTiming:{1}, Tempo:{2}", Name, StartTiming.ToString(), Tempo);
		}
	}

	public Section this[int index]
	{
		get
		{
			if( 0 <= index && index < SectionCount_ )
			{
				return Sections[index];
			}
			else
			{
				Debug.LogWarning("Section index out of range! index = " + index + ", SectionCount = " + SectionCount_);
				return null;
			}
		}
	}
	#endregion

	static Music Current_;
	static List<Music> MusicList_ = new List<Music>();

	#region editor params
	public List<Section> Sections;

	/// <summary>
	/// Just for music that doesn't start with the first timesample.
	/// if so, specify time samples before music goes into first timing = (0,0,0).
	/// </summary>
	public int EntryPointSample = 0;

	/// <summary>
	/// put your debug GUIText to see current musical time & section info.
	/// </summary>
	//public GUIText DebugText;
	#endregion

	#region public static properties
	public static bool IsPlaying { get { return Current_.IsPlaying_; } }
	/// <summary>
	/// last timing.
	/// </summary>
	public static Timing Just { get { return Current_.Just_; } }
	/// <summary>
	/// nearest timing.
	/// </summary>
	public static Timing Near { get { return Current_.Near_; } }
	/// <summary>
	/// is Just changed in this frame or not.
	/// </summary>
	public static bool IsJustChanged { get { return Current_.IsJustChanged_; } }
	/// <summary>
	/// is Near changed in this frame or not.
	/// </summary>
	public static bool IsNearChanged { get { return Current_.IsNearChanged_; } }
	/// <summary>
	/// is currently former half in a MusicTimeUnit, or last half.
	/// </summary>
	public static bool IsFormerHalf { get { return Current_.IsFormerHalf_; } }
	/// <summary>
	/// delta time from JustChanged.
	/// </summary>
	public static double TimeSecFromJust { get { return Current_.TimeSecFromJust_; } }
	/// <summary>
	/// how many times you repeat current music/block.
	/// </summary>
	public static int NumRepeat { get { return Current_.NumRepeat_; } }
	/// <summary>
	/// returns how long from nearest Just timing with sign.
	/// </summary>
	public static double Lag { get { return Current_.Lag_; } }
	/// <summary>
	/// returns how long from nearest Just timing absolutely.
	/// </summary>
	public static double LagAbs { get { return Current_.LagAbs_; } }
	/// <summary>
	/// returns normalized lag.
	/// </summary>
	public static double LagUnit { get { return Current_.LagUnit_; } }
	/// <summary>
	/// sec / musicalUnit
	/// </summary>
	public static double MusicTimeUnit { get { return Current_.MusicTimeUnit_; } }
	/// <summary>
	/// current musical time based on MusicalTimeUnit
	/// **warning** if CurrentSection.UnitPerBar changed, this is not continuous.
	/// </summary>
	public static float MusicalTime { get { return Current_.MusicalTime_; } }
	/// <summary>
	/// current musical time based on MusicalBar.
	/// This is always continuous(MusicalTime is not).
	/// </summary>
	public static float MusicalTimeBar { get { return Current_.MusicalTimeBar_; } }
	/// <summary>
	/// dif from timing to Just on musical time unit.
	/// </summary>
	/// <param name="timing"></param>
	/// <returns></returns>
	public static float MusicalTimeFrom(Timing timing)
	{
		Section section = null;
		int index = 0;
		for( int i=0; i<SectionCount; ++i )
		{
			if( i + 1 < SectionCount )
			{
				if( timing < Current_[i+1].StartTiming )
				{
					index = i;
					break;
				}
			}
			else
			{
				index = i;
			}
		}
		section = Current_[index];
		int startIndex = Mathf.Min(index, Current_.SectionIndex_);
		int endIndex = Mathf.Max(index, Current_.SectionIndex_);
		Timing currentTiming = new Timing(timing < Just ? timing : Just);
		Timing endTiming = (timing > Just ? timing : Just);
		int musicalTime = 0;
		for( int i=startIndex; i<=endIndex; ++i )
		{
			if( i < endIndex )
			{
				musicalTime += Current_[i+1].StartTiming.GetMusicalTime(Current_[i]) - currentTiming.GetMusicalTime(Current_[i]);
				currentTiming.Copy(Current_[i+1].StartTiming);
			}
			else
			{
				musicalTime += endTiming.GetMusicalTime(Current_[i]) - currentTiming.GetMusicalTime(Current_[i]);
			}
		}
		return (float)((timing > Just ? -1 : 1) * musicalTime + TimeSecFromJust / MusicTimeUnit);
	}
	/// <summary>
	/// current audio play time in sec.
	/// </summary>
	public static float AudioTimeSec { get { return Current_.AudioTimeSec_; } }
	/// <summary>
	/// current audio play sample
	/// </summary>
	public static int TimeSamples { get { return Current_.TimeSamples_; } }
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
		return Mathf.Lerp(min, max, ((float)Math.Cos(Math.PI * 2 * (MusicalTime + offset) / cycle) + 1.0f)/2.0f);
	}

	public static int UnitPerBar { get { return Current_.UnitPerBar_; } }
	public static int UnitPerBeat { get { return Current_.UnitPerBeat_; } }
	public static AudioSource CurrentSource { get { return Current_.MusicSource_; } }
	public static Section CurrentSection { get { return Current_.CurrentSection_; } }
	public static int SectionCount { get { return Current_.SectionCount_; } }
	public static string CurrentMusicName { get { return Current_.name; } }
	public static Section GetSection(int index)
	{
		return Current_[index];
	}
	public static Section GetSection(string name)
	{
		return Current_.Sections.Find((Section s) => s.Name == name);
	}
	#endregion

	#region public static predicates
	public static bool IsJustChangedWhen(System.Predicate<Timing> pred)
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
	public static bool IsJustChangedSection(string sectionName = "")
	{
		Section targetSection = (sectionName == "" ? CurrentSection : GetSection(sectionName));
		if( targetSection != null )
		{
			return IsJustChangedAt(targetSection.StartTiming);
		}
		else
		{
			Debug.LogWarning("Can't find section name: " + sectionName);
			return false;
		}
	}

	public static bool IsNearChangedWhen(System.Predicate<Timing> pred)
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
	/// <param name="MusicName">name of the GameObject that include Music</param>
	public static void Play(string MusicName) { MusicList_.Find((Music m) => m.name == MusicName).PlayStart(); }
	/// <summary>
	/// Quantize to musical time.
	/// </summary>
	public static void QuantizePlay(AudioSource source, int transpose = 0, float allowRange = 0.3f)
	{
		source.pitch = Mathf.Pow(PITCH_UNIT, transpose);
		if( IsFormerHalf && LagUnit < allowRange )
		{
			source.Play();
		}
		else
		{
			Current_.QuantizedCue_.Add(source);
		}
	}
	public static void Pause() { Current_.MusicSource_.Pause(); }
	public static void Resume() { Current_.MusicSource_.Play(); }
	public static void Stop() { Current_.MusicSource_.Stop(); }
	public static void Seek(Timing timing)
	{
		Section section = null;
		for( int i=0; i<SectionCount; ++i )
		{
			if( i + 1 < SectionCount )
			{
				if( timing < Current_[i+1].StartTiming )
				{
					section = Current_[i];
				}
			}
			else
			{
				section = Current_[i];
			}
		}
		int deltaMT = (timing.GetMusicalTime(section) - section.StartTiming.GetMusicalTime(section));
		Current_.MusicSource_.timeSamples = section.StartTimeSamples + (int)(deltaMT* MusicTimeUnit * Current_.SamplingRate_);
	}
	public static void SeekToSection(string sectionName)
	{
		Section targetSection = GetSection(sectionName);
		if( targetSection != null )
		{
			Current_.MusicSource_.timeSamples = targetSection.StartTimeSamples;
			Current_.SectionIndex_ = Current_.Sections.IndexOf(targetSection);
			Current_.OnSectionChanged();
		}
		else
		{
			Debug.LogWarning("Can't find section name: " + sectionName);
		}
	}
	public static void SetVolume(float volume)
	{
		Current_.MusicSource_.volume = volume;
	}
	#endregion

	#region private properties
	private Timing Near_;
	private Timing Just_;
	private bool IsJustChanged_;
	private bool IsNearChanged_;
	private bool IsFormerHalf_;
	private double TimeSecFromJust_;
	private int NumRepeat_;
	private double MusicTimeUnit_;

	private double Lag_
	{
		get
		{
			if( IsFormerHalf_ )
				return TimeSecFromJust_;
			else
				return TimeSecFromJust_ - MusicTimeUnit_;
		}
	}
	private double LagAbs_
	{
		get
		{
			if( IsFormerHalf_ )
				return TimeSecFromJust_;
			else
				return MusicTimeUnit_ - TimeSecFromJust_;
		}
	}
	private double LagUnit_ { get { return Lag / MusicTimeUnit_; } }
	private float MusicalTime_ { get { return (float)(Just_.GetMusicalTime(CurrentSection_) + TimeSecFromJust_ / MusicTimeUnit_); } }
	private float MusicalTimeBar_ { get { return MusicalTime_/CurrentSection_.UnitPerBar; } }
	private float AudioTimeSec_ { get { return MusicSource_.time; } }
	private int TimeSamples_ { get { return MusicSource_.timeSamples; } }
	private int SectionCount_ { get { return Sections.Count; } }
	private int UnitPerBar_ { get { return CurrentSection.UnitPerBar; } }
	private int UnitPerBeat_ { get { return CurrentSection.UnitPerBeat; } }
	private bool IsPlaying_ { get { return MusicSource_ != null && MusicSource_.isPlaying; } }
	private Section CurrentSection_ { get { return Sections[SectionIndex_]; } }
	#endregion

	#region private predicates
	private bool IsNearChangedWhen_(System.Predicate<Timing> pred)
	{
		return IsNearChanged_ && pred(Near_);
	}
	private bool IsNearChangedBar_()
	{
		return IsNearChanged_ && Near_.Beat == 0 && Near_.Unit == 0;
	}
	private bool IsNearChangedBeat_()
	{
		return IsNearChanged_ && Near_.Unit == 0;
	}
	private bool IsNearChangedAt_(int bar, int beat = 0, int unit = 0)
	{
		return IsNearChanged_ &&
			Near_.Bar == bar && Near_.Beat == beat && Near_.Unit == unit;
	}
	private bool IsJustChangedWhen_(System.Predicate<Timing> pred)
	{
		return IsJustChanged_ && pred(Just_);
	}
	private bool IsJustChangedBar_()
	{
		return IsJustChanged_ && Just_.Beat == 0 && Just_.Unit == 0;
	}
	private bool IsJustChangedBeat_()
	{
		return IsJustChanged_ && Just_.Unit == 0;
	}
	private bool IsJustChangedAt_(int bar = 0, int beat = 0, int unit = 0)
	{
		return IsJustChanged_ &&
			Just_.Bar == bar && Just_.Beat == beat && Just_.Unit == unit;
	}
	#endregion

	#region private params
	AudioSource MusicSource_;
	int SectionIndex_;
	int CurrentSample_;

	int SamplingRate_;
	int SamplesPerUnit_;
	int SamplesPerBeat_;
	int SamplesPerBar_;
	int SamplesInLoop_;

	Timing OldNear_, OldJust_;
	int NumLoopBar_;
	List<AudioSource> QuantizedCue_ = new List<AudioSource>();
	static readonly float PITCH_UNIT = Mathf.Pow(2.0f, 1.0f / 12.0f);
	#endregion

	#region private functions
	void Awake()
	{
#if UNITY_EDITOR
		if( !UnityEditor.EditorApplication.isPlaying )
		{
			OnValidate();
			return;
		}
#endif
		MusicList_.Add(this);
		MusicSource_ = GetComponent<AudioSource>();
		if( Current_ == null || MusicSource_.playOnAwake )
		{
			Current_ = this;
		}
		SamplingRate_ = MusicSource_.clip.frequency;
		if( MusicSource_.loop )
		{
			SamplesInLoop_ = MusicSource_.clip.samples;
			Section lastSection = Sections[Sections.Count - 1];
			double beatSec = (60.0 / lastSection.Tempo);
			int samplesPerBar = (int)(SamplingRate_ * lastSection.UnitPerBar * (beatSec/lastSection.UnitPerBeat));
			NumLoopBar_ = lastSection.StartTiming.Bar +
				Mathf.RoundToInt((float)(SamplesInLoop_ - lastSection.StartTimeSamples) / (float)samplesPerBar);
		}

		Initialize();

		OnSectionChanged();
	}

	// Use this for initialization
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
		if( IsPlaying_ )
		{
			UpdateTiming();
		}
	}

	void OnValidate()
	{
		if( MusicSource_ == null )
		{
			MusicSource_ = GetComponent<AudioSource>();
		}
		if( SamplingRate_ == 0 )
		{
			SamplingRate_ = (MusicSource_ != null ? MusicSource_.clip.frequency : 44100);
		}

		if( Sections == null || Sections.Count == 0 )
		{
			Sections = new List<Section>();
			Sections.Add(new Section(new Timing(0), 4, 16, 120));
			Sections[0].OnValidate(EntryPointSample);
		}
		else
		{
			bool isValidated = true;
			int timeSamples = EntryPointSample;
			for( int i = 0; i < Sections.Count; i++ )
			{
				if( Sections[i].StartTimeSamples != timeSamples ) isValidated = false;
				if( !isValidated )
				{
					isValidated = false;
					Sections[i].OnValidate(timeSamples);
				}
				if( i+1 < Sections.Count )
				{
					int d = (Sections[i+1].StartTiming.Bar  - Sections[i].StartTiming.Bar)  * Sections[i].UnitPerBar
						   +(Sections[i+1].StartTiming.Beat - Sections[i].StartTiming.Beat) * Sections[i].UnitPerBeat
						   +(Sections[i+1].StartTiming.Unit - Sections[i].StartTiming.Unit);
					timeSamples += (int)((d / Sections[i].UnitPerBeat) * (60.0f / Sections[i].Tempo) * SamplingRate_);
				}
			}
		}
	}

	void OnDestroy()
	{
		MusicList_.Remove(this);
	}

	void Initialize()
	{
		IsJustChanged_ = false;
		IsNearChanged_ = false;
		Near_ = new Timing(0, 0, -1);
		Just_ = new Timing(Near_);
		OldNear_ = new Timing(Near_);
		OldJust_ = new Timing(Just_);
		TimeSecFromJust_ = 0;
		IsFormerHalf_ = true;
		NumRepeat_ = 0;
	}

	void OnSectionChanged()
	{
		if( Sections == null || Sections.Count == 0 ) return;
		if( CurrentSection_.Tempo > 0.0f )
		{
			double beatSec = (60.0 / CurrentSection_.Tempo);
			SamplesPerUnit_ = (int)(SamplingRate_ * (beatSec/CurrentSection_.UnitPerBeat));
			SamplesPerBeat_ =(int)(SamplingRate_ * beatSec);
			SamplesPerBar_ = (int)(SamplingRate_ * CurrentSection_.UnitPerBar * (beatSec/CurrentSection_.UnitPerBeat));
			MusicTimeUnit_ = (double)SamplesPerUnit_ / (double)SamplingRate_;
		}
		else
		{
			SamplesPerUnit_ = 0;
			SamplesPerBeat_ = 0;
			SamplesPerBar_ = 0;
			MusicTimeUnit_ = 0;
		}
	}

	void PlayStart()
	{
		if( Current_ != null && IsPlaying )
		{
			Stop();
		}

		Current_ = this;
		Initialize();
		MusicSource_.Play();
	}

	void UpdateTiming()
	{
		// find section index
		int newIndex = SectionIndex_;
		int oldSample = CurrentSample_;
		CurrentSample_ = MusicSource_.timeSamples;
		if( SectionIndex_ + 1 >= Sections.Count )
		{
			if( CurrentSample_ < oldSample )
			{
				newIndex = 0;
			}
		}
		else
		{
			if( Sections[SectionIndex_ + 1].StartTimeSamples <= CurrentSample_ )
			{
				newIndex = SectionIndex_ + 1;
			}
		}

		if( newIndex != SectionIndex_ )
		{
			SectionIndex_ = newIndex;
			OnSectionChanged();
		}

		// calc current timing
		IsNearChanged_ = false;
		IsJustChanged_ = false;
		int sectionSample = CurrentSample_ - CurrentSection_.StartTimeSamples;
		if( sectionSample >= 0 )
		{
			Just_.Bar = (int)(sectionSample / SamplesPerBar_) + CurrentSection_.StartTiming.Bar;
			Just_.Beat = (int)((sectionSample % SamplesPerBar_) / SamplesPerBeat_) + CurrentSection_.StartTiming.Beat;
			Just_.Unit = (int)(((sectionSample % SamplesPerBar_) % SamplesPerBeat_) / SamplesPerUnit_) + CurrentSection_.StartTiming.Unit;
			Just_.Fix(CurrentSection_);
			if( SectionIndex_ + 1 >= Sections.Count )
			{
				if( NumLoopBar_ > 0 )
				{
					while( Just_.Bar >= NumLoopBar_ )
					{
						Just_.Decrement(CurrentSection_);
					}
				}
			}
			else
			{
				while( Just_ >= Sections[SectionIndex_+1].StartTiming )
				{
					Just_.Decrement(CurrentSection_);
				}
			}

			Just_.Subtract(CurrentSection_.StartTiming, CurrentSection_);
			TimeSecFromJust_ = (double)(sectionSample - Just_.Bar * SamplesPerBar_ - Just_.Beat * SamplesPerBeat_ - Just_.Unit * SamplesPerUnit_) / (double)SamplingRate_;
			IsFormerHalf_ = (TimeSecFromJust_ * SamplingRate_) < SamplesPerUnit_ / 2;
			Just_.Add(CurrentSection_.StartTiming, CurrentSection_);

			Near_.Copy(Just_);
			if( !IsFormerHalf_ ) Near_.Increment(CurrentSection_);
			if( SamplesInLoop_ != 0 && CurrentSample_ + SamplesPerUnit_/2 >= SamplesInLoop_ )
			{
				Near_.Init();
			}

			IsNearChanged_ = (Near_.Equals(OldNear_) == false);
			IsJustChanged_ = (Just_.Equals(OldJust_) == false);

			CallEvents();

			OldNear_.Copy(Near_);
			OldJust_.Copy(Just_);
		}

		//DebugUpdateText();
	}

	/* DebugUpdateText
	void DebugUpdateText()
	{
		if( DebugText != null )
		{
			DebugText.text = "Just_ = " + Just_.ToString() + ", MusicalTime = " + MusicalTime_;
			if( Sections.Count > 0 )
			{
				DebugText.text += System.Environment.NewLine + "section[" + SectionIndex_ + "] = " + CurrentSection_.ToString();
			}
		}
	}
	*/

	void CallEvents()
	{
		if( IsJustChanged_ ) OnJustChanged();
		if( IsJustChanged_ && Just_.Unit == 0 ) OnBeat();
		if( IsJustChanged_ && Just_.Unit == 0 && Just_.Beat == 0 ) OnBar();
		if( IsJustChanged_ && OldJust_ > Just_ )
		{
			OnRepeated();
		}
	}

	//On events (when isJustChanged)
	void OnJustChanged()
	{
		foreach( AudioSource cue in QuantizedCue_ )
		{
			cue.Play();
		}
		QuantizedCue_.Clear();
	}

	void OnBeat()
	{
	}

	void OnBar()
	{
	}

	void OnRepeated()
	{
		++NumRepeat_;
	}
	#endregion
}

[Serializable]
public class Timing : IComparable<Timing>, IEquatable<Timing>
{
	public Timing(int bar = 0, int beat = 0, int unit = 0)
	{
		Bar = bar;
		Beat = beat;
		Unit = unit;
	}

	public Timing(Timing copy)
	{
		Copy(copy);
	}
	public Timing() { this.Init(); }
	public void Init() { Bar = 0; Beat = 0; Unit = 0; }
	public void Copy(Timing copy)
	{
		Bar = copy.Bar;
		Beat = copy.Beat;
		Unit = copy.Unit;
	}

	public int Bar, Beat, Unit;

	public int CurrentMusicalTime { get { return GetMusicalTime(Music.CurrentSection); } }
	public int GetMusicalTime(Music.Section section)
	{
		return Bar * section.UnitPerBar + Beat * section.UnitPerBeat + Unit;
	}
	public void Fix(Music.Section section)
	{
		int totalUnit = Bar * section.UnitPerBar + Beat * section.UnitPerBeat + Unit;
		Bar = totalUnit / section.UnitPerBar;
		Beat = (totalUnit - Bar*section.UnitPerBar) / section.UnitPerBeat;
		Unit = (totalUnit - Bar*section.UnitPerBar - Beat * section.UnitPerBeat);
	}
	public void Increment(Music.Section section)
	{
		Unit++;
		Fix(section);
	}
	public void Decrement(Music.Section section)
	{
		Unit--;
		Fix(section);
	}
	public void IncrementBeat(Music.Section section)
	{
		Beat++;
		Fix(section);
	}
	public void Add(Timing t, Music.Section section)
	{
		Bar += t.Bar;
		Beat += t.Beat;
		Unit += t.Unit;
		Fix(section);
	}
	public void Subtract(Timing t, Music.Section section)
	{
		Bar -= t.Bar;
		Beat -= t.Beat;
		Unit -= t.Unit;
		Fix(section);
	}

	public static bool operator >(Timing t, Timing t2) { return t.Bar > t2.Bar || (t.Bar == t2.Bar && t.Beat > t2.Beat) || (t.Bar == t2.Bar && t.Beat == t2.Beat && t.Unit > t2.Unit); }
	public static bool operator <(Timing t, Timing t2) { return !(t > t2) && !(t.Equals(t2)); }
	public static bool operator <=(Timing t, Timing t2) { return !(t > t2); }
	public static bool operator >=(Timing t, Timing t2) { return t > t2 || t.Equals(t2); }

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