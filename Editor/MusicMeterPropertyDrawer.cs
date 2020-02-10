using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;


[CustomPropertyDrawer(typeof(MusicMeter))]
public class MusicMeterPropertyDrawer : PropertyDrawer
{
	private class PropertyData
	{
		// 現状MusicADX2とMusicWwiseでは使わないプロパティなので非表示
		//public SerializedProperty StartBar;
		public SerializedProperty Tempo;
		public SerializedProperty UnitPerBeat;
		public SerializedProperty UnitPerBar;
		public SerializedProperty Numerator;
		public SerializedProperty Denominator;
	}

	private Dictionary<string, PropertyData> _propertyDataPerPropertyPath = new Dictionary<string, PropertyData>();
	private PropertyData _property;
	private bool showUnitPerBarBeat_ = false;

	private void Init(SerializedProperty property)
	{
		if( _propertyDataPerPropertyPath.TryGetValue(property.propertyPath, out _property) )
		{
			return;
		}

		_property = new PropertyData();
		//_property.StartBar = property.FindPropertyRelative("StartBar");
		_property.Tempo = property.FindPropertyRelative("Tempo");
		_property.UnitPerBeat = property.FindPropertyRelative("UnitPerBeat");
		_property.UnitPerBar = property.FindPropertyRelative("UnitPerBar");
		_property.Numerator = property.FindPropertyRelative("Numerator");
		_property.Denominator = property.FindPropertyRelative("Denominator");
		_propertyDataPerPropertyPath.Add(property.propertyPath, _property);
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		Init(property);

		var fieldRect = position;
		fieldRect.height = GetPropertyHeight(property, label);

		using( new EditorGUI.PropertyScope(fieldRect, label, property) )
		{
			//int startBar = _property.StartBar.intValue;
			double tempo = _property.Tempo.doubleValue;
			int unitPerBar = _property.UnitPerBar.intValue;
			int unitPerBeat = _property.UnitPerBeat.intValue;
			int numerator = _property.Numerator.intValue;
			int denominator = _property.Denominator.intValue;

			fieldRect.height = EditorGUIUtility.singleLineHeight;

			property.isExpanded = EditorGUI.Foldout(fieldRect, property.isExpanded, string.Format("{0} ({1}/{2}, {3:F2})", label.text, numerator, denominator, tempo));
			if( property.isExpanded )
			{
				EditorGUI.indentLevel++;

				//fieldRect.y += EditorGUIUtility.singleLineHeight;
				//EditorGUI.PropertyField(fieldRect, _property.StartBar);
				fieldRect.y += EditorGUIUtility.singleLineHeight;
				EditorGUI.PropertyField(fieldRect, _property.Tempo);

				fieldRect.y += EditorGUIUtility.singleLineHeight;
				showUnitPerBarBeat_ = EditorGUI.Toggle(fieldRect, "Show Meter In UnitPerBar/Beat", showUnitPerBarBeat_);

				if( showUnitPerBarBeat_ )
				{
					fieldRect.y += EditorGUIUtility.singleLineHeight;
					EditorGUI.PropertyField(fieldRect, _property.UnitPerBeat);
					fieldRect.y += EditorGUIUtility.singleLineHeight;
					EditorGUI.PropertyField(fieldRect, _property.UnitPerBar);

					if( unitPerBar != _property.UnitPerBar.intValue || unitPerBeat != _property.UnitPerBeat.intValue )
					{
						MusicMeter.CalcMeterByUnits(_property.UnitPerBeat.intValue, _property.UnitPerBar.intValue, out numerator, out denominator);
						_property.Numerator.intValue = numerator;
						_property.Denominator.intValue = denominator;
					}
				}
				else
				{
					fieldRect.y += EditorGUIUtility.singleLineHeight;
					fieldRect = EditorGUI.PrefixLabel(fieldRect, GUIUtility.GetControlID(FocusType.Passive),  new GUIContent("Meter"));
					fieldRect.x -= 16;
					fieldRect.width = 55;
					EditorGUI.PropertyField(fieldRect, _property.Numerator, GUIContent.none);
					fieldRect.x += 60;
					fieldRect.width = 60;
					_property.Denominator.intValue = EditorGUI.IntPopup(fieldRect, _property.Denominator.intValue, MeterDenominatorCandidatesString, MeterDenominatorCandidates);

					if( numerator != _property.Numerator.intValue || denominator != _property.Denominator.intValue )
					{
						MusicMeter.CalcMeterByFraction(_property.Numerator.intValue, _property.Denominator.intValue, out unitPerBeat, out unitPerBar);
						_property.UnitPerBeat.intValue = unitPerBeat;
						_property.UnitPerBar.intValue = unitPerBar;
					}
				}

				EditorGUI.indentLevel--;
			}
		}
	}
	static string[] MeterDenominatorCandidatesString = new string[3] { "／4", "／8", "／16" };
	static int[] MeterDenominatorCandidates = new int[] { 4, 8, 16 };

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		if( property.isExpanded )
		{
			if( showUnitPerBarBeat_ )
			{
				return EditorGUIUtility.singleLineHeight * 5;
			}
			else
			{
				return EditorGUIUtility.singleLineHeight * 4;
			}
		}
		return EditorGUIUtility.singleLineHeight;
	}
}