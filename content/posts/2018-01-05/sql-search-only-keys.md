---
title: SQLの検索条件と検索項目の分離
tags:
  - データベース
  - Tips
  - Essay
type: "post"
date: 2018-01-05 23:30:37
permalink: sql-search-only-keys
---

複数の検索条件があるときに検索項目をselect句で毎回列挙するのがだるい問題について考えました。

## 問題

例えばブログシステムで、「最近の記事を列挙する」クエリーと「特定のカテゴリーの記事を列挙する」ものがあるとします。SQL文は次のようになるでしょう：

```sql
-- 最近の記事を列挙するクエリー

select
    -- 実際にはカラムがいっぱい並ぶ。
    a.title
    , a.created_at
    , comments.content
    , comments.created_at
    , categories.category_name
from articles as a
join comments
    using (article_id)
left join categories
    using (category_id)
where
    a.created_at >= :first_date
order by a.created_by desc
```

```sql
-- 特定のカテゴリーの記事を列挙するクエリー

select
    -- 上のクエリーと似たような内容になる。
    a.title
    , a.created_at
    , comments.content
    , comments.created_at
    , categories.category_name
from articles as a
join comments
    using (article_id)
left join categories
    using (category_id)
where
    -- ここは全く違う。
    categories.category_id is not null and categories.category_id = :category_id
order by a.created_by desc
```

2つのクエリーは結合するテーブルの個数や検索条件こそ異なりますが、結果として得られるリストは同じです。SQLに重複が多くて、あとで困りそうです。

## 解決策
### 1. アスタリスクとビュー
`select` 句に `*` を書くと、すべてのカラム名を列挙したことになります。

```sql
select *
from articles as a
join comments
    using (article_id)
left join categories
    using (category_id)
where
    a.created_at >= :first_date
order by a.created_by desc
```

重複が大幅に減りました。基本的にはこれで十分でしょう。なお、 `*` を使うと「カラムが追加されるたびにデータ量が増え、しかもそのことに気づきづらい」などの問題があるので使うべきでないという意見もあります。

テーブルを結合する部分が重複したままですが、これはビューを使うという手があります。

```sql
-- ビューを定義しておく。

create view article_aggregates as
    select *
    from articles as a
    join comments using (article_id)
    left join categories using (category_id);

-- 検索クエリー

select a.*
from article_aggregates as a
where
    a.created_at >= :first_date
order by a.created_by desc;
```

あらかじめテーブルを結合して検索するビュー  `article_aggregates` を用意しておき、それを `from` 句に指定することにより、テーブルが始めから結合されているのと同じ状況でクエリーを書くことができます。

依然として ``order by`` は重複したままです。

### 2. クエリービルダー

文字列連結やORM(EntityFrameworkなど)を用いて、カラムのリストを共通化する方法があります。(大げさにいうと)SQLを対象とするDSLを用意するのです。

```sql
-- 文字列連結を用いる方法。

select
    @(articleColumns.map(c => "a." + c)
    .concat(commentColumns.map(c => "comments." + c))
    .join(","))
from article_aggregates as a
where
    a.created_at >= :first_date
order by @orders.join(",")
```

```typescript
// クエリービルダーを用いる方法 (擬似コード)

    let a = db.articles_aggregate_view in
    db.query()
    .from(a)
    .where(a.created_at >= first_date)
    .order_by([ { desc: a.created_at } ])
```

前述のビューを使う方法と合わせて、重複は完全になくなりました。

この方法の問題は、SQL文を静的に取り出す(検索したりexplainにかけたりする)のが難しくなることです。また、SQLインジェクション攻撃に対策しておく必要があります。

### 3. 検索とフェッチの分離

1. あらかじめ、主キーのリストから必要なデータを列挙する方法を用意しておき、
1. 主キーだけを取得するクエリーを検索条件ごとに書く、

という方法です。例えば次のようになります：

```sql
-- 主キーの値に基づいてレコードを検索するクエリー
-- (前述のテンプレートエンジンを使っている。)

select
    a.title
    , a.created_at
    , comments.content
    , comments.created_at
    , categories.category_name
from articles as a
join comments
    using (article_id)
left join categories
    using (category_id)
where
    a.article_id in ( @articleIds.join(",") )
    and comments.comment_id in ( @commentIds.join(",") )
    and (categories.category_id is not null and category_id = @categoryId)
order by a.created_by desc
```

検索のためのクエリーは次のとおりです：

```sql
-- 最近の記事(のID)を列挙するクエリー

select
    a.article_id, comments.comment_id, a.category_id
from articles as a
join comments
    using (article_id)
where
    a.created_at >= :first_date
```

```sql
-- 特定のタグがついた記事を列挙するクエリー

select
    a.article_id, comments.comment_id, a.category_id
from articles as a
join comments
    using (article_id)
left join categories
    using (category_id)
where
    category_id is not null and category_id = :category_id
```

クエリーの数が増えますが、 (テーブルの個数 + 1) 個ぐらいなら問題ないでしょう。

## 結論
- すべてのカラムをフェッチしたい場合は `*` を使い、
- フェッチしたくないカラムがある場合はクエリーを2段階に分ける、

と良いかも！
