//#define ADX
/* if you use ADX, be sure to
add this code in Plugins/CriWare/CriWare/CriAtomSource.cs
public CriAtomExPlayer Player
{
	get { return this.player; }
}
http://www53.atwiki.jp/soundtasukeai/pages/22.html
*/
//#define WARN_IF_TIMING_SKIPPED

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// You can get musical timing from Music.SomethingYouWant.
/// Attach this as a component to a GameObject that include Music.
/// Comment out "#define ADX" line when you don't use ADX2LE.
/// </summary>
public class Music : MonoBehaviour {
	
	public class SoundCue
	{
#if ADX
		public SoundCue( CriAtomSource source ) { this.source = source; }
		public CriAtomSource source { get; private set; }
		/// <summary>
		/// If you have an error in this property, maybe you are missing the comment
		/// just under "#define ADX" (line 2) in this code.
		/// Please make sure you fixed some code of CriAtomSource.cs
		/// </summary>
		public CriAtomExPlayer Player { get { return source.Player; } }
#else
		public SoundCue( AudioSource source ) { this.source = source; }
		public AudioSource source { get; private set; }
#endif

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
#if ADX
			source.Stop();
#else
			source.Pause();
#endif
		}
		public bool IsPlaying()
		{
#if ADX
			return source.status == CriAtomSource.Status.Playing;
#else
			return source.isPlaying;
#endif
		}


	}

	private static Music Current;
    private static List<Music> MusicList = new List<Music>();

	//static properties
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
	/// is currently former half in a MusicalTime, or last half.
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
    public static double lag
    {
        get
        {
            if( Current.isFormerHalf_ )
                return Current.dtFromJust_;
            else
                return Current.dtFromJust_ - Current.MusicTimeUnit;
        }
    }
    /// <summary>
    /// returns how long from nearest Just timing absolutely.
    /// </summary>
    public static double lagAbs
    {
        get
        {
            if( Current.isFormerHalf_ )
                return Current.dtFromJust_;
            else
                return Current.MusicTimeUnit - Current.dtFromJust_;
        }
    }
    /// <summary>
    /// returns normalized lag.
    /// </summary>
    public static double lagUnit { get { return lag / Current.MusicTimeUnit; } }
    /// <summary>
    /// returns time based in beat.
    /// </summary>
	public static double MusicalTime { get { return Now.totalUnit + lagUnit; } }
	public static int mtBar { get { return Current.mtBar_; } }
	public static int mtBeat { get { return Current.mtBeat_; } }
	public static double mtUnit { get { return Current.MusicTimeUnit; } }
	public static SoundCue CurrentSource { get { return Current.MusicSource; } }

	//static predicates
	public static bool IsNowChangedWhen( System.Predicate<Timing> pred )
	{
		return Current.isNowChanged_ && pred( Current.Now_ );
	}
	public static bool IsNowChangedBar()
	{
		return Current.isNowChanged_ && Current.Now_.barUnit == 0;
	}
	public static bool IsNowChangedBeat()
	{
		return Current.isNowChanged_ && Current.Now_.unit == 0;
	}
	public static bool IsNowChangedAt( int bar, int beat = 0, int unit = 0 )
	{
		return Current.isNowChanged_ &&
                Current.Now_.totalUnit == Current.mtBar_ * bar + Current.mtBeat_ * beat + unit;
	}
	public static bool IsJustChangedWhen( System.Predicate<Timing> pred )
	{
		return Current.isJustChanged_ && pred( Current.Just_ );
	}
	public static bool IsJustChangedBar()
	{
		return Current.isJustChanged_ && Current.Just_.barUnit == 0;
	}
	public static bool IsJustChangedBeat()
	{
		return Current.isJustChanged_ && Current.Just_.unit == 0;
	}
	public static bool IsJustChangedAt( int bar = 0, int beat = 0, int unit = 0 )
	{
		return Current.isJustChanged_ &&
                Current.Just_.totalUnit == Current.mtBar_ * bar + Current.mtBeat_ * beat + unit;
	}

    //static functions
	/// <summary>
	/// Change Current Music.
	/// </summary>
	/// <param name="MusicName">name of the GameObject that include Music</param>
    public static void Play( string MusicName ) { MusicList.Find( ( Music m ) => m.name == MusicName ).PlayStart(); }
	/// <summary>
	/// Quantize to musical time( default is 16 beat ).
	/// </summary>
	/// <param name="source">your sound source( Unity AudioSource or ADX CriAtomSource )</param>
	public static void QuantizePlay( SoundCue source ) { Current.QuantizedCue.Add( source ); }
    public static bool IsPlaying() { return Current.MusicSource.IsPlaying(); }
    public static void Pause() { Current.MusicSource.Pause(); }
    public static void Resume() { Current.MusicSource.Play(); }
    public static void Stop() { Current.MusicSource.Stop(); }

	//static readonlies
	private static readonly int SamplingRate = 44100;

	//music editor params
	/// <summary>
    /// how many MusicTime in a beat. maybe 4 or 3.
	/// </summary>
	public int mtBeat_ = 4;
	/// <summary>
    /// how many MusicTime in a bar.
	/// </summary>
	public int mtBar_ = 16;
	/// <summary>
    /// Musical Tempo. how many beats in a minutes.
	/// </summary>
	public double Tempo_ = 120;


	#region private params

	//music current params
	SoundCue MusicSource;
	List<SoundCue> QuantizedCue;

	Timing Now_;
	Timing Just_;
	bool isJustChanged_;
	bool isNowChanged_;
	bool isFormerHalf_;
	double dtFromJust_;
	int numRepeat_;

	//readonly params
	double MusicTimeUnit;
	long SamplesPerUnit;
	long SamplesPerBeat;
	long SamplesPerBar;

	//others
	Timing OldNow, OldJust;

	//block information
