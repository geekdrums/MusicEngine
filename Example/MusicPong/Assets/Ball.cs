using UnityEngine;
using System.Collections;

public class Ball : MonoBehaviour {

    public Vector3 velocity;
    public GameObject beamPrefab;
    public Timing[] beamTimings;

    Vector3 initialPosition, initialVelocity;

    int shotMusicalTime;
    int beamIndex;

	// Use this for initialization
	void Start () {
        initialPosition = transform.position;
        initialVelocity = velocity;
	}

	// Update is called once per frame
	void Update () {
        if( Music.CurrentSection.Name.StartsWith( "Play" ) )
        {
            CheckShot();

            if( shotMusicalTime > 0 )
            {
                if( Music.IsJustChanged )
                {
                    --shotMusicalTime;
                }
            }
            else
            {
                UpdatePosition();
            }
        }
	}

    void CheckShot()
    {
        if( beamIndex < beamTimings.Length && Music.IsNearChanged
            && ( beamTimings[beamIndex].CurrentMusicalTime - 4 == Music.Near.CurrentMusicalTime ) )
        {
            Beam beam = (Instantiate( beamPrefab ) as GameObject).GetComponent<Beam>();
            beam.transform.parent = transform;
            beam.transform.localPosition = Vector3.zero;
            beam.Initialize( beamTimings[beamIndex] );
            ++beamIndex;
        }
    }

    void UpdatePosition()
    {
        transform.position += velocity * Time.deltaTime;
        if( transform.position.x <= -Field.FieldLength || Field.FieldLength <= transform.position.x )
        {
            //side wall
            velocity.x = Mathf.Abs( velocity.x ) * -Mathf.Sign( transform.position.x );
            Music.QuantizePlay( GetComponent<AudioSource>() );
        }
        if( Field.FieldLength <= transform.position.y )
        {
            //roof
            velocity.y = Mathf.Abs( velocity.y ) * -Mathf.Sign( transform.position.y );
            Music.QuantizePlay( GetComponent<AudioSource>(), 7 );
        }
        else if( transform.position.y <= -Field.FieldLength )
        {
            //floor
            Music.SeekToSection( "GameOver" );
        }
    }

    public void OnShot()
    {
        if( Music.CurrentSection.Name == "Play3" )
        {
            shotMusicalTime = 2;
        }
        else
        {
            shotMusicalTime = 3;
        }
    }

    public void OnRestart()
    {
        shotMusicalTime = 0;
        beamIndex = 0;
        transform.position = initialPosition;
        velocity = initialVelocity;
    }
}
