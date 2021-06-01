---
title: GUIアプリのルーティング・ページ遷移
type: "post"
date: 2021-05-31
url: 2021-05-31/gui-navigation
tags:
  - エッセイ
  - TypeScript
  - React
---

GUIアプリは複数のページ (画面) を持つことがある。これに関してぼんやり考えて採用した実装方針を書く。

<!--more-->

前半の概念は他のGUIアプリにもあてはまる (例えばWPFアプリでも採用した)。
後半のコードスケッチはReactの話が中心になる。

## 概念

GUIアプリのページ遷移に関して次の概念を考える。

### ページ

ページはナビゲーションによって生成される。
他のページに移動したとき (or 履歴から外れたとき) やアプリの終了時に破棄される。

「ページを開いていない」は「ブランクページを開いている」とみなすことで常にページはちょうど1つ開かれているとみなす。
複数のページを同時に開けるようなアプリはここでは考えない。(たぶん集合を考えればいい。)

### ルート (route)

ナビゲーション時に指定する行き先をルートと呼ぶことにする。
例えば記事投稿ページを開くためのボタンは、記事投稿ページへのルートを持つ。
ウェブアプリではURLがルートとみなされがち。

ルートはルーティングによって生成され、ナビゲーションによって消費される。

### ルーティング (routing)

ルートを動的に生成する処理をルーティングと呼ぶことにする。
例えばウェブアプリでは文字列として与えられるURLを (有効な) ルートに変換する必要がある。
クリックされた座標から押されたオブジェクトを特定するやつ (hit-test) もルーティングかもしれない。

ルーティング自体はページを開かない。
入力が不正だとルーティングは失敗するが、成功したからといってページを開けるとは限らない。
(必須のパラメータが指定されてるとか文字列の長さが上限・下限に含まれるとかの内部の性質を検証する。IDが実在のデータを指すかといった外部の性質までは普通は検査しない。)

### ナビゲーション

ルートはアプリに対するリクエストの1つであり、ルートをアプリに送る (リクエストする) 操作をナビゲーションと呼ぶことにする。
具体的に何をするかは定められないが、アプリはそのページを開くのに必要なデータを集めて、そのページを開いた状態に状態遷移するはず。

ルートが指すページとは異なるページに遷移するかもしれない。(リダイレクト)

## 型定義

(コードはスケッチ。)

ルートを表す抽象的なオブジェクトの型として `RouteObject` を定める。
`RouteObject` はそこに向かってナビゲーションができるという振る舞いだけが定められていて、詳細は隠蔽される。
ルートを判別共用体で定義してもいいが、巨大になるので扱いづらい気がする。

```ts
interface RouteObject {
    // アプリは引数にとってない (グローバルに1個だから)。
    // PageObjectはページを開くのに十分な情報を持つ何らかのオブジェクトで、UI実装に依存する。
    navigate(): Promise<PageObject>

    // ウェブアプリならa要素のhrefに設定するためのurlを持たせる。
    // url: string
}
```

ルートオブジェクトを生成するためのビルダーを用意する。
型がつくし、移動先のページの事情を知らなくて済むし、ビルダーのメソッドの参照を探すことでページ間の関係を把握しやすくなるので便利。
(ページの数だけメソッドを持つので定義が巨大になるしページの追加時に触るのがめんどくさいという難点もある。)

```ts
interface RouteBuilder {
    // 記事一覧ページへのルートを作る。(sortは既定のソート条件)
    postsIndex(sort): RouteObject

    // 記事閲覧ページへのルートを作る。
    postsView(pageId): RouteObject

    // 記事投稿ページへのルートを作る。
    postsAdd(): RouteObject

    // ...
}
```

ルートビルダーはアプリのエントリーポイントで構築してアプリ全体に渡す。
(イミュータブルだからグローバル変数でいい気がする。)
Reactだとこんな感じになりがち。

```tsx
// index.tsx (エントリーポイント)
const routeBuilder = newRouteBuilder()
const RouteBuilderContext = React.createContext(routeBuilder)
export const useRouteBuilder = useContext(RouteBuilderContext)

(() => {
    ReactDOM.render((
        <RouteBuilderContext.Provider value={routeBuilder}>
            {...}
        </RouteBuilder>
    ), containerElement)
})()

// posts_index_render.tsx (種々のページ)
const PostsIndexPage = () => {
    const routeBuilder = useRouteBuilder()
    return (...)
}
```

