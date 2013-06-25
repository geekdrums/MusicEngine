using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class Tone
{
	public static readonly int Cf = C - 1;
	public static readonly int C = 0;//帰納的に定義してるので、ここを変えたら下も全部変えられる。
	public static readonly int Cs = C + 1, Df = C + 1;
	public static readonly int D = Df + 1;
	public static readonly int Ds = D + 1, Ef = D + 1;
	public static readonly int E = Ef + 1;
	public static readonly int Ff = E;
	public static readonly int Es = E + 1;
	public static readonly int F = E + 1;
	public static readonly int Fs = F + 1, Gf = F + 1;
	public static readonly int G = Gf + 1;
	public static readonly int Gs = G + 1, Af = G + 1;
	public static readonly int A = Af + 1;
	public static readonly int As = A + 1, Bf = A + 1;
	public static readonly int B = Bf + 1;
	public static readonly int Bs = B + 1;
	public static readonly int OCTAVE = B - C + 1;

	private static char[] space = { ' ' };

	/// <summary>
	/// ド　レ♭　ミ
	/// というフレーズを作りたかったら、
	/// "C00 Df00 E00"などとする。
	/// マイナスのオクターブはC-1などとする。
	/// </summary>
	/// <param name="phrase"></param>
	/// <returns></returns>
	public static int[] Parse( string phrase )
	{//TODO:和音用のも作るのと、マイナスのオクターブにも対応すること。
		string[] stringTones = phrase.Split( space, StringSplitOptions.RemoveEmptyEntries );
		int[] tones = new int[stringTones.Length];
		int octave;
		int t;
		int i = 0;
		foreach ( string str in stringTones )
		{
			if ( str.Length <= 2 )
			{
				octave = 0;
			}
			else
			{
				octave = int.Parse( str.Substring( str.Length - 2, 2 ) );
			}
			#region バカな探索
			if ( str.StartsWith( "C" ) )
			{
				if ( str[1] == 'f' ) t = Cf;
				else if ( str[1] == 's' ) t = Cs;
				else t = C;
			}
			else if ( str.StartsWith( "D" ) )
			{
				if ( str[1] == 'f' ) t = Df;
				else if ( str[1] == 's' ) t = Ds;
				else t = D;
			}
			else if ( str.StartsWith( "E" ) )
			{
				if ( str[1] == 'f' ) t = Ef;
				else if ( str[1] == 's' ) t = Es;
				else t = E;
			}
			else if ( str.StartsWith( "F" ) )
			{
				if ( str[1] == 'f' ) t = Ff;
				else if ( str[1] == 's' ) t = Fs;
				else t = F;
			}
			else if ( str.StartsWith( "G" ) )
			{
				if ( str[1] == 'f' ) t = Gf;
				else if ( str[1] == 's' ) t = Gs;
				else t = G;
			}
			else if ( str.StartsWith( "A" ) )
			{
				if ( str[1] == 'f' ) t = Af;
				else if ( str[1] == 's' ) t = As;
				else t = A;
			}
			else if ( str.StartsWith( "B" ) )
			{
				if ( str[1] == 'f' ) t = Bf;
				else if ( str[1] == 's' ) t = Bs;
				else t = B;
			}
			else throw new ApplicationException( "invalidな音名の指定" );
			#endregion
			tones[i] = t + octave * OCTAVE;
			i++;
		}
		return tones;
	}
}