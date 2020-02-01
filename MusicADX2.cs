//#define ADX2
#if ADX2

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Xml;

[RequireComponent(typeof(CriAtomSource))]
public class MusicADX2 : Music
{
	#region params

	public MusicMeter Meter = new MusicMeter(0);

	public int SampleRate = 44100;

	[Tooltip("ブロック遷移時にタイミングを(0,0,0)から開始したい場合はtrue, AtomCraft上のタイムラインと同じように進行させたい場合はfalse")]
	public bool ResetTimingOnEachBlock = false;

	public List<BlockInfo> BlockInfos;

	[Tooltip("SetVerticalMixByIndex関数で使われるAISACコントロールのID")]
	public uint AisacControlID = 0;

	[Tooltip("AisacControlIDで変化させる状態の数。0.0f～1.0fのAisac値をこの値によって均等分割してIndexで設定できるようにする。")]
	public uint AisacStateCount = 2;

	public float AisacInitialValue = 0.0f;

	public float AisacFadeTime = 1.0f;

	#endregion


	#region ADX2 resources

	private CriAtomSource atomSource_;
	private CriAtomExPlayback playback_;
	private CriAtomExAcb acbData_;
	private CriAtomEx.CueInfo cueInfo_;

	#endregion


	#region block params

	[System.Serializable]
	public class BlockInfo
	{
		public BlockInfo(string blockName, int numBar, int startBar = 0)
		{
			this.BlockName = blockName;
			this.NumBar = numBar;
			this.StartBar = startBar;
		}
		public string BlockName = "Block";
		public int NumBar = 4;
		public int StartBar = 0;
	}

	private int currentBlockIndex_;
	private int nextBlockIndex_;
	private int prevBlockIndex_;

	public BlockInfo CurrentBlock { get { return BlockInfos[currentBlockIndex_]; } }
	public BlockInfo NextBlock { get { return BlockInfos[nextBlockIndex_]; } }
	public string NextBlockName { get { return (nextBlockIndex_ >= 0 ? NextBlock.BlockName : ""); } }

	private float lastAisacValue_;
	private float currentAisacValue_;
	private Coroutine currentAisacCoroutine_;

	#endregion


	#region unity functions

	void OnValidate()
	{
		Meter.OnValidate(SampleRate);
	}

	#endregion


	#region IMusicSource

	protected override void Initialize()
	{
		atomSource_ = GetComponent<CriAtomSource>();
		if( atomSource_.playOnStart )
		{
			atomSource_.playOnStart = false;
			PlayOnStart = true;
		}
		acbData_ = CriAtom.GetAcb(atomSource_.cueSheet);
		acbData_.GetCueInfo(atomSource_.cueName, out cueInfo_);
	}

	protected override void Ready()
	{
		base.Ready();
		currentAisacValue_ = lastAisacValue_ = AisacInitialValue;
		currentBlockIndex_ = 0;
		prevBlockIndex_ = 0;
		nextBlockIndex_ = 0;
	}

	public override bool PlayOnStart { get; set; }

	public override bool IsPlaying_ { get { return atomSource_ != null && atomSource_.status == CriAtomSource.Status.Playing; } }

	public override float Volume { get { return atomSource_.volume; } set { atomSource_.volume = value; } }

	public override string SequenceName { get { return CurrentBlock.BlockName; } }

	public override int SequenceIndex { get { return currentBlockIndex_; } }

	public override void Play_()
	{
		base.Play_();

		atomSource_ = GetComponent<CriAtomSource>();
		playback_ = atomSource_.Play();
	}

	public override void Stop_()
	{
		if( atomSource_ != null && playback_.status == CriAtomExPlayback.Status.Playing )
		{
			atomSource_.Stop();
		}
	}

	public override void Suspend_()
	{
		if( atomSource_ != null )
		{
			atomSource_.Pause(true);
		}
	}

	public override void Resume_()
	{
		if( atomSource_ != null )
		{
			atomSource_.Pause(false);
		}
	}

	public override void Seek(int sequenceIndex, Timing seekTiming)
	{
		nextBlockIndex_ = sequenceIndex;
		currentBlockIndex_ = sequenceIndex;
		atomSource_.player.SetFirstBlockIndex(sequenceIndex);
		atomSource_.startTime = (int)(((float)Meter.GetSampleFromTiming(seekTiming) / SampleRate) * 1000.0f);
	}
	
	protected override int GetCurrentSample()
	{
		if( atomSource_ != null && atomSource_.status == CriAtomSource.Status.Playing )
		{
			int startSample = 0;
			if( ResetTimingOnEachBlock )
			{
				startSample = (int)(Meter.SecPerBar * CurrentBlock.StartBar * sampleRate_);
			}
			return Math.Max(0, (int)((playback_.GetSequencePosition() / 1000.0) * sampleRate_) - startSample);
		}
		return -1;
	}

	protected override int GetSampleRate()
	{
		return SampleRate;
	}

	protected override MusicMeter GetMeterFromSample(int currentSample)
	{
		return Meter;
	}

	protected override void UpdateHorizontalState()
	{
		prevBlockIndex_ = currentBlockIndex_;
		currentBlockIndex_ = playback_.GetCurrentBlockIndex();
	}

	protected override void UpdateVerticalState()
	{

	}

