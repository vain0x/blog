---
title: IReadOnlyListがIListを実装すべき理由
tags:
  - C#
  - .NET
date: 2017-05-20 20:00:09
permalink: why-ireadonlylist-should-implement-ilist
---

主張: ``IReadOnlyList<_>`` を実装するクラスは、 ``IList<_>`` と ``IList`` も実装したほうがいい。

理由は2つあります。

## 理由1: IEnumerable 拡張メソッド
1つ目の理由は、``IEnumerable<_>`` に対する拡張メソッドが ``IReadOnlyList<_>`` ではなく ``IList<_>`` 用に最適化されているからです。

``IEnumerable<_>`` に対する拡張メソッドはインターフェイス経由では `GetEnumerator` メソッドしか起動できないので、例えばシーケンスの最後の要素を取得するメソッド (`Last`) を素直に実装するには、1度すべての要素を列挙して、最後の値を返すことになります。これではパフォーマンスが損なので、与えられたシーケンスをまず ``IList<_>`` に動的キャストできないか試みて、可能ならインデックスを使って最後の要素を取得する、という最適化が入っています。 ([Enumerable.Last の参考実装](https://referencesource.microsoft.com/#System.Core/System/Linq/Enumerable.cs,3628defc5be1468a))

したがって、「``IReadOnlyList<_>`` が ``IEnumerable<_>`` にキャストされて ``Enumerable.Last`` される」といったシナリオのパフォーマンスを最大化するために、 ``IList<_>`` を実装しておいたほうがいいわけです。

## 理由2: WPF
2つ目の理由は、WPF アプリケーションから参照されうるクラスライブラリーに限った話になりますが、「WPF の DataGrid はコレクションが非ジェネリックな `IList` を実装していることを前提としている」ことです。

簡単にいうと、DataGrid (表) に `IList` を実装していないコレクションを渡すと、セルの編集ができなくなります。詳細については、既に Qiita に記事があるので、こちらを参照してください:

[DataGrid（WPF） の ItemsSource には IList が必要](http://qiita.com/gaya_K/items/d1737fc829502c916d18)

~~また、実際の動きを確認したい場合はこちらからソースコードを入手できます:

[DataGrid.ItemsSource が IList を実装している場合としていない場合の比較](https://github.com/vain0x/VainZero.Sandbox.CSharp/tree/2017-05-20-DataGrid)~~ (リンク切れ)

## 例外
もちろん上記で述べたような懸念があたらないことが分かっているケースでは、``IList<_>`` や `IList` を実装しなくてもよいでしょう。

## おまけ: ReadOnlyCollection
``IList<_>`` はメンバーの数が多くて、実装するのは非常にめんどうですが、 ``IList<_>`` をラップして読み取り専用なリスト (``IReadOnlyList<_>``) として振る舞わせる ``ReadOnlyCollection<_>`` というのが標準にあります。これを継承して実装すると楽です。
