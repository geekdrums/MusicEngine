using UnityEngine;
using System.Collections;

public class SampleCode : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		//music sync UI
		guiText.text = "Just: " + Music.Just.ToString();
		if ( Music.IsJustChangedBar() )
		{
			guiText.material.color = Music.Just.bar % 2 == 0 ? Color.white : Color.yellow;
		}
		int mtBeat = Music.mtBeat;// == 4
		guiText.transform.position = new Vector3(0.5f,
			0.5f + 0.02f * (float)( mtBeat - Music.MusicalTime % mtBeat )/mtBeat, 0 );
		

		//easy quantize play
		if ( Input.GetMouseButtonDown( 0 ) )
		{
			Music.QuantizePlay( new Music.SoundCue( audio ) );
		}

		//you can pause, resume
		if ( Input.GetMouseButtonDown( 1 ) )
		{
			if ( Music.IsPlaying() )
			{
				Music.Pause();
			}
			else
			{
				Music.Resume();
			}
		}

#if !ADX
		//and change pitch, still you can get an accurate timing.
		if ( Input.GetKeyDown( KeyCode.Space ) && Music.IsPlaying() )
		{
			Music.CurrentSource.source.pitch = Music.CurrentSource.source.pitch > 1.0f ? 1.0f : 2.0f;
		}
#endif
	}
}
