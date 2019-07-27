---
title: async関数が例外を捕捉する挙動について
type: "post"
date: 2017-12-23 00:00:00
permalink: async-function-and-exception
tags:
  - C#
  - 非同期
  - Tips
---

<!--more-->

## 現象
C# では async キーワードでマークされた関数の内部では await 式が使えるようになります。では、 await を使わなくていい場合はつけなくてもいいのでしょうか？

例えば、次の `NeverAsync` メソッドと `NeverNoAsync` メソッドは、どちらもタスクを返す非同期な関数ですが、実際に非同期な処理 (`FooAsync`) を実行する前に例外を投げてしまうとします。

```csharp
using System;
using System.Threading.Tasks;

class SampleClass
{
    // async キーワードがついている。
    public async Task<int> NeverAsync()
    {
        throw new Exception();
        return await FooAsync();
    }

    // async キーワードなし。
    public Task<int> NeverNoAsync()
    {
        throw new Exception();
        return FooAsync();
    }

    public async Task<int> FooAsync()
    {
        // なにか非同期な処理
    }
}
```

これらのメソッドを起動した結果は次のようになります。

- async がついている `NeverAsync` のほうは `throw` の時点で return してタスクを返します。返されたタスクは Faulted 状態になっていて、 `Exception` プロパティーから送出された例外を取得できます。
- async のついていない `NeverNoAsync` のほうは、当たり前ですが、例外を伝播します。

つまり async キーワードがついているだけで、次のような try-catch 文が生成されていると解釈できます。

```csharp
public Task<int> NeverAsync_Compiled()
{
    try
    {
        /* async メソッドの中身 (throw とか FooAsync とか) */
        return /* 返されるタスク */;
    }
    catch (Exception ex)
    {
        return Task.FromException<int>(ex);
    }
}
```

## 影響
この挙動の違いの影響を受ける例を挙げます。次のように非同期操作のエラー処理を `ContinueWith` で書くと、

```csharp
BarAsync()
.ContinueWith(task =>
{
    switch (task.Status)
    {
        case TaskStatus.Faulted:
            var ex = task.Exception;
            // エラー処理
            break;
        //...
    }
});
```

非同期メソッド `BarAsync` に async キーワードがついていなくて例外が投げられたとき、エラー処理が行われるのではなく例外が伝播されます。

## まとめ
- async キーワードをつけると、例外が捕捉されてエラー状態のタスクを返すようになる。
- ``async () => await FooAsync()`` と ``() => FooAsync()`` は例外発生時の挙動が異なる。

## 参考
- [C# 非同期メソッドを作るにあたり、例外が出るタイミングでハマったメモ - ..たれろぐ..](http://d.hatena.ne.jp/naga_sawa/20160703/1467517912) (2017年10月16日閲覧)
