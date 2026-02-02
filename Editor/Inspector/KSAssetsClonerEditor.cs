// 参考 : https://github.com/Narazaka/CopyAssetsWithDependency/blob/main/CopyAssetsWithDependency.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
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

            ListView u_CloneObjects = Bind<ListView>(uxml, UxmlNames.CloneObjects, serializedObject, nameof(KSAssetsCloner._CloneObjects));
            Toggle u_RelativeDistDir = Bind<Toggle>(uxml, UxmlNames.RelativeDistDir, serializedObject, nameof(KSAssetsCloner._RelativeDistDir));
            TextField u_DistDir = Bind<TextField>(uxml, UxmlNames.DistDir, serializedObject, nameof(KSAssetsCloner._DistDir));
            Toggle u_Clone2Variant = Bind<Toggle>(uxml, UxmlNames.Clone2Variant, serializedObject, nameof(KSAssetsCloner._Clone2Variant));

            Button u_ReferenceButton = uxml.Q<Button>(UxmlNames.ReferenceButton);
            u_ReferenceButton.clicked += () => { };
            Button u_CloneButton = uxml.Q<Button>(UxmlNames.CloneButton);
            u_CloneButton.clicked += () =>
            {
                OnCloneButtonClickedEventHandler(uxml, serializedObject);
            };

            return uxml;
        }

        private static void OnReferenceButtonClickedEventHandler(VisualElement uxml)
        {
        }

        private static void OnCloneButtonClickedEventHandler(VisualElement uxml, SerializedObject so)
        {
            SerializedProperty cloneObjectsSP = so.FindProperty(nameof(KSAssetsCloner._CloneObjects));
            SerializedProperty distDirSP = so.FindProperty(nameof(KSAssetsCloner._DistDir));

            string currentDir = Environment.CurrentDirectory;

            string thisAssetFullPath = Path.GetFullPath(AssetDatabase.GetAssetPath(so.targetObject));
            string thisAssetFullDir = Path.GetDirectoryName(thisAssetFullPath);

            string distDir = distDirSP.stringValue;
            // 出力先ディレクトリ(正規化済)
            string fullDistDir = Path.GetFullPath($"{thisAssetFullDir}/{distDir}");

            Dictionary<string, CloneInfo> assetPath2CloneInfoMap = new();
            for (int i = 0; i < cloneObjectsSP.arraySize; i++)
            {
                SerializedProperty elementSP = cloneObjectsSP.GetArrayElementAtIndex(i);
                string basePath = AssetDatabase.GetAssetPath(elementSP.objectReferenceValue);

                if (Directory.Exists(basePath))
                {
                    // フォルダの場合
                    string baseDistPath = $"{fullDistDir}/{Path.GetFileName(basePath)}";
                    string[] excludePaths = assetPath2CloneInfoMap.Where(kvp => kvp.Value.DoClone == true).Select(kvp => kvp.Value.CloneFullPath).ToArray();
                    // 必要に応じてインデックス付きのクローンパスを取得
                    string fixedBaseDistPath = GetFixedSavePath(baseDistPath, excludePaths, true);

                    string baseFullPath = Path.GetFullPath(basePath);

                    assetPath2CloneInfoMap[baseFullPath] = new() { OriginalFullPath = baseFullPath, CloneFullPath = fixedBaseDistPath, IsFolder = true, DoClone = true };

                    foreach (string filePath in Directory.EnumerateFiles(baseFullPath, "*", SearchOption.AllDirectories))
                    {
                        if (Path.GetExtension(filePath) == ".meta")
                            continue;
                        string relativeFilePath = GetRelativePath(baseFullPath + "/", filePath);
                        assetPath2CloneInfoMap[filePath] = new() { OriginalFullPath = filePath, CloneFullPath = Path.Combine(fixedBaseDistPath, relativeFilePath), IsFolder = false, DoClone = true };
                    }

                    foreach (string filePath in Directory.EnumerateDirectories(baseFullPath, "*", SearchOption.AllDirectories))
                    {
                        string relativeFolderPath = GetRelativePath(baseFullPath + "/", filePath);
                        assetPath2CloneInfoMap[filePath] = new() { OriginalFullPath = filePath, CloneFullPath = Path.Combine(fixedBaseDistPath, relativeFolderPath), IsFolder = true, DoClone = true };
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

                    assetPath2CloneInfoMap[baseFullPath] = new() { OriginalFullPath = baseFullPath, CloneFullPath = fixedBaseDistPath, IsFolder = false, DoClone = true };
                }
            }

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


        public static T Bind<T>(
            VisualElement root,
            string elementName,
            SerializedObject so,
            string spPath
        ) where T : VisualElement, IBindable
        {
            T element = root.Q<T>(elementName);
            SerializedProperty property = so.FindProperty(spPath) ?? throw new ArgumentException($"SerializedProperty not found: path='{spPath}'", nameof(spPath));
            element.BindProperty(property);
            return element;
        }

        private record CloneInfo
        {
            public string OriginalFullPath { get; set; }
            public string OriginalPath => GetRelativePath(ProjectRootDir, OriginalFullPath);
            public string CloneFullPath { get; set; }
            public string ClonePath => GetRelativePath(ProjectRootDir, CloneFullPath);
            public string OriginalGUID { get; set; }
            public string CloneGUID { get; set; }
            public bool DoClone { get; set; }
            public bool IsFolder { get; set; }
        }

        public record UxmlNames
        {
            public static readonly string CloneObjects = "CloneObjects";
            public static readonly string RelativeDistDir = "RelativeDistDir";
            public static readonly string DistDir = "DistDir";
            public static readonly string ReferenceButton = "ReferenceButton";
            public static readonly string Clone2Variant = "Clone2Variant";
            public static readonly string CloneButton = "CloneButton";
        }
    }
}