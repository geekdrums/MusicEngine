using UnityEngine;
using System.Collections;

public class BGController : MonoBehaviour {

	public Color[] SecionColors;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if( Music.IsJustChangedAt(0) )
		{
			Camera.main.backgroundColor = SecionColors[Music.CurrentSectionIndex];
		}
	}
}
