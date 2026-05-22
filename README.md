# KS Assets Cloner
指定したアセットを参照関係を保持しながら複製するエディタ拡張

## 概要
指定したアセットを依存関係を維持したまま複製します。  
フォルダを指定して一括で複製したり、一部は複製しない設定にすることもできます。

作成にあたり Narazaka さんの CopyAssetsWithDependency のソースコードを参考・改変して使用させていただきました。  
https://github.com/Narazaka/CopyAssetsWithDependency

## インストール
### VCC(ALCOM)を利用する方法
1. https://k_stand.github.io/vpm-repos/ の`Add to VCC`を押してVCCにリポジトリを追加します。
2. 導入したいプロジェクトに`KS Assets Cloner`をインストールしてください。

### VPAI unitypackageでVCCにインストールする方法
1. 以下から任意のバージョンの`com.github.k-stand.ksassetscloner.X.x.x-installer.unitypackage`をダウンロードして、導入したいプロジェクトにインポートしてください。

2.x.x : [com.github.k-stand.ksassetscloner.2.x.x-installer.unitypackage](https://github.com/k-stand/KSAssetsCloner/releases/download/2.0.0/com.github.k-stand.ksanimatorclipboard.2.x.x-installer.unitypackage)

## 使用方法
### 基本的な使い方
1. Projectウィンドウで任意のディレクトリで右クリックをし、`Create`->`K-Stand`->`KS Assets Cloner`を選択します。
2. 作成した`KS Assets Cloner`アセットのInspectorで、`複製するアセット(ファイル・フォルダ)`と書かれたリストに複製したいアセットを設定します。
3. リネーム欄の設定を行うと、複製されたアセット名のリネームや、後述する保存先のサブディレクトリに出力先を変更できます。
4. 保存先のパスを指定します。直接パスを入力するか、`...`ボタンを押して保存先のディレクトリを選択します。  
ボタンから選択した場合、相対パスが設定されます。  
パスの設定についての詳しい説明は下記を参照してください。
5. `複製`ボタンを押すとリストに指定されたアセット及びフォルダ内のアセットが、依存関係を維持したまま複製されます。

### 詳しい説明

#### パスの指定について
`Assets/`、`Packages/`から始まるパスは絶対パスとして解釈されます。  
`./`から始まるパスは相対パスとして解釈されます。  
保存先は同プロジェクト内の`Assets`フォルダか`Packages`フォルダ内である必要があります。  
存在しないディレクトリの場合、複製時に作成されます。

#### リネームについて
リネーム欄が入力されている場合、そのアセットのクローンの名前はリネーム欄で指定した名前に変更されます。  
相対パスを入力すると、保存先として指定したフォルダの中であればサブフォルダにクローンの出力先を変更できます。  
相対パス入力時に先頭に`./`の入力は不要です。
相対パスによる出力先の指定が保存先のフォルダの外になる場合クローンに失敗します。

#### 複製しない設定
複製リストのアセット指定部の左側のチェックボックスからチェックを外すと、そのアセットは複製されなくなります。  
後述の複製リストの優先度と組み合わせることで細かく複製可否の設定をできます

#### 複製リストの順序と優先度について
同じアセット、もしくは同じアセットを含むフォルダを複製リストに複数設定している場合、リストのより下の設定が優先されます。  
この仕様を利用して後述するように複製・保存先の設定を変更できます。

#### フォルダの中身の一部を保存場所を変更して複製する
複製リストの順序を適切に設定することで、「フォルダを丸ごと複製するが、フォルダ内の一部のアセットは例外的に出力先ディレクトリに複製する」という設定が可能です。  
< 例 >  
以下のようにアセットが存在する時、
```
Assets/
├SrcFolder/
│└FolderA/
│ ├Asset1.asset
│ └FolderB/
│  └FolderC/
│   └Asset2.asset
└DistFolder/
```
`Assets/SrcFolder/FolderA`が複製リストに設定されていて、保存先が`Assets/DistFolder`と設定されている状態で複製を実行すると、通常は以下のようになります。
```
Assets/
├SrcFolder/
│└FolderA/
│ ├Asset1.asset
│ └FolderB/
│  └FolderC/
│   └Asset2.asset
└DistFolder/
 └FolderA/
  ├Asset1.asset
  └FolderB/
   └FolderC/
    └Asset2.asset
```
この時、複製リストに2つ目の項目を追加し、`Assets/SrcFolder/FolderA/FolderB/FolderC`を複製する設定を追加すると、`FolderC`は`Assets/DistFolder`に保存するように設定が上書きされるため、複製を実行すると以下のような結果になります。
```
Assets/
├SrcFolder/
│└FolderA/
│ ├Asset1.asset
│ └FolderB/
│  └FolderC/
│   └Asset2.asset
└DistFolder/
 ├FolderA/
 │├Asset1.asset
 │└FolderB/
 └FolderC/
  └Asset2.asset
```

#### フォルダの中身の一部を複製しないように設定する
複製リストの順序と、複製<u>**しない**</u>設定を適切に設定することで、「フォルダを丸ごと複製するが、フォルダ内の一部のアセットは例外的に複製しない」という設定が可能です。  
< 例 >  
以下のようにアセットが存在する時、
```
Assets/
├SrcFolder/
│└FolderA/
│ ├Asset1.asset
│ └FolderB/
│  └FolderC/
│   └Asset2.asset
└DistFolder/
```
`Assets/SrcFolder/FolderA`が複製リストに設定されていて、保存先が`Assets/DistFolder`と設定されている状態で複製を実行すると、通常は以下のようになります。
```
Assets/
├SrcFolder/
│└FolderA/
│ ├Asset1.asset
│ └FolderB/
│  └FolderC/
│   └Asset2.asset
└DistFolder/
 └FolderA/
  ├Asset1.asset
  └FolderB/
   └FolderC/
    └Asset2.asset
```
この時、複製リストに2つ目の項目を追加し、`Assets/SrcFolder/FolderA/FolderB/FolderC`を複製<u>**しない**</u>設定を追加すると、`FolderC`は複製を行わないように設定が上書きされるため、複製を実行すると以下のような結果になります。
```
Assets/
├SrcFolder/
│└FolderA/
│ ├Asset1.asset
│ └FolderB/
│  └FolderC/
│   └Asset2.asset
└DistFolder/
 └FolderA/
  ├Asset1.asset
  └FolderB/
```

## ライブラリとしての利用
クローン機能は `KSAssetsClonerLib` として公開されており、スクリプトから直接呼び出すことができます。  
名前空間: `com.github.k_stand.ksassetscloner.editor`

### 主なAPI

#### `KSAssetsClonerLib.ResolveAndValidateDistDir`
出力先パスの解決と検証を行います。

```csharp
string fullDistDir = KSAssetsClonerLib.ResolveAndValidateDistDir(distDir, baseDir, out string errorMessage);
if (fullDistDir == null)
{
    Debug.LogError(errorMessage);
    return;
}
```

| 引数 | 型 | 説明 |
|---|---|---|
| `distDir` | `string` | `"Assets/"`・`"Packages/"`始まりの絶対パス、または`"./"`始まりの相対パス |
| `baseDir` | `string` | 相対パス解決の基準となるディレクトリの絶対パス |
| `errorMessage` (out) | `string` | 検証失敗時のエラーメッセージ。成功時は `null` |
| 戻り値 | `string` | 正規化済みの絶対パス。無効な場合は `null` |

---

#### `KSAssetsClonerLib.BuildCloneInfoMap`
クローン対象アセットのリストからクローン情報のマップを構築します。  
重複回避のインデックス付与やフォルダの再帰展開もここで行われます。

```csharp
var inputs = new List<CloneAssetInput>
{
    new CloneAssetInput(asset: myAsset, doClone: true, rename: ""),
    new CloneAssetInput(asset: myFolder, doClone: true, rename: "NewFolderName"),
};

var map = KSAssetsClonerLib.BuildCloneInfoMap(inputs, fullDistDir, out string errorMessage);
if (map == null)
{
    Debug.LogError(errorMessage);
    return;
}
```

| 引数 | 型 | 説明 |
|---|---|---|
| `inputs` | `IEnumerable<CloneAssetInput>` | クローン対象アセットの入力リスト |
| `fullDistDir` | `string` | 出力先ディレクトリの絶対パス |
| `errorMessage` (out) | `string` | 失敗時のエラーメッセージ。成功時は空文字 |
| 戻り値 | `Dictionary<string, AssetCloneInfo>` | 元ファイルの絶対パスをキーとするクローン情報辞書。失敗時は `null` |

---

#### `KSAssetsClonerLib.ExecuteClone`
構築したクローン情報リストを元に、実際のクローン処理を実行します。  
ファイルコピー → Import → GUID書き換え → Refresh の順で処理されます。

```csharp
KSAssetsClonerLib.ExecuteClone(map.Values.ToList());
```

| 引数 | 型 | 説明 |
|---|---|---|
| `cloneInfos` | `List<AssetCloneInfo>` | `BuildCloneInfoMap` で構築したマップの `Values` |

---

### CloneAssetInput

| プロパティ | 型 | 説明 |
|---|---|---|
| `Asset` | `UnityEngine.Object` | クローン対象のアセット（ファイル・フォルダ） |
| `DoClone` | `bool` | このアセットをクローンするかどうか |
| `Rename` | `string` | クローン後のアセット名。空文字の場合は元の名前を使用 |

### AssetCloneInfo

| プロパティ | 型 | 説明 |
|---|---|---|
| `OriginalFullPath` | `string` | クローン元の絶対パス |
| `CloneFullPath` | `string` | クローン先の絶対パス |
| `DoClone` | `bool` | クローンを実行するかどうか |
| `IsFolder` | `bool` | フォルダかどうか |
| `Rename` | `string` | クローン後のアセット名。空文字の場合は元の名前を使用 |

### 使用例

```csharp
using com.github.k_stand.ksassetscloner.editor;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MyCloneScript
{
    [MenuItem("Tools/Clone My Assets")]
    public static void CloneAssets()
    {
        // クローン対象のアセット
        var targetAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/MyAsset.mat");

        // 出力先パスの解決と検証
        string baseDir = Application.dataPath; // 例: Assetsフォルダを基準にする
        string fullDistDir = KSAssetsClonerLib.ResolveAndValidateDistDir(
            "Assets/ClonedAssets", baseDir, out string validationError);

        if (fullDistDir == null)
        {
            EditorUtility.DisplayDialog("エラー", validationError, "OK");
            return;
        }

        // クローン情報マップの構築
        var inputs = new[]
        {
            new CloneAssetInput(asset: targetAsset, doClone: true, rename: "MyAsset_Clone"),
        };

        var map = KSAssetsClonerLib.BuildCloneInfoMap(inputs, fullDistDir, out string buildError);
        if (map == null)
        {
            EditorUtility.DisplayDialog("エラー", buildError, "OK");
            return;
        }

        // クローンの実行
        KSAssetsClonerLib.ExecuteClone(map.Values.ToList());

        Debug.Log("クローン完了");
    }
}
```

## License
[Zlib License](https://github.com/k-stand/KSAssetsCloner/blob/main/LICENSE.txt)

## 更新履歴
[YYYY-MM-DD] x.x.x  
- (次バージョンの変更内容をここに記載)

[2026-05-23] 2.0.2  
- リネーム機能が動作しない問題

[2026-05-23] 2.0.1  
- package.jsonのLicense設定が間違っている問題の修正
- README.mdのリンクの誤りの修正

[2026-05-23] 2.0.0  
- 作者名義の変更に伴い、アセンブリ、名前空間の変更、及び新リポジトリへの移行
- システムの全体的なリファクタリング
- クローン機能をライブラリとして公開
- クローンフォルダ、クローンファイルの名前を指定できるリネーム機能を追加

[2026-02-05] 1.0.1  
- README.mdに内容を追加  
- その他、軽微な不具合の修正及び調整

[2026-02-02] 1.0.0  
- 公開