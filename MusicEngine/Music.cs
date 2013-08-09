#define ADX

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// You can get musical information.
/// Attach this as a component to GameObject include Music SoundCue.
/// Known issues...
/// When using ADX2LE, when you use block loop, GetNumPlayedSamples will fail just after every loop.
/// </summary>
public class Music : MonoBehaviour {
	
	public class SoundCue
	{
#if ADX
		public SoundCue( CriAtomSource source ) { this.source = source; }
		public CriAtomSource source { get; private set; }
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

	/// <summary>
	/// Get a currently playing music.
	/// Be suer to play only one Music Cue at once.
	/// </summary>
	private static Music Current;
    private static List<Music> MusicList = new List<Music>();

	//static properties
	public static int mtBar { get { return Current.mtBar_; } }
	public static int mtBeat { get { return Current.mtBeat_; } }
    public static double mtUnit { get { return Current.MusicTimeUnit; } }
	public static Timing Now { get { return Current.Now_; } }
	public static Timing Just { get { return Current.Just_; } }
	public static bool isJustChanged { get { return Current.isJustChanged_; } }
    public static bool isNowChanged { get { return Current.isNowChanged_; } }
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
    public static void Play( string MusicName ) { MusicList.Find( ( Music m ) => m.name == MusicName ).PlayStart(); }
    public static bool IsPlaying() { return Current.MusicSource.IsPlaying(); }
    public static void Pause() { Current.MusicSource.Pause(); }
    public static void Resume() { Current.MusicSource.Play(); }
    public static void Stop() { Current.MusicSource.Stop(); }
    public static void QuantizePlay( SoundCue source ) { Current.QuantizedCue.Add( source ); }


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
#endif

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
	public double Tempo_ = 128;

	#region private params
	//music current params
	/// <summary>
    /// means nearest timing.
	/// </summary>
	Timing Now_;
	/// <summary>
    /// means last timing.
	/// </summary>
	Timing Just_;
	/// <summary>
    /// is Just changed in this frame or not.
	/// </summary>
	bool isJustChanged_;
    /// <summary>
    /// is Now changed in this frame or not.
	/// </summary>
	bool isNowChanged_;
	/// <summary>
    /// is currently former half in a MusicalTime, or last half.
	/// </summary>
	bool isFormerHalf_;
	/// <summary>
    /// delta time from JustChanged.
	/// </summary>
	double dtFromJust_;
	/// <summary>
    /// how many times you repeat current music/block.
	/// </summary>
	int numRepeat_;

	SoundCue MusicSource;
	List<SoundCue> QuantizedCue;
#if ADX
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
    long SamplesInBlock { get { return NumBlockBar * SamplesPerBar; } }
#else
    readonly int NumBlockBar = 0;
#endif

	//readonly params
	double MusicTimeUnit;
	long SamplesPerUnit;
	long SamplesPerBeat;
	long SamplesPerBar;

	//others
	/// <summary>
	/// Cache of Now and Just a frame ago.
	/// </summary>
	Timing OldNow, OldJust;
    long OldNumSamples;

	#endregion

	#region Unity Interfaces
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
        OldNumSamples = 0;
        numRepeat_ = 0;
#if ADX
        CurrentBlockIndex = 0;
        OldBlockIndex = 0;
        NextBlockIndex = 0;
#endif
    }

	// Use this for initialization
	void Start()
	{
	}

    public void PlayStart()
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
#else
        MusicSource.Play();
#endif
        OnBlockChanged();

		NumBlockBar = BlockInfos[playback.GetCurrentBlockIndex()].NumBar;
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
			UpdateNumBlockBar( numSamples );

            Just_.bar = (int)(numSamples / SamplesPerBar);
			if ( NumBlockBar != 0 ) Just_.bar %= NumBlockBar;
			Just_.beat = (int)( ( numSamples % SamplesPerBar ) / SamplesPerBeat );
            Just_.unit = (int)((numSamples % SamplesPerBeat) / SamplesPerUnit);
            isFormerHalf_ = ( numSamples % SamplesPerUnit ) < SamplesPerUnit / 2;
			dtFromJust_ = (double)( numSamples % SamplesPerUnit ) / (double)SamplingRate;
            //Debug.Log( "NumBlockBar:" + NumBlockBar + " Just_:" + Just_.ToString() + " OldJust:" + OldJust.ToString() );

			Now_.Copy( Just_ );
			if ( !isFormerHalf_ ) Now_.Increment();
#if ADX
			if ( numSamples + SamplesPerUnit/2 >= SamplesInBlock )
			{
				Now_.Init();
			}
#endif

			isNowChanged_ = Now_.totalUnit != OldNow.totalUnit;
			isJustChanged_ = Just_.totalUnit != OldJust.totalUnit;

			CallEvents();

			OldNow.Copy( Now_ );
			OldJust.Copy( Just_ );

            OldNumSamples = numSamples;
		}
		else
		{
			//Debug.LogWarning( "Warning!! Failed to GetNumPlayedSamples" );
		}
	}

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
				WillBlockRepeat();
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
				OnBlockRepeated();
#if ADX
			}
			else
			{
				OnBlockChanged();
            }
			OldBlockIndex = CurrentBlockIndex;
#endif
        }

		if ( isJustChanged_ && Just_.totalUnit > 0 )
		{
			Timing tempOld = new Timing( OldJust );
			tempOld.Increment();
			if ( tempOld.totalUnit != Just_.totalUnit )
			{
				Debug.LogWarning( "Warning!! OldJust = " + OldJust.ToString() + ", Just = " + Just_.ToString() );
			}
		}
	}
	#endregion

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

	void OnBlockRepeated()
	{
		++numRepeat_;
		//Debug.Log( "NumRepeat = " + numRepeat );
	}

	void OnBlockChanged()
	{
		numRepeat_ = 0;
	}

	//Will events (when isNowChanged)
	void WillBlockRepeat()
	{
	}

	void WillBlockChange()
	{
	}
}
