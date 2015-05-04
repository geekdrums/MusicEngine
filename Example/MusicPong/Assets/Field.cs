using UnityEngine;
using System.Collections;

public class Field : MonoBehaviour {

    public static Field instance;
    public static float FieldLength { get { return instance.fieldLength; } }
    public static float BarRadius { get { return instance.paddle.transform.localScale.x * 10.0f / 2.0f; } }
    public static float BallRadius { get { return instance.ball.transform.localScale.x * 10.0f; } }

    public Ball ball;
    public Paddle paddle;
    public GameObject EndBar;
    public GUIText retryText, clearText, titleText;

    public float fieldLength;
    public float lerpMusicalTime;
    public float accelerate;
    public Color EndBarColor, EndBarLerpColor;
    public Material fieldMaterial;

    [System.Serializable]
    public class Level
    {
        public Color materialColor, lerpMaterialColor;
        public Color BGColor, BGChangeColor;
    }

    public Level[] levels;

    Vector3 endbarInitialScale, endbarGameOverScale;
    Camera MainCamera;
    int currentLevel;

	// Use this for initialization
	void Start () {
        instance = this;

        endbarInitialScale = EndBar.transform.localScale;
        endbarGameOverScale = endbarInitialScale;
        endbarGameOverScale.z = 3.0f;
        retryText.enabled = false;
        clearText.enabled = false;
        MainCamera = GameObject.Find( "Main Camera" ).GetComponent<Camera>();
        MainCamera.backgroundColor = levels[0].BGColor;
        fieldMaterial.color = levels[0].materialColor;
	}
	
	// Update is called once per frame
	void Update () {
        switch( Music.CurrentSection.Name )
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

            if( Music.IsJustChangedSection( "Play2" ) )
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
            if( Music.IsNearChangedAt( 2 ) )
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
            EndBar.GetComponent<Renderer>().material.color = Color.white;
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

        if( Music.IsJustChangedAt( Music.CurrentSection.StartBar + 3 ) )
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
            retryText.text = "Survived level: " + currentLevel + "/8\nClick to restart.";
            EndBar.GetComponent<Renderer>().material.color = EndBarLerpColor;
        }

        EndBar.transform.localScale = Vector3.Lerp( EndBar.transform.localScale, endbarGameOverScale, 0.2f );

		if( Music.IsJustChangedAt(Music.CurrentSection.StartBar + 3) )
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
        ball.OnRestart();
        paddle.OnRestart();
        retryText.enabled = false;
        clearText.enabled = false;
        MainCamera.backgroundColor = levels[0].BGColor;
        fieldMaterial.color = levels[0].materialColor;
        currentLevel = 0;
    }

    void UpdateColor()
    {
        currentLevel = (Music.Just.Bar - 2) / 4;

		EndBar.GetComponent<Renderer>().material.color = Color.Lerp(EndBarColor, EndBarLerpColor, Music.MusicalCos(lerpMusicalTime));
		fieldMaterial.color = Color.Lerp(levels[currentLevel].materialColor, levels[currentLevel].lerpMaterialColor, Music.MusicalCos(lerpMusicalTime));

        if( Music.IsJustChangedBar() )
        {
            MainCamera.backgroundColor = Music.Just.Bar % 2 == 0 ?
                levels[currentLevel].BGColor : levels[currentLevel].BGChangeColor;
        }
    }
}
