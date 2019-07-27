---
title: 'ボツネタ: Immutable/Builder パターンの静的コード生成'
tags:
    - CSharp
    - アナライザー
    - WIP
draft: true
---

## 動機
- 諸般の理由でデータ構造を不変(immutable)にしたい。
- C# にしても F# にしても immutable データ構造の「更新」は構文論的につらい。
- mutable バージョンと immutable バージョンの2通り書いて相互変換するという手がある。
- 手書きするとコードが2重化してかなりつらいので、静的コード生成で頑張ろう。

## 課題
現在、以下の問題をかかえていて、実装フェイズに入れないでいる。何らかの改善をするか、諦めるか。

- immutable ↔ mutable の相互変換を利用して更新する方針だと、「変更されなかった部分のインスタンスを共有できる」という immutable の利点を失うことになる。
- 再帰的に型を変換する場合、変換処理も再帰的なコードにする必要がある。
    - 例えば、 ``List<ImmutableHashSet<T>.Builder>>`` は ``ImmutableArray<ImmutableHashSet<T>>`` にマップしたい。そのときの変換処理は ``list.Select(builder => builder.MakeImmutable()).ToImmutableArray()`` になるが、こういう規則を定義する構文が思いついていない。複雑な宣言を解析する処理の実装は実にめんどくさそう。
- Builder のコンストラクターに一定の制約を与えないと ToBuilder の実装を生成できない。

## 案
まず2つの型が互いに mutable 版/immutable 版 の関係になっていることを宣言する必要がある。例えば string と StringBuilder 、 List[T] と ImmutableArray[T] などだ。以下のように書けばいいだろう。

```csharp
using System.Collections.Immutable;
using System.Text;

// StringBuilder と string が相互変換できることを宣言し、変換方法を提供する。
internal struct StringBuilderToStringConverter
{
    public string ToImmutable(StringBuilder sb) =>
        sb.ToString();

    public StringBuilder FromImmutable(string s) =>
        new StringBuilder(s);
}

// List<T> と ImmutableArray<T> が相互変換できることを宣言し、変換方法を提供する。
internal struct ListToImmutableArrayConverter<T>
{
    public ImmutableArray<T> ToImmutable(List<T> list) =>
        list.ToImmutableArray();

    public List<T> FromImmutable(ImmutableArray<T> array) =>
        array.ToList();
}
```

次に、定義したい型の mutable 版を手書きする。次のように:

```csharp
public sealed class partial Book
{
    public sealed partial class Builder
    {
        public string Title { get; set; }
        public List<string> Tags { get; }
    }
}
```

`Book` は本を表す immutable な型 (になる予定) で、 ``Book.Builder`` がそれの mutable 版になる。`Title` プロパティーは setter を持っているので可変的だ。タグのリストは readonly なフィールドで持っているが、値が動的配列なので可変的といえる。

ここで `Book` ではなく mutable 版の `Builder` を手書きしているのは、 mutable → immutable の変換時は単純にすべてを immutable にすればいいだけである一方、 immutable → mutable の場合は「immutable のままにしたい部分」がしばしばあるからだ。この例でいうと、本の名前やタグを immutable な string 型のまま扱っていて、普通 StringBuilder にはしない。(追記: そもそも string に StringBuilder をマップしたのが間違いなので例が悪いという意見もある。でも似たようなことは他の型にもいえると思う。)

そして、ここから Book のプロパティーの定義と相互変換用のコードを自動生成する:

```csharp
public class partial Book
{
    public partial class Builder
    {
        private Book MakeImmutableCore()
        {
            return new Book(Title, ListToImmutableArrayConverter.ToImmutable(Tags));
        }
    }

    public string Title { get; }
    public ImmutableArray<Tag> Tags { get; }

    public Builder ToBuilder()
    {
        // ...
    }

    public Book(string title, ImmutableArray<Tag> tags)
    {
        Title = title;
        Tags = tags;
    }
}
```

`Book` のプロパティーは `Builder` の各プロパティーの型を immutable 版に置き換えて readonly にしたものになる。 `Book` のコンストラクターはいわゆる完全コンストラクターで、フィールドに値を詰めるだけのものにしている。 `Builder` から `Book` のインスタンスを生成するには `MakeImmutableCore` を使う。データ検証や追加の変換は、単純にこれをラップしたメソッドを用意することで対処できると思う。例えばタイトルが空であるような本を拒絶したければ次のようにする:

```csharp
public partial class Book
{
    public partial class Builder
    {
        public Book MakeImmutable()
        {
            if (string.IsNullOrEmpty(Title))
            {
                throw new InvalidOperationException("タイトルは空欄にできません。");
            }
            return MakeImmutableCore();
        }
    }
}
```

TBD
