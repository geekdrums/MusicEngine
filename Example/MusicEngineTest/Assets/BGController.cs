using UnityEngine;
using System.Collections;

public class BGController : MonoBehaviour {

	public Color[] SecionColors;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if( Music.IsJustChangedSection() )
		{
			Camera.main.backgroundColor = SecionColors[Music.CurrentSectionIndex];
		}
	}
}
