---
title: awaitでスレッドを切り替える
tags:
    - 古い記事
#   - CSharp
#   - 非同期
type: "post"
date: 2017-04-05 15:00:00
url: 2017-04-05/switch-on-await
deprecated: true
---

**追記**: 現在は推奨していません。

<!--more-->

awaitでスレッドを切り替えるための簡単なヘルパーメソッドを作ったので紹介します。

実装とサンプルはここにあります: [await-for-context-switching.cs](https://gist.github.com/vain0x/fd5880b77d019cdb91d4a58dd52813a2)

## 前提: Awaitable パターン
- Awaitable パターンについては [非同期メソッドの内部実装](http://ufcpp.net/study/csharp/sp5_awaitable.html#awaiter) などを参照。

## OK: await → UI 処理
まず await が1つだけの非同期メソッドでは、Task に対する await が自動的に同期コンテクスト (SynchronizationContext) を捕捉する機能を用いることで、以下のように簡単にかけます。

```csharp
public async Task Do()
{
    // Do は UI スレッドで起動されるとする。

    // 重い処理を非同期で実行する。
    var x = await HeavyTask();

    // UI スレッドで返り値を使う。
    UIOperation(x);
}
```

## 問題: await → await → UI 操作
しかし await が2回以上ある場合に同様に書くと、必要以上に早く UI スレッドに戻ってしまいます。以下の例では、2つ目の重たい非同期処理である `SecondHeavyTask` が完了するまで、UI スレッドがブロックされます。

```csharp
public async Task Do()
{
    // Do は UI スレッドで起動されるとする。

    // 重たい非同期処理
    var x = await HeavyTask();

    // もう1つ重たい非同期処理 (!)
    var y = await SecondHeavyTask(x);

    // ここは UI スレッドに戻って処理したい。
    UIOperation(x, y);
}
```

これを避けるには、 Task.Run や ContinueWith などを使って、await を1つにまとめる必要があります。

```csharp
public async Task Do()
{
    var a =
        await Task.Run(async () =>
        {
            var x = await HeavyTask();
            var y = await SecondHeavyTask(x);
            return new { x, y };
        });

    UIOperation(a.x, a.y);
}
```

インデントが2段階深くなることと、変数を匿名型経由で渡していることが気になります。

冒頭のヘルパーメソッドを使うと次のようにできます。

```csharp
public async Task Do()
{
    var context = await TaskModule.SwitchToTaskPool();
    var x = await HeavyTask();
    var y = await SecondHeavyTask();
    await context;
    UIOperation(x, y);
}
```

これには

- インデントが浅くなった。
- 同期コンテクストを使うこと (UI 操作の直前で UI スレッドに戻ること) が明確になった。

という利点があります。

## 仕組み
ヘルパーメソッドの仕組みを簡単に説明しておきます。

### 継続
`await` には継続を取り出す機能があります。どういうことかというと、例えば次の「task を await して、その値を使って何か処理をする」コードは:

```csharp
    var x = await task;
    F(x);
```

`await` の時点で `task` が完了していなかったとすると、次のようなコードと同様の振る舞いになります:

```csharp
    var awaiter = task.GetAwaiter();
    awaiter.OnCompleted(() =>
    {
        var x = awaiter.GetResult();
        F(x);
    });
    return nextTask;  // ←コンパイラーが生成するタスク
```

※実際にこのように変換されるわけではありません。雰囲気大事。

ここで ``awaiter.OnCompleted`` にラムダ式が渡されていますが、これが継続です。

`GetAwaiter` メソッド経由で生成される awaiter を自作することで、この継続を好きなように使えます。

### SwitchToTaskPool
[TaskModule.SwitchToTaskPool](https://gist.github.com/vain0x/fd5880b77d019cdb91d4a58dd52813a2#file-await-for-context-switching-cs-L186) は awaitable のインスタンスを生成するだけのメソッドです。ついでにここで同期コンテクストを捕まえています。

awaitable/awaiter の実装は [これ](https://gist.github.com/vain0x/fd5880b77d019cdb91d4a58dd52813a2#file-await-for-context-switching-cs-L94) です。`GetAwaiter` が起動されたときにすることが特にないので、awaitable と awaiter を同じインスタンスにしています。先述の通り、この awaitable の `OnCompleted` メソッドに継続が渡されるわけですが、これは継続をタスクプール上で実行させるために `Task.Run` に渡します。

```csharp
public void OnCompleted(Action continuation)
{
    Task.Run(continuation);
}
```

そのため、 ``await TaskModule.SwitchToTaskPool`` より後ろの部分がタスクプールで実行されることになります。

また、await 式の値は ``awaiter.GetResult`` から取得されます。 ``Task<_>`` の場合はタスクの結果の値がそうです。`SwitchToTaskPoolAwaitable` の場合は保存しておいた同期コンテクストを返すようになっています。

### 同期コンテクストを await する
`await` 式には、`Task` に限らず `GetAwaiter` メソッドを提供する任意の値を渡せますが、 `GetAwaiter` は拡張メソッドでもかまいません。

`SynchronizationContext` はいかにも `await` 可能な感じなので、次のように `GetAwaiter` を生やしています。

```csharp
public static SynchronizationContextAwaiter GetAwaiter(this SynchronizationContext @this)
{
    return new SynchronizationContextAwaiter(@this);
}
```

これが生成する [SynchronizationContextAwaiter](https://gist.github.com/vain0x/fd5880b77d019cdb91d4a58dd52813a2#file-await-for-context-switching-cs-L138) は、先ほどの SwitchToTaskPoolAwaitable とほぼ同じで、継続をタスクプールではなく同期コンテクストに放り込むものです。
