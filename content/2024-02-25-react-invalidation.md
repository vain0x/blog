---
title: "Reactとインバリデーション"
type: "post"
date: 2024-02-25
url: 2024-02-25/react-invalidation
tags:
  - React
---

Reactでイベントハンドラを書くときのインバリデーション問題でしばらく混乱してしまっていたので、記事に書きました。この記事の結論は `use-event-callback` です

<!--more-->

## イベント

前提として、ここでは「更新ボタンを押して一覧をリロードする」とか「送信ボタンを押してフォームデータを登録する」といった操作をイベントと呼んでいます

## イベントハンドラ

Reactでは、要素が持つ `onClick` などのプロパティに関数を指定するとイベント発生時に呼ばれます

```jsx
const Component = () => {
    return (
        <button type="button" onClick={() => {
            // ここに処理を書く...
        }}>...</button>
    )
}
```

しかしイベントハンドラの中に直接、処理を記述したくないことがたまにあります:

- (A) コンポーネントが分割されていて、処理に使うデータをコンポーネントが受け取っていないから
- (B) イベントハンドラの記述をUI構築のなかに埋もれさせるのが好ましくないから

(A) の解決は必須です。
解決策は2通りあって、状態を渡すか関数を渡すかです。
propsにイベントハンドラが使うデータをすべて渡せば対処できます。
ただし、これは (B) の問題の解決になりません。
もう1つの方法は、イベントハンドラ自体を関数としてpropsで渡すことです。
どちらの解決策も、後述する「メモ化の過剰なインバリデーション」の問題につながります

(B) はコードの分かりやすさの問題で、いわゆる凝集度が下がることです。
問題とみなすかは場合によります。
イベントの発生と処理が近いほうが構文的に「凝集」していますが、その凝集の意義が薄いこともあります。
ボタンをどこに置くかはUIの関心ごとであってその機能と関係が薄かったり、同一のイベントが複数個所から発生することもあったりするからです。
複数あるイベントハンドラを分散させるより、イベントハンドラ同士をまとめておいたほうが機能的に「凝集」しているといえます

## メモ化の過剰なインバリデーション

コンポーネントのレンダリングは一定のコストがかかり、再レンダリングの頻度は少なくありません。
Reactのメモ化の仕組みを使い、不必要なレンダリングを省くと効率的です

※コンポーネントの特定の実装によってはレンダリングのコストが十分に小さいこともあります。その場合は問題がなく、この記事に書いてあることは適用されません

(A) に戻り、イベントハンドラの処理をコンポーネントの追加のパラメーターにして記述を分離したとします:

```jsx
const Component = memo(function Component(props) {
    return (
        <button type="button" onClick={props.onClick}>...</button>
    )
})
```

このコンポーネントの利用者側は次のようになっています:

```jsx
const Ancestor = () => {
    const [state, dispatch] = useReducer(reducer, init)

    const handleClick = useCallback(() => {
        // ここに処理を書く...
    }, [state])

    return (
        <>
            <Component onClick={handleClick} />
            ...
        </>
    )
}
```

くだんのイベントハンドラ (`handleClick`) はここに記述されるようになり、(B) の問題は解決しました

ここではイベントハンドラが `state` のほとんど全部を参照すると仮定します。
`handleClick` を `useCallback` でメモ化しようとしていますが、あまり効果がありません。
`state` が変化するたびに `handleClick` は参照的に異なるオブジェクトになり、それによって `Component` も再レンダリングされてしまいます。
実際に `Component` が `state` を使うのは「クリックされたとき」であり、クリックされるまでに起こる `handleClick` のみの変化に起因する再レンダリングは過剰な作業です。
こういうものをメモ化の過剰なインバリデーションと呼んでいます

Reactのリポジトリに対応するissueがあります:

> [useCallback() invalidates too often in practice · Issue #14099 · facebook/react](https://github.com/facebook/react/issues/14099)

繰り返しになりますが、あくまで再レンダリングのコストが追加でかかるという話なので、再レンダリングのコストが低ければ問題になりません。
例えば大きいテーブルのなかにボタンがあって、そのクリック時のイベントハンドラがテーブルデータ全体を参照するといった状況で発生します

----

### `useEffect` で状態を監視する (非推奨)

(典型的なアンチパターンの1つを書きますが、読み飛ばしてもいいです)

イベントの発生を状態として持ち、その状態を監視してハンドラーを実行するという方法 (**非推奨**) が考えられます:

```jsx
const Ancestor = () => {
    const [state, dispatch] = useReducer(reducer, init)

    useEffect(() => {
        // NOT RECOMMENDED
        const { ev } = state
        if (ev != null) { // イベント発生中の状態の場合
            try {
                // ここに処理を書く...
            } finally {
                dispatch({ ev: null }) // 処理済みのイベントを破棄
            }
        }
    }, [state])
    // ...
}

const Component = memo(function Component(props) {
    const { dispatch } = props
    return (
        <button type="button" onClick={() => {
            dispatch({ ev: true })
        }}>...</button>
    )
})
```

`dispatch` は参照的に安定しているため、コンポーネントに渡してもメモ化を阻害しません。
(A), (B) の問題は解決します。
`dispatch` の際に `Ancestor` の再レンダリングが引き起こされますが、メモ化がうまくいくなら、そのコストは低いとみなせます

ただし、Reactの新しくなったドキュメントでは「こういうことをしないように」書かれています:

> **エフェクトは、特定のイベントによってではなく、レンダー自体によって引き起こされる副作用を指定するためのものです**
>
> ([エフェクトを使って同期を行う – React](https://ja.react.dev/learn/synchronizing-with-effects))

新しいバージョンのStrict Modeで `useEffect` が2回ずつ呼び出される仕様になったため、このような実装は開発環境でうまく動かないはずです

### `useRef` で状態を持つ

`useRef` で可変なフィールドを用意して「最新の」状態を書き込んでおき、イベントの発生時にそれを読み出すという方法も考えられます:

```jsx
const Ancestor = () => {
    const [state, dispatch] = useReducer(reducer, init)

    // NOT RECOMMENDED
    const stateRef = useRef()

    useLayoutEffect(() => {
        stateRef.current = state
    })

    const handleClick = useCallback(() => {
        const state = stateRef.current
        if (state != null) {
            // ここに処理を書く...
        }
    }, []) // 依存値なし

    // ...
}
```

これは (A), (B) の両方を解決しています。
しかし状態が `stateRef` だけでなく、propsを使っていたり、複数の `useState` がある場合に、コードが冗長になりやすいです。
基本的には次の解決策のほうがいいです

### `useEventCallback`

可変なフィールドに、状態ではなく、イベントハンドラ自体を持てばいいです。
[前掲のissue内にこれを提案するコメント](https://github.com/facebook/react/issues/14099#issuecomment-440013892) があり、現時点 (React v18) において有用な方法だと思います:

```jsx
const Ancestor = () => {
    const [state, dispatch] = useReducer(reducer, init)

    const handleClickRef = useRef()
    useLayoutEffect(() => {
        handleClickRef.current = () => {
            // ここに処理を書く...
        }
    })
    const handleClick = useCallback(() => {
        handleClickRef.current?.()
    }, []) // 依存値なし

    // ...
}
```

これはカスタムフックで部品化しやすいです。
そのためのパッケージ ([`use-event-callback`](https://github.com/Volune/use-event-callback)) もあり、次のようにすっきりします:

```jsx
import { useEventCallback } from "use-event-callback"

const Ancestor = () => {
    const [state, dispatch] = useReducer(reducer, init)

    const handleClick = useEventCallback(() => {
        // ここに処理を書く...
    })

    // ...
}
```

なお `useEventCallback` で作ったコールバックには制限があります。
レンダリング中にそのコールバックを呼び出すと、可変な `ref.current` をレンダリング中に読み取ってはいけないというReactのルールに抵触します

refの更新について:

- レンダリング中に可変な `ref.current` を読み書きしてはいけないと、[useRef](https://ja.react.dev/reference/react/useRef) のリファレンスの注意点の部分に書かれています。
    - [前掲のissue内に問題を指摘するコメント](https://github.com/facebook/react/issues/14099#issuecomment-440794435) があり、並行レンダリングとの相互作用が問題となるようです
- `useEffect` ではなく `useLayoutEffect` を使っているのは、Reactのコミット作業 (DOM要素への変更の適用) が完了してから `useEffect` の実行までにタイムラグがあって、その間にイベントが発生してしまうおそれがあるからだそうです (試したことはない)

#### 公式で非推奨だった

旧ドキュメントでは `useEventCallback` を取り上げて非推奨と指摘していました:

> **このパターンは薦められず**、網羅性のために示しているに過ぎません。代わりにコールバックを深く受け渡していくことを回避するのが望ましいパターンです。
>
> https://ja.legacy.reactjs.org/docs/hooks-faq.html#how-to-read-an-often-changing-value-from-usecallback

望ましいパターンとして `dispatch` を渡せばいいと書かれていますが、(A) の問題は解決しません。
新しいドキュメントに同様の記述はみつかりませんでした

#### 構文的な凝集

イベントハンドラの記述をコンポーネントに置く場合も同じAPIでできます。
イベントハンドラの処理内容をパラメータにとる、汎用的なイベントハンドラを作って渡せばいいです:

```jsx
const Ancestor = props => {
    const [state, dispatch] = useReducer(reducer, init)

    // runHandler: (h: (props, state) => void) => void
    const runHandler = useEventCallback(h => h(props, state))
    // ...
}

const Component = props => {
    const { runHandler } = props
    return (
        <button type="button" onClick={() => {
            runHandler((props, state) => {
                // ここに処理を書く...
            })
        }}>...</button>
    )
}
```

### `useEffect` の過剰な再実行

前項までは「DOM要素が起こすイベント」のイベントハンドラの話でした。
「`useEffect` で獲得したリソースが起こすイベント」のイベントハンドラについても触れておきます

`useEffect` でリソースを獲得する (Reactのガイドの例でいえばチャットルームに接続する) コードがフックのなかにあり、そのなかでリソースが起こすイベントにイベントハンドラを持たせるとします。
リソースの獲得で使うデータだけでなく、イベントハンドラが使うデータまで `useEffect` の依存値に含まれてしまいます。
公式のガイド ([エフェクトからイベントを分離する](https://ja.react.dev/learn/separating-events-from-effects)) を読んでいると、まさにその問題が提起され、`useEffectEvent` というexperimental APIが登場します

なお `useEffectEvent` には `useEventCallback` より厳しい制限があり、コールバックは同じコンポーネント内の `useEffect` からのみ使用可能です (詳細はリンク先を参照)