#if ADX
	[System.Serializable]
	public class BlockInfo
	{
		public BlockInfo( string BlockName, int NumBar = 4 )
		{
			this.BlockName = BlockName;
			this.NumBar = NumBar;
		}
		public string BlockName;
		public int NumBar = 4;
	}

	public static void SetNextBlock( string blockName )
	{
		int index = Current.BlockInfos.FindIndex( ( BlockInfo info ) => info.BlockName==blockName );
		if ( index >= 0 )
		{
			Current.NextBlockIndex = index;
			Current.playback.SetNextBlockIndex( index );
		}
		else
		{
			Debug.LogError( "Error!! Music.SetNextBlock Can't find block name: " + blockName );
		}
	}
	public static void SetNextBlock( int index )
	{
		if ( index < Current.CueInfo.numBlocks )
		{
			Current.NextBlockIndex = index;
			Current.playback.SetNextBlockIndex( index );
		}
		else
		{
			Debug.LogError( "Error!! Music.SetNextBlock index is out of range: " + index );
		}
	}
	public static int GetNextBlock() { return Current.NextBlockIndex; }
	public static string GetNextBlockName() { return Current.BlockInfos[Current.NextBlockIndex].BlockName; }
	public static int GetCurrentBlock() { return Current.CurrentBlockIndex; }
	public static string GetCurrentBlockName() { return Current.BlockInfos[Current.CurrentBlockIndex].BlockName; }

	public static void SetFirstBlock( int index )
	{
		if ( index < Current.CueInfo.numBlocks )
		{
			Current.NextBlockIndex = index;
			Current.CurrentBlockIndex = index;
			Current.MusicSource.Player.SetFirstBlockIndex( index );
		}
		else
		{
			Debug.LogError( "Error!! Music.SetFirstBlock index is out of range: " + index );
		}
	}
	public static void SetFirstBlock( string blockName )
	{
		int index = Current.BlockInfos.FindIndex( ( BlockInfo info ) => info.BlockName==blockName );
		if ( index >= 0 )
		{
			Current.NextBlockIndex = index;
			Current.CurrentBlockIndex = index;
			Current.MusicSource.Player.SetFirstBlockIndex( index );
		}
		else
		{
			Debug.LogError( "Error!! Music.SetFirstBlock Can't find block name: " + blockName );
		}
	}

	
	CriAtomExPlayback playback;
	CriAtomExAcb ACBData;
	CriAtomEx.CueInfo CueInfo;
	
	int CurrentBlockIndex;
	/// <summary>
    /// you can't catch NextBlockIndex if ADX automatically change next block.
	/// </summary>
    int NextBlockIndex;
    int OldBlockIndex;
	public List<BlockInfo> BlockInfos;
	int NumBlockBar;
    long SamplesInLoop { get { return NumBlockBar * SamplesPerBar; } }
    long OldNumSamples;
#else
	/// <summary>
	/// this helps you get a more accurate timing when you loop this music.
	/// </summary>
	public int NumBar;
	long SamplesInLoop { get { return NumBar * SamplesPerBar; } }
