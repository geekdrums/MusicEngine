using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

[CustomEditor(typeof(MusicUnity))]
[CanEditMultipleObjects]
public class MusicUnityEditor : Editor
{
	SerializedProperty sectionListProperty_;
	SerializedProperty modeListProperty_;
	SerializedProperty modeTransitionParamProperty_;
	SerializedProperty sectionTransitionOverridesListProperty_;
	SerializedProperty modeTransitionOverridesListProperty_;

	SerializedProperty volumeProperty_;
	SerializedProperty playOnStartProperty_;
	SerializedProperty mixerGroupProperty_;
	SerializedProperty seekBarProperty_;
	SerializedProperty previewSectionProperty_;
	SerializedProperty previewModeProperty_;
	SerializedProperty numTracksProperty_;

	bool showOverrides_;
	bool showOptions_;
	bool showHelpTexts_;
	bool showUnitPerBarBeat_;
	string[] sectionListDisplayOptions_;
	string[] sectionListDisplayOptionsWithAny_;
	int[] sectionListValueOptions_;
	string[] modeListDisplayOptions_;
	string[] modeListDisplayOptionsWithAny_;
	int[] modeListValueOptions_;

	void InitializeProperties()
	{
		sectionListProperty_ = serializedObject.FindProperty("Sections");
		modeListProperty_ = serializedObject.FindProperty("Modes");
		modeTransitionParamProperty_ = serializedObject.FindProperty("ModeTransitionParam");

		sectionTransitionOverridesListProperty_ = serializedObject.FindProperty("SectionTransitionOverrides");
		modeTransitionOverridesListProperty_ = serializedObject.FindProperty("ModeTransitionOverrides");

		volumeProperty_ = serializedObject.FindProperty("Volume");
		playOnStartProperty_ = serializedObject.FindProperty("PlayOnStart");
		mixerGroupProperty_ = serializedObject.FindProperty("OutputMixerGroup");
		seekBarProperty_ = serializedObject.FindProperty("SeekBar");
		previewSectionProperty_ = serializedObject.FindProperty("PreviewSectionIndex");
		previewModeProperty_ = serializedObject.FindProperty("PreviewModeIndex");
		numTracksProperty_ = serializedObject.FindProperty("NumTracks");
	}

