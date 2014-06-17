using UnityEngine;
using System.Collections;

public class Field : MonoBehaviour {

    public static Field instance;

    public Ball ball;
    public Paddle paddle;
    public GameObject EndBar;
    public GUIText retryText, clearText, titleText;

    public float fieldLength;
    public float lerpMusicalTime;
    public float accelerate;

    public Material fieldMaterial;

    public Color LerpColor1, LerpColor2;
    public Color ClearColor;
    public Color Lv1Color, Lv1BGColor;
    public Color Lv2Color, Lv2BGColor;
    public Color Lv3Color, Lv3BGColor;
    public Color Lv4Color, Lv4BGColor;
    public Color Lv5Color, Lv5LerpColor, Lv5BGColor;
    public Color Lv6Color, Lv6LerpColor, Lv6BGColor;
    public Color Lv7Color, Lv7LerpColor, Lv7BGColor, Lv7BGChangeColor;
    public Color Lv8Color, Lv8LerpColor, Lv8BGColor, Lv8BGChangeColor;

    Vector3 endbarInitialScale, endbarGameOverScale;
    Camera MainCamera;
    int level;

	// Use this for initialization
	void Start () {
        instance = this;
        endbarInitialScale = EndBar.transform.localScale;
        endbarGameOverScale = endbarInitialScale;
        endbarGameOverScale.z = 3.0f;
        retryText.enabled = false;
        clearText.enabled = false;
        MainCamera = GameObject.Find( "Main Camera" ).GetComponent<Camera>();
        MainCamera.backgroundColor = Lv1BGColor;
        fieldMaterial.color = Lv1Color;
	}
	
	// Update is called once per frame
	void Update () {
        switch( Music.CurrentSection.name )
        {
        case "Start":
            UpdateStart();
            break;
        case "Clear":
            UpdateClear();
            break;
        case "GameOver":
            UpdateGameOver();
            break;
        default://Play
            UpdateColor();

            if( Music.IsJustChangedAt( 18 ) )//level4
            {
                ball.velocity *= accelerate;
            }
            break;
        }
	}

    void UpdateStart()
    {
        EndBar.transform.localScale = Vector3.Lerp( EndBar.transform.localScale, endbarInitialScale, 0.1f );
        if( titleText.enabled )
        {
            titleText.color = Color.Lerp( Color.white, Color.clear, (float)Music.MusicalTime / 16.0f - 1.0f );
            if( Music.IsNowChangedAt( 2 ) )
            {
                titleText.enabled = false;
            }
        }

        if( Input.GetMouseButtonDown( 0 ) )
        {
            Music.SeekToSection( "Play" );
            paddle.OnSeektoPlay();
            EndBar.transform.localScale = endbarInitialScale;
            titleText.enabled = false;
        }
    }

    void UpdateClear()
    {
        if( Music.IsJustChangedSection() )
        {
            EndBar.renderer.material.color = ClearColor;
            clearText.enabled = true;
            if( paddle.totalDamage == 0 )
            {
                clearText.text = "No Damage!\nPerfect Game!!!";
                clearText.fontStyle = FontStyle.Bold;
                fieldMaterial.color = Color.yellow;
            }
            else
            {
                clearText.text = "Game Clear!\n" + "Total Damage: " + paddle.totalDamage;
                clearText.fontStyle = FontStyle.Normal;
                fieldMaterial.color = Color.black;
            }
        }
        EndBar.transform.localScale = Vector3.Lerp( EndBar.transform.localScale, endbarGameOverScale, 0.2f );

        if( Music.IsJustChangedAt( Music.CurrentSection.StartTiming_.bar + 3 ) )
        {
            Music.Stop();
        }
        if( Input.GetMouseButtonDown( 0 ) )
        {
            Restart();
        }
    }

    void UpdateGameOver()
    {
        if( Music.IsJustChangedSection() )
        {
            retryText.enabled = true;
            retryText.text = "Survived level: " + (level - 1).ToString() + "/8\nClick to restart.";
            EndBar.renderer.material.color = LerpColor2;
        }
        EndBar.transform.localScale = Vector3.Lerp( EndBar.transform.localScale, endbarGameOverScale, 0.2f );

        if( Music.IsJustChangedAt( Music.CurrentSection.StartTiming_.bar + 2 ) )
        {
            Music.Stop();
        }
        if( Input.GetMouseButtonDown( 0 ) )
        {
            Restart();
        }
    }

    void Restart()
    {
        Music.SeekToSection( "Start" );
        Music.Play( "Music" );
        ball.Initialize();
        paddle.Initialize();
        retryText.enabled = false;
        clearText.enabled = false;
        MainCamera.backgroundColor = Lv1BGColor;
        fieldMaterial.color = Lv1Color;
        level = 0;
    }

    void UpdateColor()
    {
        EndBar.renderer.material.color = Color.Lerp( LerpColor1, LerpColor2, (Mathf.Cos( Mathf.PI * (float)Music.MusicalTime / lerpMusicalTime ) + 1.0f) / 2.0f );

        level = 1 + (Music.Just.bar - 2) / 4;
        switch( level )
        {
        case 1:
            if( Music.IsJustChangedBar() )
            {
                MainCamera.backgroundColor = Lv1BGColor;
                fieldMaterial.color = Lv1Color;
            }
            break;
        case 2:
            if( Music.IsJustChangedBar() )
            {
                MainCamera.backgroundColor = Lv2BGColor;
                fieldMaterial.color = Lv2Color;
            }
            break;
        case 3:
            if( Music.IsJustChangedBar() )
            {
                MainCamera.backgroundColor = Lv3BGColor;
                fieldMaterial.color = Lv3Color;
            }
            break;
        case 4:
            if( Music.IsJustChangedBar() )
            {
                MainCamera.backgroundColor = Lv4BGColor;
                fieldMaterial.color = Lv4Color;
            }
            break;
        case 5:
            if( Music.IsJustChangedBar() )
            {
                MainCamera.backgroundColor = Lv5BGColor;
            }
            fieldMaterial.color = Color.Lerp( Lv5Color, Lv5LerpColor, (Mathf.Cos( Mathf.PI * (float)Music.MusicalTime / lerpMusicalTime ) + 1.0f) / 2.0f );
            break;
        case 6:
            if( Music.IsJustChangedBar() )
            {
                MainCamera.backgroundColor = Lv6BGColor;
            }
            fieldMaterial.color = Color.Lerp( Lv6Color, Lv6LerpColor, (Mathf.Cos( Mathf.PI * (float)Music.MusicalTime / lerpMusicalTime ) + 1.0f) / 2.0f );
            break;
        case 7:
            if( Music.IsJustChangedBar() )
            {
                MainCamera.backgroundColor = Music.Just.bar % 2 == 0 ? Lv7BGColor : Lv7BGChangeColor;
            }
            fieldMaterial.color = Color.Lerp( Lv7Color, Lv7LerpColor, (Mathf.Cos( Mathf.PI * (float)Music.MusicalTime / lerpMusicalTime ) + 1.0f) / 2.0f );
            break;
        case 8:
            if( Music.IsJustChangedBar() )
            {
                MainCamera.backgroundColor = Music.Just.bar % 2 == 0 ? Lv8BGColor : Lv8BGChangeColor;
            }
            fieldMaterial.color = Color.Lerp( Lv8Color, Lv8LerpColor, (Mathf.Cos( Mathf.PI * (float)Music.MusicalTime / lerpMusicalTime ) + 1.0f) / 2.0f );
            break;
        }
    }
}
