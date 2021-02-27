---
title: React as First-Class UI
type: "post"
date: 2020-09-18
url: 2020-09-18/react-as-first-class-ui
tags:
  - TypeScript
  - React
---

# React as First-Class UI

React のよいところは UI が第一級オブジェクトになっていること。

そのため、何かやりたいときは必ずしも React の機能に頼らなくても、TypeScript の言語機能で書けて、そのまま型がつく、ということが多い。

- 条件によって表示を分けたい: 条件演算子 `?:` を使えばいい
- リストの要素をデータの数だけ表示したい: `Array.map` を使えばいい
- コンポーネントを再利用するとき、使う場所によって一部を変えたい: 追加の引数で受け取ればいい

```ts
import React from "react"

// 分岐: エラーがあるときだけメッセージを表示する
const Alert: React.FC<{ hasError: boolean, message: string }> = props =>
    props.hasError ? (
        <div>エラー: {props.message}</div>
    ) : null

// 反復: データの個数だけリストの要素を表示する
const Posts: React.FC<{ items: { id: number, text: string }[] }> = props => (
    <ul>
        {props.items.map(item => (
            <li key={item.id}>{item.text}</li>
        )}
    </ul>
)

// 抽象化: コンポーネントの一部を引数で受け取る
const Panel: React.FC<{ Header: React.FC }> = props => (
    <div>
        <h2>
            <props.Header />
        </h2>

        <div>
            {props.children}
        </div>
    </div>
)

// Panel を使う側
<Panel Header={props => (
    <a href="example.com">
        ヘッダー
    </a>
)}>
    ボディ
</Panel>
```

(ここでは React.FC を引数に渡したり、関数から返したり、配列やオブジェクトに持たせたりできることをもって、第一級オブジェクトであると表現している。)
