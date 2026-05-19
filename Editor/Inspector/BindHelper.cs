/*
Portions of this code are based on https://qiita.com/k7a/items/eb5a3ee4ed6448343543 by k7a
and https://github.com/Narazaka/CopyAssetsWithDependency by Narazaka.
*/
/*
Copyright (c) 2020 Narazaka
Copyright (c) 2026 k-stand

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
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace com.github.k_stand.ksassetscloner.editor
{
    internal class BindHelper
    {
        internal static T Bind<T>(
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

        public static T BindRelative<T>(
            VisualElement root,
            string elementName,
            SerializedProperty parentSP,
            string relativePath
        ) where T : VisualElement, IBindable
        {
            T element = root.Q<T>(elementName);
            SerializedProperty property = parentSP.FindPropertyRelative(relativePath) ?? throw new ArgumentException($"Relative SerializedProperty not found: parent='{parentSP.propertyPath}', relativePath='{relativePath}'", nameof(relativePath));
            element.BindProperty(property);
            return element;
        }
    }
}