---
title: Mapをレコード代わりにするための型付け
type: "post"
date: 2020-09-07
url: 2020-09-07/type-of-map-as-record
tags:
  - TypeScript
---

TypeScript ではレコードにも連想配列にも object が使われがち。レコードのために object ではなく Map を使いつつ、object と同じような入力補完などの恩恵を受ける方法を考えた。実用的ではない。

<!--more-->

Map とオブジェクトの比較は MDN に書いてある: [Map - JavaScript | MDN](https://developer.mozilla.org/ja/docs/Web/JavaScript/Reference/Global_Objects/Map)

version: TypeScript 3.9

## 動機

クエリ (`?` 部分) をパースするとき、キーの集合は静的には限定されないので、結果は連想配列になる。しかし有効なキーの集合は本来限定されているので、レコードになってほしい。このギャップを埋めたかった。

## レコードみたいな型がついた Map

次のような特定の組み合わせのキーからなる object をレコードと呼ぶことにする。(キーは動的に増えたり減ったりしない。これらのキーには常に値が設定されている。)
`.` 記法でプロパティを参照するとき、入力補完や型検査の恩恵を得られる。

```ts
type MyRecord = {
    ok: boolean
    status: number
}

declare const myRecord: MyRecord
const ok = myRecord.ok
//                  ^^ ok, status が補完される
//    ^^ ok: boolean
```

Map の場合は、get, set などのキーに特定の文字列を受け取るオーバーロードを定義すれば、同様に入力補完などの恩恵を得られる。

```ts
type MyMap = {
    get(key: "ok"): boolean // | undefined をつけてもよい
    get(key: "status"): number

    set(key: "ok", value: boolean): void
    set(key: "status", value: number): void
}

declare const myMap: MyMap
const ok = myMap.get("ok")
//                   ^^^^ "ok", "status" が補完される
//    ^^ ok: boolean
```

型定義はメタプログラミングを使えば短く書ける。

```ts
/**
 * キーと値の型の対応がうまくついた get/set メソッドを持つ、Map 的なオブジェクトの型を作る型レベル演算子。
 *
 * USAGE: RecordMap<{ k: v, ... }>
 **/
type RecordMap<T extends object> = {
    get<K extends keyof T>(key: K): T[K]
    set<K extends keyof T>(key: K, value: T[K]): void

    // has<K extends keyof T>(key: K): boolean etc.
}

// 上の myMap とだいたい同じ型になる。
type MyMap = RecordMap<{
    ok: boolean
    status: number
}>
```

インスタンスを作るには、Map リテラルはないので、代わりにオブジェクトリテラルから変換する。

```ts
const toMap = <T extends object>(record: T): RecordMap<T> =>
    new Map<unknown, unknown>(Object.entries(record)) as RecordMap<T>

const myMap = toMap({
    ok: true,
    status: 200,
})

const ok = myMap.get("ok") // ちゃんと型がつく
```

オブジェクトのキーにならないものをキーに含めたいときは entries (キーと値のペアからなる配列) から作る。

```ts
/**
 * entries の型からキーの型を取る。
 */
type EntriesToKeyType<E extends [unknown, unknown][]> =
    E[number] extends infer TPair ? (
        TPair extends [unknown, unknown] ? (
            TPair[0]
        ) : never
    ) : never

/**
 * entries からキーに対応する値の型を探す。
 */
type EntriesFindValueType<E extends [unknown, unknown][], K> =
    E[number] extends infer TPair ? (
        TPair extends [K, unknown] ? (
            TPair[1]
        ) : never
    ) : never

/**
 * entries の型からマップのようなものの型を作る。
 */
type EntriesToMapType<E extends [unknown, unknown][]> = {
    get<K extends EntriesToKeyType<E>>(key: K): EntriesFindValueType<E, K>
    set<K extends EntriesToKeyType<E>>(key: K, value: EntriesFindValueType<E, K>): void
}

const fromEntries = <E extends [unknown, unknown][]>(entries: E): EntriesToMapType<E> =>
    new Map<unknown, unknown>(entries) as EntriesToMapType<E>

const map = fromEntries([
    [fromEntries, 0],
])
const zero = map.get(fromEntries)
```

あるいは `Map<unknown, unknown>` を動的に検査してから `as` で強制的にキャストする。スマートキャストは効かなさそう。

```ts
const unknownMap = new Map<unknown, unknown>(JSON.parse(entriesJsonText))

const valid = typeof unknownMap.get("ok") === "boolean"
    && typeof unknownMap.get("status") === "number"
if (valid) {
    const myMap = unknownMap as MyMap
}
```
