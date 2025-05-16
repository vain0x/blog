---
title: 'RadioButton: IsChecked へのバインディング'
type: "post"
date: 2025-05-17
url: 2025-05-17/radio-check-binding
tags:
  - CSharp
  - WPF
---

WPF の RadioButton に選択されている値をバインディングする方法を記述しています

<!--more-->

## RadioButton へのバインディング

[RadioButton] は複数の選択肢から排他的に1つを選択する UI です。UI の状態は、選択されている値を1つ持つことで表現できます

しかし、「選択されている値」をバインディングするための依存関係プロパティは用意されていません。代わりに、ラジオボタンの `IsChecked` プロパティにバインディングすることにします

### 準備

前提として、選択肢となる適当な `enum` 型があるとします

```csharp
// file: MyEnum.cs
namespace WpfLearn.Examples;

enum MyEnum
{
    A,
    B,
}
```

ビューモデルに、選択されている値を持つプロパティがあるとします

```csharp
// file: ViewModel.cs
namespace WpfLearn.Examples;

public partial class ViewModel : ObservableObject // implements INotifyPropertyChanged
{
    [ObservableProperty]
    public partial MyEnum? SelectedValue { get; set; }
}
```

ラジオボタン A, B を用意して、その選択状態をビューモデルにバインディングしたい、というのが今回のタスクです

```
    [UI イメージ]                [DataContext]

    (o) A                       SelectedValue=A
    ( ) B
```

### 実装

選択されている値 (`MyEnum?`) から選択状態 (`bool`) への変換を行う [コンバーター] を定義します

```csharp
// file: RadioCheckConverter.cs
namespace WpfLearn.Examples;

[ValueConversion(typeof(object), typeof(bool))]
public class RadioCheckConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Equals(value, parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isChecked = (bool?)value;
        if (isChecked == true)
        {
            return parameter;
        }

        return DependencyProperty.UnsetValue;
    }
}
```

それぞれのメソッドについて:

- `Convert`:
    - `value` パラメーターは選択値が来ます。コンバーターを汎用的に扱えるように `object?` 型で宣言していますが、実行時は `MyEnum?` 型の値が入っています
    - `parameter` に、ラジオボタンの値が来ます (`MyEnum.A` または `B`)。この値はラジオボタンの定義時に指定します。こちらも実行時は `MyEnum?` 型の値が入ります
    - 両値が等しければ true (= 選択されている) を返せばいいです
- `ConvertBack`:
    - `value` はチェック状態 (`bool?`) が来ます
    - `parameter` は Converter と同じく、ラジオボタンの値が来ます
    - Convert とは逆に、true (= 選択されている) なら選択値を返します
        - これにより、ラジオボタンがチェックされたときに選択された値がバインディング先のプロパティにセットされます
    - 非チェック状態なら [Unset] (値なし) とします

コンバーターのインスタンスを StaticResource として登録しておきます (`App.xaml` の `Resources` 内に置く)

```xml
<!-- file: App.xaml
    xmlns:local="clr-namespace:WpfLearn.Examples" が宣言されていると仮定 -->

    <App.Resources>
        <local:RadioCheckConverter x:Key="RadioCheckConverter" />
    </App.Resources>
```

以上で準備が完了しました

ラジオボタンを UI 上に置き、次のように Binding のコンバーターとパラメーターを指定します

```xml
<!-- file: View.xaml
    xmlns:local="clr-namespace:WpfLearn.Examples" が宣言されていると仮定 -->

<StackPanel>
    <RadioButton IsChecked="{Binding SelectedValue,
        Converter={StaticResource RadioCheckConverter},
        ConverterParameter={x:Static local:MyEnum.A}}">
        A
    </RadioButton>

    <RadioButton IsChecked="{Binding SelectedValue,
        Converter={StaticResource RadioCheckConverter},
        ConverterParameter={x:Static local:MyEnum.B}}">
        B
    </RadioButton>
</StackPanel>
```

- `Converter={ ... }`
    - 前述で定義したコンバーターのインスタンスを指定しています
- `ConverterParameter={ ... }`
    - このラジオボタンが表す値を指定しています
    - `{x:Static}` 拡張を使うことで、enum のメンバーや、`static` 宣言されているフィールド・プロパティの値を XAML の中で使えます

## おまけ: `null` の選択肢

ラジオボタンは一度チェックをつけたら外せないので、選択肢を省略可能にする場合は、省略用の選択肢を用意します (例えば「該当なし」)

該当なしの値を `null` にするには、`ConverterParameter` を `null` にすればよいです (指定しなければそうなる)。このラジオボタンは `SelectedValue` が `null` の場合にチェック状態となります

```xml
    <RadioButton IsChecked="{Binding SelectedValue,
        Converter={StaticResource RadioCheckConverter}}">
        該当なし
    </RadioButton>
```

## まとめ

- 選択されている値をデータバインディングで管理するには、ラジオボタンごとに `IsChecked` へ選択状態をバインディングする
- `Converter` によって「選択されている値」→「チェック状態」の変換を行う
- `ConverterParameter` でラジオボタンの値を設定する



[RadioButton]: https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.controls.radiobutton
[コンバーター]: https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/data/how-to-convert-bound-data?view=netframeworkdesktop-4.8
[Unset]: https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.dependencyproperty.unsetvalue?view=windowsdesktop-9.0
