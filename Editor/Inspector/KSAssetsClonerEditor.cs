using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.github.k_stand.ksassetscloner.editor
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
            KSAssetsCloner assetsCloner = (KSAssetsCloner)so.targetObject;

            string thisAssetFullPath = Path.GetFullPath(AssetDatabase.GetAssetPath(assetsCloner));
            string thisAssetFullDir = Path.GetDirectoryName(thisAssetFullPath);

            string fullDistDir = KSAssetsClonerLib.ResolveAndValidateDistDir(
                assetsCloner._DistDir, thisAssetFullDir, out string validErr);
            if (fullDistDir == null) { EditorUtility.DisplayDialog("KS Assets Cloner", validErr, "OK"); return; }

            IEnumerable<CloneAssetInput> inputs = assetsCloner._CloneAssets.Select(a => new CloneAssetInput(a._CloneAsset, a._DoClone, a._Rename));

            Dictionary<string, AssetCloneInfo> map = KSAssetsClonerLib.BuildCloneInfoMap(inputs, fullDistDir, out string buildErr);
            if (map == null) { EditorUtility.DisplayDialog("KS Assets Cloner", buildErr, "OK"); return; }

            List<AssetCloneInfo> cloneInfos = map.Values.ToList();
            int cloneCount = cloneInfos.Count(ci => ci.DoClone);
            if (cloneCount == 0) { EditorUtility.DisplayDialog("KS Assets Cloner", "複製すべきアセットがありません", "OK"); return; }
            bool clickedOK = EditorUtility.DisplayDialog("KS Assets Cloner", $"{cloneCount}個のアセットを複製します", "OK", "Cancel");
            if (!clickedOK) return;

            KSAssetsClonerLib.ExecuteClone(cloneInfos);
        }

        private static string GetRelativePath(string fromPath, string toPath)
        {
            return Uri.UnescapeDataString(new Uri(fromPath).MakeRelativeUri(new Uri(toPath)).ToString());
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