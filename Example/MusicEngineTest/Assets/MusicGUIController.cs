using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Music))]
public class MusicGUIController : MonoBehaviour {

	public Rect PlayButton;
	public Rect SectionButton;
	public Rect SectionLeft, SectionRight;

	int sectionIndex_ = 0;

	// Use this for initialization
	void Start () {
		Music.Play(name);
	}
	
	// Update is called once per frame
	void OnGUI () {
		if( GUI.Button(PlayButton, Music.IsPlaying || Music.IsTransitioning ? "Stop" : "Play") )
		{
			if( Music.IsPlaying ) Music.Stop();
			else Music.Play(name, Music.GetSection(sectionIndex_).Name);
		}

		if( GUI.Button(SectionLeft, "<") )
		{
			--sectionIndex_;
			if( sectionIndex_ < 0 ) sectionIndex_ += Music.SectionCount;
		}
		if( GUI.Button(SectionRight, ">") )
		{
			++sectionIndex_;
			sectionIndex_ %= Music.SectionCount;
		}
		if( GUI.Button(SectionButton, ( Music.IsTransitioning ? "transition..." : Music.GetSection(sectionIndex_).Name ) ) )
		{
			Music.SetNextSection(sectionIndex_, Music.SyncType.NextBar);
		}
	}
}
