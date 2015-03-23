using UnityEngine;
using System.Collections;

public class Paddle : MonoBehaviour {

    public float curve;
    public float damageScale;

    public int totalDamage { get; private set; }

    Camera MainCamera;
    GameObject redBar;
    Vector3 initialScale, initialPosition;

    int damageMusicalTime;
	float reflectTime = 0;


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
        if( Music.CurrentSection.Name == "Start" )
        {
            transform.localScale = new Vector3( initialScale.x * Mathf.Clamp01( (float)Music.MusicalTime / 16.0f ), initialScale.y, initialScale.z );
        }
		else if( Music.CurrentSection.Name.StartsWith("Play") )
        {
            //input
            Ray ray = MainCamera.ScreenPointToRay( Input.mousePosition );
            RaycastHit hit;
            Physics.Raycast( ray.origin, ray.direction, out hit, Mathf.Infinity );
            if( hit.collider != null )
            {
				transform.position = new Vector3(Mathf.Clamp(hit.point.x, -Field.FieldLength + Field.BarRadius, Field.FieldLength - Field.BarRadius), initialPosition.y - reflectTime * 0.6f, 0);
            }

            //reflect
            Ball ball = Field.instance.ball;
            Vector3 d = ball.transform.position - transform.position;
            if( ball.velocity.y < 0 && Mathf.Abs( d.y ) < Field.BallRadius && Mathf.Abs( d.x ) < Field.BarRadius )
            {
                ball.velocity = Vector3.Reflect( ball.velocity, Quaternion.AngleAxis( -curve * d.x / Field.BarRadius, Vector3.forward ) * Vector3.up );
                ball.velocity.y = Mathf.Max( ball.velocity.y, 2.5f );
                Music.QuantizePlay( GetComponent<AudioSource>(), -3 );
				reflectTime = ball.velocity.magnitude * 0.025f;
            }
			if( reflectTime > 0 )
			{
				reflectTime -= Time.deltaTime;
				if( reflectTime < 0 ) reflectTime = 0;
			}

            //damage
            if( damageMusicalTime > 0 )
            {
                redBar.GetComponent<Renderer>().material.color = Music.IsFormerHalf ? Color.red : Color.white;
                if( Music.IsJustChanged )
                {
                    --damageMusicalTime;
                    if( damageMusicalTime <= 0 )
                    {
                        redBar.transform.localScale = Vector3.one;
                    }
				}
				if( Music.IsJustChanged || Music.IsNearChanged )
				{
					if( damageMusicalTime > 4 )
					{
						Vector2 randCircle = Random.insideUnitCircle * 0.03f * (damageMusicalTime -4);
						Camera.main.transform.position = new Vector3(randCircle.x, randCircle.y, -10);
					}
					else
					{
						Camera.main.transform.position = new Vector3(0, 0, -10);
					}
				}
            }
        }
	}

    public bool OnShot( float x )
    {
        if( Mathf.Abs( x - transform.position.x ) < Field.BarRadius )
        {
            transform.localScale = new Vector3( transform.localScale.x * damageScale, transform.localScale.y, transform.localScale.z );
            redBar.transform.localScale = new Vector3( 1.0f / damageScale, 1.0f, 1.0f );
            damageMusicalTime = 8;
            ++totalDamage;
            return true;
        }
        else return false;
    }

    public void OnRestart()
    {
        transform.position = initialPosition;
        totalDamage = 0;
        damageMusicalTime = 0;
        redBar.transform.localScale = Vector3.one;
    }

    public void OnSeektoPlay()
    {
        transform.localScale = initialScale;
    }
}
