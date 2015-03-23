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
            GetComponent<Renderer>().material.color = ( d > 0 ? Color.Lerp( Color.red, Color.clear, (d - 1) / 2.0f ) : Color.white );
        }
        else
        {
            //hide or destroy
            if( Music.Just < shotTiming )
            {
                transform.localScale = Vector3.one;
                GetComponent<Renderer>().material.color = Color.white;
            }
            else
            {
                Destroy( gameObject );
            }
        }

        if( Music.IsJustChangedAt( shotTiming ) )
        {
            //shot
            Field.instance.ball.OnShot();
            if( Field.instance.paddle.OnShot( this.transform.position.x ) )
            {
                GetComponent<AudioSource>().Play();
            }
        }
	}
}
