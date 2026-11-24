using System.IO;
using UnityEngine;
using UnityEditor;

namespace RelicDash.Editor
{
    public static class InspectClipCurves
    {
        public static void CheckWalkCurves()
        {
            string path = "Assets/Models/Characters/character-a.fbx";
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            AnimationClip torsoWalk = null;
            foreach (var a in all)
            {
                if (a is AnimationClip clip && clip.name == "torso|walk")
                {
                    torsoWalk = clip;
                    break;
                }
            }

            if (torsoWalk != null)
            {
                Debug.Log($"Found torso|walk, length: {torsoWalk.length}");
                var bindings = AnimationUtility.GetCurveBindings(torsoWalk);
                Debug.Log($"Bindings count: {bindings.Length}");
                foreach (var b in bindings)
                {
                    Debug.Log($"  path: '{b.path}', type: {b.type.Name}, property: '{b.propertyName}'");
                }
            }
            else
            {
                Debug.LogError("Could not find torso|walk");
            }
        }
    }
}
