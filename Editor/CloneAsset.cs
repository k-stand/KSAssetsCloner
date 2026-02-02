
using System;
using UnityEngine;

namespace io.github.kiriumestand.ksassetscloner.editor
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