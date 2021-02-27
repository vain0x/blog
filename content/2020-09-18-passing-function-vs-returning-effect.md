---
title: 関数を渡すvs.エフェクトを返す
type: "post"
date: 2020-09-18
url: 2020-09-18/passing-function-vs-returning-effect
tags:
  - TypeScript
  - 設計
---

どちらも計算の一部を抽象化できる。

<!--more-->

TODO: 意味のある例

## 関数を渡す

```ts
const foo = (f: (i: number) => number): number => {
    let x = 0
    for (const i of [1, 2, 3]) {
        x += f(i)
    }
    return x
}

foo(i => i) //=> 6
foo(i => i * i) //=> 14
```

## エフェクトを返す

副作用を表現するデータと、その副作用を処理した後の続きの計算を表す関数を返す。

言語側でサポートがないとめちゃくちゃ煩雑になる。(ジェネレータ構文を使えばできるが、TypeScript ではうまく型がつかなさそう。F# のコンピュテーション式なら yield をオーバーロードできるので型をつけられる気がする。)

```ts
type FooResult<A> =
    {
        kind: "FOO_YIELD"
        value: number
        cont: (value: number) => FooResult<A>
    } | {
        kind: "FOO_RETURN"
        value: A
    }

const foo = (): FooResult<number> => {
    let x = 0
    let i = 0

    const aux = (a: number) => {
        x += a
        if (i <= 3) {
            i++
            return { "FOO_YIELD", value: i, cont: aux }
        }
        return { kind: "FOO_RETURN", value: x }
    }
    return aux(1)
}

const performFooResult = (result: FooResult<number>) => {
    while (true) {
        switch (result.kind) {
            case "FOO_YIELD":
                result = result.cont(result.value)
                continue

            case "FOO_RETURN":
                return result.value

            default:
                throw "never"
        }
    }
}

performFooResult(foo()) //=> 6
```

DI と違って副作用に対して行うべき処理を引数で渡さなくてよいので、副作用に対処する手段がなくても関数を呼べる。例えば関数をテストするとき、入力によっては、一部の副作用が発生しないことが分かっていることがある。そのようなテストケースのためにモックを用意する必要がない。(とはいえ TypeScript なら `null!` を渡した方が楽かもしれない。)

```ts
const result = foo()
assert.equal(result.kind, "FOO_YIELD")
```
