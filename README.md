MusicEngine
===========

**概要:**  
UnityまたはADX2LEで音楽を再生する時に、  
Musicコンポーネントを追加してテンポや拍子を指定する事で  
音楽のタイミングを取得したりクオンタイズさせたり  
音楽に合わせた演出を簡単に作れるようになります。  


**注意点:**  
テンポが途中で変わったり、拍子が途中で変わったり、  
あるいは1拍目がデータの最初に無かったりするものには現在対応しておりません。  
また、音楽は常にひとつしか鳴らないという仮定から、すべての情報は  
Music.◯◯という形でstaticメンバから参照できます。  
複数の音楽を切り替える際も、Music.Play(Musicを含んだGameObjectの名前)を使ってください。  


**利用方法:**  
Example/AssetsフォルダのtestScene.unityをご参照ください。  
SampleCode.csにいくつかの利用方法と、少し変わったことをしても  
正確に拍を取得できる事を確認できる機能を入れました。  
自分のプロジェクトに利用する場合は、単にMusic.csを追加するだけでOKです。  
何か使い方や変数の意味がわからなかったり、使いにくかったりする場合は  
@geekdrums までご連絡いただけると、今後の改善の参考にさせていただきます。  


**Abstract:**  
You can get musical timing information with Unity( or ADX2LE ).  
Add Music component and set your tempo and beats, you can get timing,  
quantize play, make music sync effect easilly and so on.  

**Notes:**  
I didn't support tempo change or beat change in a music.  
And, I also didn't support a music don't start with the first beat.  
I suppose music is only one in any timing, so you can access any information by  
Music.SomethingYouWant ( means this class has many static members ).  
If you want to change music, please use Music.Play( "music gameObject name" ).  

**How To Use:**  
Please see Example/Assets/testScene.unity and SampleCode.cs.  
Only you have to do is Add Music.cs to your project.  
If you have any questions or suggestions, please let me(@geekdrums) know.  
