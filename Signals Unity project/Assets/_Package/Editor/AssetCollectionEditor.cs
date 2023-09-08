using System.Collections;
using System.Collections.Generic;
using Coft.AssetCollection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AssetCollection))]
[CanEditMultipleObjects]
public class AssetCollectionEditor : Editor
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnInspectorGUI()
    {
        GUILayout.Button("Test");
    }
}
