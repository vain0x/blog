---
title: "イベント駆動のReact"
type: "post"
date: 2024-02-27
url: 2024-02-27/event-driven-react
tags:
  - React
---

前回の記事([Reactとインバリデーション](https://vain0x.github.io/blog/2024-02-25/react-invalidation/))の実質的な続きで、イベントによるコンポーネント間の通信について書きます。
実用的な話ではないと思います

<!--more-->

*TL;DR*: イベントによるコンポーネント間の通信は `use-event-callback` とほとんど同じ (子要素が親要素のイベントを起こす方向の場合)

## `EventTarget`

Web APIに含まれる [`EventTarget`](https://developer.mozilla.org/ja/docs/Web/API/EventTarget) はイベントの購読と発行を提供するオブジェクトで、DOM要素とかの親要素にもなっています。
Node.jsでもバージョン14ぐらいから追加されていて、importなしで使えます: [EventTarget (Node.js)](https://nodejs.org/api/events.html#eventtarget-and-event-api)

`EventTarget` の素のAPIを使うとコードが無駄に冗長になるため次のヘルパーを使います:

## イベントターゲットの内部構造

イベントターゲットは「関数を値として持つ可変なコレクション」への参照を共有することで実現できます。
このことを詳しくみていきます

話を簡単にするため、`EventTarget` の機能をこの記事では使うものだけに絞ります。
`type`, `options`, イベントハンドラの引数はないものとします

簡略化版 `EventTarget` は、配列を1個持って次のような操作を提供するだけです:

- `addEventTarget`: ハンドラを配列に追加する
- `removeEventTarget`: ハンドラを配列から除去する
- `dispatchEvent`: 配列内のハンドラをそれぞれ呼び出す

<details>
<summary>class SingleEventTarget {...}:</summary>

```ts
class SingleEventTarget<T> {
    // 購読しているイベントハンドラからなる配列
    #handlers: (() => void)[] = []

    // イベントを購読する = ハンドラを配列に追加する
    addEventListener(_type: string, handler: () => void) {
        this.#handlers.push(handler)
    }

    // イベントの購読を解除する = ハンドラを配列から取り除く
    removeEventListener(_type: string, handler: () => void) {
        const index = this.#handlers.indexOf(handler)
        if (index >= 0) {
            this.#handlers.splice(index, 1)
        }
    }

    // イベントを発生させる = 配列にあるハンドラをそれぞれ呼び出す
    dispatchEvent(_ev: unknown) {
        for (const handler of this.#handlers) {
            handler()
        }
    }
}
```
</details>

## コンポーネント間の通信

親となるコンポーネントが `EventTarget` のインスタンスを1つ持ち、それにイベントを起こす関数 (`emit`) を作ります。この関数はメモ化して参照的に同一にでき、これをサブコンポーネントに渡してもメモ化を阻害しません

イベントを `useEffect` で購読しておき、イベントの発生時にクリック時の処理を行うとします。
イベントハンドラをメモ化しても、前回記事のとおり「過剰なインバリデーション」が起きるので、メモ化しません。
この `useEffect` はレンダリングごとに再実行します。
イベントの購読と解除が繰り返し起きますが、コストが小さいので問題ないです。
前述のとおりこれらの操作は実質的に配列要素の追加と削除だからです

```jsx
const Ancestor = () => {
    const [state, dispatch] = useReducer(reducer, init)

    // `SingleEventTarget` のインスタンスを作る。
    // (コンポーネントがマウントされている間、常に同じインスタンスを持つ)
    const targetRef = useRef<SingleEventTarget>()
    targetRef.current ??= new SingleEventTarget()
    const target = targetRef.current

    const handleClick = () => {
        // ここに処理を書く...
    }
    useEffect(() => {
        target.addEventListener(handleClick)
        return () => target.removeEventListener(handleClick)
    }) // 毎回再実行する

    // ....

    return (
        <>
            <Component emit={emit} />
            ...
        </>
    )
}
```

```jsx
const Component = memo(function Component(props) {
    const { emit } = props
    return (
        <button type="button" onClick={() => {
            emit()
        }}>...</button>
    )
})
```

こうすると前回記事の問題 (A), (B) (データ依存関係の問題、凝集性の懸念) は解決できます

### 再考

あらためてこの部分をみると:

```js
    useEffect(() => {
        target.addEventListener(handleClick)
        return () => target.removeEventListener(handleClick)
    }) // 毎回再実行する
```

このユースケースにおいて `target` は長さ0か1の配列なので、nullableなフィールドで置き換えることができます:

```js
    const targetRef = useRef<(() => void) | null>(null)
    useEffect(() => {
        targetRef.current = handleClick  // add
        return () => { targetRef.current = null }  // remove
    }) // 毎回再実行する
```

こうしてみると `use-event-callback` とほとんど同じことをやっています