なおルートビルダーを作る際にルートビルダー自身への参照が必要になりがち (後述のリダイレクトの例)。再帰的な参照を生み出すための何らかの方法 (可変性など) を使う。

ルートビルダーは分野ごとに分割して後から合成してもいい。例えば `/posts/` 以下のルートを作るビルダー、`/users/` 以下のルートを作るビルダー、といった感じに分ける。合成は継承や `&` でやると短い。

## ページコンテナの実装

Reactだと1個のページのUIを1個のコンポーネントに任せることになると思う。
その親となり、ページを所有すべきコンポーネントをページコンテナと呼ぶことにする。
ページの状態を持つ必要があるが、種々のページの状態をすべてコンテナに管理させると型が巨大になって扱いづらいので、抽象的な型で持つ。
Reactの場合はコンポーネントをレンダーした結果の型 (`<Component />`　の型) である `ReactNode` を使う。

```tsx
type PageObject = ReactNode | { redirect: true, route: RouteObject } // 上述

const PageContainer = () => {
    const [page, setPage] = React.useState(<BlankPage />)

    React.useState(() => {
        setPage(initialPage())
    }, [])

    const navigate = React.useCallback(route => {
        // エラー処理とかキャンセルとか省略、良くないコード
        route.navigate().then(page => {
            if ((page as any)?.redirect) {
                navigate(page.route)
            } else {
                setPage(page)
            }
        })
    }, [])

    return (
        <NavigateContext.Provider value={navigate}>
            {page}
        </NavigateContext.Provider>
    )
}
```

## 種々のページの実装

種々のページは `RouteObject` を生成する関数を提供する。
ルートを生成するコンストラクタと、ルーティングの際に必要な情報。
(ナビゲーションの方法やナビゲーション後の処理は `RouteObject` に埋め込まれるからexportしなくていい。)

```tsx
// posts_index_render.tsx (種々のページ)

export const newPostsIndexRoute = (sort): RouteObject =>
    ({
        navigate: async () => {
            const postList = await fetchPostList(sort)
            return (<PostsIndexPage postList={postList} />)
        },
    })

const PostsIndexPage = (props: { postList: PostList }) => {
    return (...)
}
```

以下はリダイレクトを含む例。
記事閲覧ページを開こうとして存在しなかったら NOT FOUND エラーページに飛ばしてる。(この挙動よりページを開きつつエラー的な表示を出すほうがいいかもしれない。)

```tsx
// posts_view_render.tsx (種々のページ)

export const newPostsViewRoute = (pageId, routeBuilder): RouteObject =>
    ({
        navigate: async () => {
            const postData = await fetchPostView(pageId)
            if (postData == null) {
                return { redirect: true, route: routeBuilder.notFoundError() }
            }

            return (<PostsIndexPage postList={postList} />)
        },
    })

const PostsViewPage = (props: { postData: PostData }) => {
    return (...)
}
```

(ページが状態をprops経由で受け取るのがReact的によくないという微妙さがある。)

## サーバーサイドレンダリング (SSR)

(余談)

Reactはサーバーサイドでレンダリングができる、つまりサーバー側でHTMLを生成してレスポンスとして送れる。
クライアントがReactのロードとかをしている間にUIの構築をやってもらえて、Reactの起動時の処理を減らせる。
サーバー側でルーティング処理を行ってページの状態を生成し、レンダリングすればいいだけなので、特に問題はない。

クライアント側で最初のページを開くときにナビゲーションを行うと、ページに必要なデータをfetchしようとするが、ラウンドトリップが1回増えるので無駄なのが気になりつつある。
対策としてはサーバー側でページに必要な情報を埋め込んでしまえばいい。
上述の `PageObject` にそのための情報を持たせておく。つまり「ページの種類」と「そのページの初期表示に必要なデータ」のペアをHTMLに埋め込んで置く。クライアント側で「ページの種類」からレンダリングすべきページを特定して、その「データ」を渡してレンダリングする。これなら非同期処理は挟まらない。

## その他

これが最強の方法だ、という感じは微塵もない。いい方法があったら教えてほしい。

改めて書いていて思ったこと: アプリを独立した部分 (ページ) に分割したいという考えと、ナビゲーションをくっつけて考えるのがよくないかもしれない。
