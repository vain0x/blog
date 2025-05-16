---
title: 'RadioSelector: RadioButtonのリストをSelectorコントロールで表示'
type: "post"
date: 2025-05-12
url: 2025-05-12/radio-selector
tags:
  - CSharp
  - WPF
---

RadioButtonのリストを表示するコントロールについて考えました

<!--more-->

## 要点

- 複数の RadioButton を表示して選択値にバインディングする機能を持つコントロールを作ってみました
- コントロールの実装例は [RadioSelector.cs] にあり、使用箇所の例は [RadioSelectorExample] にあります
- おすすめというほどでもない

## 背景

### RadioButton と ListBox

[RadioButton] で enum の値から1つを選択するUIを想定します。選択されている値を ViewModel のプロパティに持ち、データバインディングによって選択状態を共有したいです

WPF の RadioButton は選択された値をバインディングするプロパティがないようです (ボタンごとに IsChecked プロパティがある)

一方、[ListBox] は複数の項目から1つを選択する機能を提供していて、その選択状態も DependencyProperty によって公開されているので、選択値にデータバインディングが可能です

実際、ListBox を使ってラジオボタンの機能は実現できて、見た目が違うだけです。見た目は Template の差し替えによって自由に変更できます。この方向性の実装を紹介している記事もあります: [RadioButton を ListBox で実装する \| frog.raindrop.jp.knowledge](https://frog.raindrop.jp/knowledge/archives/002386.html) (2009)

### Selector

ListBox の基底クラスである [Selector] が「複数の要素を表示して、そのうちどれが選択されているかを管理する」という機能を表現しています

Selector を使って RadioButton のリストを作るという方法が自然な気がしました。実際にどうか確かめるため、作ってみることにしました。このコントロールを RadioSelector と呼ぶことにします

> ```csharp
> internal class RadioSelector : Selector
> {
> }
> ```

## 実装

### アイテムコンテナ

方向性として、ListBox から不要な機能やUIを取り除き、各項目を ListBoxItem ではなく RadioButton で包むようにすればいいはずです。そのために ListBox や [ItemsControl] を調べました

ListBox の項目はそれぞれ ListBoxItem でラップされています。この構造は ListBox の基底クラスである ItemsControl 側の仕組みで、そのようなラッパー要素をアイテムコンテナというようです

ListBox (あるいは ItemsControl 全般) 内の構造は、コントロール本体の中にパネルがあり、その中に項目ごとのコンテナ、そしてコンテンツという構造になっているようです (実際の構造は XAML ライブツリービューなどを参照)

```
    コントロール (ListBox)
        パネル (StackPanel)
            <for item in ItemsSource>
                コンテナ (ListBoxItem)
                    コンテンツ (ContentPresenter)
```

WPF の ListBox の実装 ([ListBox.cs]) を覗いてみると、次のようなオーバーライドによって要素が作られていました

> ```csharp
>    protected override DependencyObject GetContainerForItemOverride()
>    {
>        return new ListBoxItem();
>    }
>    // (dotnet/wpf から引用)
> ```

ここを `RadioButton` にすればよさそうです

また、選択項目のラジオボタンをチェック状態にするため、[Selector.IsSelected 添付プロパティ](https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.controls.primitives.selector.isselected) をバインディングしておきます

> ```csharp
>    protected override DependencyObject GetContainerForItemOverride()
>    {
>        var radio = new RadioButton();
>        radio.SetBinding(RadioButton.GroupNameProperty, new Binding(nameof(GroupName)) { Source = this });
>        radio.SetBinding(RadioButton.IsCheckedProperty, new Binding("(Selector.IsSelected)") { Source = radio });
>        return radio;
>    }
>    // (RadioSelector.cs から引用)
> ```

### チェックと選択状態のバインディング

次に、RadioButton のチェック時に Selector の選択状態が変わるようにします

※`IsChecked` のバインディングを `Mode=TwoWay` にしてもこの方向の同期はされませんでした (詳細は不明、GroupName によって選択が排他されるからかも)

RadioButton.Checked にイベントハンドラーを登録して、選択項目をセットすればいいはずです

ItemsControl はアイテムコンテナをリサイクルするようなので (実際にどうかは未確認)、生成時ではなく、`PrepareContainerForItemOverride` というそれらしきフックを使って登録しました (解除は `ClearContainerForItemOverride`)

> ```csharp
>     protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
>     {
>         base.PrepareContainerForItemOverride(element, item);
>
>         if (element is RadioButton radio)
>         {
>             radio.Checked += RadioChecked;
>         }
>     }
>
>    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
>    {
>        base.ClearContainerForItemOverride(element, item);
>
>        if (element is RadioButton radio)
>        {
>            radio.Checked -= RadioChecked;
>        }
>    }
>
>    private void RadioChecked(object sender, RoutedEventArgs e)
>    {
>        var radio = (RadioButton)sender;
>        if (radio.IsChecked == true)
>        {
>            var item = ItemContainerGenerator.ItemFromContainer(radio);
>            SelectedItem = item;
>        }
>    }
>    // (RadioSelector.cs から引用)
> ```

### 完成

以上で完成となります。Selector の薄いラッパーなので、コード量は少なく済みました

## 使用例

このコントロールを使う側の実装は ListBox と同様で、ユーザーからは普通の RadioButton と同様の表示・操作性です

Selector のおかげで、SelectedValue, SelectedValuePath, DisplayMemberPath などのプロパティがそのまま動きます

```xml
    <local:RadioSelector Focusable="False"
        GroupName="GroupBasic"
        ItemsSource="{Binding Items}"
        SelectedItem="{Binding SelectedItem}"
        SelectedValue="{Binding SelectedValue}"
        SelectedValuePath="Value"
        DisplayMemberPath="Display" />
    <!-- (RadioSelectorExample.xaml から引用、一部調整・削除) -->
```

## 評価

- IsChecked + Converter と比べると、RadioSelector のほうが「WPF らしさ」があってうれしい
- ItemsControl と比べると、RadioButton のリストを扱ってるのに選択値へのバインディングがないという中途半端さがない

しかし実用面で、たいした優位はなさそうです。WPF 側に RadioSelector があるならともかく、新たにコントロールを用意するほどではないかもしれません



[ItemsControl]: https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.controls.itemscontrol
[ListBox.cs]: https://github.com/dotnet/wpf/blob/564072fd2bd5200c2a440684061549de3bb730bf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/ListBox.cs
[ListBox]: https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.controls.listbox
[RadioButton]: https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.controls.radiobutton
[RadioSelector.cs]: https://github.com/vain0x/wpf-learn/blob/e5026a3856793675b647d68ba8a2b5f24a6be435/WpfLearn/Examples/RadioSelector.cs
[RadioSelectorExample]: https://github.com/vain0x/wpf-learn/blob/e5026a3856793675b647d68ba8a2b5f24a6be435/WpfLearn/Examples/RadioSelectorExample.xaml#L16
[Selector]: https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.controls.primitives.selector
