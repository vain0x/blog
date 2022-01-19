---
title: break可能なループを書こう - 関数型プログラミングのテクニック
type: "post"
date: 2017-03-05 00:00:00
url: 2017-03-05/breakable-loop-in-fsharp
tags:
  - FSharp
  - CSharp
  - 関数型プログラミング
canonicalUrl: http://qiita.com/vain0x/items/ddef84e3959dffe6a88d
---

[Qiita](http://qiita.com/vain0x/items/ddef84e3959dffe6a88d)

手続き型言語を使う人に「F# のループ (for/while) は `break` できない」というと驚かれるかもしれません。筆者は驚きました。途中で終了する可能性のあるループを書けなくて困りそうですが、その心配は不要です。F# では **末尾再帰関数** を使って、`break` や `continue` のあるループと同じことができるからです。

<!--more-->

## 例1: 無条件の無限ループ

まずは最も簡単な例を挙げます。`break` も `continue` も使わないループを、末尾再帰関数を使って書いてみましょう。ひたすら `yes` を出力するだけの、通称 yes コマンドです。C# だとこうですね。

```csharp
public void YesAll()
{
    while (true)
    {
        Console.WriteLine("yes");
    }
}
```

これなら F# の `while` でも同様に書けますが、練習のため末尾再帰で書いてみましょう。

```fsharp
let yesAll () =
    let rec loop () =
        Console.WriteLine("yes")
        loop ()
    loop ()
```

コードの説明をします。冒頭の ``let yesAll () = ...`` は関数の定義で、残りの部分がその本体です。``let rec loop () = ...`` も関数の定義ですが (関数の中に関数！)、`rec` キーワードがついているので `loop` 関数は再帰的(**rec**ursive)です (再帰的な関数については後述)。

F# は字下げに依存した構文を採用しています。`loop` 関数の定義は、字下げが `let` と同じ深さに戻ったところで終わります。すなわち、`loop` の本体は2行からなり、字下げの減っている最後の ``loop ()`` は含まれません。

`loop` 関数の定義の後ろにある ``loop ()`` は、事実上 `yesAll` が最初に実行する式ですが、単に loop 関数を起動するだけです。

再帰についてもう少し解説します。`loop` の本体は「yes を出力する」式と「loop を起動する」式の2つからなります。loop の中で loop を起動すると、また「yes を出力する」と「loop を起動する」を実行することになります。すなわち、

    loop を起動する
    = yes を出力して、次に loop を起動する
    = yes を出力して、次に yes を出力して、次に loop を起動する
    = yes を出力して、次に yes を出力して、次に yes を出力して、次に loop を起動する
    = ……

という計算になります。無限ループですね。実際、これは C# で書いたものとほぼ同じループにコンパイルされるはずです。

### 機械的翻訳

C# の視点から loop 関数を解釈する手段を紹介します。まず C# のコードのうち、`while` の「末尾」に到達する部分に `continue` を挿入します。

```csharp
// C#

public void YesAll()
{
    while (true)
    {
        Console.WriteLine("yes"); // body
        continue; // 追加
    }
}
```

そして、一定の規則で F# のコードに変換します。

```fsharp
// F#

let yesAll () =
    let rec loop () =            // while (true) {
        Console.WriteLine("yes") //   body
        loop ()                  //   continue;
    loop ()                      // }
```

つまり、 ``while (true) { ... }`` を

```fsharp
    let rec loop =
        ...
    loop()
```

に置き換え、ループの本体のうち `continue` を ``loop ()`` に置き換えました。

こうして簡単に末尾再帰バージョンを手に入れることができます。

## 例2: 停止する無限ループ

先ほどの例で基本的な考え方を会得したので、`break` を使うループの例を見ていきましょう。

以下の関数は、標準入力から行を読み込むたびに「叫ぶ」(大文字に変換して出力する)ものです。入力を読み切ったら自動的に終了することにします。

```csharp
// C#

private void ScreamLine(string line)
{
    Console.WriteLine(line.ToUpper() + "!");
}

public void Scream()
{
    while (true)
    {
        // 標準入力から1行を取得する。
        // 入力の終端に到達していたら、null が返る。
        var line = Console.ReadLine();
        if (line == null) break;

        ScreamLine(line);
    }
}
```

これを少しだけ変形します。`if` 文には常に `else` をつけ、末尾に到達する部分に `continue` を挿入します。

```csharp
// C#

public void Run()
{
    while (true)
    {
        var line = Console.ReadLine();
        if (line == null)
        {
            break;
        }
        else
        {
            ScreamLine(line);
            continue;
        }
    }
}
```

そして、前述の変換に加えて `break` を `()` に置き換えると完成です：

```fsharp
// F#

let screamLine (line: string) =
    Console.WriteLine(line.ToUpper() + "!")

let scream () =
    let rec loop () =                   // while (true) {
        let line = Console.ReadLine()   //   var line = ...;
        if line = null then             //   if (line == null) {
            ()                          //     break;
        else                            //   } else {
            scream line                 //     ...;
            loop ()                     //     continue;
                                        //   }
    loop ()                             // }
```

`()` と `break` が対応することのイメージが分からないと思いますが、直接的な対応はないので、 ``scream ()`` の挙動を説明します。

この関数を ``scream ()`` のように起動すると、先程の ``yesAll ()`` と同じく ``loop ()`` が開始します。`loop` の結果は、読み取った行が `null` なら (= 入力が終了したら) `()` (ユニットという名前の定数) で、そうでなければ `else` 節の値になります。`else` 節では、入力を叫んだあとループをやり直しますが、yes コマンドとは違っていつかは入力が終わり `()` が返ってきます。結局、標準入出力の副作用を除けば

    scream ()
    = loop ()
    = ...
    = loop ()
    = ()

となります。`()` という「ループを伸ばさない式」のおかげで `loop` の連鎖が切れて、つまりループが終了して (`break` して) いますね。

C# ではループを続けるのに `continue` は書かなくていい代わりに、終わらせるときに `break` を書きます。一方この末尾再帰関数のやりかたでは、ループを終わらせるのに `break` は書かなくていい代わりに、続けるときに ``loop ()`` を書くのです。

## 例3: 有限回のループ

前の2つの例の `loop` 関数は、引数として `()` を受け取りましたが、実際は任意の引数が使えます。ループの「状態」を引数で持ち運ぶのはよくあることです。

最後の例は、リストの各要素を1行ずつ表示していくループです。 F# だと `for` で書けますが、練習のため末尾再帰関数で書きます。

```csharp
public void PrintList<X>(IReadOnlyList<X> list)
{
    var index = 0;
    while (index < list.Count)
    {
        Console.WriteLine("{0}", list[index]);
        index++;
    }
}
```

今回は `while` にガード節がありますが、これは `if` と `break` に簡単に分解できて、次のように変形できます：

```csharp
// C#

public void PrintList<X>(IReadOnlyList<X> list)
{
    var index = 0;
    while (true)
    {
        if (index < list.Count) // 条件節
        {
            Console.WriteLine("{0}", list[index]);
            index++;
            continue; // 追加
        }
        else
        {
            break; // 条件不成立 (index >= list.Count) なら終了。
        }
    }
}
```

```fsharp
// F#

let printList (list: IReadOnlyList<_>) =
    let rec loop index =                           // while (true) {
        if index < list.Count then                 //    if (...) {
            Console.WriteLine("{0}", list.[index]) //      ...;
            loop (index + 1)                       //      continue;
        else                                       //    } else {
            ()                                     //      break;
                                                   //    }
    loop 0                                         // }
```

`loop` 関数の実行を簡単に追ってみましょう。`list` を長さ 3 のリストとすると、

    loop 0
    = 0 < 3 なら、出力して loop 1
    = loop 1
    = 1 < 3 なら、出力して loop 2
    = loop 2
    = 2 < 3 なら、出力して loop 3
    = loop 3
    = 3 < 3 なら、出力して loop 4
    = (なにもしない)
    = ()

となります。

## まとめ: 変換規則

1. `for` や `foreach` は `while` に書き換える。
1. `while` の条件があれば、 ``while (true)`` にする代わりに ``if (! 条件) break;`` を挿入する。
1. すべての `if` 文に `else` 節を補う。
1. `while` の末尾に到達する部分に `continue` を補う。
1. `break` を `()` にする。

## おわりに

本稿では、C# のループを比較的単純に末尾再帰関数に変換できることを紹介しました。実際のところ、再帰は再帰として理解したほうがいいと思いますが、こういう小手先のテクニックを用いて理解を深めていくのも1つの手かもしれません。
