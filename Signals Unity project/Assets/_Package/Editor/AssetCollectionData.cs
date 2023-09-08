// using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Coft.AssetCollection
{
    public class AssetCollectionData : ScriptableObject
    {
        // [HideInInlineEditors]
        public AssetCollection Collection;
        // [InlineEditor]
        public List<Object> Assets;

#if UNITY_EDITOR
        // [HideInInlineEditors]
        // [Button(nameof(Regenerate))]
        public void Regenerate()
        {
            var path = AssetDatabase.GetAssetPath(this);
            var collection =
                AssetDatabase.FindAssets(
                        Collection.SearchQuery,
                        new[] { Path.GetDirectoryName(path) }
                    )
                    .Select(guid => AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(obj => obj != this && obj != Collection)
                    .ToList();
            Assets = collection;
            AssetDatabase.RemoveObjectFromAsset(this);
            AssetDatabase.CreateAsset(this, path);
        }
#endif
    }
}
