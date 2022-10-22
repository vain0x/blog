---
title: データベースをスコープで分けてテストしやすくする
type: "post"
date: 2019-11-28
url: 2019-11-28/scoped-database
tags:
  - データベース
---

データベースを使うテストを書きやすくできるかもしれない方法について述べます。

<!--more-->

※これが良い方法かは分かりません。今度やってみたいので、その前に整理する目的で書いています。

## 動機

以下のような事情があるとします。

- アプリの実装上、データベース操作が占める割合が大きい。
- データベースやいわゆるリポジトリのテストダブルを作るコストが高い。
- 純粋関数で書いた部分のテストだけでは、検証できる範囲に限界がある。
- データベースを動かして SQL 文が壊れてないことをこまめに検査したい。

## 例

仮にブックマークサービスを考えます。

- ユーザーはウェブページをブックマークしてコメントをつけられる。
- ブックマークはちょうど1個のカテゴリーに分類される。
    - カテゴリーはシステム側で用意し、まれに運用中に増減させる。

## データベースの雰囲気

(重要でない部分は略しています。)

ユーザーはスコープに所属するので、ID や名前に加えて scope フィールドを持ちます。

```sql
create table users(
    user_id char(36) primary key,
    scope char(36) -- 所属スコープ
);
```

ブックマークは作成したユーザーからスコープが決まるので、scope フィールドは不要です。

```sql
create table bookmarks(
    bookmark_id char(36) primary key,
    creator_id char(36), -- 作成ユーザーID
    bookmark_uri varchar(1000)
);

-- 他人のユーザーのブックマークへのいいね
create table likes(
    bookmark_id char(36),
    user_id char(36),
    primary key (bookmark_id, user_id) -- この2つは同じスコープに属す
);
```

ユーザー以外 scope がいらないかというとそうでもなく、例えばカテゴリーはユーザーやブックマークとは独立に存在するのでスコープがいります。

```sql
create table categories(
    category_id char(36) primary key,
    scope char(36)
);
```

このように各レコードにスコープが定まるようにします。

## テストコードの雰囲気

このデータベースを使うアプリに対して、以下のようなテストコードを書いていきます。

```js
// ヘルパー
const freshScope = () => createNewUuidV4()
const freshId = () => createNewUuidV4()

const test_ユーザーがブックマークをつけられる = async () => {
    // このテストのためのスコープを作る。
    const scope = freshScope()

    // スコープ内にユーザーを作る。
    const u1 = freshId()
    const userName = "John Doe"
    await createUser(u1, userName, scope)

    // ブックマークを作る。
    const bookmarkId = freshId()
    const uri = "https://example.com"
    await createBookmark(bookmarkId, u1, uri)

    // ユーザー自身のブックマーク一覧に、
    // いま作ったブックマークがあることを表明する。
    const bookmarks = await getMyBookmarkList(u1)
    assert.ok(bookmarks.some(bookmark => bookmark.id === bookmarkId))
}
```

## メリット

- 使うデータはテストケース自身が作るので、準備したデータを投入するフェイズがなくなる。
- 不要なデータを適切に消さなくてよくなる。
    - あるテストケースの後でデータベースの状態が想定外になって他のテストケースが落ちてしまう、という現象は起きないはず。
- いわゆるマスターデータ (テスト中・運用中に変更されることがないデータ) がすべてのテストケース間で使いまわせる。
    - 事前に手動で投入すればOK。

## デメリット

- 本番環境においてほぼ意味のない条件式 (`where scope = :scope`) をすべてのクエリーに書くことになる。
- 本番環境においてほぼ意味のないフィールド (scope) を一部のレコードが持つことになる。
- 機能性ではなく保守性の都合でデータベースの設計を「普通でなく」している。

## その他

- テストが互いに干渉しないから、という複数のテストをマルチスレッドで平行実行できる、というのはメリットではない。
    - 並列化するだけならテストやデータベースのプロセスを増やせばいい。
- 「削除フラグ」を「他のスコープに移動させる」ことで代用できるかもしれない。
    - 削除フラグのチェック漏れは気づきにくいが、スコープのチェック漏れはテストを壊すから気づきやすい(？)
    - (そもそも削除フラグはやめよう)
