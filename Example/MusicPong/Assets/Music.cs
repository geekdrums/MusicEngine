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
		public enum ClipType
		{
			None,
			Loop,
			Through,
			End,
		}
		public string Name;
		public int UnitPerBeat;
		public int UnitPerBar;
		public double Tempo;
		public int StartBar;
		// this will be automatically setted on validate.
		public int StartTimeSamples;
		// this will only work when CreateSectionClips == true.
		public ClipType LoopType;

		public Section(int startBar, int mtBeat = 4, int mtBar = 16, double tempo = 120)
		{
			StartBar = startBar;
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
			return string.Format("\"{0}\" StartBar:{1}, Tempo:{2}", Name, StartBar, Tempo);
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

	public bool CreateSectionClips;
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
				if( timing.Bar < Current_[i+1].StartBar )
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
				musicalTime += Current_[i+1].StartBar * Current_[i].UnitPerBar - currentTiming.GetMusicalTime(Current_[i]);
				currentTiming.Set(Current_[i+1].StartBar);
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
	public static int CurrentSectionIndex { get { return Current_.sectionIndex_; } }
	public static int SectionCount { get { return Current_.SectionCount_; } }
	public static string CurrentMusicName { get { return Current_.name; } }
	public static Section GetSection(int index)
	{
		return Current_[index];
	}
	public static Section GetSection(string sectionName)
	{
		return Current_.Sections.Find((Section s) => s.Name == sectionName);
	}
	/// <summary>
	/// this will only work when CreateSectionClips == true.
	/// </summary>
	public static bool IsTransitioning { get { return Current_.isTransitioning_; } }
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
			return IsJustChangedAt(targetSection.StartBar);
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
	public static void Play(string musicName, string sectionName = "") { MusicList_.Find((Music m) => m.name == musicName).PlayStart(sectionName); }
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
	public static void Stop()
	{ 
		Current_.musicSource_.Stop();
		if( Current_.isTransitioning_ )
		{
			foreach( AudioSource source in Current_.sectionSources_ )
			{
				source.Stop();
			}
			Current_.isTransitioning_ = false;
		}
	}
	public static void Seek(Timing timing)
	{
		Section section = null;
		for( int i=0; i<SectionCount; ++i )
		{
			if( i + 1 < SectionCount )
			{
				if( timing.Bar < Current_[i+1].StartBar )
				{
					section = Current_[i];
				}
			}
			else
			{
				section = Current_[i];
			}
		}
		int deltaMT = (timing.GetMusicalTime(section) - section.StartBar * section.UnitPerBar);
		Current_.musicSource_.timeSamples = section.StartTimeSamples + (int)(deltaMT* MusicTimeUnit * Current_.samplingRate_);
	}
	public static void SeekToSection(string sectionName)
	{
		Current_.SeekToSection_(sectionName);
	}
	public static void SetVolume(float volume)
	{
		Current_.musicSource_.volume = volume;
		if( Current_.CreateSectionClips )
		{
			foreach( AudioSource source in Current_.sectionSources_ )
			{
				source.volume = volume;
			}
		}
	}

	public enum SyncType
	{
		NextBeat,
		Next2Beat,
		NextBar,
		Next2Bar,
		Next4Bar,
		Next8Bar,
		SectionEnd,
	}

	public static void SetNextSection(int sectionIndex, SyncType syncType = SyncType.NextBar)
	{
		Current_.SetNextSection_(sectionIndex, syncType);
	}
	public static void SetNextSection(string name, SyncType syncType = SyncType.NextBar)
	{
		SetNextSection(Current_.Sections.FindIndex((Section s) => s.Name == name), syncType);
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
	private int numLoopBar_ = -1;
	private List<AudioSource> quantizedCue_ = new List<AudioSource>();
	private static readonly float PITCH_UNIT = Mathf.Pow(2.0f, 1.0f / 12.0f);

	private List<AudioSource> sectionSources_ = new List<AudioSource>();
	private bool isTransitioning_ = false;
	private Timing transitionTiming_ = new Timing(0);
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
	private bool IsPlaying_ { get { return (musicSource_ != null && musicSource_.isPlaying); } }
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
			numLoopBar_ = lastSection.StartBar + Mathf.RoundToInt((float)(samplesInLoop_ - lastSection.StartTimeSamples) / (float)samplesPerBar);
		}

		if( CreateSectionClips )
		{
			AudioClip[] clips = new AudioClip[Sections.Count];
			int previousSectionSample = 0;
			for( int i=0; i<Sections.Count; ++i )
			{
				int nextSectionSample = ( i + 1 < Sections.Count ? Sections[i+1].StartTimeSamples : musicSource_.clip.samples);
				clips[i] = AudioClip.Create(Sections[i].Name + "_clip", nextSectionSample - previousSectionSample, musicSource_.clip.channels, musicSource_.clip.frequency, false);
				previousSectionSample = nextSectionSample;
				float[] waveData = new float[clips[i].samples * clips[i].channels];
				musicSource_.clip.GetData(waveData, Sections[i].StartTimeSamples);
				clips[i].SetData(waveData, 0);
				AudioSource sectionSource = new GameObject("section_" + Sections[i].Name, typeof(AudioSource)).GetComponent<AudioSource>();
				sectionSource.transform.parent = this.transform;
				sectionSource.clip = clips[i];
				sectionSource.loop = Sections[i].LoopType == Section.ClipType.Loop;
				sectionSource.outputAudioMixerGroup = musicSource_.outputAudioMixerGroup;
				sectionSource.volume = musicSource_.volume;
				sectionSource.pitch = musicSource_.pitch;
				sectionSource.playOnAwake = false;
				sectionSources_.Add(sectionSource);
			}
			musicSource_.Stop();
			musicSource_.enabled = false;
			musicSource_ = sectionSources_[0];
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
		if( IsPlaying_ || ( CreateSectionClips && CheckClipChange() ) )
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
			Sections.Add(new Section(0, 4, 16, 120));
			Sections[0].OnValidate(EntryPointSample);
		}
		else
		{
			bool isValidated = true;
			int timeSamples = EntryPointSample;
			for( int i = 0; i < Sections.Count; i++ )
			{
				if( Sections[i].StartTimeSamples != timeSamples ) isValidated = false;
				if( isValidated == false )
				{
					Sections[i].OnValidate(timeSamples);
				}
				if( i+1 < Sections.Count )
				{
					if( Sections[i+1].StartBar < Sections[i].StartBar )
						Sections[i+1].StartBar = Sections[i].StartBar + 1;
					int d = (Sections[i+1].StartBar  - Sections[i].StartBar) * Sections[i].UnitPerBar;
					timeSamples += (int)((d / Sections[i].UnitPerBeat) * (60.0f / Sections[i].Tempo) * samplingRate_);
				}
			}
		}

		if( CreateSectionClips )
		{
			musicSource_.playOnAwake = false;
			for( int i = 0; i < Sections.Count; i++ )
			{
				if( Sections[i].LoopType == Section.ClipType.None )
					Sections[i].LoopType = Section.ClipType.Loop;
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
		sectionIndex_ = 0;
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
			if( CreateSectionClips )
			{
				samplesInLoop_ = musicSource_.clip.samples;
				numLoopBar_ = Mathf.RoundToInt(samplesInLoop_ / (float)samplesPerBar_);
				if( CurrentSection_.LoopType == Section.ClipType.Through )
				{
					SetNextSection_(sectionIndex_ + 1, SyncType.SectionEnd);
				}
			}
		}
		else
		{
			samplesPerUnit_ = 0;
			samplesPerBeat_ = 0;
			samplesPerBar_ = 0;
			musicTimeUnit_ = 0;
		}
	}

	void PlayStart(string sectionName = "")
	{
		if( Current_ != null && IsPlaying )
		{
			Stop();
		}

		Current_ = this;
		Initialize();
		if( sectionName != "" )
		{
			if( CreateSectionClips )
			{
				int index = Sections.FindIndex((Section s) => s.Name == sectionName);
				if( index >= 0 )
				{
					musicSource_ = sectionSources_[index];
					sectionIndex_ = index;
					OnSectionChanged();
				}
				else
				{
					Debug.LogWarning("Can't find section name: " + sectionName);
				}
			}
			else
			{
				SeekToSection_(sectionName);
			}
		}
		musicSource_.Play();
	}

	void SeekToSection_(string sectionName)
	{
		Section targetSection = GetSection(sectionName);
		if( targetSection != null )
		{
			musicSource_.timeSamples = targetSection.StartTimeSamples;
			sectionIndex_ = Sections.IndexOf(targetSection);
			OnSectionChanged();
		}
		else
		{
			Debug.LogWarning("Can't find section name: " + sectionName);
		}
	}

	void SetNextSection_(int sectionIndex, SyncType syncType = SyncType.NextBar)
	{
		if( CreateSectionClips == false || isTransitioning_ )
			return;

		if( sectionIndex < 0 || SectionCount <= sectionIndex || sectionIndex == sectionIndex_ )
			return;

		int syncUnit = 0;
		transitionTiming_.Copy(just_);
		switch( syncType )
		{
		case SyncType.NextBeat:
			syncUnit = samplesPerBeat_;
			transitionTiming_.Beat += 1;
			transitionTiming_.Unit = 0;
			break;
		case SyncType.Next2Beat:
			syncUnit = samplesPerBeat_ * 2;
			transitionTiming_.Beat += 2;
			transitionTiming_.Unit = 0;
			break;
		case SyncType.NextBar:
			syncUnit = samplesPerBar_;
			transitionTiming_.Bar += 1;
			transitionTiming_.Beat = transitionTiming_.Unit = 0;
			break;
		case SyncType.Next2Bar:
			syncUnit = samplesPerBar_ * 2;
			transitionTiming_.Bar += 2;
			transitionTiming_.Beat = transitionTiming_.Unit = 0;
			break;
		case SyncType.Next4Bar:
			syncUnit = samplesPerBar_ * 4;
			transitionTiming_.Bar += 4;
			transitionTiming_.Beat = transitionTiming_.Unit = 0;
			break;
		case SyncType.Next8Bar:
			syncUnit = samplesPerBar_ * 8;
			transitionTiming_.Bar += 8;
			transitionTiming_.Beat = transitionTiming_.Unit = 0;
			break;
		case SyncType.SectionEnd:
			syncUnit = samplesInLoop_;
			transitionTiming_.Bar = CurrentSection_.StartBar + numLoopBar_;
			transitionTiming_.Beat = transitionTiming_.Unit = 0;
			break;
		}
		transitionTiming_.Fix(CurrentSection_);
		if( CurrentSection_.LoopType == Section.ClipType.Loop && transitionTiming_.Bar >= CurrentSection_.StartBar + numLoopBar_ )
		{
			transitionTiming_.Bar -= numLoopBar_;
		}

		if( syncUnit <= 0 )
			return;

		double transitionTime = AudioSettings.dspTime + (syncUnit - musicSource_.timeSamples % syncUnit) / (double)samplingRate_ / musicSource_.pitch;
		sectionSources_[sectionIndex].PlayScheduled(transitionTime);
		sectionSources_[sectionIndex_].SetScheduledEndTime(transitionTime);
		isTransitioning_ = true;
	}

	bool CheckClipChange()
	{
		if( musicSource_.isPlaying == false )
		{
			foreach( AudioSource source in sectionSources_ )
			{
				if( source.isPlaying )
				{
					musicSource_ = source;
					isTransitioning_ = false;
					return true;
				}
			}
		}
		return false;
	}

	void UpdateTiming()
	{
		// find section index
		int newIndex = sectionIndex_;
		if( CreateSectionClips )
		{
			newIndex = sectionSources_.IndexOf(musicSource_);
			currentSample_ = musicSource_.timeSamples;
		}
		else
		{
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
		}

		if( newIndex != sectionIndex_ )
		{
			sectionIndex_ = newIndex;
			OnSectionChanged();
		}

		// calc current timing
		isNearChanged_ = false;
		isJustChanged_ = false;
		int sectionSample =  currentSample_ - (CreateSectionClips ? 0 : CurrentSection_.StartTimeSamples);
		if( sectionSample >= 0 )
		{
			just_.Bar = (int)(sectionSample / samplesPerBar_) + CurrentSection_.StartBar;
			just_.Beat = (int)((sectionSample % samplesPerBar_) / samplesPerBeat_);
			just_.Unit = (int)(((sectionSample % samplesPerBar_) % samplesPerBeat_) / samplesPerUnit_);
			just_.Fix(CurrentSection_);
			if( CreateSectionClips )
			{
				if( CurrentSection_.LoopType == Section.ClipType.Loop && numLoopBar_ > 0 )
				{
					just_.Bar -= CurrentSection_.StartBar;
					while( just_.Bar >= numLoopBar_ )
					{
						just_.Decrement(CurrentSection_);
					}
					just_.Bar += CurrentSection_.StartBar;
				}

				if( isTransitioning_ && just_.Equals(transitionTiming_) )
				{
					if( CurrentSection_.LoopType == Section.ClipType.Loop && just_.Bar == CurrentSection_.StartBar )
						just_.Bar = CurrentSection_.StartBar + numLoopBar_;
					just_.Decrement(CurrentSection_);
				}
			}
			else
			{
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
					while( just_.Bar >= Sections[sectionIndex_+1].StartBar )
					{
						just_.Decrement(CurrentSection_);
					}
				}
			}

			just_.Bar -= CurrentSection_.StartBar;
			timeSecFromJust_ = (double)(sectionSample - just_.Bar * samplesPerBar_ - just_.Beat * samplesPerBeat_ - just_.Unit * samplesPerUnit_) / (double)samplingRate_;
			isFormerHalf_ = (timeSecFromJust_ * samplingRate_) < samplesPerUnit_ / 2;
			just_.Bar += CurrentSection_.StartBar;

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
	public void Set(int bar, int beat = 0, int unit = 0)
	{
		Bar = bar;
		Beat = beat;
		Unit = unit;
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