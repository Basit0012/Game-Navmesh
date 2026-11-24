using System.IO;
using UnityEngine;
using UnityEditor;

namespace RelicDash.Editor
{
    public static class InspectAssets
    {
        public static void CheckCharacter()
        {
            string path = "Assets/Models/Characters/character-a.fbx";
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            Debug.Log($"Loaded {all.Length} sub-assets from {path}:");
            foreach (var a in all)
            {
                if (a is AnimationClip clip)
                {
                    Debug.Log($"  AnimationClip: '{clip.name}', length: {clip.length}s, isLooping: {clip.isLooping}");
                }
                else if (a is Avatar av)
                {
                    Debug.Log($"  Avatar: '{av.name}', isValid: {av.isValid}, isHuman: {av.isHuman}");
                }
                else if (a is Mesh m)
                {
                    Debug.Log($"  Mesh: '{m.name}', vertexCount: {m.vertexCount}");
                }
            }
        }
    }
}