#endif

	#endregion

	#region Initialize & Update

	void Awake()
	{
		Current = this;
        MusicList.Add( this );
#if ADX
		MusicSource = new SoundCue( GetComponent<CriAtomSource>() );
		ACBData = CriAtom.GetAcb( MusicSource.source.cueSheet );
		ACBData.GetCueInfo( MusicSource.source.cueName, out CueInfo );
#else
		MusicSource = new SoundCue( GetComponent<AudioSource>() );
#endif
		QuantizedCue = new List<SoundCue>();

		SamplesPerUnit = (long)( SamplingRate * ( 60.0 / ( Tempo_ * mtBeat_ ) ) );
		SamplesPerBeat = SamplesPerUnit*mtBeat_;
		SamplesPerBar = SamplesPerUnit*mtBar_;

		MusicTimeUnit = (double)SamplesPerUnit / (double)SamplingRate;

        Initialize();
	}

    void Initialize()
    {
        Now_ = new Timing( 0, 0, -1 );
        Just_ = new Timing( Now_ );
        OldNow = new Timing( Now_ );
        OldJust = new Timing( Just_ );
        dtFromJust_ = 0;
        isFormerHalf_ = true;
        numRepeat_ = 0;
#if ADX
        CurrentBlockIndex = 0;
        OldBlockIndex = 0;
        NextBlockIndex = 0;
        OldNumSamples = 0;
#endif
	}
	
	void PlayStart()
	{
		if ( Current != null && IsPlaying() )
		{
			Stop();
		}

		Current = this;
		Initialize();

		WillBlockChange();
#if ADX
		playback = MusicSource.source.Play();
		NumBlockBar = BlockInfos[playback.GetCurrentBlockIndex()].NumBar;
#else
		MusicSource.Play();
#endif
		OnBlockChanged();

	}

	// Use this for initialization
	void Start()
	{
	}

	// Update is called once per frame
	void Update () {
        long numSamples;
        isNowChanged_ = false;
        isJustChanged_ = false;
#if ADX
        if( playback.GetStatus() != CriAtomExPlayback.Status.Playing ) return;
		int tempOut;
		if ( !playback.GetNumPlayedSamples( out numSamples, out tempOut ) )
		{
			numSamples = -1;
		}
#else
        if( !MusicSource.IsPlaying() ) return;
        numSamples = MusicSource.source.timeSamples;
#endif
		if( numSamples >= 0 )
		{
			Just_.bar = (int)( numSamples / SamplesPerBar );
#if ADX
			UpdateNumBlockBar( numSamples );
			if( NumBlockBar != 0 ) Just_.bar %= NumBlockBar;
#else
			if ( NumBar != 0 ) Just_.bar %= NumBar;
#endif
			Just_.beat = (int)( ( numSamples % SamplesPerBar ) / SamplesPerBeat );
            Just_.unit = (int)((numSamples % SamplesPerBeat) / SamplesPerUnit);
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
	}
	
	void CallEvents()
	{
		if ( isNowChanged_ ) OnNowChanged();
		if ( isNowChanged_ && OldNow > Now_ )
		{
#if ADX
			if ( NextBlockIndex == CurrentBlockIndex )
			{
#endif
				WillRepeat();
#if ADX
			}
			else
			{
				WillBlockChange();
			}
#endif
        }
		if ( isJustChanged_ ) OnJustChanged();
		if ( isJustChanged_ && Just_.unit == 0 ) OnBeat();
		if ( isJustChanged_ && Just_.barUnit == 0 ) OnBar();
		if ( isJustChanged_ && OldJust > Just_ )
		{
#if ADX
			CurrentBlockIndex = playback.GetCurrentBlockIndex();
			Debug.Log( "CurrentBlockIndex is " + CurrentBlockIndex );
			if ( OldBlockIndex == CurrentBlockIndex )
			{
#endif
				OnRepeated();
#if ADX
			}
			else
			{
				OnBlockChanged();
            }
			OldBlockIndex = CurrentBlockIndex;
#endif
        }

#if WARN_IF_TIMING_SKIPPED
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
#endif
	}

#if ADX
	void UpdateNumBlockBar( long numSamples )
	{
		//BlockChanged
		if ( OldNumSamples > numSamples )
		{
			NumBlockBar = BlockInfos[playback.GetCurrentBlockIndex()].NumBar;
		}
		//BlockChanged during this block
		else if ( playback.GetCurrentBlockIndex() != CurrentBlockIndex && Just_.bar != BlockInfos[CurrentBlockIndex].NumBar - 1 )
		{
			NumBlockBar = Just_.bar + 1;
		}
        OldNumSamples = numSamples;
	}
#endif
	#endregion

	#region Events ( called from CallEvents() )
	//On events (when isJustChanged)
	void OnNowChanged()
	{
	}

	void OnJustChanged()
	{
		foreach ( SoundCue cue in QuantizedCue )
		{
			cue.Play();
		}
		QuantizedCue.Clear();
		//Debug.Log( "OnJust " + Just.ToString() );
	}

	void OnBeat()
	{
		//Debug.Log( "OnBeat " + Just.ToString() );
	}

	void OnBar()
	{
		//Debug.Log( "OnBar " + Just.ToString() );
	}

	void OnRepeated()
	{
		++numRepeat_;
		//Debug.Log( "NumRepeat = " + numRepeat );
	}

	void OnBlockChanged()
	{
		numRepeat_ = 0;
	}

	//Will events (when isNowChanged)
	void WillRepeat()
	{
	}

	void WillBlockChange()
	{
	}
	#endregion
}