	protected override Timing GetSequenceEndTiming()
	{
		return ResetTimingOnEachBlock ? new Timing(CurrentBlock.NumBar) : null;
	}

	protected override void OnRepeated()
	{
		nextBlockIndex_ = -1;
	}

	protected override void OnHorizontalSequenceChanged()
	{
		nextBlockIndex_ = -1;
	}



	public override void SetHorizontalSequence(string name)
	{
		if( name == CurrentBlock.BlockName ) return;
		int index = BlockInfos.FindIndex((BlockInfo info) => info.BlockName == name);
		if( index >= 0 )
		{
			nextBlockIndex_ = index;
			playback_.SetNextBlockIndex(index);
		}
		else
		{
			Debug.LogError("Error!! MusicSourceADX2.SetHorizontalSequence Can't find block name: " + name);
		}
	}

	public override void SetHorizontalSequenceByIndex(int index)
	{
		if( index == currentBlockIndex_ ) return;
		if( index < cueInfo_.numBlocks )
		{
			nextBlockIndex_ = index;
			playback_.SetNextBlockIndex(index);
		}
		else
		{
			Debug.LogError("Error!! MusicSourceADX2.SetHorizontalSequenceByIndex index is out of range: " + index);
		}
	}

	public override void SetVerticalMix(string name)
	{
		Debug.LogWarning("MusicADX2.SetVerticalMix is not implemented.");
	}

	public override void SetVerticalMixByIndex(int index)
	{
		if( atomSource_ != null && AisacControlID  >= 0 && AisacStateCount > 1 && playback_.status == CriAtomExPlayback.Status.Playing )
		{
			// 前のコルーチンが終わってなければ殺す
			if( currentAisacCoroutine_ != null )
			{
				StopCoroutine(currentAisacCoroutine_);
			}
			currentAisacCoroutine_ = StartCoroutine(FadeAisacCoroutine((float)index / (AisacStateCount - 1)));
		}
	}
	
	IEnumerator FadeAisacCoroutine(float targetAisacValue)
	{
		// 現在値を反映。これがないと、連続で切り替えた時にAISAC値が飛んでしまう
		lastAisacValue_ = currentAisacValue_;
		// FadeTimeプロパティを使ってフェード
		for( float t = 0; t < AisacFadeTime; t += Time.deltaTime )
		{
			currentAisacValue_ = Mathf.Lerp(lastAisacValue_, targetAisacValue, Mathf.Clamp01(t / AisacFadeTime));
			atomSource_.SetAisacControl(AisacControlID, currentAisacValue_);
			yield return null;
		}

		// フェード完了
		currentAisacValue_ = lastAisacValue_ = targetAisacValue;
		atomSource_.SetAisacControl(AisacControlID, targetAisacValue);

		currentAisacCoroutine_ = null;
	}

	#endregion


	#region utils

	public static void UpdateBlockInfo(string outputAssetsRoot)
	{
		string[] acbInfoFileList = System.IO.Directory.GetFiles(outputAssetsRoot.Replace("/Assets", ""), "*_acb_info.xml", System.IO.SearchOption.AllDirectories);
		List<MusicADX2> musicList = new List<MusicADX2>(GameObject.FindObjectsOfType<MusicADX2>());

		foreach( string acbInfoFile in acbInfoFileList )
		{
			string cueSheetName = System.IO.Path.GetFileName(acbInfoFile).Replace("_acb_info.xml", "");

			XmlReaderSettings settings = new XmlReaderSettings();
			settings.IgnoreWhitespace = true;
			settings.IgnoreComments = true;
			using( XmlReader reader = XmlReader.Create(System.IO.File.OpenText(acbInfoFile), settings) )
			{
				while( reader.Read() )
				{
					// if this is a Cue and it has Bpm setting
					if( reader.GetAttribute("CueID") != null && reader.GetAttribute("Bpm") != null )
					{
						string cueName = reader.GetAttribute("OrcaName");
						MusicADX2 musicADX2 = musicList.Find((m) => m.GetComponent<CriAtomSource>().cueSheet == cueSheetName
																 && m.GetComponent<CriAtomSource>().cueName == cueName);
						if( musicADX2 != null )
						{
							musicADX2.LoadAcbInfoData(reader.ReadSubtree(), double.Parse(reader.GetAttribute("Bpm")));
							musicList.Remove(musicADX2);
						}
					}
				}
				reader.Close();
			}
		}
	}

	void LoadAcbInfoData(XmlReader reader, double Bpm)
	{
		Meter.Tempo = Bpm;
		Meter.OnValidate(SampleRate);
		BlockInfos = new List<BlockInfo>();
		int startBar = 0;
		while( reader.Read() )
		{
			if( Meter.Tempo > 0 && reader.GetAttribute("BlockEndPositionMs") != null )
			{
				string blockName = reader.GetAttribute("OrcaName");
				double sec = double.Parse(reader.GetAttribute("BlockEndPositionMs")) / 1000.0;
				int bar = Mathf.RoundToInt((float)(sec / Meter.SecPerBar));
				MusicADX2.BlockInfo blockInfo = new MusicADX2.BlockInfo(blockName, bar, startBar);
				BlockInfos.Add(blockInfo);
				startBar += bar;
			}
		}
	}

	#endregion
}

#endif