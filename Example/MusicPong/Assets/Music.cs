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
	public TextMesh DebugText;
	#endregion

	#region public static properties
	public static bool IsPlaying { get { return Current_.IsPlaying_; } }
	/// <summary>
	/// last timing.
	/// </summary>
	public static Timing Just { get { return Current_.just_; } }
	/// <summary>
	/// nearest timing.
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
	/// delta time from JustChanged.
	/// </summary>
	public static double TimeSecFromJust { get { return Current_.timeSecFromJust_; } }
	/// <summary>
	/// how many times you repeat current music/block.
	/// </summary>
	public static int NumRepeat { get { return Current_.numRepeat_; } }
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
	public static double MusicTimeUnit { get { return Current_.musicTimeUnit_; } }
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
		int startIndex = Mathf.Min(index, Current_.sectionIndex_);
		int endIndex = Mathf.Max(index, Current_.sectionIndex_);
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
	public static AudioSource CurrentSource { get { return Current_.musicSource_; } }
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
			Current_.quantizedCue_.Add(source);
		}
	}
	public static void Pause() { Current_.musicSource_.Pause(); }
	public static void Resume() { Current_.musicSource_.Play(); }
	public static void Stop() { Current_.musicSource_.Stop(); }
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
		Current_.musicSource_.timeSamples = section.StartTimeSamples + (int)(deltaMT* MusicTimeUnit * Current_.samplingRate_);
	}
	public static void SeekToSection(string sectionName)
	{
		Section targetSection = GetSection(sectionName);
		if( targetSection != null )
		{
			Current_.musicSource_.timeSamples = targetSection.StartTimeSamples;
			Current_.sectionIndex_ = Current_.Sections.IndexOf(targetSection);
			Current_.OnSectionChanged();
		}
		else
		{
			Debug.LogWarning("Can't find section name: " + sectionName);
		}
	}
	public static void SetVolume(float volume)
	{
		Current_.musicSource_.volume = volume;
	}
	#endregion

	#region private params
	private Timing just_;
	private Timing near_;
	private bool isJustChanged_;
	private bool isNearChanged_;
	private bool isFormerHalf_;
	private double timeSecFromJust_;
	private int numRepeat_;
	private double musicTimeUnit_;

	private AudioSource musicSource_;
	private int sectionIndex_;
	private int currentSample_;

	private int samplingRate_;
	private int samplesPerUnit_;
	private int samplesPerBeat_;
	private int samplesPerBar_;
	private int samplesInLoop_;

	private Timing oldNear_, oldJust_;
	private int numLoopBar_;
	private List<AudioSource> quantizedCue_ = new List<AudioSource>();
	private static readonly float PITCH_UNIT = Mathf.Pow(2.0f, 1.0f / 12.0f);
	#endregion

	#region private properties
	private double Lag_
	{
		get
		{
			if( isFormerHalf_ )
				return timeSecFromJust_;
			else
				return timeSecFromJust_ - musicTimeUnit_;
		}
	}
	private double LagAbs_
	{
		get
		{
			if( isFormerHalf_ )
				return timeSecFromJust_;
			else
				return musicTimeUnit_ - timeSecFromJust_;
		}
	}
	private double LagUnit_ { get { return Lag / musicTimeUnit_; } }
	private float MusicalTime_ { get { return (float)(just_.GetMusicalTime(CurrentSection_) + timeSecFromJust_ / musicTimeUnit_); } }
	private float MusicalTimeBar_ { get { return MusicalTime_/CurrentSection_.UnitPerBar; } }
	private float AudioTimeSec_ { get { return musicSource_.time; } }
	private int TimeSamples_ { get { return musicSource_.timeSamples; } }
	private int SectionCount_ { get { return Sections.Count; } }
	private int UnitPerBar_ { get { return CurrentSection.UnitPerBar; } }
	private int UnitPerBeat_ { get { return CurrentSection.UnitPerBeat; } }
	private bool IsPlaying_ { get { return musicSource_ != null && musicSource_.isPlaying; } }
	private Section CurrentSection_ { get { return Sections[sectionIndex_]; } }
	#endregion

	#region private predicates
	private bool IsNearChangedWhen_(System.Predicate<Timing> pred)
	{
		return isNearChanged_ && pred(near_);
	}
	private bool IsNearChangedBar_()
	{
		return isNearChanged_ && near_.Beat == 0 && near_.Unit == 0;
	}
	private bool IsNearChangedBeat_()
	{
		return isNearChanged_ && near_.Unit == 0;
	}
	private bool IsNearChangedAt_(int bar, int beat = 0, int unit = 0)
	{
		return isNearChanged_ &&
			near_.Bar == bar && near_.Beat == beat && near_.Unit == unit;
	}
	private bool IsJustChangedWhen_(System.Predicate<Timing> pred)
	{
		return isJustChanged_ && pred(just_);
	}
	private bool IsJustChangedBar_()
	{
		return isJustChanged_ && just_.Beat == 0 && just_.Unit == 0;
	}
	private bool IsJustChangedBeat_()
	{
		return isJustChanged_ && just_.Unit == 0;
	}
	private bool IsJustChangedAt_(int bar = 0, int beat = 0, int unit = 0)
	{
		return isJustChanged_ &&
			just_.Bar == bar && just_.Beat == beat && just_.Unit == unit;
	}
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
		musicSource_ = GetComponent<AudioSource>();
		if( Current_ == null || musicSource_.playOnAwake )
		{
			Current_ = this;
		}
		samplingRate_ = musicSource_.clip.frequency;
		if( musicSource_.loop )
		{
			samplesInLoop_ = musicSource_.clip.samples;
			Section lastSection = Sections[Sections.Count - 1];
			double beatSec = (60.0 / lastSection.Tempo);
			int samplesPerBar = (int)(samplingRate_ * lastSection.UnitPerBar * (beatSec/lastSection.UnitPerBeat));
			numLoopBar_ = lastSection.StartTiming.Bar +
				Mathf.RoundToInt((float)(samplesInLoop_ - lastSection.StartTimeSamples) / (float)samplesPerBar);
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
		if( musicSource_ == null )
		{
			musicSource_ = GetComponent<AudioSource>();
		}
		if( samplingRate_ == 0 )
		{
			samplingRate_ = (musicSource_ != null && musicSource_.clip != null ? musicSource_.clip.frequency : 44100);
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
					timeSamples += (int)((d / Sections[i].UnitPerBeat) * (60.0f / Sections[i].Tempo) * samplingRate_);
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
		isJustChanged_ = false;
		isNearChanged_ = false;
		near_ = new Timing(0, 0, -1);
		just_ = new Timing(near_);
		oldNear_ = new Timing(near_);
		oldJust_ = new Timing(just_);
		timeSecFromJust_ = 0;
		isFormerHalf_ = true;
		numRepeat_ = 0;
	}

	void OnSectionChanged()
	{
		if( Sections == null || Sections.Count == 0 ) return;
		if( CurrentSection_.Tempo > 0.0f )
		{
			double beatSec = (60.0 / CurrentSection_.Tempo);
			samplesPerUnit_ = (int)(samplingRate_ * (beatSec/CurrentSection_.UnitPerBeat));
			samplesPerBeat_ =(int)(samplingRate_ * beatSec);
			samplesPerBar_ = (int)(samplingRate_ * CurrentSection_.UnitPerBar * (beatSec/CurrentSection_.UnitPerBeat));
			musicTimeUnit_ = (double)samplesPerUnit_ / (double)samplingRate_;
		}
		else
		{
			samplesPerUnit_ = 0;
			samplesPerBeat_ = 0;
			samplesPerBar_ = 0;
			musicTimeUnit_ = 0;
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
		musicSource_.Play();
	}

	void UpdateTiming()
	{
		// find section index
		int newIndex = sectionIndex_;
		int oldSample = currentSample_;
		currentSample_ = musicSource_.timeSamples;
		if( sectionIndex_ + 1 >= Sections.Count )
		{
			if( currentSample_ < oldSample )
			{
				newIndex = 0;
			}
		}
		else
		{
			if( Sections[sectionIndex_ + 1].StartTimeSamples <= currentSample_ )
			{
				newIndex = sectionIndex_ + 1;
			}
		}

		if( newIndex != sectionIndex_ )
		{
			sectionIndex_ = newIndex;
			OnSectionChanged();
		}

		// calc current timing
		isNearChanged_ = false;
		isJustChanged_ = false;
		int sectionSample = currentSample_ - CurrentSection_.StartTimeSamples;
		if( sectionSample >= 0 )
		{
			just_.Bar = (int)(sectionSample / samplesPerBar_) + CurrentSection_.StartTiming.Bar;
			just_.Beat = (int)((sectionSample % samplesPerBar_) / samplesPerBeat_) + CurrentSection_.StartTiming.Beat;
			just_.Unit = (int)(((sectionSample % samplesPerBar_) % samplesPerBeat_) / samplesPerUnit_) + CurrentSection_.StartTiming.Unit;
			just_.Fix(CurrentSection_);
			if( sectionIndex_ + 1 >= Sections.Count )
			{
				if( numLoopBar_ > 0 )
				{
					while( just_.Bar >= numLoopBar_ )
					{
						just_.Decrement(CurrentSection_);
					}
				}
			}
			else
			{
				while( just_ >= Sections[sectionIndex_+1].StartTiming )
				{
					just_.Decrement(CurrentSection_);
				}
			}

			just_.Subtract(CurrentSection_.StartTiming, CurrentSection_);
			timeSecFromJust_ = (double)(sectionSample - just_.Bar * samplesPerBar_ - just_.Beat * samplesPerBeat_ - just_.Unit * samplesPerUnit_) / (double)samplingRate_;
			isFormerHalf_ = (timeSecFromJust_ * samplingRate_) < samplesPerUnit_ / 2;
			just_.Add(CurrentSection_.StartTiming, CurrentSection_);

			near_.Copy(just_);
			if( !isFormerHalf_ ) near_.Increment(CurrentSection_);
			if( samplesInLoop_ != 0 && currentSample_ + samplesPerUnit_/2 >= samplesInLoop_ )
			{
				near_.Init();
			}

			isNearChanged_ = (near_.Equals(oldNear_) == false);
			isJustChanged_ = (just_.Equals(oldJust_) == false);

			CallEvents();

			oldNear_.Copy(near_);
			oldJust_.Copy(just_);
		}
		
		if( DebugText != null )
		{
			DebugText.text = "Just = " + Just.ToString() + ", MusicalTime = " + MusicalTime_;
			if( Sections.Count > 0 )
			{
				DebugText.text += System.Environment.NewLine + "section[" + sectionIndex_ + "] = " + CurrentSection_.ToString();
			}
		}
	}

	void CallEvents()
	{
		if( isJustChanged_ ) OnJustChanged();
		if( isJustChanged_ && just_.Unit == 0 ) OnBeat();
		if( isJustChanged_ && just_.Unit == 0 && just_.Beat == 0 ) OnBar();
		if( isJustChanged_ && oldJust_ > just_ )
		{
			OnRepeated();
		}
	}

	//On events (when isJustChanged)
	void OnJustChanged()
	{
		foreach( AudioSource cue in quantizedCue_ )
		{
			cue.Play();
		}
		quantizedCue_.Clear();
	}

	void OnBeat()
	{
	}

	void OnBar()
	{
	}

	void OnRepeated()
	{
		++numRepeat_;
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