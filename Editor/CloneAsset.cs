
using System;
using UnityEngine;

namespace com.github.k_stand.ksassetscloner.editor
{
    [Serializable]
    internal class CloneAsset
    {
        [SerializeField]
        internal bool _DoClone = true;
        [SerializeField]
        internal UnityEngine.Object _CloneAsset;
    }
}