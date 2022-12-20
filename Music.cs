//Copyright (c) 2022 geekdrums
//Released under the MIT license
//http://opensource.org/licenses/mit-license.php
//Feel free to use this for your lovely musical games :)

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Static access to currently playing music.
/// Music.Play will set and change current instance.
/// </summary>
public static class Music
{
	#region [public] current music

	public static MusicBase Current { get { return current_; } }
	public static string CurrentMusicName { get { return current_ != null ? current_.name : ""; } }
	public static bool IsPlaying { get { return current_ != null ? current_.IsPlaying : false; } }
	public static bool IsPlayingOrSuspended { get { return current_ != null ? current_.IsPlayingOrSuspended : false; } }
	public static PlayState State { get { return current_ != null ? current_.State : PlayState.Invalid; } }

	#endregion


	#region [public] just / near timing

	/// <summary>
	/// 経過した最後のタイミングを指し示します
	/// Last timing.
	/// </summary>
	public static Timing Just { get { return current_ != null ? current_.Just : null; } }

	/// <summary>
	/// is Just changed in this frame or not.
	/// </summary>
	public static bool IsJustChanged { get { return current_ != null ? current_.IsJustChanged : false; } }

	/// <summary>
	/// current musical time in units
	/// </summary>
	public static int JustTotalUnits { get { return current_ != null ? current_.JustTotalUnits : 0; } }

	public static bool IsJustChangedWhen(Predicate<Timing> pred)
	{
		return current_ != null ? current_.IsJustChangedWhen(pred) : false;
	}
	public static bool IsJustChangedBar()
	{
		return current_ != null ? current_.IsJustChangedBar() : false;
	}
	public static bool IsJustChangedBeat()
	{
		return current_ != null ? current_.IsJustChangedBeat() : false;
	}
	public static bool IsJustChangedAt(int bar = 0, int beat = 0, int unit = 0)
	{
		return current_ != null ? current_.IsJustChangedAt(bar, beat, unit) : false;
	}
	public static bool IsJustChangedAt(Timing t)
	{
		return current_ != null ? current_.IsJustChangedAt(t.Bar, t.Beat, t.Unit) : false;
	}
	public static bool IsJustLooped()
	{
		return current_ != null ? current_.IsJustLooped() : false;
	}

	/// <summary>
	/// 最も近いタイミングを指し示します。
	/// Nearest timing.
	/// </summary>
	public static Timing Near { get { return current_ != null ? current_.Near : null; } }

	/// <summary>
	/// is Near changed in this frame or not.
	/// </summary>
	public static bool IsNearChanged { get { return current_ != null ? current_.IsNearChanged : false; } }

	/// <summary>
	/// current musical time in units
	/// </summary>
	public static int NearTotalUnits { get { return current_ != null ? current_.NearTotalUnits : 0; } }

	public static bool IsNearChangedWhen(Predicate<Timing> pred)
	{
		return current_ != null ? current_.IsNearChangedWhen(pred) : false;
	}
	public static bool IsNearChangedBar()
	{
		return current_ != null ? current_.IsNearChangedBar() : false;
	}
	public static bool IsNearChangedBeat()
	{
		return current_ != null ? current_.IsNearChangedBeat() : false;
	}
	public static bool IsNearChangedAt(int bar, int beat = 0, int unit = 0)
	{
		return current_ != null ? current_.IsNearChangedAt(bar, beat, unit) : false;
	}
	public static bool IsNearChangedAt(Timing t)
	{
		return current_ != null ? current_.IsNearChangedAt(t.Bar, t.Beat, t.Unit) : false;
	}

	/// <summary>
	/// is currently former half in a MusicTimeUnit, or last half.
	/// </summary>
	public static bool IsFormerHalf { get { return current_ != null ? current_.IsFormerHalf : false; } }
	/// <summary>
	/// returns sec from last Just timing.
	/// </summary>
	public static double SecFromJust { get { return current_ != null ? current_.SecFromJust : 0; } }
	/// <summary>
	/// returns normalized time (0 to 1) from last Just timing.
	/// </summary>
	public static double UnitFromJust { get { return current_ != null ? current_.UnitFromJust : 0; } }

	#endregion


	#region [public] play / stop / suspend / resume

	/// <summary>
	/// Change current music and play.
	/// </summary>
	/// <param name="musicName">name of the GameObject that include Music</param>
	public static void Play(string musicName)
	{
		MusicBase music = musicList_.Find((MusicBase m) => m != null && m.name == musicName);
		if( music != null )
		{
			music.Play();
		}
		else
		{
			Debug.Log("Can't find music: " + musicName);
		}
	}
	public static void PlayFrom(string musicName, Timing seekTiming, int sequenceIndex = 0)
	{
		MusicBase music = musicList_.Find((MusicBase m) => m != null && m.name == musicName);
		if( music != null )
		{
			music.Seek(seekTiming, sequenceIndex);
			music.Play();
		}
		else
		{
			Debug.Log("Can't find music: " + musicName);
		}
	}
	public static void Suspend() { current_?.Suspend(); }
	public static void Resume() { current_?.Resume(); }
	public static void Stop() { current_?.Stop(); }

	#endregion


	#region [public] interactive music

	/// <summary>
	/// 横の遷移を実行します
	/// execute Horizontal Resequencing
	///  in MusicUnity, SetNextSection
	///  in MusicADX2, SetNextBlock
	///  in MusicWwise, SetState
	/// </summary>
	/// <param name="name">Section/Block/State name</param>
	public static void SetHorizontalSequence(string name) { current_?.SetHorizontalSequence(name); }

