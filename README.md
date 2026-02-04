# KS Assets Cloner
指定したアセットを参照関係を保持しながら複製するエディタ拡張

## 概要
指定したアセットを依存関係を維持したまま複製します。  
フォルダを指定して一括で複製したり、一部は複製しない設定にすることもできます。

作成にあたり Narazaka さんの CopyAssetsWithDependency のソースコードを参考・改変して使用させていただきました。  
https://github.com/Narazaka/CopyAssetsWithDependency

## インストール
### VCC(ALCOM)を利用する方法
1. https://kiriumestand.github.io/vpm-repos/ の`Add to VCC`を押してVCCにリポジトリを追加します。
2. 導入したいプロジェクトに`KS Assets Cloner`をインストールしてください。

### VPAI unitypackageでVCCにインストールする方法
1. https://github.com/KiriumeStand/KSAssetsCloner/releases/latest から`io.github.kiriumestand.ksassetscloner-installer.unitypackage`をダウンロードして、導入したいプロジェクトにインポートしてください。

## 使用方法
### 基本的な使い方
1. Projectウィンドウで任意のディレクトリで右クリックをし、`Create`->`Kiriume Stand`->`KS Assets Cloner`を選択します。
2. 作成した`KS Assets Cloner`アセットのInspectorで、`複製するアセット・フォルダ`と書かれたリストに複製したいアセットもしくはフォルダを設定します。
3. 保存先のパスを指定します。直接パスを入力するか、`...`ボタンを押して保存先のディレクトリを選択します。  
ボタンから選択した場合、相対パスが設定されます。  
パスの設定についての詳しい説明は下記を参照してください。
4. `複製`ボタンを押すとリストに指定されたアセット、フォルダ、及びフォルダ内のアセットが、依存関係を維持したまま複製されます。

### 詳しい説明

#### パスの指定について
`Assets/`、`Packages/`から始まるパスは絶対パスとして解釈されます。  
`./`から始まるパスは相対パスとして解釈されます。  
保存先は同プロジェクト内の`Assets`フォルダか`Packages`フォルダ内である必要があります。
存在しないディレクトリの場合、複製時に作成されます。

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

## License
[Zlib License](https://github.com/KiriumeStand/KSAssetsCloner/blob/main/LICENSE.txt)

## 更新履歴
[2026-02-02] 1.0.0  
公開
