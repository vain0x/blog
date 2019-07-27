---
title: IReadOnlyListの列挙時にヒープ領域の使用を避ける
type: "post"
date: 2016-12-21 00:00:00
permalink: lists-with-no-alloc-enumerator
tags:
  - C#
type: post
---

[Qiita](http://qiita.com/vain0x/items/8f89843325ab303c6e04)

IReadOnlyListの列挙時にヒープ領域の使用を避ける方法を紹介します。

## 前提知識
### 値型のボックス化
値型とは何か、ボックス化とは何か、は以下の記事を参照。

- [値型と参照型 - C# によるプログラミング入門 | ++C++; // 未確認飛行 C](http://ufcpp.net/study/csharp/oo_reference.html)
- [ボックス化 - C# によるプログラミング入門 | ++C++; // 未確認飛行 C](http://ufcpp.net/study/csharp/RmBoxing.html)

系として次のことがいえます。

- 値型をインターフェイス型にキャストすると、ボックス化が起こります。
- ~~型変数 `T` に `struct` 制約がついていないとき、値型の値を型 `T` にキャストすると、ボックス化が起こります。~~ (追記: 起こりません。)

### foreach 文のダックタイピング
foreach 文は、渡された列挙対象のコレクションが `GetEnumerator` という名前のメソッドを public に公開していたら、それを使って列挙を行います [^foreach_duck_typing] 。`GetEnumerator` の返値型が ``IEnumerator<_>`` である必要はなく、返値が ``IEnumerator<_>`` にキャストされることもありません。

[^foreach_duck_typing]: ちなみにコレクションが `IEnumerable` や ``IEnumerable<_>`` を実装している必要はありません。また、 `GetEnumerator` が返すインスタンスが `IEnumerator` や ``IEnumerator<_>`` を実装している必要もありません。

## ストーリー
### ヒープ確保を避けたい
``IEnumerable<T>.GetEnumerator()`` の返値型は ``IEnumerator<T>`` なので、新しいオブジェクトを生成して返そうと思うと、ヒープ確保を避けられません。(値型を返そうとすると、ボックス化されてしまう。)

```csharp
void F(IEnumerable<int> xs)
{
    // xs.GetEnumerator() が実行される。
    foreach (var x in xs)
    {
        // ...
    }
}
```

いま、型が ``IEnumerable<T>`` ではなく ``IReadOnlyList<T>`` だとすると、ヒープ確保を回避しつつ列挙を行えます。配列と同じように、インデックスで順次アクセスしていけばいいわけです。

```csharp
void G(IReadOnlyList<int> xs)
{
    // ボックス化は起こらない。
    var count = xs.Count;
    for (var i = 0; i < count; ++i)
    {
        var x = xs[i];
        // ...
    }
}
```

### 実装の重複を避けたい
よりよいパフォーマンスを求めるために、シーケンスの走査を行うメソッドに IReadOnlyList 版と IEnumerable 版の2つを用意することにしたとします。問題となるのは、2つのオーバーロードをどのように実装するかです。

### ラッパーで解決
上記の2つのコードの違いは列挙方法の違いだけなので、列挙子を使って抽象化できます。

列挙子の実装例はここにあります: [StructEnumerator.cs](https://github.com/DotNetKit/DotNetKit.StructEnumerator/blob/v0.1.1/DotNetKit.StructEnumerator/Collections/StructEnumerator.cs)

実際に使ってみましょう。例として、First メソッドと同じものを作ってみます。

```csharp
    static class MyLinq
    {
        static X MyFirstCore<X, TEnumerator>(TEnumerator enumerator)
            where TEnumerator : struct, IEnumerator<X>
        {
            using (enumerator)
            {
                while (enumerator.MoveNext())
                {
                    return enumerator.Current;
                }
            }
            throw new InvalidOperationException();
        }

        // IReadOnlyList を受け取るオーバーロード
        // 列挙子がボックス化されない。
        public static X MyFirst<X>(IReadOnlyList<X> list)
        {
            return MyFirstCore<X, ReadOnlyListEnumerator<X>>(new ReadOnlyListEnumerator<X>(list));
        }

        // IEnumerable を受け取るオーバーロード
        // ヒープを使うけれど、実装の共通化はできている。
        public static X MyFirst<X>(IEnumerable<X> enumerable)
        {
            return MyFirstCore<X, StructEnumerator<X>>(new StructEnumerator<X>(enumerable.GetEnumerator()));
        }
    }
```

列挙時にヒープ確保が行われないことを確認するには、次のように ``GC.GetTotalMemory`` を使います。

```csharp
    class Program
    {
        static void Main(string[] args)
        {
            var xs = Enumerable.Range(0, 100).ToArray();
            var sum = 0L;
            MyLinq.MyFirst(xs); // おまじない

            GC.Collect();
            var before = GC.GetTotalMemory(false);
            {
                for (var i = 0; i < 10000; i++)
                {
                    sum += MyLinq.MyFirst(xs);
                }
            }
            var after = GC.GetTotalMemory(false);
            var difference = after - before;

            // difference == 0

            Console.WriteLine("Memory addition: {0}", difference);
        }
    }
```

## 余談: ``List<T>`` は構造体列挙子を提供している
``List<T>`` は ``IEnumerable<T>`` を明示的に実装しつつ、``List<T>.Enumerator`` という構造体を返す `GetEnumerator` メソッドを提供しています。``List<T>`` 型の変数を `foreach` で回すときには、こちらが使用されるので、列挙子はボックス化されません。

参考: [List<T>.GetEnumerator メソッド](https://msdn.microsoft.com/ja-jp/library/b0yss765(v=vs.110).aspx)

## 余談: 実行時型をみてリストか否か判断する (追記)
標準ライブラリーの ``IEnumerable<_>.First`` メソッドに配列などのリストを渡しても、列挙子のボックス化によるヒープ確保は起こりません。というのも、メソッドが受け取る型こそ ``IEnumerable<_>`` ですが、その実行時型がリスト (``IList<_>``) であるかどうかをメソッドの内部で動的に判定していて、もしそうだったらインデックスでアクセスする (``list[0]`` を返す) ようになっているためです。他のメソッドも同様です。

参考: [First の実装](https://referencesource.microsoft.com/#System.Core/System/Linq/Enumerable.cs,921)

そういう意味で、本稿の `MyFirst` は若干ながら手抜きになってしまっています。
