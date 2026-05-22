
using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace com.github.k_stand.ksassetscloner.editor
{
    [MovedFrom(false,
    sourceNamespace: "io.github.kiriumestand.ksassetscloner.editor",
    sourceAssembly: "io.github.kiriumestand.ksassetscloner.editor",
    sourceClassName: null)]
    [Serializable]
    internal class CloneAsset
    {
        [SerializeField]
        internal bool _DoClone = true;
        [SerializeField]
        internal UnityEngine.Object _CloneAsset;
        [SerializeField]
        internal string _Rename = "";
    }
}