using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

// http://light11.hatenadiary.com/entry/2018/08/04/195629
[CustomPropertyDrawer(typeof(Timing))]
public class TimingPropertyDrawer : PropertyDrawer
{
	private class PropertyData
	{
		public SerializedProperty Bar;
		public SerializedProperty Beat;
		public SerializedProperty Unit;
	}

	private Dictionary<string, PropertyData> _propertyDataPerPropertyPath = new Dictionary<string, PropertyData>();
	private PropertyData _property;

	private void Init(SerializedProperty property)
	{
		if( _propertyDataPerPropertyPath.TryGetValue(property.propertyPath, out _property) )
		{
			return;
		}

		_property = new PropertyData();
		_property.Bar = property.FindPropertyRelative("Bar");
		_property.Beat = property.FindPropertyRelative("Beat");
		_property.Unit = property.FindPropertyRelative("Unit");
		_propertyDataPerPropertyPath.Add(property.propertyPath, _property);
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		Init(property);
		var fieldRect = position;
		fieldRect.height = EditorGUIUtility.singleLineHeight;

		// Prefab化した後プロパティに変更を加えた際に太字にしたりする機能を加えるためPropertyScopeを使う
		using( new EditorGUI.PropertyScope(fieldRect, label, property) )
		{
			// ラベルを表示し、ラベルの右側のプロパティを描画すべき領域のpositionを得る
			fieldRect = EditorGUI.PrefixLabel(fieldRect, GUIUtility.GetControlID(FocusType.Passive), label);

			// ここでIndentを0に
			var preIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// プロパティを描画
			var rect = fieldRect;
			rect.width /= 3;
			EditorGUI.PropertyField(rect, _property.Bar, GUIContent.none);

			rect.x += rect.width;
			rect.width /= 2;
			EditorGUI.PropertyField(rect, _property.Beat, GUIContent.none);

			rect.x += rect.width;
			EditorGUI.PropertyField(rect, _property.Unit, GUIContent.none);

			EditorGUI.indentLevel = preIndent;
		}
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		Init(property);

		return EditorGUIUtility.singleLineHeight;
	}
}