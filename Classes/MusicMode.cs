using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class MusicMode
{
	public string Name;
	public List<float> LayerVolumes;
	public float TotalVolume = 1.0f;

	[Serializable]
	public class TransitionParams
	{
		public float FadeTime = 1.0f;
		public Music.TimeUnitType TimeUnitType = Music.TimeUnitType.Sec;
		public Music.SyncType SyncType = Music.SyncType.Immediate;
		public int SyncFactor = 1;
		public float FadeOffset = 0.0f;

		public float FadeTimeSec { get { return Music.TimeUtility.ConvertTime(FadeTime, TimeUnitType, Music.TimeUnitType.Sec); } }
		public float FadeOffsetSec { get { return Music.TimeUtility.ConvertTime(FadeOffset, TimeUnitType, Music.TimeUnitType.Sec); } }
	}
}