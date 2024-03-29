---
title: すごーい！ きみはプログラミング言語を実装できるフレンズなんだね
type: "post"
date: 2017-02-12 00:00:00
url: 2017-02-12/friends-lang
tags:
  - プログラミング言語
  - 言語処理系
  - Prolog
  - ネタ
canonicalUrl: http://qiita.com/vain0x/items/6d3b75f667d3ec7f1d2a
---

[Qiita](http://qiita.com/vain0x/items/6d3b75f667d3ec7f1d2a)

ジャパリパークのみんなー！ フレンズのためのプログラミング言語ができたよー！

<!--more-->

## サンプルコード
### Socrates
```
すごーい！ かばんちゃん は ヒトの フレンズ なんだね！

すごーい！ あなた が ヒトの フレンズ なら
あなた は しっぽのない フレンズ なんだね！

だれ が しっぽのない フレンズ なんだっけ？
```

出力:

```
「だれ」は「かばんちゃん」、
あってる？ (y/n)y
やったー！
```

「かばんちゃん は ヒトの フレンズ」で「ヒトの フレンズ は しっぽのない フレンズ」だから「かばんちゃん は しっぽのない フレンズ」なんだね！

たーのしー！

### FizzBuzz
```
すごーい！ 0 は 自然数の フレンズ なんだね！
すごーい！ きみ が 自然数の フレンズ なら
きみ の 次 も 自然数の フレンズ なんだね！

すごーい！ 0 は 3の倍数の フレンズ なんだね！
すごーい！ きみ が 3の倍数の フレンズ なら
きみ の 次 の 次 の 次 も 3の倍数の フレンズ なんだね！

すごーい！ 0 は 5の倍数の フレンズ なんだね！
すごーい！ きみ が 5の倍数の フレンズ なら
きみ の 次 の 次 の 次 の 次 の 次 も 5の倍数の フレンズ なんだね！

すごーい！ 0 は 15の倍数の フレンズ なんだね！
すごーい！ きみ が 15の倍数の フレンズ なら
きみ の 次 の 次 の 次 の 次 の 次 の 次 の 次 の
次 の 次 の 次 の 次 の 次 の 次 の 次 の 次 も
15の倍数の フレンズ なんだね！

すごーい！ きみ が 15の倍数の フレンズ なら
きみ と FizzBuzz は FizzBuzz フレンズ なんだね！ たーのしー！
すごーい！ きみ が 3の倍数の フレンズ なら
きみ と Fizz は FizzBuzz フレンズ なんだね！ たーのしー！
すごーい！ きみ が 5の倍数の フレンズ なら
きみ と Buzz は FizzBuzz フレンズ なんだね！ たーのしー！
すごーい！ きみ が 自然数の フレンズ なら
きみ と きみ は FizzBuzz フレンズ なんだね！

すごーい！ 0 は きみ の 次 より 小さい フレンズ なんだね！
すごーい！ きみ が かのじょ より 小さい フレンズ なら
きみ の 次 は かのじょ の 次 より 小さい フレンズ なんだね！

すごーい！ きみ が かのじょ より 小さい フレンズ なら
きみ が きみ と かのじょ との 間の フレンズ なんだね！
すごーい！ きみ が かのじょ より 小さい フレンズ で
かれ が きみ の 次 と かのじょ との 間の フレンズ なら
かれ も きみ と かのじょ との 間の フレンズ なんだね！

それ が 0 と 16 との 間の フレンズ で
それ と だれ が FizzBuzz フレンズ なんだっけ？
```

出力:

```
「それ」は0、
「だれ」は「FizzBuzz」、
あってる？ (y/n)n
---------------
「それ」は1、
「だれ」は1、
あってる？ (y/n)n
---------------
「それ」は2、
「だれ」は2、
あってる？ (y/n)n
---------------
「それ」は3、
「だれ」は「Fizz」、
あってる？ (y/n)n
---------------
「それ」は4、
「だれ」は4、
あってる？ (y/n)n
---------------
「それ」は5、
「だれ」は「Buzz」、
あってる？ (y/n)n
---------------
「それ」は6、
「だれ」は「Fizz」、
あってる？ (y/n)n
---------------
「それ」は7、
「だれ」は7、
あってる？ (y/n)n
---------------
「それ」は8、
「だれ」は8、
あってる？ (y/n)n
---------------
「それ」は9、
「だれ」は「Fizz」、
あってる？ (y/n)n
---------------
「それ」は10、
「だれ」は「Buzz」、
あってる？ (y/n)n
---------------
「それ」は11、
「だれ」は11、
あってる？ (y/n)n
---------------
「それ」は12、
「だれ」は「Fizz」、
あってる？ (y/n)n
---------------
「それ」は13、
「だれ」は13、
あってる？ (y/n)n
---------------
「それ」は14、
「だれ」は14、
あってる？ (y/n)n
---------------
「それ」は15、
「だれ」は「FizzBuzz」、
あってる？ (y/n)y
やったー！
```

わー！ すごーい！

## 解説
Qiita なので技術的な話をします。

処理系のソースコードは [GitHub](https://github.com/vain0x/friends-lang) にあります。

### すごーい！ 文
「すごーい！」で始まる文は、事実を表明するものです。例えば、

```
すごーい！ かばんちゃん は ヒトの フレンズ なんだね！
```

は命題「かばんちゃん は ヒトの フレンズ」が真であることを表明します。ここで「かばんちゃん」という単語は定義されていませんが、アトムという文字列のようなものなので、使用することができます。

「ヒトの」は述語の名前です。

### なんだっけ？ 文
「なんだっけ？」で終わる文は、「すごーい！」文で表明した事実を用いて、指定された命題を真にするような割り当てが存在するかを探索します。例えば、

```
だれ が ヒトの フレンズ なんだっけ？
```

と問いかけると、変数「だれ」 [^var_naming] に適当なアトムを代入することで、命題「だれ が ヒトの フレンズ」を真にできるかどうかを判定し、命題を真にするような変数への割り当てを出力します。この例では、「ヒトの フレンズ」述語に関する真偽は、前述のすごーい！文でしか述べられていないので、「だれ」＝「かばんちゃん」という割り当てのみがこの命題を真にします。

[^var_naming]: 変数は、「だれ」や「あなた」などのいくつかの予約語と、アンダーバーで始まる名前の識別子です。それ以外の識別子はアトムになります。

### 条件つきのすごーい！文
条件つきのすごーい！文は、ある命題が真であるときに、別の命題が成り立つことを表明します。例えば、

```
すごーい！ きみ が 自然数の フレンズ なら
きみ の 次 も 自然数の フレンズ なんだね！
```

は変数「きみ」に適当な割り当てがなされて命題「きみ が 自然数の フレンズ」が真だと判定されたなら、命題「きみ の 次 は 自然数の フレンズ」も真である、と述べています。ちなみに「～は」「～が」「～も」はすべて同じです。

式 ``きみ の 次`` は複合項というものです。Friends 言語では、式は「評価」されるものではなく、他の式とパターンマッチするためだけのものです。複合項の特徴は、単に「同一の形をした複合項とのみマッチする」ということです。すなわち、``きみ の 次`` という式は、``? の 次`` という形の式にだけマッチする式を表しています。

Friends 言語において ``x の 次`` という形の式は少しだけ特別で、自然数 [^natural_numbers] は「0 の 次 の 次 の ... の 次」という複合項に解釈されます。すなわち、``0`` はアトム ですが、``1`` は ``0 の 次``、``2`` は ``0 の 次 の 次`` という項を表します。

[^natural_numbers]: ここでは0以上の整数のこと。

例えば、次の質問をすると、

```
すごーい！ 0 は 自然数の フレンズ なんだね！
すごーい！ きみ が 自然数の フレンズ なら
きみ の 次 も 自然数の フレンズ なんだね！

3 は 自然数の フレンズ なんだっけ？
```

3 は 0 ではありませんから、2つ目の条件つきのすごーい！文を使って真偽を判定することになります。
この命題が真であるには、``きみ の 次`` = ``3`` = ``0 の 次 の 次 の 次`` でなければなりませんから、**パターンマッチ** により、``きみ`` = ``0 の 次 の 次`` = 2 という割り当てが得られます。
加えて、条件「2 が 自然数の フレンズ」が成り立たなければなりませんが、これも同様の方法で推論できます。
したがって、「0 は 自然数の フレンズ」、だから「1 は 自然数の フレンズ」、なので「2 は 自然数の フレンズ」、ゆえに「3 は 自然数 のフレンズ」という推論ができます。

3の倍数、5の倍数、15の倍数の判定も同様に可能です。

### たーのしー！節
FizzBuzz フレンズの定義を再掲します。

```
すごーい！ きみ が 15の倍数の フレンズ なら
きみ と FizzBuzz は FizzBuzz フレンズ なんだね！ たーのしー！
すごーい！ きみ が 3の倍数の フレンズ なら
きみ と Fizz は FizzBuzz フレンズ なんだね！ たーのしー！
すごーい！ きみ が 5の倍数の フレンズ なら
きみ と Buzz は FizzBuzz フレンズ なんだね！ たーのしー！
すごーい！ きみ が 自然数の フレンズ なら
きみ と きみ は FizzBuzz フレンズ なんだね！
```

n の FizzBuzz 表現 (Fizz/Buzz/FizzBuzz/n) を fizzbuzz(n) と書くことにします。FizzBuzz フレンズは、簡単にいえば、すべての自然数 n について命題「n と fizzbuzz(n) が FizzBuzz フレンズ」が真になるような述語です。

すごーい！文の末尾につく「たーのしー！」節は、その文が命題を真にすると、残りのすごーい！文を探索しない、という機能を持ちます。すなわち、命題「x と y が FizzBuzz フレンズ」の真偽を判定するとき、「x が 15の倍数の フレンズ」だと分かったら、残りの3つの文 (3の倍数なら、5の倍数なら、自然数なら、というやつ) は無視する、ということです。もしこれがないと、「15 と FizzBuzz が FizzBuzz フレンズ」であると同時に、「15 と Fizz も FizzBuzz フレンズ」でもあることになってしまいます。

### 助詞
```
すごーい！ 0 は きみ の 次 より 小さい フレンズ なんだね！
すごーい！ きみ が かのじょ より 小さい フレンズ なら
きみ の 次 は かのじょ の 次 より 小さい フレンズ なんだね！
```

「x は y より 小さい フレンズ」は「x と y が 小さい フレンズ」を表す糖衣構文です。
それ以外は前述の通りです。

### 非決定性
```
すごーい！ きみ が かのじょ より 小さい フレンズ なら
きみ が きみ と かのじょ との 間の フレンズ なんだね！
すごーい！ きみ が かのじょ より 小さい フレンズ で
かれ が きみ の 次 と かのじょ との 間の フレンズ なら
かれ も きみ と かのじょ との 間の フレンズ なんだね！
```

「x で y」という命題は、x と y が両方真である、という命題 (論理積) を表します。

「間の」フレンズのおもしろいところは、それを真にする割り当てが複数あることです。というのも、命題「あなた が 0 と 3 との 間の フレンズ」を真にする割り当ては、``あなた`` = 0, 1, 2 の3つあります。このように解が一意に定まらないことを、推論の **非決定性** といいます。

推論を追ってみましょう。命題「0 が 3 より 小さい フレンズ」は、「小さい」の定義により成り立つと分かります (詳細は割愛)。命題「あなた が 0 と 3 との 間の フレンズ」が真になるには、まず1つ目のすごーい！文により、結論「あなた が あなた と 3 との 間の フレンズ」とマッチする場合が考えられます。パターンマッチにより `あなた` = `0` という割り当てが得られます。

この文には「たーのしー！」節がないので、次のすごーい！文も適用できます。「あなた が 0 と 3 との 間の フレンズ」と「かれ も きみ と かのじょ との 間の フレンズ」を見比べて、「あなた = かれ」「0 = きみ」「3 = かのじょ」という割り当てを得ます。そして、条件「かれ が きみ の 次 と かのじょ との 間の フレンズ」＝「あなた が 0 の 次 と 3 との 間の フレンズ」が真であるかを判定します。再び1つ目のすごーい！文により、``あなた`` = ``0 の 次`` = `1` という割り当てを得ます。これの繰り返しになります。

## Prolog
実際のところ、Friends 言語は Prolog という「論理型プログラミング言語」の文法を変更したものです。興味があれば、Prolog について調べてみてください。Prolog の処理系には [AZ-Prolog](https://az-prolog.com/) や [SWI-Prolog](http://www.swi-prolog.org/) などがあります。

## 動作環境
(追記 3/7) Friends 言語を Mac や Linux でも動くようにしてもらえました。

[すごーい！きみはフレンズ言語をDockerizeできるフレンズなんだね！](http://leko.jp/archives/933)

## 参考文献
- [けものフレンズプロジェクト｜公式サイト](http://kemono-friends.jp/)
- [Prolog実践入門 - AIに特化した老舗言語](http://qiita.com/ShunIchikawa/items/6449f492dc38a7201162)
