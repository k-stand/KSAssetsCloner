/*
Portions of this code are based on https://qiita.com/k7a/items/eb5a3ee4ed6448343543 by k7a
and https://github.com/Narazaka/CopyAssetsWithDependency by Narazaka.
*/
/*
Copyright (c) 2020 Narazaka
Copyright (c) 2026 KiriumeStand

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

   1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.

   2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.

   3. This notice may not be removed or altered from any source
   distribution.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.kiriumestand.ksassetscloner.editor
{
    [CustomEditor(typeof(KSAssetsCloner))]
    public class KSAssetsClonerEditor : Editor
    {
        [SerializeField]
        private StyleSheet styleSheet;

        [SerializeField]
        private VisualTreeAsset visualTreeAsset;

        /// <summary>
        /// YAML形式のEncoding
        /// </summary>
        private static Encoding Encoding { get { return Encoding.GetEncoding("UTF-8"); } }

        private static string ProjectRootDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../"));

        public sealed override VisualElement CreateInspectorGUI()
        {
            // UXML をインスタンス化
            VisualElement uxml = visualTreeAsset.CloneTree();
            // ussを適用
            uxml.styleSheets.Add(styleSheet);

            KSAssetsCloner castedTargetObject = serializedObject.targetObject as KSAssetsCloner;

            ListView u_CloneAssets = BindHelper.Bind<ListView>(uxml, UxmlNames.CloneAssets, serializedObject, nameof(KSAssetsCloner._CloneAssets));
            TextField u_DistDir = BindHelper.Bind<TextField>(uxml, UxmlNames.DistDir, serializedObject, nameof(KSAssetsCloner._DistDir));

            u_CloneAssets.itemsAdded += (e) =>
            {
                foreach (int i in e)
                {
                    castedTargetObject._CloneAssets[i] = new() { _DoClone = true, _CloneAsset = null };
                }
            };

            Button u_ReferenceButton = uxml.Q<Button>(UxmlNames.ReferenceButton);
            u_ReferenceButton.clicked += () =>
            {
                OnReferenceButtonClickedEventHandler(uxml, serializedObject);
            };
            Button u_CloneButton = uxml.Q<Button>(UxmlNames.CloneButton);
            u_CloneButton.clicked += () =>
            {
                OnCloneButtonClickedEventHandler(uxml, serializedObject);
            };

            return uxml;
        }

        private static void OnReferenceButtonClickedEventHandler(VisualElement uxml, SerializedObject so)
        {
            string thisAssetDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(so.targetObject));
            string selectDir = EditorUtility.OpenFolderPanel("title", thisAssetDir, "");
            if (selectDir != "")
            {
                string relativePath = "./" + GetRelativePath(Path.GetFullPath(thisAssetDir + "\\"), selectDir + "\\");
                KSAssetsCloner assetCloner = so.targetObject as KSAssetsCloner;
                assetCloner._DistDir = relativePath;
            }
        }

        private static void OnCloneButtonClickedEventHandler(VisualElement uxml, SerializedObject so)
        {
            SerializedProperty cloneAssetsSP = so.FindProperty(nameof(KSAssetsCloner._CloneAssets));
            SerializedProperty distDirSP = so.FindProperty(nameof(KSAssetsCloner._DistDir));

            string currentDir = Environment.CurrentDirectory;

            string thisAssetFullPath = Path.GetFullPath(AssetDatabase.GetAssetPath(so.targetObject));
            string thisAssetFullDir = Path.GetDirectoryName(thisAssetFullPath);

            string distDir = distDirSP.stringValue;
            // 出力先ディレクトリ(正規化済)
            string fullDistDir = "";
            if (distDir.StartsWith("./") || distDir == "")
            {
                fullDistDir = Path.GetFullPath($"{thisAssetFullDir}/{distDir}");
            }
            else
            {
                if (distDir.StartsWith("Assets/") || distDir.StartsWith("Packages/"))
                {
                    fullDistDir = Path.GetFullPath($"{ProjectRootDir}/{distDir}");
                }
                else
                {
                    EditorUtility.DisplayDialog("KS Assets Cloner", "絶対パスで保存する場合、\"Assets/\"か\"Packages/\"で始まる必要があります", "OK");
                    return;
                }
            }

            if (!fullDistDir.Contains(ProjectRootDir + "Assets\\") && !fullDistDir.Contains(ProjectRootDir + "Packages\\"))
            {
                EditorUtility.DisplayDialog("KS Assets Cloner", "保存先はプロジェクトの\"Assets/\"か\"Packages/\"フォルダ内でないといけません", "OK");
                return;
            }

            Dictionary<string, CloneInfo> assetPath2CloneInfoMap = new();
            for (int i = 0; i < cloneAssetsSP.arraySize; i++)
            {
                SerializedProperty elementSP = cloneAssetsSP.GetArrayElementAtIndex(i);
                CloneAsset cloneAssetInfo = elementSP.managedReferenceValue as CloneAsset;
                string basePath = AssetDatabase.GetAssetPath(cloneAssetInfo._CloneAsset);

                if (Directory.Exists(basePath))
                {
                    // フォルダの場合
                    string baseDistPath = $"{fullDistDir}/{Path.GetFileName(basePath)}";
                    string[] excludePaths = assetPath2CloneInfoMap.Where(kvp => kvp.Value.DoClone == true).Select(kvp => kvp.Value.CloneFullPath).ToArray();
                    // 必要に応じてインデックス付きのクローンパスを取得
                    string fixedBaseDistPath = GetFixedSavePath(baseDistPath, excludePaths, true);

                    string baseFullPath = Path.GetFullPath(basePath);

                    assetPath2CloneInfoMap[baseFullPath] = new() { OriginalFullPath = baseFullPath, CloneFullPath = fixedBaseDistPath, IsFolder = true, DoClone = cloneAssetInfo._DoClone };

                    foreach (string filePath in Directory.EnumerateFiles(baseFullPath, "*", SearchOption.AllDirectories))
                    {
                        if (Path.GetExtension(filePath) == ".meta")
                            continue;
                        string relativeFilePath = GetRelativePath(baseFullPath + "/", filePath);
                        assetPath2CloneInfoMap[filePath] = new() { OriginalFullPath = filePath, CloneFullPath = Path.Combine(fixedBaseDistPath, relativeFilePath), IsFolder = false, DoClone = cloneAssetInfo._DoClone };
                    }

                    foreach (string filePath in Directory.EnumerateDirectories(baseFullPath, "*", SearchOption.AllDirectories))
                    {
                        string relativeFolderPath = GetRelativePath(baseFullPath + "/", filePath);
                        assetPath2CloneInfoMap[filePath] = new() { OriginalFullPath = filePath, CloneFullPath = Path.Combine(fixedBaseDistPath, relativeFolderPath), IsFolder = true, DoClone = cloneAssetInfo._DoClone };
                    }
                }
                else
                {
                    // ファイルの場合
                    string baseDistPath = $"{fullDistDir}/{Path.GetFileName(basePath)}";
                    string[] excludePaths = assetPath2CloneInfoMap.Where(kvp => kvp.Value.DoClone == true).Select(kvp => kvp.Value.CloneFullPath).ToArray();
                    // 必要に応じてインデックス付きのクローンパスを取得
                    string fixedBaseDistPath = GetFixedSavePath(baseDistPath, excludePaths, false);

                    string baseFullPath = Path.GetFullPath(basePath);

                    assetPath2CloneInfoMap[baseFullPath] = new() { OriginalFullPath = baseFullPath, CloneFullPath = fixedBaseDistPath, IsFolder = false, DoClone = cloneAssetInfo._DoClone };
                }
            }

            int cloneCount = assetPath2CloneInfoMap.Values.Count(ci => ci.DoClone);
            bool clickedOK = EditorUtility.DisplayDialog("KS Assets Cloner", $"{cloneCount}個のアセットを複製します", "OK", "Cancel");
            if (!clickedOK) return;

            foreach (CloneInfo cloneInfo in assetPath2CloneInfoMap.Values)
            {
                cloneInfo.OriginalGUID = AssetDatabase.AssetPathToGUID(cloneInfo.OriginalPath);
            }

            foreach (CloneInfo cloneInfo in assetPath2CloneInfoMap.Values)
            {
                // アセットのコピーを行う
                if (!cloneInfo.DoClone) continue;

                if (cloneInfo.IsFolder)
                {
                    if (!Directory.Exists(cloneInfo.CloneFullPath))
                        Directory.CreateDirectory(cloneInfo.CloneFullPath);
                }
                else
                {
                    string cloneDir = Path.GetDirectoryName(cloneInfo.CloneFullPath);
                    if (!Directory.Exists(cloneDir))
                        Directory.CreateDirectory(cloneDir);
                    File.Copy(cloneInfo.OriginalFullPath, cloneInfo.CloneFullPath);
                }

                // 一旦ImportしてUnityにGUIDを生成させる
                string clonePath = cloneInfo.ClonePath;
                AssetDatabase.ImportAsset(clonePath);
                cloneInfo.CloneGUID = AssetDatabase.AssetPathToGUID(clonePath);

                // メタファイルをGUIDを書き換えてコピーする
                using (StreamReader sr = new($"{cloneInfo.OriginalFullPath}.meta", Encoding))
                {
                    string s = sr.ReadToEnd();
                    // GUIDを置換
                    s = s.Replace(cloneInfo.OriginalGUID, cloneInfo.CloneGUID);

                    using (FileStream fs = new($"{cloneInfo.CloneFullPath}.meta", FileMode.Truncate, FileAccess.Write))
                    {
                        using (StreamWriter sw = new(fs))
                        {
                            sw.Write(s);
                        }
                    }
                }
            }

            // アセットの再読み込みを行う
            // 書き換え直後にImportすると上手く読み込んでくれないことがあるので一通り処理した後に回している
            IEnumerable<string> clonePaths = assetPath2CloneInfoMap.Where(kvp => kvp.Value.DoClone).Select(kvp => kvp.Value.ClonePath);
            foreach (string clonePath in clonePaths)
                AssetDatabase.ImportAsset(clonePath);

            foreach (CloneInfo cloneInfo in assetPath2CloneInfoMap.Values)
            {
                if (!cloneInfo.DoClone) continue;
                if (!File.Exists(cloneInfo.OriginalFullPath)) continue;

                // prefabやanimationが参照するGUIDの書き換えを行う
                using (StreamReader sr = new(cloneInfo.OriginalFullPath, Encoding))
                {
                    string s = sr.ReadToEnd();
                    // YAML形式の場合のみ参照先のGUIDの書き換え処理
                    if (s.StartsWith("%YAML"))
                    {
                        foreach (CloneInfo cloneInfo2 in assetPath2CloneInfoMap.Values)
                        {
                            if (!cloneInfo2.DoClone) continue;
                            s = s.Replace(cloneInfo2.OriginalGUID, cloneInfo2.CloneGUID);
                        }

                        using (StreamWriter sw = new(cloneInfo.CloneFullPath, false, Encoding))
                        {
                            sw.Write(s);
                        }
                    }
                }
            }

            // 再読み込みを走らせる（ImportAssetだと上手くいかない）
            AssetDatabase.Refresh();
        }

        private static string GetFixedSavePath(string path, string[] excludePaths, bool folderMode)
        {
            string fileDir = Path.GetDirectoryName(path);
            string fileName = folderMode ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
            string fileExtension = folderMode ? "" : Path.GetExtension(path);
            string fixedBasePath = $"{fileDir}/{fileName}{fileExtension}";

            int index = 0;
            while (
                Directory.Exists(fixedBasePath) ||
                File.Exists(fixedBasePath) ||
                excludePaths.Contains(fixedBasePath)
            )
            {
                index++;
                fixedBasePath = $"{fileDir}/{fileName}({index}){fileExtension}";
            }

            return fixedBasePath;
        }

        private static string GetRelativePath(string fromPath, string toPath)
        {
            return Uri.UnescapeDataString(new Uri(fromPath).MakeRelativeUri(new Uri(toPath)).ToString());
        }

        private record CloneInfo
        {
            internal string OriginalFullPath { get; set; }
            internal string OriginalPath => GetRelativePath(ProjectRootDir, OriginalFullPath);
            internal string CloneFullPath { get; set; }
            internal string ClonePath => GetRelativePath(ProjectRootDir, CloneFullPath);
            internal string OriginalGUID { get; set; }
            internal string CloneGUID { get; set; }
            internal bool DoClone { get; set; }
            internal bool IsFolder { get; set; }
        }

        internal record UxmlNames
        {
            internal static readonly string CloneAssets = "CloneAssets";
            internal static readonly string DistDir = "DistDir";
            internal static readonly string ReferenceButton = "ReferenceButton";
            internal static readonly string CloneButton = "CloneButton";
        }
    }
}