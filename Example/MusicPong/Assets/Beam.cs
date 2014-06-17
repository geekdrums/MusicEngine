using UnityEngine;
using System.Collections;

public class Beam : MonoBehaviour {

    public float beamScale, beamPow;

    Timing shotTiming;

    public void Initialize( Timing timing )
    {
        this.shotTiming = timing;
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        float d = Music.MusicalTimeFrom( shotTiming );
        if( Mathf.Abs( d ) <= 4.0f )
        {
            //effect
            float x = Mathf.Pow( beamPow, (-d - 0.5f) * beamScale );
            transform.localScale = new Vector3( 1.0f + x, 1.0f, 0.03f + 1.0f / x );
            renderer.material.color = ( d > 0 ? Color.Lerp( Color.red, Color.clear, (d - 1) / 2.0f ) : Color.white );
        }
        else
        {
            transform.localScale = Vector3.one;
            renderer.material.color = Color.white;

            if( Music.Just > shotTiming )
            {
                Destroy( gameObject );
            }
        }

        if( Music.IsJustChangedAt( shotTiming ) )
        {
            //shot
            Field.instance.ball.OnShot();
            if( Mathf.Abs( this.transform.position.x - Field.instance.paddle.transform.position.x ) < Field.instance.paddle.BarRadius )
            {
                Field.instance.paddle.Damage();
                audio.Play();
            }
        }
	}
}
