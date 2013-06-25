using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Chord : IEnumerable<int>
{
	//=============コンストラクタ=================
	public Chord( params int[] tones )
	{
		chord = new List<int>( tones );
		chord.TrimExcess();
		chord.Sort();
	}
	public Chord( List<int> tones )
	{
		chord = tones;
		chord.TrimExcess();
		chord.Sort();
	}
	public Chord( string cho ) : this( Tone.Parse( cho ) ) { }

	//中身はこれだけ
	private List<int> chord;

	//===========プロパティなど============
	public int numTones { get { return chord.Count; } }
	public int this[int i]
	{
		private set { }
		get
		{
			if ( 0 <= i && i < numTones )
				return chord[i];
			else throw new ApplicationException( "コードのインデックス外にアクセスしようとしました" );
		}
	}
	public void AddTone( int t ) { if ( !chord.Contains( t ) ) { chord.Add( t ); } }
	#region IEnumerator実装
	public IEnumerator<int> GetEnumerator()
	{
		foreach ( int t in chord )
			yield return t;
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
	#endregion

	//============コード変更の関数=================
	/// <summary>
	/// basechord上で演奏されることを仮定したtones（和音）を
	/// this.chord上での和音に変換する。
	/// </summary>
	/// <param name="tones">変更する音</param>
	/// <param name="basschord">変更する音が入っているMotifがそのまま演奏されるときのコード</param>
	/// <returns></returns>
	public Chord Fix( Chord tones, Chord basechord )
	{
		if ( basechord == null ) return tones;
		if ( numTones == basechord.numTones )
		{
			bool isSame = true;
			for ( int i = 0; i < numTones; i++ )
			{
				if ( this[i] != basechord[i] )
				{
					isSame = false;
					break;
				}
			}
			if ( isSame )
			{
				return tones;
			}
		}

		int length = tones.numTones;
		int[] result = new int[length];
		float toneOnBasechord;
		int toneOnDefaultOctave;
		int octave;
		for ( int i = 0; i < length; i++ )
		{

			//元のコードにおいて、何オクターブ目の音か
			octave = ( tones[i] - basechord[0] + ( tones[i] - basechord[0] < 0 ? 1 : 0 ) ) / Tone.OCTAVE + ( tones[i] - basechord[0] < 0 ? -1 : 0 );
			//考えた末にこうなった。すなわち、今簡単のためbasechrd[0]==0とすると、-12～-1はオクターブが－１としたいのだが、
			//-1～-11は/Tone.OCTAVEしたときに０になるけど、-12だけは－１になってしまうので、それを避けるためにあえて先に一つずらしている。

			toneOnBasechord = 256.0f;//適当に、再生しようとしたらエラーが出る音にしておく。
			{
				#region 元のコード上のどの位置にあるかを算出
				// これにbasschord上の音の位置を格納する。簡単に言うと、例えばソの音のCmaj上の位置は2、
				// ファの音なら1.33、ミなら1、レが0.5、ドが0、などというように、中間の音はその前後の音との比によって与えられる。
				//オクターブをすべて揃える。
				int t = tones[i];
				if ( t % Tone.OCTAVE == basechord[0] % Tone.OCTAVE ) toneOnBasechord = 0.0f;//場合わけ。このときはわかりきっているのですぐ設定して抜ける
				else
				{
					t -= Tone.OCTAVE * octave;

					//コード上を探索
					int l = basechord.numTones;
					bool flag = false;//場所がループの中で見つかったかを格納。見つかってなければ、一番高い音より上にあるのでその場合の処理に。
					for ( int j = 0; j < l; j++ )
					{
						if ( basechord[j] == t )
						{
							toneOnBasechord = (float)j;
							flag = true;
							break;
						}
						else if ( t < basechord[j] )
						{
							float d = basechord[j] - basechord[j - 1];// j != 0 は↑で保障されている。
							toneOnBasechord = (float)( j - 1 ) + (float)( t - basechord[j - 1] ) / d;
							flag = true;
							break;
						}
					}
					//コードの一番下の音のオクターブ上と、一番上の音との間で計算。
					if ( !flag )
					{
						float d2 = basechord[0] + Tone.OCTAVE - basechord[l - 1];
						toneOnBasechord = (float)( l - 1 ) + (float)( t - basechord[l - 1] ) / d2;
					}
				}
				#endregion
			}
			toneOnDefaultOctave = 256;//適当に、再生しようとしたらエラーが出る音にしておく。
			{
				#region 得られた位置に対して、自身のコード上での位置の音を計算する。
				int l = this.numTones;
				bool flag = false;//音がループの中で見つかったかを格納。見つかってなければ、その上の音を設定する。
				//コード上を探索
				for ( int j = 0; j < l; j++ )
				{
					if ( toneOnBasechord == (float)j )
					{
						toneOnDefaultOctave = this[j];
						flag = true;
						break;
					}
					else if ( toneOnBasechord < j )
					{
						float dec = toneOnBasechord - ( j - 1 );//decimal（は型名だから使えないので）つまり1以下の数になる
						int d = this[j] - this[j - 1];
						toneOnDefaultOctave = this[j - 1] + (int)( dec * d );
						flag = true;
						break;
					}
				}
				//見つからなかった＝元となるコードの音の方が音数が多い（もしくは同じだが最後のインデックスの音より高い音）
				if ( !flag )
				{
					float dec2 = ( toneOnBasechord - (float)( l - 1 ) ) / (float)( basechord.numTones - l + 1 );
					int d2 = this[0] + Tone.OCTAVE - this[l - 1];
					toneOnDefaultOctave = this[l - 1] + (int)( dec2 * d2 );
				}
				#endregion
			}

			//以上の情報から、自身のコード上の対応する音を算出。
			result[i] = toneOnDefaultOctave + octave * Tone.OCTAVE;
		}

		return new Chord( result );
	}

	//===========平行移動、展開した音の作成============
	public Chord Transpose( int t )
	{
		if ( t == 0 ) return new Chord( this.chord );
		else
		{
			int[] res = new int[numTones];
			for ( int i = 0; i < res.Length; i++ )
			{
				res[i] = this[i] + t;
			}
			return new Chord( res );
		}
	}
	/// <summary>
	/// コードを上に展開する
	/// </summary>
	public Chord RollUp( int time )
	{
		List<int> res = new List<int>( this );
		for ( int i = 0; i < time; i++ )
		{
			res[0] += Tone.OCTAVE;
			res.Sort();
		}
		return new Chord( res );
	}
	/// <summary>
	/// コードを下に展開する
	/// </summary>
	public Chord RollDown( int time )
	{
		List<int> res = new List<int>( this );
		for ( int i = 0; i < time; i++ )
		{
			chord[numTones - 1] -= Tone.OCTAVE;
			chord.Sort();
		}
		return new Chord( res );
	}
	/// <summary>
	/// 和音を作る
	/// </summary>
	/// <param name="t">重ねる音との距離</param>
	/// <returns>新しい和音</returns>
	public Chord Harmonize( int t )
	{
		if ( t == 0 ) return new Chord( this.chord );
		else
		{
			int[] res = new int[numTones*2];
			for ( int i = 0; i < numTones; i++ )
			{
				res[i * 2] = this[i];
				res[i * 2 + 1] = this[i] + t;
			}
			return new Chord( res );
		}
	}

	private static char[] comma = { ',' };
	//==============staticメソッド=================
	/// <summary>
	/// Parse("C00,C00 E00 G00,D00 F00")
	/// みたく、コードの音をカンマで分けて渡す。
	/// </summary>
	/// <param name="chordphrase"></param>
	/// <returns></returns>
	public static Chord[] Parse( string chordphrase )
	{
		string[] strs = chordphrase.Split( comma, StringSplitOptions.RemoveEmptyEntries );
		Chord[] chords = new Chord[strs.Length];
		for ( int i = 0; i < chords.Length; i++ )
			chords[i] = new Chord( strs[i] );
		return chords;
	}
	public static Chord MakeChord( int tone, string option )
	{
		if ( option == "maj" )
		{
			return new Chord( tone, tone + 4, tone + 7 );
		}
		else if ( option == "min" )
		{
			return new Chord( tone, tone + 3, tone + 7 );
		}
		else if ( option == "sus4" )
		{
			return new Chord( tone, tone + 5, tone + 7 );
		}
		else if ( option == "7" )
		{
			return new Chord( tone, tone + 4, tone + 7, tone + 10 );
		}
		else if ( option == "maj7" )
		{
			return new Chord( tone, tone + 4, tone + 7, tone + 11 );
		}
		else if ( option == "min7" )
		{
			return new Chord( tone, tone + 3, tone + 7, tone + 10 );
		}
		else if ( option == "dim" )
		{
			return new Chord( tone, tone + 3, tone + 6, tone + 9 );
		}
		else throw new ApplicationException( "そのコードは自分で入力してくれ" );
	}
}
