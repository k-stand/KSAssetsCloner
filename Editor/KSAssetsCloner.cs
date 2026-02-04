
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace io.github.kiriumestand.ksassetscloner.editor
{
    [CreateAssetMenu(menuName = "Kiriume Stand/KS Assets Cloner")]
    [Serializable]
    internal class KSAssetsCloner : ScriptableObject
    {
        [SerializeField]
        [SerializeReference]
        internal List<CloneAsset> _CloneAssets = new() { new CloneAsset() };
        [SerializeField]
        internal string _DistDir = "";
        [SerializeField]
        internal bool _Clone2Variant;
    }
}