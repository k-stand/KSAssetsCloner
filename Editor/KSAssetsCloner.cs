
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace io.github.kiriumestand.ksassetscloner.editor
{
    [CreateAssetMenu(menuName = "KiriumeStand/KSAssetsCloner")]
    [Serializable]
    public class KSAssetsCloner : ScriptableObject
    {
        [SerializeField]
        internal List<UnityEngine.Object> _CloneObjects = new();
        [SerializeField]
        internal bool _RelativeDistDir;
        [SerializeField]
        internal string _DistDir = "";
        [SerializeField]
        internal bool _Clone2Variant;
    }
}