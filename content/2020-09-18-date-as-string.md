---
title: 日時を文字列で持つ案 (Dateのことは忘れる)
type: "post"
date: 2020-09-18
url: 2020-09-18/date-as-string
tags:
  - JavaScript
  - TypeScript
---

# 日時を文字列で持つ案 (Dateのことは忘れる)

TypeScript には日時を表現するための組み込みの Date オブジェクトがあるが、かなり貧弱である。

## 要点

- 一般的に、日時処理用には [luxon](https://moment.github.io/luxon/) や [date-fns](https://date-fns.org/) などのライブラリを使うべき。
- 個人的には、Date オブジェクトの存在は無視して、日時は文字列で持つのがよいと思っている。

## Date は何か

MDN の [Date](https://developer.mozilla.org/ja/docs/Web/JavaScript/Reference/Global_Objects/Date) のページを参照。
Date オブジェクトは単に「ある1点の時刻」を表している。タイムゾーンやロケールなどの情報は持っていない。

## Date はなぜダメか

例えば次のようなクエリに対する操作が用意されていない。

- 特定の形式のフォーマットで文字列に変換する (`yyyy-MM-dd` や `yyyy/MM/dd HH:mm` など)
- 時刻成分を切り落として、1日の始まりの時点を得る
- 同じ月の最後の日付に変換する
- etc.

## 日時を文字列で持つ

Date のメソッドはどうせ使わないので、インスタンス化する必要はない。

代わりに luxon の DateTime オブジェクトなどを使ってもいいが、文字列で持っておけば replacer/reviver を用意しなくても JSON でシリアライズできて嬉しい。
(あるいは `{ year: number, month: number, ... }` とか、何にせよプレインなデータで。)

## 日時を表す文字列の型

日時を文字列で持つとしても、string 型だと混乱を招くので、いわゆる [nominal type](https://basarat.gitbook.io/typescript/main-1/nominaltyping) を使う。

```ts
declare const IS_TIMESTAMP: unique symbol

/**
 * 日時を表す文字列 (ISO 8601 形式)
 */
export type Timestamp = string & { [IS_TIMESTAMP]: true }
```

こうしておくと Timestamp は string の厳密な部分型になる。
(Timestamp 型の値を string 型の引数に渡す方の型変換は許可されるが、逆に string 型の値を Timestamp 型の引数に渡すことはできない。
本当にやりたければ `as Timestamp` と明記する必要がある。)

後は Timestamp を操作するための関数を用意していく。
実装は日時処理用のライブラリ (luxon や date-fns) の機能を流用すればいい。

```ts
import { DateTime } from "luxon"

/**
 * 日時を表す ISO 8601 形式の文字列をパースする。
 */
export const timestampFromISO = (s: string): Timestamp | null => {
    const dateTime = DateTime.fromISO(s, { locale: "ja-jp", zone: "Asia/Tokyo" })
    if (!dateTime.isValid) {
        return null
    }

    return dateTime.toISO() as Timestamp
}

const timestampToDateTime = (timestamp: Timestamp): DateTime =>
    DateTime.fromISO(timestamp, { locale: "ja-jp", zone: "Asia/Tokyo" })

/**
 * 日時を特定のフォーマットの文字列に変換する。
 */
export const timestampToFormat = (timestamp: Timestamp, fmt: string): string =>
    timestampToDateTime(timestamp).toFormat(fmt)
```

例:

```ts
//    timestamp: Timestamp
const timestamp = timestampFromISO("2020-01-02T03:04:05.666+09:00")!

const formatted = timestampToFormat(timestamp, "yyyy-MM-dd")
assert.strictEqual(formatted, "2020-01-02")
```

## 日付型と時間型

同様に日付 (時刻なし) の型や時間 (日付なし) の型なども用意しておくと便利。

```ts
declare const IS_DATE_STRING: unique symbol

/**
 * 日付を表す yyyy-MM-dd 形式の文字列
 */
export type DateString = string & { [IS_DATE_STRING]: true }
```

```ts
declare const IS_HHMM_TIME_STRING: unique symbol

/**
 * 時間を表す HH:mm 形式の文字列
 */
export type HHMMTimeString = string & { [IS_HHMM_TIME_STRING]: true }
```
