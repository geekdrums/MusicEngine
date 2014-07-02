//Copyright (c) 2014 geekdrums
//Released under the MIT license
//http://opensource.org/licenses/mit-license.php
//Feel free to use this for your lovely musical games :)

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class Music : MonoBehaviour
{
	//editor params
	public List<Section> sections;
	//indexer
	public Section this[int index]
	{
		get
		{
			if( 0 <= index && index < SectionCount_ )
			{
				return sections[index];
			}
			else
			{
				Debug.LogWarning( "Section index out of range! index = " + index + ", SectionCount = " + SectionCount_ );
				return null;
			}
		}
	}

	/// <summary>
	/// Just for music that doesn't start with the first timesample.
	/// if so, specify time samples before music goes into first timing = (0,0,0).
	/// </summary>
	public int delayTimeSamples = 0;
	/// <summary>
	/// put your debug GUIText to see current musical time & section info.
	/// </summary>
	public GUIText debugText;

	[Serializable]
	public class Section
	{
		public string name;
		/// <summary>
		/// how many MusicTime in a beat. maybe 4 or 3.
		/// </summary>
		public int mtBeat_;
		/// <summary>
		/// how many MusicTime in a bar.
		/// </summary>
		public int mtBar_;
		/// <summary>
		/// Musical Tempo. how many beats in a minutes.
		/// </summary>
		public double Tempo_;

		public Timing StartTiming_;

		public int StartTimeSamples_;
		public int StartTotalUnit_ { get; private set; }
		public bool isValidated_ { get; private set; }

		public Section( Timing startTiming, int mtBeat = 4, int mtBar = 16, double Tempo = 120 )
		{
			StartTiming_ = startTiming;
			mtBeat_ = mtBeat;
			mtBar_ = mtBar;
			Tempo_ = Tempo;
		}

		public void OnValidate( int startTimeSample, int startTotalUnit )
		{
			StartTimeSamples_ = startTimeSample;
			StartTotalUnit_ = startTotalUnit;
			isValidated_ = true;
		}

		public override string ToString()
		{
			return string.Format( "\"{0}\" startTiming:{1}, Tempo:{2}", name, StartTiming_.ToString(), Tempo_ );
		}
	}

	public class SoundCue
	{
		public SoundCue( AudioSource source ) { this.source = source; }
		public AudioSource source { get; private set; }

		public void Play()
		{
			source.Play();
		}
		public void Stop()
		{
			source.Stop();
		}
		public void Pause()
		{
			source.Pause();
		}
		public bool IsPlaying()
		{
			return source.isPlaying;
		}
		public long GetTimeSamples()
		{
			return source.timeSamples;
		}
		public float GetTime()
		{
			return source.time;
		}
		public void SetVolume( float volume )
		{
			source.volume = volume;
		}
	}

	static Music Current;
	static List<Music> MusicList = new List<Music>();
	
	#region public static properties
	/// <summary>
	/// means last timing.
	/// </summary>
	public static Timing Just { get { return Current.Just_; } }
	/// <summary>
	/// means nearest timing.
	/// </summary>
	public static Timing Now { get { return Current.Now_; } }
	/// <summary>
	/// is Just changed in this frame or not.
	/// </summary>
	public static bool isJustChanged { get { return Current.isJustChanged_; } }
	/// <summary>
	/// is Now changed in this frame or not.
	/// </summary>
	public static bool isNowChanged { get { return Current.isNowChanged_; } }
	/// <summary>
	/// is currently former half in a MusicTimeUnit, or last half.
	/// </summary>
	public static bool isFormerHalf { get { return Current.isFormerHalf_; } }
	/// <summary>
	/// delta time from JustChanged.
	/// </summary>
	public static double dtFromJust { get { return Current.dtFromJust_; } }
	/// <summary>
	/// how many times you repeat current music/block.
	/// </summary>
	public static int numRepeat { get { return Current.numRepeat_; } }
	/// <summary>
	/// returns how long from nearest Just timing with sign.
	/// </summary>
	public static double lag{ get{ return Current.lag_; } }
	/// <summary>
	/// returns how long from nearest Just timing absolutely.
	/// </summary>
	public static double lagAbs{ get{ return Current.lagAbs_; } }
	/// <summary>
	/// returns normalized lag.
	/// </summary>
	public static double lagUnit{ get{ return Current.lagUnit_; } }

	public static double MusicalTime { get { return Current.MusicalTime_; } }   //sec per MusicTimeUnit
	public static float AudioTime { get{ return Current.AudioTime_; } }		 //sec
	public static long TimeSamples { get{ return Current.TimeSamples_; } }	  //sample

	public static int mtBar { get { return Current.mtBar_; } }
	public static int mtBeat { get { return Current.mtBeat_; } }
	public static double MusicTimeUnit { get { return Current.MusicTimeUnit_; } }

	public static string CurrentMusicName { get { return Current.name; } }
	public static SoundCue CurrentSource { get { return Current.MusicSource; } }

	public static Section CurrentSection { get { return Current.CurrentSection_; } }
	public static int SectionCount { get { return Current.SectionCount_; } }
	#endregion

	#region public static predicates
	public static bool IsJustChangedWhen( System.Predicate<Timing> pred )
	{
		return Current.IsJustChangedWhen_( pred );
	}
	public static bool IsJustChangedBar()
	{
		return Current.IsJustChangedBar_();
	}
	public static bool IsJustChangedBeat()
	{
		return Current.IsJustChangedBeat_();
	}
	public static bool IsJustChangedAt( int bar = 0, int beat = 0, int unit = 0 )
	{
		return Current.IsJustChangedAt_( bar, beat, unit );
	}
	public static bool IsJustChangedAt( Timing t )
	{
		return Current.IsJustChangedAt_( t.bar, t.beat, t.unit );
	}
	public static bool IsJustChangedSection( string sectionName = "" )
	{
		Section targetSection = (sectionName == "" ? CurrentSection : GetSection( sectionName ));
		if( targetSection != null )
		{
			return IsJustChangedAt( targetSection.StartTiming_ );
		}
		else
		{
			Debug.LogWarning( "Can't find section name: " + sectionName );
			return false;
		}
	}

	public static bool IsNowChangedWhen( System.Predicate<Timing> pred )
	{
		return Current.IsNowChangedWhen_(pred);
	}
	public static bool IsNowChangedBar()
	{
		return Current.IsNowChangedBar_();
	}
	public static bool IsNowChangedBeat()
	{
		return Current.IsNowChangedBeat_();
	}
	public static bool IsNowChangedAt( int bar, int beat = 0, int unit = 0 )
	{
		return Current.IsNowChangedAt_(bar,beat,unit);
	}
	public static bool IsNowChangedAt( Timing t )
	{
		return Current.IsNowChangedAt_( t.bar, t.beat, t.unit );
	}

	public static Section GetSection( int index )
	{
		return Current[index];
	}
	public static Section GetSection( string name )
	{
		return Current.sections.Find( ( Section s ) => s.name == name );
	}
	public static float MusicalTimeFrom( Timing timing )
	{
		return (Now - timing) + (float)lagUnit;
	}
	#endregion

	#region public static functions
	/// <summary>
	/// Change Current Music.
	/// </summary>
	/// <param name="MusicName">name of the GameObject that include Music</param>
	public static void Play( string MusicName ) { MusicList.Find( ( Music m ) => m.name == MusicName ).PlayStart(); }
	/// <summary>
	/// Quantize to musical time.
	/// </summary>
	public static void QuantizePlay( AudioSource source, int transpose = 0 )
	{
		source.pitch = Mathf.Pow( pitchUnit, transpose );
		if( isFormerHalf && lagUnit < 0.3f )
		{
			source.Play();
		}
		else
		{
			Current.QuantizedCue.Add( source );
		}
	}
	public static bool IsPlaying() { return Current.MusicSource.IsPlaying(); }
	public static void Pause() { Current.MusicSource.Pause(); }
	public static void Resume() { Current.MusicSource.Play(); }
	public static void Stop() { Current.MusicSource.Stop(); }
	public static void Seek( Timing timing )
	{
		Current.MusicSource.source.timeSamples = (int)(timing.totalUnit * MusicTimeUnit * Current.SamplingRate);
	}
	public static void SeekToSection( string sectionName )
	{
		Section targetSection = GetSection( sectionName );
		if( targetSection != null )
		{
			Current.MusicSource.source.timeSamples = targetSection.StartTimeSamples_;
			Current.SectionIndex = Current.sections.IndexOf( targetSection );
			Current.OnSectionChanged();
		}
		else
		{
			Debug.LogWarning( "Can't find section name: " + sectionName );
		}
	}
	public static void SetVolume( float volume )
	{
		Current.MusicSource.SetVolume( volume );
	}
	#endregion

	#region private properties
	private Timing Now_;
	private Timing Just_;
	private bool isJustChanged_;
	private bool isNowChanged_;
	private bool isFormerHalf_;
	private double dtFromJust_;
	private int numRepeat_;
	private double MusicTimeUnit_;

	private double lag_
	{
		get
		{
			if ( isFormerHalf_ )
				return dtFromJust_;
			else
				return dtFromJust_ - MusicTimeUnit_;
		}
	}
	private double lagAbs_
	{
		get
		{
			if ( isFormerHalf_ )
				return dtFromJust_;
			else
				return MusicTimeUnit_ - dtFromJust_;
		}
	}
	private double lagUnit_ { get { return lag / MusicTimeUnit_; } }
	private double MusicalTime_ { get { return Just.totalUnit + dtFromJust / MusicTimeUnit; } }
	private float AudioTime_ { get { return MusicSource.GetTime(); } }
	private long TimeSamples_ { get { return MusicSource.GetTimeSamples(); } }
	private int SectionCount_ { get { return sections.Count; } }
	private int mtBar_ { get { return CurrentSection.mtBar_; } }
	private int mtBeat_ { get { return CurrentSection.mtBeat_; } }
	private bool isPlaying { get { return MusicSource != null && MusicSource.IsPlaying(); } }
	#endregion

	#region private predicates
	private bool IsNowChangedWhen_( System.Predicate<Timing> pred )
	{
		return isNowChanged_ && pred( Now_ );
	}
	private bool IsNowChangedBar_()
	{
		return isNowChanged_ && Now_.barUnit == 0;
	}
	private bool IsNowChangedBeat_()
	{
		return isNowChanged_ && Now_.unit == 0;
	}
	private bool IsNowChangedAt_( int bar, int beat = 0, int unit = 0 )
	{
		return isNowChanged_ &&
			Now_.bar == bar && Now_.beat == beat && Now_.unit == unit;
	}
	private bool IsJustChangedWhen_( System.Predicate<Timing> pred )
	{
		return isJustChanged_ && pred( Just_ );
	}
	private bool IsJustChangedBar_()
	{
		return isJustChanged_ && Just_.barUnit == 0;
	}
	private bool IsJustChangedBeat_()
	{
		return isJustChanged_ && Just_.unit == 0;
	}
	private bool IsJustChangedAt_( int bar = 0, int beat = 0, int unit = 0 )
	{
		return isJustChanged_ &&
			Just_.bar == bar && Just_.beat == beat && Just_.unit == unit;
	}
	#endregion

	#region private params
	//music current params
	SoundCue MusicSource;
	int SectionIndex;
	Section CurrentSection_ { get { return sections[SectionIndex]; } }
	List<AudioSource> QuantizedCue = new List<AudioSource>();

	//readonly params
	static readonly float pitchUnit = Mathf.Pow( 2.0f, 1.0f / 12.0f );
	int SamplingRate;// = 44100;
	long SamplesPerUnit;
	long SamplesPerBeat;
	long SamplesPerBar;
	long SamplesInLoop;

	//others
	Timing OldNow, OldJust;
	int NumLoopBar;
	#endregion

	#region Initialize & Update
	void Awake()
	{
#if UNITY_EDITOR
		if( !UnityEditor.EditorApplication.isPlaying )
		{
			OnValidate();
			return;
		}
#endif
		MusicList.Add( this );
		if( Current == null || audio.playOnAwake )
		{
			Current = this;
		}
		MusicSource = new SoundCue( audio );
		SamplingRate = audio.clip.frequency;
		if( audio.loop )
		{
			SamplesInLoop = audio.clip.samples;
			Section lastSection = sections[sections.Count - 1];
			long samplesPerUnit = (long)(SamplingRate * (60.0 / (lastSection.Tempo_ * lastSection.mtBeat_)));
			long samplesPerBar = samplesPerUnit * lastSection.mtBar_;
			NumLoopBar = lastSection.StartTiming_.bar +
				Mathf.RoundToInt( (float)(SamplesInLoop - lastSection.StartTimeSamples_) / (float)samplesPerBar );
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
		if( isPlaying )
		{
			UpdateTiming();
		}
	}

	void OnValidate()
	{
		if( SamplingRate == 0 )
		{
			SamplingRate = (audio != null ? audio.clip.frequency : 44100);
		}

		if( sections == null || sections.Count == 0 )
		{
			sections = new List<Section>();
			sections.Add( new Section( new Timing( 0 ), 4, 16, 120 ) );
			sections[0].OnValidate( delayTimeSamples, 0 );
		}
		else
		{
			bool isValidated = true;
			int timeSamples = delayTimeSamples;
			int totalUnit = 0;
			for( int i = 0; i < sections.Count; i++ )
			{
				if( sections[i].StartTotalUnit_ != totalUnit || sections[i].StartTimeSamples_ != timeSamples ) isValidated = false;
				if( !isValidated )
				{
					isValidated = false;
					sections[i].OnValidate( timeSamples, totalUnit );
				}
				if( i+1 < sections.Count )
				{
					int d = (sections[i+1].StartTiming_.bar  - sections[i].StartTiming_.bar)  * sections[i].mtBar_
						   +(sections[i+1].StartTiming_.beat - sections[i].StartTiming_.beat) * sections[i].mtBeat_
						   +(sections[i+1].StartTiming_.unit - sections[i].StartTiming_.unit);
					totalUnit += d;
					timeSamples += (int)((d / sections[i].mtBeat_) * (60.0f / sections[i].Tempo_) * SamplingRate);
				}
			}
		}
	}

	void Initialize()
	{
		if( Current != null && isJustChanged && Just.totalUnit == 0 )
		{
			Now_ = new Timing( 0, 0, 0 );
			isJustChanged_ = true;
		}
		else
		{
			Now_ = new Timing( 0, 0, -1 );
			isJustChanged_ = false;
		}
		Just_ = new Timing( Now_ );
		OldNow = new Timing( Now_ );
		OldJust = new Timing( Just_ );
		dtFromJust_ = 0;
		isFormerHalf_ = true;
		numRepeat_ = 0;
	}

	void OnSectionChanged()
	{
		if( sections == null || sections.Count == 0 ) return;
		if( CurrentSection_.Tempo_ > 0.0f )
		{
			SamplesPerUnit = (long)( SamplingRate * ( 60.0 / ( CurrentSection_.Tempo_ * CurrentSection_.mtBeat_ ) ) );
			SamplesPerBeat = SamplesPerUnit*CurrentSection_.mtBeat_;
			SamplesPerBar = SamplesPerUnit*CurrentSection_.mtBar_;
			MusicTimeUnit_ = (double)SamplesPerUnit / (double)SamplingRate;
		}
		else
		{
			SamplesPerUnit = int.MaxValue;
			SamplesPerBeat = int.MaxValue;
			SamplesPerBar = int.MaxValue;
			MusicTimeUnit_ = int.MaxValue;
		}
	}

	void PlayStart()
	{
		if ( Current != null && IsPlaying() )
		{
			Stop();
		}
		if( Current.debugText != null )
		{
			Current.debugText.text = "";
		}

		Current = this;
		Initialize();
		MusicSource.Play();
	}

	void UpdateTiming()
	{
		isNowChanged_ = false;
		isJustChanged_ = false;
		
		if( SectionIndex < 0 || sections.Count <= SectionIndex )
		{
			Debug.LogWarning( "Music:" + name + " has invalid SectionIndex = " + SectionIndex + ", sections.Count = " + sections.Count );
			return;
		}
		long numSamples = MusicSource.GetTimeSamples();


		int NewIndex = -1;
		for( int i = SectionIndex; i < sections.Count; i++ )
		{
			if( sections[i].StartTimeSamples_ <= numSamples && (sections.Count <= i + 1 || numSamples < sections[i + 1].StartTimeSamples_) )
			{
				NewIndex = i;
				break;
			}
		}
		if( NewIndex < 0 )
		{
			if( 0 <= numSamples && numSamples < delayTimeSamples )
			{
				NewIndex = 0;
				Initialize();
				OnSectionChanged();
			}
			else
			{
				for( int i = 0; i < SectionIndex; i++ )
				{
					if( sections[i].StartTimeSamples_ <= numSamples && numSamples < sections[i + 1].StartTimeSamples_ )
					{
						NewIndex = i;
					}
				}
			}
		}

		if( NewIndex != SectionIndex )
		{
			SectionIndex = NewIndex;
			OnSectionChanged();
		}

		numSamples -= CurrentSection_.StartTimeSamples_;
		if ( numSamples >= 0 )
		{
			Just_.bar = (int)( numSamples / SamplesPerBar ) + CurrentSection_.StartTiming_.bar;
			Just_.beat = (int)((numSamples % SamplesPerBar) / SamplesPerBeat) +CurrentSection_.StartTiming_.beat;
			Just_.unit = (int)(((numSamples % SamplesPerBar) % SamplesPerBeat) / SamplesPerUnit) +CurrentSection_.StartTiming_.unit;
			if( Just_.unit >= CurrentSection_.mtBeat_ )
			{
				Just_.beat += (int)(Just_.unit / CurrentSection_.mtBeat_);
				Just_.unit %= CurrentSection_.mtBeat_;
			}
			int barUnit = Just_.beat * CurrentSection_.mtBeat_ + Just_.unit;
			if( barUnit >= CurrentSection_.mtBar_ )
			{
				Just_.bar += (int)(barUnit / CurrentSection_.mtBar_);
				Just_.beat = 0;
				Just_.unit = (barUnit % CurrentSection_.mtBar_);
				if( Just_.unit >= CurrentSection_.mtBeat_ )
				{
					Just_.beat += (int)(Just_.unit / CurrentSection_.mtBeat_);
					Just_.unit %= CurrentSection_.mtBeat_;
				}
			}
			if ( NumLoopBar > 0 ) Just_.bar %= NumLoopBar;

			isFormerHalf_ = ( numSamples % SamplesPerUnit ) < SamplesPerUnit / 2;
			dtFromJust_ = (double)( numSamples % SamplesPerUnit ) / (double)SamplingRate;

			Now_.Copy( Just_ );
			if ( !isFormerHalf_ ) Now_.Increment();
			if ( SamplesInLoop != 0 && numSamples + SamplesPerUnit/2 >= SamplesInLoop )
			{
				Now_.Init();
			}

			isNowChanged_ = Now_.totalUnit != OldNow.totalUnit;
			isJustChanged_ = Just_.totalUnit != OldJust.totalUnit;

			CallEvents();

			OldNow.Copy( Now_ );
			OldJust.Copy( Just_ );
		}
		else
		{
			//Debug.LogWarning( "Warning!! Failed to GetNumPlayedSamples" );
		}

		DebugUpdateText();
	}

	void DebugUpdateText()
	{
		if( debugText != null )
		{
			debugText.text = "Just = " + Just_.ToString() + ", MusicalTime = " + MusicalTime_;
			if( sections.Count > 0 )
			{
				debugText.text += System.Environment.NewLine + "section[" + SectionIndex + "] = " + CurrentSection_.ToString();
			}
		}
	}
	#endregion

	#region Events
	void CallEvents()
	{
		if ( isNowChanged_ ) OnNowChanged();
		if ( isNowChanged_ && OldNow > Now_ )
		{
			WillRepeat();
		}
		if ( isJustChanged_ ) OnJustChanged();
		if ( isJustChanged_ && Just_.unit == 0 ) OnBeat();
		if ( isJustChanged_ && Just_.barUnit == 0 ) OnBar();
		if ( isJustChanged_ && OldJust > Just_ )
		{
			OnRepeated();
		}
		
		/*
		if ( isJustChanged_ && Just_.totalUnit > 0 )
		{
			Timing tempOld = new Timing( OldJust );
			tempOld.Increment();
			if ( tempOld.totalUnit != Just_.totalUnit )
			{
				//This often happens when the frame rate is slow.
				Debug.LogWarning( "Skipped some timing: OldJust = " + OldJust.ToString() + ", Just = " + Just_.ToString() );
			}
		}
		*/
	}

	//On events (when isJustChanged)
	void OnNowChanged()
	{
	}

	void OnJustChanged()
	{
		foreach ( AudioSource cue in QuantizedCue )
		{
			cue.Play();
		}
		QuantizedCue.Clear();
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

	//Will events (when isNowChanged)
	void WillRepeat()
	{
	}
	#endregion
}

[Serializable]
public class Timing : IComparable<Timing>, IEquatable<Timing>
{
	public Timing( int b = 0, int be = 0, int u = 0 )
	{
		bar = b;
		beat = be;
		unit = u;
		_cachedSectionIndex = 0;
	}

	public Timing( Timing copy )
	{
		Copy( copy );
	}
	public Timing() { this.Init(); }
	public void Init() { bar = 0; beat = 0; unit = 0; _cachedSectionIndex = 0; }
	public void Copy( Timing copy )
	{
		bar = copy.bar;
		beat = copy.beat;
		unit = copy.unit;
		_cachedSectionIndex = copy._cachedSectionIndex;
	}

	public int bar, beat, unit;

	public int totalUnit
	{
		get
		{
			return CurrentSection.StartTotalUnit_
				+ ( bar  - CurrentSection.StartTiming_.bar )  * CurrentSection.mtBar_
					+ ( beat - CurrentSection.StartTiming_.beat ) * CurrentSection.mtBeat_
					+ ( unit - CurrentSection.StartTiming_.unit );
		}
	}
	public int barUnit { get { return ( unit < 0 ? CurrentSection.mtBar_ - unit : CurrentSection.mtBeat_ * beat + unit )%CurrentSection.mtBar_; } }
	public void Increment()
	{
		unit++;
		if ( CurrentSection.mtBeat_ * beat + unit >= CurrentSection.mtBar_ )
		{
			unit = 0;
			beat = 0;
			bar += 1;
		}
		else if ( unit >= CurrentSection.mtBeat_ )
		{
			unit = 0;
			beat += 1;
		}
	}
	public void IncrementBeat()
	{
		beat++;
		if ( CurrentSection.mtBeat_ * beat + unit >= CurrentSection.mtBar_ )
		{
			beat = 0;
			bar += 1;
		}
	}

	int _cachedSectionIndex = 0;
	int sectionIndex
	{
		get
		{
			if( _cachedSectionIndex >= Music.SectionCount || 
			   this < Music.GetSection(_cachedSectionIndex).StartTiming_ || 
				( _cachedSectionIndex < Music.SectionCount-1 && Music.GetSection(_cachedSectionIndex+1).StartTiming_ <= this ) )
			{
				if( Music.GetSection(Music.SectionCount-1).StartTiming_ <= this )
				{
					_cachedSectionIndex = Music.SectionCount-1;
				}
				else
				{
					_cachedSectionIndex = 0;
					for( int i=1; i<Music.SectionCount; i++ )
					{
						if( this < Music.GetSection(i).StartTiming_ )
						{
							_cachedSectionIndex = i - 1;
							break;
						}
					}
				}
			}
			return _cachedSectionIndex;
		}
	}
	Music.Section CurrentSection{ get{ return Music.GetSection(sectionIndex); } }

	public static bool operator >( Timing t, Timing t2 ) { return t.bar > t2.bar || ( t.bar == t2.bar && t.beat > t2.beat ) || ( t.bar == t2.bar && t.beat == t2.beat && t.unit > t2.unit ) ; }
	public static bool operator <( Timing t, Timing t2 ) { return !( t > t2 ) && !( t.Equals( t2 ) ); }
	public static bool operator <=( Timing t, Timing t2 ) { return !( t > t2 ); }
	public static bool operator >=( Timing t, Timing t2 ) { return t > t2 || t.Equals( t2 ); }
	public static int operator -( Timing t, Timing t2 ) { return t.totalUnit - t2.totalUnit; }

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
		return ( this.bar == other.bar && this.beat == other.beat && this.unit == other.unit );
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