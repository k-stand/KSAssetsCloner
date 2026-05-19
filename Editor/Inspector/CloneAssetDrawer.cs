using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.github.k_stand.ksassetscloner.editor
{
    [Serializable]
    internal class CloneAssetDrawer : ScriptableObject
    {
        [SerializeField]
        internal StyleSheet styleSheet;
        [SerializeField]
        internal VisualTreeAsset visualTreeAsset;

        protected CloneAssetDrawer() { }
    }

    [CustomPropertyDrawer(typeof(CloneAsset))]
    public class CloneAssetDrawerImpl : PropertyDrawer
    {
        private CloneAssetDrawer _drawer;
        internal CloneAssetDrawer Drawer
        {
            get
            {
                if (_drawer == null)
                {
                    _drawer = ScriptableObject.CreateInstance<CloneAssetDrawer>();
                }
                return _drawer;
            }
        }

        public StyleSheet StyleSheet => Drawer.styleSheet;
        public VisualTreeAsset VisualTreeAsset => Drawer.visualTreeAsset;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // UXML をインスタンス化
            VisualElement uxml = VisualTreeAsset.CloneTree();
            // ussを適用
            uxml.styleSheets.Add(StyleSheet);

            Toggle u_DoClone = BindHelper.BindRelative<Toggle>(uxml, UxmlNames.DoClone, property, nameof(CloneAsset._DoClone));
            ObjectField u_CloneAsset = BindHelper.BindRelative<ObjectField>(uxml, UxmlNames.CloneAsset, property, nameof(CloneAsset._CloneAsset));

            return uxml;
        }

        public record UxmlNames
        {
            public static readonly string DoClone = "DoClone";
            public static readonly string CloneAsset = "CloneAsset";
        }
    }
}