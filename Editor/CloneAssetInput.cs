namespace com.github.k_stand.ksassetscloner.editor
{
    /// <summary>
    /// クローン対象となるアセットの入力情報
    /// </summary>
    public record CloneAssetInput
    {
        /// <summary>クローン対象のUnityObject（ファイル・フォルダ）</summary>
        public UnityEngine.Object Asset { get; private set; }

        /// <summary>このアセットをクローンするかどうか</summary>
        public bool DoClone { get; private set; }

        /// <summary>クローン後のアセット名。空文字の場合は元の名前を使用</summary>
        public string Rename { get; private set; }

        public CloneAssetInput(UnityEngine.Object asset, bool doClone = true, string rename = "")
        {
            Asset = asset;
            DoClone = doClone;
            Rename = rename;
        }
    }

}