using UnityEngine;
using System.Collections;

public class Paddle : MonoBehaviour {

    public float curve;
    public float damageScale;

    public float BarRadius { get { return transform.localScale.x * 10.0f / 2.0f; } }
    public float BallRadius { get { return Field.instance.ball.transform.localScale.x * 10.0f; } }
    public int totalDamage { get; private set; }

    Camera MainCamera;
    GameObject redBar;
    Vector3 initialScale, initialPosition;

    int damageMusicalTime;


    public void Initialize()
    {
        transform.position = initialPosition;
        totalDamage = 0;
        damageMusicalTime = 0;
        redBar.transform.localScale = Vector3.one;
    }

	// Use this for initialization
	void Start () {
        MainCamera = GameObject.Find( "Main Camera" ).GetComponent<Camera>();
        redBar = transform.FindChild( "RedBar" ).gameObject;
        initialPosition = transform.position;
        initialScale = transform.localScale;
	}
	
	// Update is called once per frame
    void Update()
    {
        switch( Music.CurrentSection.name )
        {
        case "Start":
            transform.localScale = new Vector3( initialScale.x * Mathf.Clamp01( (float)Music.MusicalTime / 16.0f ), initialScale.y, initialScale.z );
            break;
        case "Play":
        case "Play2":
            //input
            Ray ray = MainCamera.ScreenPointToRay( Input.mousePosition );
            RaycastHit hit;
            Physics.Raycast( ray.origin, ray.direction, out hit, Mathf.Infinity );
            if( hit.collider != null )
            {
                transform.position = new Vector3( Mathf.Clamp( hit.point.x, -Field.instance.fieldLength + BarRadius, Field.instance.fieldLength - BarRadius ), transform.position.y, 0 );
            }

            //reflect
            Ball ball = Field.instance.ball;
            Vector3 d = ball.transform.position - transform.position;
            if( ball.velocity.y < 0 && Mathf.Abs( d.y ) < BallRadius && Mathf.Abs( d.x ) < BarRadius )
            {
                ball.velocity = Vector3.Reflect( ball.velocity, Quaternion.AngleAxis( -curve * d.x / BarRadius, Vector3.forward ) * Vector3.up );
                ball.velocity.y = Mathf.Max( ball.velocity.y, 2.5f );
                Music.QuantizePlay( audio, -3 );
            }

            //damage
            if( damageMusicalTime > 0 )
            {
                redBar.renderer.material.color = Music.isFormerHalf ? Color.red : Color.white;
                if( Music.isJustChanged )
                {
                    --damageMusicalTime;
                    if( damageMusicalTime <= 0 )
                    {
                        redBar.transform.localScale = Vector3.one;
                    }
                }
            }
            break;
        }
	}

    public void Damage()
    {
        transform.localScale = new Vector3( transform.localScale.x * damageScale, transform.localScale.y, transform.localScale.z );
        redBar.transform.localScale = new Vector3( 1.0f / damageScale, 1.0f, 1.0f );
        damageMusicalTime = 8;
        ++totalDamage;
    }

    public void OnSeektoPlay()
    {
        transform.localScale = initialScale;
    }
}
