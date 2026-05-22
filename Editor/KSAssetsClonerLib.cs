using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace com.github.k_stand.ksassetscloner.editor
{

    /// <summary>
    /// アセットクローン処理のコアライブラリ。
    /// UIや SerializedObject に依存しない静的APIを提供します。
    /// </summary>
    public static class KSAssetsClonerLib
    {
        /// <summary>YAML形式ファイルの文字コード</summary>
        private static Encoding YamlEncoding => Encoding.GetEncoding("UTF-8");

        /// <summary>プロジェクトルートディレクトリの絶対パス</summary>
        public static string ProjectRootDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../"));

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// 指定されたアセット群から、コピー先パスとのマッピング（CloneInfo辞書）を構築します。
        /// ファイル・フォルダの展開、重複回避のインデックス付与を行います。
        /// </summary>
        /// <param name="inputs">クローン対象のアセット入力リスト</param>
        /// <param name="fullDistDir">コピー先ディレクトリの絶対パス</param>
        /// <param name="errorMessage">失敗した場合のエラーメッセージ</param>
        /// <returns>元ファイルの絶対パスをキーとするCloneInfo辞書</returns>
        public static Dictionary<string, AssetCloneInfo> BuildCloneInfoMap(
            IEnumerable<CloneAssetInput> inputs,
            string fullDistDir,
            out string errorMessage)
        {
            Dictionary<string, AssetCloneInfo> map = new();

            bool addRes = true;
            errorMessage = "";
            foreach (CloneAssetInput input in inputs)
            {
                string basePath = AssetDatabase.GetAssetPath(input.Asset);
                if (string.IsNullOrEmpty(basePath)) continue;

                if (Directory.Exists(basePath))
                    addRes = AddFolderToMap(map, basePath, fullDistDir, input.DoClone, input.Rename, out errorMessage);
                else
                    addRes = AddFileToMap(map, basePath, fullDistDir, input.DoClone, input.Rename, out errorMessage);

                if (!addRes) break;
            }

            if (!addRes)
            {
                return null;
            }

            return map;
        }

        /// <summary>
        /// CloneInfo辞書に基づいてアセットのクローンを実行します。
        /// ファイルコピー → Import → GUID書き換え → Refresh の順で処理します。
        /// </summary>
        /// <param name="cloneInfos">BuildCloneInfoMap で構築したマップ</param>
        public static void ExecuteClone(List<AssetCloneInfo> cloneInfos)
        {
            // Step 1: ファイル・フォルダのコピーと初回Import（UnityにGUIDを生成させる）
            foreach (AssetCloneInfo info in cloneInfos)
            {
                if (!info.DoClone) continue;

                if (info.IsFolder)
                {
                    if (!Directory.Exists(info.CloneFullPath))
                        Directory.CreateDirectory(info.CloneFullPath);
                }
                else
                {
                    string cloneDir = Path.GetDirectoryName(info.CloneFullPath);
                    if (!Directory.Exists(cloneDir))
                        Directory.CreateDirectory(cloneDir);
                    File.Copy(info.OriginalFullPath, info.CloneFullPath);
                }
            }

            List<string> pathes = cloneInfos.Where(i => i.DoClone).Select(i => GetUnityRelativePath(i.CloneFullPath)).ToList();
            ImportAssets(pathes);

            // Step 2: .metaファイルのGUIDを書き換えてコピー
            foreach (AssetCloneInfo info in cloneInfos)
            {
                if (!info.DoClone) continue;
                RewriteMetaGuid(info, cloneInfos);
            }

            // Step 3: クローンした全アセットを再Import
            ImportAssets(pathes);

            // Step 4: YAML内の参照GUIDを書き換え
            foreach (AssetCloneInfo info in cloneInfos)
            {
                if (!info.DoClone) continue;
                if (!File.Exists(info.OriginalFullPath)) continue;
                RewriteYamlGuids(info, cloneInfos);
            }

            // Step 5: Unityに変更を反映（ImportAssetだと上手くいかない）
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 出力先ディレクトリの絶対パスを検証します。
        /// Assetsまたは Packages フォルダ配下でなければ null を返します。
        /// </summary>
        /// <param name="distDir">
        /// 相対パス（"./"始まり）またはUnity相対パス（"Assets/"・"Packages/"始まり）
        /// </param>
        /// <param name="baseDir">
        /// distDir が相対パスの場合の基準ディレクトリ絶対パス
        /// </param>
        /// <param name="errorMessage">検証失敗時のエラーメッセージ（成功時はnull）</param>
        /// <returns>正規化済みの絶対パス。無効な場合は null</returns>
        public static string ResolveAndValidateDistDir(string distDir, string baseDir, out string errorMessage)
        {
            errorMessage = null;
            string fullDistDir;

            if (distDir.StartsWith("./") || distDir == "")
            {
                fullDistDir = Path.GetFullPath(Path.Combine(baseDir, distDir) + "/");
            }
            else if (distDir.StartsWith("Assets/") || distDir.StartsWith("Packages/"))
            {
                fullDistDir = Path.GetFullPath(Path.Combine(ProjectRootDir, distDir) + "/");
            }
            else
            {
                errorMessage = "絶対パスで保存する場合、\"Assets/\"か\"Packages/\"で始まる必要があります";
                return null;
            }

            bool insideAssets = fullDistDir.StartsWith(Path.Combine(ProjectRootDir, "Assets\\"));
            bool insidePackages = fullDistDir.StartsWith(Path.Combine(ProjectRootDir, "Packages\\"));
            if (!insideAssets && !insidePackages)
            {
                errorMessage = "保存先はプロジェクトの\"Assets/\"か\"Packages/\"フォルダ内でないといけません";
                return null;
            }

            // 最後の"/"を除去して返す
            return fullDistDir[..^1];
        }

        /// <summary>
        /// 指定パスが既存ファイル・フォルダ、または除外リストと衝突しない
        /// インデックス付きパスを返します。
        /// 例: foo.mat → foo(1).mat → foo(2).mat …
        /// </summary>
        public static string GetNonConflictingPath(string path, string[] excludePaths, bool isFolder)
        {
            string dir = Path.GetDirectoryName(path);
            string name = isFolder ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
            string ext = isFolder ? "" : Path.GetExtension(path);
            string candidate = Path.Combine(dir, $"{name}{ext}");

            int index = 0;
            while (Directory.Exists(candidate) || File.Exists(candidate) || excludePaths.Contains(candidate))
            {
                index++;
                candidate = Path.Combine(dir, $"{name}({index}){ext}");
            }

            return candidate;
        }

        /// <summary>
        /// プロジェクトルートからの相対パスをUnity形式（"Assets/..."）で返します。
        /// </summary>
        public static string GetUnityRelativePath(string fullPath)
            => GetRelativePath(ProjectRootDir, fullPath);

        /// <summary>
        /// fromPath から toPath への相対パスを返します。
        /// </summary>
        public static string GetRelativePath(string fromPath, string toPath)
            => Uri.UnescapeDataString(new Uri(fromPath).MakeRelativeUri(new Uri(toPath)).ToString());

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private static bool AddFolderToMap(
            Dictionary<string, AssetCloneInfo> map,
            string basePath,
            string fullDistDir,
            bool doClone,
            string rename,
            out string errorMessage)
        {
            string folderName = rename != "" ? rename : Path.GetFileName(basePath);
            string baseDistFullPath = Path.GetFullPath(Path.Combine(fullDistDir, folderName));

            if (!baseDistFullPath.StartsWith(fullDistDir + "\\"))
            {
                errorMessage = "ファイルの出力パスが出力フォルダ外になっています";
                return false;
            }

            string[] excluded = ExcludedPaths(map);
            string fixedDistFullPath = GetNonConflictingPath(baseDistFullPath, excluded, true);
            string baseSrcFullPath = Path.GetFullPath(basePath);

            // フォルダ自体
            map[baseSrcFullPath] = new(baseSrcFullPath, fixedDistFullPath, doClone, true);

            // フォルダ内のファイル
            foreach (string filePath in Directory.EnumerateFiles(baseSrcFullPath, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(filePath) == ".meta") continue;
                string rel = GetRelativePath(baseSrcFullPath + "/", filePath);
                map[filePath] = new(filePath, Path.Combine(fixedDistFullPath, rel), doClone, false);
            }

            // フォルダ内のサブフォルダ
            foreach (string dirPath in Directory.EnumerateDirectories(baseSrcFullPath, "*", SearchOption.AllDirectories))
            {
                string rel = GetRelativePath(baseSrcFullPath + "/", dirPath);
                map[dirPath] = new(dirPath, Path.Combine(fixedDistFullPath, rel), doClone, true);
            }

            errorMessage = "";
            return true;
        }

        private static bool AddFileToMap(
            Dictionary<string, AssetCloneInfo> map,
            string basePath,
            string fullDistDir,
            bool doClone,
            string rename,
            out string errorMessage)
        {
            string fileName = rename != "" ? rename + Path.GetExtension(basePath) : Path.GetFileName(basePath);
            string baseDistFullPath = Path.GetFullPath(Path.Combine(fullDistDir, fileName));

            if (!baseDistFullPath.StartsWith(fullDistDir + "\\"))
            {
                errorMessage = "ファイルの出力パスが出力フォルダ外になっています";
                return false;
            }

            string[] excluded = ExcludedPaths(map);
            string fixedDistFullPath = GetNonConflictingPath(baseDistFullPath, excluded, false);
            string baseSrcFullPath = Path.GetFullPath(basePath);

            map[baseSrcFullPath] = new(baseSrcFullPath, fixedDistFullPath, doClone, false);

            errorMessage = "";
            return true;
        }

        private static string[] ExcludedPaths(Dictionary<string, AssetCloneInfo> map)
            => map.Where(kvp => kvp.Value.DoClone).Select(kvp => kvp.Value.CloneFullPath).ToArray();

        private static void ImportAssets(IEnumerable<string> pathes)
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string path in pathes)
                {
                    AssetDatabase.ImportAsset(path);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static void RewriteMetaGuid(AssetCloneInfo info, List<AssetCloneInfo> infos)
        {
            string origGuid = AssetDatabase.AssetPathToGUID(GetUnityRelativePath(info.OriginalFullPath));
            string cloneGuid = AssetDatabase.AssetPathToGUID(GetUnityRelativePath(info.CloneFullPath));

            string metaPath = $"{info.OriginalFullPath}.meta";
            if (!File.Exists(metaPath)) return;

            using StreamReader sr = new(metaPath, YamlEncoding);
            string content = sr.ReadToEnd().Replace(origGuid, cloneGuid);

            using FileStream fs = new($"{info.CloneFullPath}.meta", FileMode.Truncate, FileAccess.Write);
            using StreamWriter sw = new(fs);
            sw.Write(content);
        }

        private static void RewriteYamlGuids(AssetCloneInfo info, List<AssetCloneInfo> infos)
        {
            using StreamReader sr = new(info.OriginalFullPath, YamlEncoding);
            string content = sr.ReadToEnd();

            if (!content.StartsWith("%YAML")) return;

            foreach (AssetCloneInfo other in infos.Where(i => i.DoClone))
            {
                string origGuid = AssetDatabase.AssetPathToGUID(GetUnityRelativePath(other.OriginalFullPath));
                string cloneGuid = AssetDatabase.AssetPathToGUID(GetUnityRelativePath(other.CloneFullPath));
                content = content.Replace(origGuid, cloneGuid);
            }

            using StreamWriter sw = new(info.CloneFullPath, false, YamlEncoding);
            sw.Write(content);
        }
    }

}