	public override void OnInspectorGUI()
	{
		if( sectionListProperty_ == null )
		{
			InitializeProperties();
		}

		serializedObject.Update();

		// params
		EditorGUILayout.PropertyField(volumeProperty_);
		EditorGUILayout.PropertyField(playOnStartProperty_);
		EditorGUILayout.PropertyField(mixerGroupProperty_);

		// 複数選択では以降無視
		if( serializedObject.isEditingMultipleObjects )
		{
			EditorGUILayout.LabelField("Cannot draw section list while multi editing", EditorStyles.boldLabel);
			serializedObject.ApplyModifiedProperties();
			return;
		}

		// musicを取得
		MusicUnity music = serializedObject.targetObject as MusicUnity;
		sectionListDisplayOptions_ = new string[music.Sections.Length];
		for( int i = 0; i < sectionListProperty_.arraySize; ++i )
		{
			sectionListDisplayOptions_[i] = music.Sections[i].Name;
		}
		modeListDisplayOptions_ = new string[music.Modes.Length];
		for( int i = 0; i < modeListProperty_.arraySize; ++i )
		{
			modeListDisplayOptions_[i] = music.Modes[i].Name;
		}

		// num tracks
		EditorGUILayout.PropertyField(numTracksProperty_);

		// sections
		sectionListProperty_.isExpanded = EditorGUILayout.Foldout(sectionListProperty_.isExpanded, "Sections");
		if( sectionListProperty_.isExpanded )
		{
			EditorGUI.indentLevel++;
			for( int i = 0; i < sectionListProperty_.arraySize; ++i )
			{
				DrawSection(sectionListProperty_.GetArrayElementAtIndex(i), (i < music.Sections.Length ? music.Sections[i] : null));
			}
			DrawContainerButtons(sectionListProperty_);
			EditorGUI.indentLevel--;
		}

		// modes
		modeListProperty_.isExpanded = EditorGUILayout.Foldout(modeListProperty_.isExpanded, "Modes");
		if( modeListProperty_.isExpanded )
		{
			EditorGUI.indentLevel++;
			for( int i = 0; i < modeListProperty_.arraySize; ++i )
			{
				DrawMode(modeListProperty_.GetArrayElementAtIndex(i));
			}
			DrawContainerButtons(modeListProperty_);
			DrawModeTransitionParam(modeTransitionParamProperty_);
			EditorGUI.indentLevel--;
		}

		showOverrides_ = EditorGUILayout.Foldout(showOverrides_, "Overrides");
		if( showOverrides_ )
		{
			EditorGUI.indentLevel++;

			// section transition overrides
			sectionTransitionOverridesListProperty_.isExpanded = EditorGUILayout.Foldout(sectionTransitionOverridesListProperty_.isExpanded, "Section Transition Overrides");
			if( sectionTransitionOverridesListProperty_.isExpanded )
			{
				if( sectionTransitionOverridesListProperty_.arraySize > 0 )
				{
					sectionListDisplayOptionsWithAny_ = new string[music.Sections.Length + 1];
					sectionListValueOptions_ = new int[sectionListDisplayOptionsWithAny_.Length];
					sectionListDisplayOptionsWithAny_[0] = "Any";
					sectionListValueOptions_[0] = -1;
					for( int i = 0; i < music.Sections.Length; ++i )
					{
						sectionListDisplayOptionsWithAny_[i + 1] = music.Sections[i].Name;
						sectionListValueOptions_[i + 1] = i;
					}
				}

				EditorGUI.indentLevel++;
				sectionTransitionOverridesListProperty_.arraySize = EditorGUILayout.IntField("Size", sectionTransitionOverridesListProperty_.arraySize);
				for( int i = 0; i < sectionTransitionOverridesListProperty_.arraySize; ++i )
				{
					DrawSectionTransitionOverrideParam(sectionTransitionOverridesListProperty_.GetArrayElementAtIndex(i));
				}
				EditorGUI.indentLevel--;
			}

			// mode transition overrides
			modeTransitionOverridesListProperty_.isExpanded = EditorGUILayout.Foldout(modeTransitionOverridesListProperty_.isExpanded, "Mode Transition Overrides");
			if( modeTransitionOverridesListProperty_.isExpanded )
			{
				if( modeTransitionOverridesListProperty_.arraySize > 0 )
				{
					modeListDisplayOptionsWithAny_ = new string[music.Modes.Length + 1];
					modeListValueOptions_ = new int[modeListDisplayOptionsWithAny_.Length];
					modeListDisplayOptionsWithAny_[0] = "Any";
					modeListValueOptions_[0] = -1;
					for( int i = 0; i < music.Modes.Length; ++i )
					{
						modeListDisplayOptionsWithAny_[i + 1] = music.Modes[i].Name;
						modeListValueOptions_[i + 1] = i;
					}
				}

				EditorGUI.indentLevel++;
				modeTransitionOverridesListProperty_.arraySize = EditorGUILayout.IntField("Size", modeTransitionOverridesListProperty_.arraySize);
				for( int i = 0; i < modeTransitionOverridesListProperty_.arraySize; ++i )
				{
					DrawModeTransitionOverrideParam(modeTransitionOverridesListProperty_.GetArrayElementAtIndex(i));
				}
				EditorGUI.indentLevel--;
			}

			EditorGUI.indentLevel--;
		}

		// options
		showOptions_ = EditorGUILayout.Foldout(showOptions_, "Options");
		if( showOptions_ )
		{
			EditorGUI.indentLevel++;
			//showHelpTexts_ = EditorGUILayout.Toggle("Show Help", showHelpTexts_);
			showUnitPerBarBeat_ = EditorGUILayout.Toggle("Show Meters in UnitPerBeat/Bar", showUnitPerBarBeat_);
			EditorGUI.indentLevel--;
		}
		
		// player
		if( UnityEditor.EditorApplication.isPlaying )
		{
			GUILayout.BeginHorizontal();
			{
				if( GUILayout.Button("Play") )
				{
					if( music.IsPlaying == false )
					{
						music.Play();
					}
				}
				if( GUILayout.Button(music.State == Music.PlayState.Suspended ? "Resume" : "Suspend") )
				{
					if( music.State == Music.PlayState.Suspended )
					{
						music.Resume();
					}
					else
					{
						music.Suspend();
					}
				}
				if( GUILayout.Button("Stop") )
				{
					if( music.IsPlaying || music.State == Music.PlayState.Suspended )
					{
						music.Stop();
					}
				}
			}
			GUILayout.EndHorizontal();
			
			if( music.State == Music.PlayState.Playing || music.State == Music.PlayState.Suspended )
			{
				EditorGUILayout.BeginHorizontal();
				int sequenceIndex = EditorGUILayout.Popup("Section", music.SequenceIndex, sectionListDisplayOptions_);
				if( sequenceIndex != music.SequenceIndex )
				{
					music.SetHorizontalSequenceByIndex(sequenceIndex);
				}
				if( music.NextSectionIndex >= 0 && music.NextSectionIndex != music.SectionIndex )
				{
					EditorGUILayout.LabelField("to " + sectionListDisplayOptions_[music.NextSectionIndex], GUILayout.Width(120));
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				int modeIndex = EditorGUILayout.Popup("Mode", music.ModeIndex, modeListDisplayOptions_);
				if( modeIndex != music.ModeIndex )
				{
					music.SetVerticalMixByIndex(modeIndex);
				}
				if( music.NextModeIndex >= 0 && music.NextModeIndex != music.ModeIndex )
				{
					EditorGUILayout.LabelField("to " + modeListDisplayOptions_[music.NextModeIndex], GUILayout.Width(120));
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Slider("Seek Bar", music.MusicalTime, music.CurrentSection.EntryPointTiming.IsZero() ? 0 : -1, music.CurrentSection.ExitPointTiming.Bar);
				EditorUtility.SetDirty(serializedObject.targetObject);
			}
			else
			{
				previewSectionProperty_.intValue = EditorGUILayout.Popup("Section", previewSectionProperty_.intValue, sectionListDisplayOptions_);
				previewModeProperty_.intValue = EditorGUILayout.Popup("Mode", previewModeProperty_.intValue, modeListDisplayOptions_);
				seekBarProperty_.intValue = EditorGUILayout.IntSlider("Seek Bar", seekBarProperty_.intValue, music.Sections[previewSectionProperty_.intValue].EntryPointTiming.IsZero() ? 0 : -1, music.Sections[previewSectionProperty_.intValue].ExitPointTiming.Bar);
			}
		}

		serializedObject.ApplyModifiedProperties();
	}

	void DrawContainerButtons(SerializedProperty containerProp)
	{
		EditorGUILayout.BeginHorizontal();
		if( GUILayout.Button("Add") )
		{
			containerProp.InsertArrayElementAtIndex(containerProp.arraySize);
		}
		if( GUILayout.Button("Remove") && containerProp.arraySize > 0 )
		{
			containerProp.DeleteArrayElementAtIndex(containerProp.arraySize - 1);
		}
		if( GUILayout.Button("Clear") && containerProp.arraySize > 0 )
		{
			containerProp.ClearArray();
		}
		EditorGUILayout.EndHorizontal();
	}

	void DrawSection(SerializedProperty sectionProp, MusicSection section)
	{
		SerializedProperty nameProp = sectionProp.FindPropertyRelative("Name");
		sectionProp.isExpanded = EditorGUILayout.Foldout(sectionProp.isExpanded, nameProp.stringValue);
		if( sectionProp.isExpanded )
		{
			EditorGUI.indentLevel++;

			// name
			EditorGUILayout.PropertyField(nameProp);

			// type
			SerializedProperty transitionTypeProp = sectionProp.FindPropertyRelative("TransitionType");
			MusicSection.AutoTransitionType transitionType = (MusicSection.AutoTransitionType)transitionTypeProp.enumValueIndex;
			if( transitionType == MusicSection.AutoTransitionType.Transition )
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(transitionTypeProp, new GUIContent("Transition Type"), GUILayout.Width(EditorGUIUtility.labelWidth + 80));
				SerializedProperty destinationProp = sectionProp.FindPropertyRelative("TransitionDestinationIndex");
				destinationProp.intValue = EditorGUILayout.Popup(destinationProp.intValue, sectionListDisplayOptions_);
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				EditorGUILayout.PropertyField(transitionTypeProp);
			}

			// clips
			SerializedProperty clipListProp = sectionProp.FindPropertyRelative("Clips");
			clipListProp.arraySize = Mathf.Max(1, numTracksProperty_.intValue);
			for( int i = 0; i < clipListProp.arraySize; ++i )
			{
				EditorGUILayout.PropertyField(clipListProp.GetArrayElementAtIndex(i), new GUIContent("Track " + i.ToString()));
			}

			// meters
			SerializedProperty meterListProp = sectionProp.FindPropertyRelative("Meters");
			meterListProp.isExpanded = EditorGUILayout.Foldout(meterListProp.isExpanded, "Meters " + (section != null && section.IsValid ? section.Meters[0].ToString() : ""));
			if( meterListProp.isExpanded )
			{
				EditorGUI.indentLevel++;
				meterListProp.arraySize = Mathf.Max(1, EditorGUILayout.IntField("Size", meterListProp.arraySize));
				for( int i = 0; i < meterListProp.arraySize; ++i )
				{
					DrawMeter(meterListProp.GetArrayElementAtIndex(i));
				}
				EditorGUI.indentLevel--;
			}

			// markers
			SerializedProperty markerListProp = sectionProp.FindPropertyRelative("Markers");
			EditorGUILayout.PropertyField(markerListProp, includeChildren: true);

			// transition
			DrawSectionTransitionParam(sectionProp);

			// entry / exit / loop
			if( section != null && section.IsValid )
			{
				EditorGUILayout.PropertyField(sectionProp.FindPropertyRelative("EntryPointTiming"));
				EditorGUILayout.PropertyField(sectionProp.FindPropertyRelative("ExitPointTiming"));
				float minValue = section.EntryPointTiming.Bar;
				float maxValue = section.EntryPointTiming.Bar + section.ExitPointTiming.Bar;
				EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, 0, section.ClipEndTiming.Bar);
				if( transitionType == MusicSection.AutoTransitionType.Loop )
				{
					EditorGUILayout.PropertyField(sectionProp.FindPropertyRelative("LoopStartTiming"));
					EditorGUILayout.PropertyField(sectionProp.FindPropertyRelative("LoopEndTiming"));
					minValue = section.EntryPointTiming.Bar + section.LoopStartTiming.Bar;
					maxValue = section.EntryPointTiming.Bar + section.LoopEndTiming.Bar;
					EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, 0, section.ClipEndTiming.Bar);
				}
			}

			EditorGUI.indentLevel--;
		}
	}

	void DrawMeter(SerializedProperty meterProp)
	{
		SerializedProperty startBarProp = meterProp.FindPropertyRelative("StartBar");
		SerializedProperty tempoProp = meterProp.FindPropertyRelative("Tempo");
		SerializedProperty unitPerBarProp = meterProp.FindPropertyRelative("UnitPerBar");
		SerializedProperty unitPerBeatProp = meterProp.FindPropertyRelative("UnitPerBeat");
		SerializedProperty numeratorProp = meterProp.FindPropertyRelative("Numerator");
		SerializedProperty denominatorProp = meterProp.FindPropertyRelative("Denominator");

		int startBar = startBarProp.intValue;
		double tempo = tempoProp.doubleValue;
		int unitPerBar = unitPerBarProp.intValue;
		int unitPerBeat = unitPerBeatProp.intValue;
		int numerator = numeratorProp.intValue;
		int denominator = denominatorProp.intValue;

		meterProp.isExpanded = EditorGUILayout.Foldout(meterProp.isExpanded, string.Format("Meter {0}～ ({1}/{2}, {3:F2})", startBar, numerator, denominator, tempo));
		if( meterProp.isExpanded )
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(startBarProp);
			EditorGUILayout.PropertyField(tempoProp);

			if( showUnitPerBarBeat_ )
			{
				EditorGUILayout.PropertyField(unitPerBeatProp);
				EditorGUILayout.PropertyField(unitPerBarProp);

				if( unitPerBar != unitPerBarProp.intValue || unitPerBeat != unitPerBeatProp.intValue )
				{
					MusicMeter.CalcMeterByUnits(unitPerBeatProp.intValue, unitPerBarProp.intValue, out numerator, out denominator);
					numeratorProp.intValue = numerator;
					denominatorProp.intValue = denominator;
				}
			}
			else
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.LabelField("Meter", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
					EditorGUIUtility.labelWidth = 1;
					EditorGUILayout.PropertyField(numeratorProp, GUILayout.Width(55));
					denominatorProp.intValue = EditorGUILayout.IntPopup("/", denominatorProp.intValue, MeterDenominatorCandidatesString, MeterDenominatorCandidates);
					EditorGUIUtility.labelWidth = 0;

					if( numerator != numeratorProp.intValue || denominator != denominatorProp.intValue )
					{
						MusicMeter.CalcMeterByFraction(numeratorProp.intValue, denominatorProp.intValue, out unitPerBeat, out unitPerBar);
						unitPerBeatProp.intValue = unitPerBeat;
						unitPerBarProp.intValue = unitPerBar;
					}
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.indentLevel--;
		}
	}
	static string[] MeterDenominatorCandidatesString = new string[3] { "／4", "／8", "／16" };
	static int[] MeterDenominatorCandidates = new int[] { 4, 8, 16 };

	void DrawSyncType(SerializedProperty syncTypeProp, SerializedProperty syncFactorProp)
	{
		Music.SyncType syncType = (Music.SyncType)syncTypeProp.enumValueIndex;
		if( Music.SyncType.Unit <= syncType && syncType <= Music.SyncType.Marker )
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(syncFactorProp, new GUIContent("Sync Type"));
			EditorGUILayout.PropertyField(syncTypeProp, GUIContent.none);
			EditorGUILayout.EndHorizontal();
		}
		else
		{
			EditorGUILayout.PropertyField(syncTypeProp);
		}
	}

	void DrawSectionTransitionParam(SerializedProperty ownerProp)
	{
		SerializedProperty transitionProp = ownerProp.FindPropertyRelative("Transition");

		transitionProp.isExpanded = EditorGUILayout.Foldout(transitionProp.isExpanded, "Transition");
		if( transitionProp.isExpanded )
		{
			EditorGUI.indentLevel++;

			// sync
			DrawSyncType(ownerProp.FindPropertyRelative("SyncType"), ownerProp.FindPropertyRelative("SyncFactor"));

			// fade
			SerializedProperty useFadeOutProp = transitionProp.FindPropertyRelative("UseFadeOut");
			SerializedProperty useFadeInProp = transitionProp.FindPropertyRelative("UseFadeIn");
			EditorGUILayout.PropertyField(useFadeOutProp);
			if( useFadeOutProp.boolValue )
			{
				EditorGUILayout.PropertyField(transitionProp.FindPropertyRelative("FadeOutTime"));
				EditorGUILayout.PropertyField(transitionProp.FindPropertyRelative("FadeOutOffset"));
			}
			EditorGUILayout.PropertyField(useFadeInProp);
			if( useFadeInProp.boolValue )
			{
				EditorGUILayout.PropertyField(transitionProp.FindPropertyRelative("FadeInTime"));
				EditorGUILayout.PropertyField(transitionProp.FindPropertyRelative("FadeInOffset"));
			}

			EditorGUI.indentLevel--;
		}
	}

	void DrawSectionTransitionOverrideParam(SerializedProperty transitionOverrideProp)
	{
		SerializedProperty fromProp = transitionOverrideProp.FindPropertyRelative("FromSectionIndex");
		SerializedProperty toProp = transitionOverrideProp.FindPropertyRelative("ToSectionIndex");

		string fromStr = sectionListDisplayOptionsWithAny_[fromProp.intValue + 1];
		string toStr = sectionListDisplayOptionsWithAny_[toProp.intValue + 1];

		transitionOverrideProp.isExpanded = EditorGUILayout.Foldout(transitionOverrideProp.isExpanded, string.Format("from {0} to {1}", fromStr, toStr));
		if( transitionOverrideProp.isExpanded )
		{
			EditorGUI.indentLevel++;

			fromProp.intValue = EditorGUILayout.IntPopup("From", fromProp.intValue, sectionListDisplayOptionsWithAny_, sectionListValueOptions_);
			toProp.intValue = EditorGUILayout.IntPopup("To", toProp.intValue, sectionListDisplayOptionsWithAny_, sectionListValueOptions_);

			// transition
			DrawSectionTransitionParam(transitionOverrideProp);

			EditorGUI.indentLevel--;
		}
	}

	void DrawMode(SerializedProperty modeProp)
	{
		SerializedProperty nameProp = modeProp.FindPropertyRelative("Name");
		modeProp.isExpanded = EditorGUILayout.Foldout(modeProp.isExpanded, nameProp.stringValue);
		if( modeProp.isExpanded )
		{
			EditorGUI.indentLevel++;

			EditorGUILayout.PropertyField(nameProp);
			EditorGUILayout.PropertyField(modeProp.FindPropertyRelative("TotalVolume"));

			SerializedProperty layerVolumeProp = modeProp.FindPropertyRelative("LayerVolumes");
			{
				EditorGUI.indentLevel++;
				layerVolumeProp.arraySize = Mathf.Max(1, numTracksProperty_.intValue);
				for( int i = 0; i < layerVolumeProp.arraySize; ++i )
				{
					SerializedProperty layerProp = layerVolumeProp.GetArrayElementAtIndex(i);
					layerProp.floatValue = EditorGUILayout.Slider("Layer " + i.ToString(), layerProp.floatValue, 0.0f, 1.0f);
				}
				EditorGUI.indentLevel--;
			}

			EditorGUI.indentLevel--;
		}
	}

	void DrawModeTransitionParam(SerializedProperty modeTransitionProp)
	{
		modeTransitionProp.isExpanded = EditorGUILayout.Foldout(modeTransitionProp.isExpanded, "Transition");
		if( modeTransitionProp.isExpanded )
		{
			EditorGUI.indentLevel++;

			// sync
			SerializedProperty syncTypeProp = modeTransitionProp.FindPropertyRelative("SyncType");
			DrawSyncType(syncTypeProp, modeTransitionProp.FindPropertyRelative("SyncFactor"));

			EditorGUILayout.PropertyField(modeTransitionProp.FindPropertyRelative("TimeUnitType"));
			EditorGUILayout.PropertyField(modeTransitionProp.FindPropertyRelative("FadeTime"));
			if( (Music.SyncType)syncTypeProp.enumValueIndex != Music.SyncType.Immediate )
			{
				EditorGUILayout.PropertyField(modeTransitionProp.FindPropertyRelative("FadeOffset"));
			}

			EditorGUI.indentLevel--;
		}
	}

	void DrawModeTransitionOverrideParam(SerializedProperty modeTransitionOverrideProp)
	{
		SerializedProperty fromProp = modeTransitionOverrideProp.FindPropertyRelative("FromModeIndex");
		SerializedProperty toProp = modeTransitionOverrideProp.FindPropertyRelative("ToModeIndex");

		string fromStr = modeListDisplayOptionsWithAny_[fromProp.intValue + 1];
		string toStr = modeListDisplayOptionsWithAny_[toProp.intValue + 1];

		modeTransitionOverrideProp.isExpanded = EditorGUILayout.Foldout(modeTransitionOverrideProp.isExpanded, string.Format("from {0} to {1}", fromStr, toStr));
		if( modeTransitionOverrideProp.isExpanded )
		{
			EditorGUI.indentLevel++;

			fromProp.intValue = EditorGUILayout.IntPopup("From", fromProp.intValue, modeListDisplayOptionsWithAny_, modeListValueOptions_);
			toProp.intValue = EditorGUILayout.IntPopup("To", toProp.intValue, modeListDisplayOptionsWithAny_, modeListValueOptions_);

			DrawModeTransitionParam(modeTransitionOverrideProp.FindPropertyRelative("Transition"));

			EditorGUI.indentLevel--;
		}
	}
}