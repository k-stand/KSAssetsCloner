
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace io.github.kiriumestand.ksassetscloner.editor
{
    [CreateAssetMenu(menuName = "KiriumeStand/KSAssetsCloner")]
    [Serializable]
    internal class KSAssetsCloner : ScriptableObject
    {
        [SerializeField]
        [SerializeReference]
        internal List<CloneAsset> _CloneAssets = new();
        [SerializeField]
        internal bool _RelativeDistDir;
        [SerializeField]
        internal string _DistDir = "";
        [SerializeField]
        internal bool _Clone2Variant;
    }
}