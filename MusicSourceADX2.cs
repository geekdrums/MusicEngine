//#define ADX2
#if ADX2

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(CriAtomSource))]
public class MusicSourceADX2 : MonoBehaviour, IMusicSource
{
#region interactive music params

	public Meter Meter = new Meter(0);

	public int SampleRate = 44100;

	public List<BlockInfo> BlockInfos;

	[TooltipAttribute("SetVerticalMix関数で使われるAISACコントロールのID")]
	public uint AisacControlID = 0;

	[TooltipAttribute("SetVerticalMixByName関数で使われるセレクタ名")]
	public string SelectorName = "";

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
		public BlockInfo(string BlockName, int NumBar = 4)
		{
			this.BlockName = BlockName;
			this.NumBar = NumBar;
		}
		public string BlockName = "Block";
		public int NumBar = 4;
	}

	private int currentBlockIndex_;
	private int nextBlockIndex_;
	private int oldBlockIndex_;

	public BlockInfo CurrentBlock { get { return BlockInfos[currentBlockIndex_]; } }
	public BlockInfo NextBlock { get { return BlockInfos[nextBlockIndex_]; } }
	public string NextBlockName { get { return (nextBlockIndex_ >= 0 ? NextBlock.BlockName : ""); } }
	
	void Initialize()
	{
		currentBlockIndex_ = 0;
		oldBlockIndex_ = 0;
		nextBlockIndex_ = 0;
	}

#endregion


#region unity functions

	void Awake()
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

	void OnValidate()
	{
		Meter.OnValidate(SampleRate);
	}

	void Update()
	{
	}

#endregion


#region IMusicSource

	public bool PlayOnStart { get; private set; }

	public bool IsPlaying { get { return atomSource_ != null && atomSource_.status == CriAtomSource.Status.Playing; } }

	public string SequenceName { get { return CurrentBlock.BlockName; } }

	public int SequenceIndex { get { return currentBlockIndex_; } }

	public void Play()
	{
		Initialize();

		atomSource_ = GetComponent<CriAtomSource>();
		playback_ = atomSource_.Play();
	}

	public void Stop()
	{
		if( atomSource_ != null && playback_.status == CriAtomExPlayback.Status.Playing )
		{
			atomSource_.Stop();
		}
	}

	public void Suspend()
	{
		if( atomSource_ != null )
		{
			atomSource_.Pause(true);
		}
	}

	public void Resume()
	{
		if( atomSource_ != null )
		{
			atomSource_.Pause(false);
		}
	}

	public int GetCurrentSample()
	{
		if( atomSource_ != null && atomSource_.status == CriAtomSource.Status.Playing )
		{
			long numSamples;
			int sampleRate;
			if( !playback_.GetNumPlayedSamples(out numSamples, out sampleRate) )
			{
				numSamples = -1;
			}
			return (int)numSamples;
		}
		return -1;
	}

	public int GetSampleRate()
	{
		return SampleRate;
	}

	public Meter GetCurrentMeter(int currentSample)
	{
		return Meter;
	}

	public void UpdateSequenceState()
	{
		currentBlockIndex_ = playback_.GetCurrentBlockIndex();
	}

	public Timing GetSequenceEndTiming()
	{
		return new Timing(BlockInfos[currentBlockIndex_].NumBar);
	}

	public void OnRepeated()
	{
		nextBlockIndex_ = -1;
	}

	public void OnHorizontalSequenceChanged()
	{
		nextBlockIndex_ = -1;
	}



	public void SetHorizontalSequence(string name)
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

	public void SetHorizontalSequenceByIndex(int index)
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

	public void SetVerticalMix(float value)
	{
		if( atomSource_ != null && AisacControlID  >= 0 )
		{
			atomSource_.SetAisacControl(AisacControlID, value);
		}
	}

	public void SetVerticalMixByName(string name)
	{
		if( atomSource_ != null && string.IsNullOrEmpty(SelectorName) == false )
		{
			atomSource_.player.SetSelectorLabel(SelectorName, name);
			atomSource_.player.Update(playback_);
		}
	}

#endregion


#region ADX2 unique functions

	public void SetFirstBlock(string blockName)
	{
		int index = BlockInfos.FindIndex((BlockInfo info) => info.BlockName == blockName);
		if( index >= 0 )
		{
			nextBlockIndex_ = index;
			currentBlockIndex_ = index;
			atomSource_.player.SetFirstBlockIndex(index);
		}
		else
		{
			Debug.LogError("Error!! MusicSourceADX2.SetFirstBlock Can't find block name: " + blockName);
		}
	}

	public void SetFirstBlock(int index)
	{
		if( index < cueInfo_.numBlocks )
		{
			nextBlockIndex_ = index;
			currentBlockIndex_ = index;
			atomSource_.player.SetFirstBlockIndex(index);
		}
		else
		{
			Debug.LogError("Error!! MusicSourceADX2.SetFirstBlock index is out of range: " + index);
		}
	}

	public void SetVolume(float volume)
	{
		if( atomSource_ != null )
		{
			atomSource_.volume = volume;
		}
	}

	public string GetBlockDebugText()
	{
		return String.Format("block[{0}] = {1}({2}bar)", currentBlockIndex_, CurrentBlock.BlockName, BlockInfos[currentBlockIndex_].NumBar);
	}
	
#endregion
}

#endif