	/// <summary>
	/// 横の遷移を実行します
	/// execute Horizontal Resequencing
	///  in MusicUnity, SetNextSection
	///  in MusicADX2, SetNextBlock
	///  in MusicWwise, not implemented
	/// </summary>
	/// <param name="name">Section/Block index</param>
	public static void SetHorizontalSequenceByIndex(int index) { current_?.SetHorizontalSequenceByIndex(index); }

	/// <summary>
	/// 縦の遷移を実行します
	/// execute Vertical Remixing
	///  in MusicUnity, SetMode
	///  in MusicADX2, not implemented
	///  in MusicWwise, SetState
	/// </summary>
	/// <param name="name">Mode/State name</param>
	public static void SetVerticalMix(string name) { current_?.SetVerticalMix(name); }

	/// <summary>
	/// 縦の遷移を実行します
	/// execute Vertical Remixing
	///  in MusicUnity, SetMode
	///  in MusicADX2, SetAisacControl
	///  in MusicWwise, not implemented
	/// </summary>
	/// <param name="index">Mode/Aisac index</param>
	public static void SetVerticalMixByIndex(int index) { current_?.SetVerticalMixByIndex(index); }

	#endregion


	#region [public] musical time / meter / sequence

	// musical time

	/// <summary>
	/// current musical time in bars
	/// </summary>
	public static float MusicalTime { get { return current_ != null ? current_.MusicalTime : 0; } }

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
		return Mathf.Lerp(min, max, ((float)Math.Cos(Math.PI * 2 * (CurrentUnitPerBar * MusicalTime + offset) / cycle) + 1.0f) / 2.0f);
	}

	// current meter

	public static bool HasValidMeter { get { return current_ != null && current_.CurrentMeter != null; } }
	public static MusicMeter CurrentMeter { get { return current_ != null ? current_.CurrentMeter : null; } }
	public static double CurrentTempo { get { return current_ != null ? current_.CurrentMeter.Tempo : 0; } }
	public static int CurrentUnitPerBar { get { return current_ != null ? current_.CurrentMeter.UnitPerBar : 0; } }
	public static int CurrentUnitPerBeat { get { return current_ != null ? current_.CurrentMeter.UnitPerBeat : 0; } }

	// current sequence

	public static string CurrentSequenceName { get { return current_ != null ? current_.SequenceName : ""; } }
	public static int CurrentSequenceIndex { get { return current_ != null ? current_.SequenceIndex : 0; } }
	public static int NumRepeat { get { return current_ != null ? current_.NumRepeat : 0; } }

	#endregion


	#region params

	static MusicBase current_;
	static List<MusicBase> musicList_ = new List<MusicBase>();

	public static void RegisterMusic(MusicBase music)
	{
		if( musicList_.Contains(music) == false )
		{
			musicList_.Add(music);
		}
	}

	public static void OnPlay(MusicBase music)
	{
		if( current_ != null && current_ != music && current_.IsPlaying )
		{
			current_.Stop();
		}

		current_ = music;
	}

	public static void OnFinish(MusicBase music)
	{
		if( current_ == music )
		{
			current_ = null;
		}
	}

	#endregion


	#region enum / delegate

	public enum PlayState
	{
		Invalid,
		Ready,
		Playing,
		Suspended,
		Finished
	};

	public enum SyncType
	{
		Immediate,
		Unit,
		Beat,
		Bar,
		Marker,
		ExitPoint,
	};

	public enum TimeUnitType
	{
		Sec,
		MSec,
		Bar,
		Beat,
		Unit,
	};

	public static class TimeUtility
	{
		public static float DefaultBPM = 120;

		public static float ConvertTime(float time, TimeUnitType from, TimeUnitType to = TimeUnitType.Sec)
		{
			if( from == to ) return time;

			float sec = time;
			if( from == TimeUnitType.Sec )
			{
				sec = time;
			}
			else if( from == TimeUnitType.MSec )
			{
				sec = time / 1000.0f;
			}
			else
			{
				if( HasValidMeter )
				{
					switch( from )
					{
						case TimeUnitType.Bar:
							sec = time * (float)current_.CurrentMeter.SecPerBar;
							break;
						case TimeUnitType.Beat:
							sec = time * (float)current_.CurrentMeter.SecPerBeat;
							break;
						case TimeUnitType.Unit:
							sec = time * (float)current_.CurrentMeter.SecPerUnit;
							break;
					}
				}
				else
				{
					switch( from )
					{
						case TimeUnitType.Bar:
							sec = time * (60.0f * 4.0f / DefaultBPM);
							break;
						case TimeUnitType.Beat:
							sec = time * (60.0f / DefaultBPM);
							break;
						case TimeUnitType.Unit:
							sec = time * (60.0f / 4.0f / DefaultBPM);
							break;
					}
				}
			}

			if( to == TimeUnitType.Sec )
			{
				return sec;
			}
			else if( to == TimeUnitType.MSec )
			{
				return sec * 1000.0f;
			}
			else
			{
				if( HasValidMeter )
				{
					switch( to )
					{
						case TimeUnitType.Bar:
							return sec / (float)current_.CurrentMeter.SecPerBar;
						case TimeUnitType.Beat:
							return sec / (float)current_.CurrentMeter.SecPerBeat;
						case TimeUnitType.Unit:
							return sec / (float)current_.CurrentMeter.SecPerUnit;
					}
				}
				else
				{
					switch( to )
					{
						case TimeUnitType.Bar:
							return sec / (60.0f * 4.0f / DefaultBPM);
						case TimeUnitType.Beat:
							return sec / (60.0f / DefaultBPM);
						case TimeUnitType.Unit:
							return sec / (60.0f / 4.0f / DefaultBPM);
					}
				}
			}

			return sec;
		}
	}
	static readonly float PITCH_UNIT = Mathf.Pow(2.0f, 1.0f / 12.0f);

	#endregion
}