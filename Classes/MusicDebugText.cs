using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Music))]
public class MusicDebugText : MonoBehaviour
{
	public TextMesh TextMesh;
	public Text TextUI;

	Music music_;

	void Awake()
	{
		music_ = GetComponent<Music>();
		UpdateText();
	}

	void Update()
	{
		UpdateText();
	}

	void UpdateText()
	{
		string debugText = music_.ToString();
		if( TextMesh != null )
		{
			TextMesh.text = debugText;
		}
		if( TextUI != null )
		{
			TextUI.text = debugText;
		}
	}
}
