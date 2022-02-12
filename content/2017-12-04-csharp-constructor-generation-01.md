---
title: '[C#] コンストラクタの自動生成 #1'
tags:
    - 古い記事
#   - CSharp
#   - Essay
type: "post"
date: 2017-12-04 00:31:05
url: 2017-12-04/csharp-constructor-generation-01
deprecated: true
---

**追記**: 現在は推奨していません。

<!--more-->

## 要約
C# のフィールドや自動実装プロパティーの定義から、完全コンストラクターを自動生成したい。

## 例
- 引数の値をフィールドやプロパティーに代入する処理だけからなるコンストラクターを **完全コンストラクター** と呼ぶ。
    - 現実のコンストラクターは入力検証 (参照型の値が null でないことを検査したり、数値の範囲を検査したりすること) を含むことが多いが、それでも完全コンストラクターと呼ぶかは **調整中** 。

次のようなクラスがあるとき、

```csharp
public sealed class Person
{
    public int Age { get; }
    public string Name { get; }
}
```

次のように完全コンストラクターを機械的に生成したい。

```csharp
public sealed class Person
{
    public int Age { get; }
    public string Name { get; }

    // 生成されたコンストラクター
    public Person(int age, string name)
    {
        Age = age;
        Name = name;
    }
}
```

## 関連ツールと言語サポート
完全コンストラクターを生成するツールはすでにある。

### **[RecordConstructorGenerator](https://github.com/ufcpp/RecordConstructorGenerator)**
Visual Studio 2015 でアナライザーが使えるようになり、静的コード生成などの処理を IDE 上で行なうのが簡単になった。この「RecordConstructorGenerator」アナライザーをインストールしておくと、手軽に完全コンストラクターを自動生成できる。

極めて便利だが、しかし、少しだけ不満がある。

- 自動実装プロパティーへの代入処理は生成されるが、フィールドへの代入処理も生成してほしい。
- null 検査を生成してほしいことがある。

実装が分かりやすくて、後述の自作アナライザーの参考になった。

### Visual Studio 2017
Visual Studio 2017 では、標準で完全コンストラクターを生成する機能が追加された。(Roslyn の機能か？)

どのフィールド・プロパティーについて代入処理を生成するかを選べるので便利。また、同じ方法で、同値性の定義なども生成できる。とはいえ、メンバーが追加・削除されたときの自動修正にまでは対応していない。

### 自作/[RecordTypeAnalyzer](httpshttps://github.com/vain0x/playground/tree/4cafe15dd57d0df68c8bc9c8864b6f6fcf7dbba5/2017-07-26-record-type-analyzer)
困ったときは自作。

完全コンストラクターやコピーコンストラクタ―のみならず、等価性や比較の自動生成など、 F# のレコード型が備えるような、さまざまな機能を自動生成し、さらに定義の変更に合わせて自動修正する機能を備えたアナライザー、というのを目指した。

ところで、レコード型で等価性などを定義できるのは、型がイミュータブルだからだ。イミュータブルでない型に構造的同値性を定義するわけにはいかないので、型がイミュータブルかどうかを自動で判定する機能を実装した。問題は、完全コンストラクターを実装したい対象の型は、イミュータブルなものに限らないということだ。このあたりを考慮すると、仕様が複雑化してくる。

やや詰め込みすぎて、アナライザー初心者にはつらくなってきた。そこで、いったん仕様を縮小して動くものを作ることにした。いま思うと、この時点で「RecordConstructorGenerator を改造する」方向に進まなかったのは悪い癖だろう。

### 自作/[BoilerplateConstructorGenerator](https://github.com/vain0x/playground/tree/4cafe15dd57d0df68c8bc9c8864b6f6fcf7dbba5/2017-08-16-boilerplate-ctor-gen)

完全コンストラクターやコピーコンストラクタ―の自動生成と、定義が変わったときの自動修正機能を備えたアナライザー、というのを目指した。

自動修正はコンストラクターの実装を分析して検出しようとしたが、前述のようにバリデーションの扱いのせいで誤検出がひどかったのでやめた。 ``// analyzer: complete-constructor`` というマジックコメントを挿入することで検出することにした。

現時点ではこれを使っている。コピーコンストラクターの自動生成機能がないことと、なぜか変更点がないのに変更が検出されている状態になる不具合があるのでリリースに至っていないが、なかなか快適だ。

## 余談: レコード型
C# にも言語機能としてレコード型を導入する計画がある。 C# 8.0 候補とのことなので期待して待っていよう。

[Champion "Records" · Issue #39 · dotnet/csharplang](https://github.com/dotnet/csharplang/issues/39)
