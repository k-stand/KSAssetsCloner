namespace com.github.k_stand.ksassetscloner.editor
{
    /// <summary>
    /// アセットのクローンに関する情報を保持するクラス
    /// </summary>
    public record AssetCloneInfo
    {
        /// <summary>クローン元のフルパス</summary>
        public string OriginalFullPath { get; private set; }

        /// <summary>クローン先のフルパス</summary>
        public string CloneFullPath { get; private set; }

        /// <summary>クローンを実行するかどうか</summary>
        public bool DoClone { get; private set; }

        /// <summary>フォルダかどうか</summary>
        public bool IsFolder { get; private set; }

        /// <summary>クローンのリネーム（空文字列でリネーム無し）</summary>
        public string Rename { get; private set; }

        public AssetCloneInfo(string originalFullPath, string cloneFullPath, bool doClone = true, bool isFolder = false, string rename = "")
        {
            OriginalFullPath = originalFullPath;
            CloneFullPath = cloneFullPath;
            DoClone = doClone;
            IsFolder = isFolder;
            Rename = rename;
        }
    }
}