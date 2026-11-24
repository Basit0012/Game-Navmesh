using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace RelicDash.Editor
{
    [Serializable]
    public class AnimKeyframeJson
    {
        public float t;
        public float x;
        public float y;
        public float z;
        public float w;
    }

    [Serializable]
    public class AnimCurveJson
    {
        public string node;
        public string rel_path;
        public string type;
        public List<float> times;
        public List<List<float>> values;
    }

    [Serializable]
    public class AnimEntryJson
    {
        public string name;
        public float duration;
        public List<AnimCurveJson> curves;
    }

    public static class CharacterAnimationSetup
    {
        [MenuItem("RelicDash/Setup Character Animations & Controllers", priority = 5)]
        public static void GenerateAnimationsAndControllers()
        {
            Directory.CreateDirectory("Assets/Animations");

            string jsonPath = "Assets/Animations/CharacterAnimationData.json";
            if (!File.Exists(jsonPath))
            {
                Debug.LogError("[CharacterAnimationSetup] CharacterAnimationData.json not found at " + jsonPath);
                return;
            }

            string jsonText = File.ReadAllText(jsonPath);
            var animMap = ParseAnimationJson(jsonText);

            // 1. Build Player Clips and Controller
            var pIdle = CreateClipFromJson(animMap, "idle", "Assets/Animations/Player_Idle.anim", true, false);
            var pWalk = CreateClipFromJson(animMap, "walk", "Assets/Animations/Player_Walk.anim", true, false);
            var pSprint = CreateClipFromJson(animMap, "sprint", "Assets/Animations/Player_Sprint.anim", true, false);

            BuildPlayerController(pIdle, pWalk, pSprint);

            // 2. Build Enemy Clips and Controller
            var eIdle = CreateClipFromJson(animMap, "idle", "Assets/Animations/Enemy_Idle.anim", true, true);
            var eWalk = CreateClipFromJson(animMap, "walk", "Assets/Animations/Enemy_Walk.anim", true, true);
            var eSprint = CreateClipFromJson(animMap, "sprint", "Assets/Animations/Enemy_Sprint.anim", true, true);
            var eAttack = CreateClipFromJson(animMap, "attack-melee-right", "Assets/Animations/Enemy_Attack.anim", false, true);

            BuildEnemyController(eIdle, eWalk, eSprint, eAttack);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88><b>[CharacterAnimationSetup] Successfully generated authentic rotation-based player and enemy animation clips and animator controllers!</b></color>");
        }

        private static Dictionary<string, AnimEntryJson> ParseAnimationJson(string jsonText)
        {
            var dict = new Dictionary<string, AnimEntryJson>();
            string[] animKeys = new string[] { "idle", "walk", "sprint", "attack-melee-right" };

            foreach (var key in animKeys)
            {
                int keyIdx = jsonText.IndexOf("\"" + key + "\":");
                if (keyIdx < 0) continue;

                int startBrace = jsonText.IndexOf('{', keyIdx);
                if (startBrace < 0) continue;

                // Find matching closing brace for this animation block
                int braceDepth = 0;
                int endBrace = -1;
                for (int p = startBrace; p < jsonText.Length; p++)
                {
                    if (jsonText[p] == '{') braceDepth++;
                    else if (jsonText[p] == '}')
                    {
                        braceDepth--;
                        if (braceDepth == 0)
                        {
                            endBrace = p;
                            break;
                        }
                    }
                }
                if (endBrace < startBrace) continue;

                string animBlock = jsonText.Substring(startBrace, endBrace - startBrace + 1);
                var entry = new AnimEntryJson { name = key, curves = new List<AnimCurveJson>() };

                // Parse duration
                int durIdx = animBlock.IndexOf("\"duration\":");
                if (durIdx > 0)
                {
                    int durValStart = durIdx + 11;
                    int durValEnd = animBlock.IndexOfAny(new char[] { ',', '\n', '\r', '}' }, durValStart);
                    if (durValEnd > durValStart && float.TryParse(animBlock.Substring(durValStart, durValEnd - durValStart).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float dur))
                    {
                        entry.duration = dur;
                    }
                }

                // Extract each curve within this animation block
                string[] nodeNames = new string[] { "torso", "arm-left", "arm-right", "head", "leg-left", "leg-right" };
                foreach (var node in nodeNames)
                {
                    int nIdx = animBlock.IndexOf("\"node\": \"" + node + "\"");
                    if (nIdx < 0) continue;

                    var curveJson = ParseCurveForNode(animBlock, nIdx);
                    if (curveJson != null && curveJson.times.Count > 0)
                    {
                        entry.curves.Add(curveJson);
                    }
                }

                dict[key] = entry;
            }

            return dict;
        }

        private static AnimCurveJson ParseCurveForNode(string text, int nIdx)
        {
            var curve = new AnimCurveJson { times = new List<float>(), values = new List<List<float>>() };

            // Find rel_path
            int relIdx = text.IndexOf("\"rel_path\":", nIdx);
            if (relIdx > 0)
            {
                int q1 = text.IndexOf('"', relIdx + 11);
                int q2 = text.IndexOf('"', q1 + 1);
                if (q1 > 0 && q2 > q1)
                {
                    curve.rel_path = text.Substring(q1 + 1, q2 - q1 - 1);
                }
            }

            // Find times array
            int timesIdx = text.IndexOf("\"times\":", nIdx);
            if (timesIdx > 0)
            {
                int timesStart = text.IndexOf('[', timesIdx);
                int timesEnd = text.IndexOf(']', timesStart);
                if (timesStart > 0 && timesEnd > timesStart)
                {
                    string timesStr = text.Substring(timesStart + 1, timesEnd - timesStart - 1);
                    string[] tTokens = timesStr.Split(',');
                    foreach (var tk in tTokens)
                    {
                        if (float.TryParse(tk.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float tVal))
                        {
                            curve.times.Add(tVal);
                        }
                    }
                }
            }

            // Find values array with bracket depth matching
            int valsIdx = text.IndexOf("\"values\":", nIdx);
            if (valsIdx > 0)
            {
                int valsStart = text.IndexOf('[', valsIdx);
                if (valsStart > 0)
                {
                    int depth = 0;
                    int valsEnd = -1;
                    for (int p = valsStart; p < text.Length; p++)
                    {
                        if (text[p] == '[') depth++;
                        else if (text[p] == ']')
                        {
                            depth--;
                            if (depth == 0)
                            {
                                valsEnd = p;
                                break;
                            }
                        }
                    }

                    if (valsEnd > valsStart)
                    {
                        string inner = text.Substring(valsStart + 1, valsEnd - valsStart - 1);
                        int searchPos = 0;
                        while (searchPos < inner.Length)
                        {
                            int openB = inner.IndexOf('[', searchPos);
                            if (openB < 0) break;
                            int closeB = inner.IndexOf(']', openB);
                            if (closeB < 0) break;

                            string tupleStr = inner.Substring(openB + 1, closeB - openB - 1);
                            string[] parts = tupleStr.Split(',');
                            if (parts.Length == 4)
                            {
                                var q = new List<float>();
                                for (int c = 0; c < 4; c++)
                                {
                                    if (float.TryParse(parts[c].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float qVal))
                                    {
                                        q.Add(qVal);
                                    }
                                }
                                if (q.Count == 4)
                                {
                                    curve.values.Add(q);
                                }
                            }
                            searchPos = closeB + 1;
                        }
                    }
                }
            }

            return curve;
        }

        private static AnimationClip CreateClipFromJson(Dictionary<string, AnimEntryJson> animMap, string actionKey, string outPath, bool isLooping, bool isEnemy)
        {
            var clip = new AnimationClip();
            clip.name = Path.GetFileNameWithoutExtension(outPath);

            if (!animMap.ContainsKey(actionKey))
            {
                Debug.LogWarning("[CharacterAnimationSetup] Action key not found: " + actionKey);
                AssetDatabase.CreateAsset(clip, outPath);
                return clip;
            }

            var entry = animMap[actionKey];

            foreach (var curveData in entry.curves)
            {
                if (curveData.times.Count == 0 || curveData.values.Count == 0) continue;

                int count = Mathf.Min(curveData.times.Count, curveData.values.Count);
                Keyframe[] keysX = new Keyframe[count];
                Keyframe[] keysY = new Keyframe[count];
                Keyframe[] keysZ = new Keyframe[count];
                Keyframe[] keysW = new Keyframe[count];

                for (int i = 0; i < count; i++)
                {
                    float t = curveData.times[i];
                    var q = curveData.values[i];
                    // glTF quat is [x, y, z, w].
                    // For pure pitch / forward-backward limb swing around the X axis:
                    // q.x is pitch, q.w is cos(theta/2).
                    // In Unity: [qx, qy, qz, qw]
                    keysX[i] = new Keyframe(t, q[0]);
                    keysY[i] = new Keyframe(t, q[1]);
                    keysZ[i] = new Keyframe(t, q[2]);
                    keysW[i] = new Keyframe(t, q[3]);
                }

                AnimationCurve cX = new AnimationCurve(keysX);
                AnimationCurve cY = new AnimationCurve(keysY);
                AnimationCurve cZ = new AnimationCurve(keysZ);
                AnimationCurve cW = new AnimationCurve(keysW);

                // Smooth tangents for clean motion
                for (int k = 0; k < count; k++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(cX, k, AnimationUtility.TangentMode.Auto);
                    AnimationUtility.SetKeyRightTangentMode(cX, k, AnimationUtility.TangentMode.Auto);
                    AnimationUtility.SetKeyLeftTangentMode(cY, k, AnimationUtility.TangentMode.Auto);
                    AnimationUtility.SetKeyRightTangentMode(cY, k, AnimationUtility.TangentMode.Auto);
                    AnimationUtility.SetKeyLeftTangentMode(cZ, k, AnimationUtility.TangentMode.Auto);
                    AnimationUtility.SetKeyRightTangentMode(cZ, k, AnimationUtility.TangentMode.Auto);
                    AnimationUtility.SetKeyLeftTangentMode(cW, k, AnimationUtility.TangentMode.Auto);
                    AnimationUtility.SetKeyRightTangentMode(cW, k, AnimationUtility.TangentMode.Auto);
                }

                // Primary path (e.g. root/torso/arm-left) when Animator is on Humanoid_Character / Stalker_Monster
                string path1 = curveData.rel_path;
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path1, typeof(Transform), "m_LocalRotation.x"), cX);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path1, typeof(Transform), "m_LocalRotation.y"), cY);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path1, typeof(Transform), "m_LocalRotation.z"), cZ);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path1, typeof(Transform), "m_LocalRotation.w"), cW);

                // Secondary path (e.g. ModelRoot/Humanoid_Character/root/torso/arm-left) when Animator is on Player/Enemy root
                string prefix = isEnemy ? "Guardian_Model/Stalker_Monster/" : "ModelRoot/Humanoid_Character/";
                string path2 = prefix + curveData.rel_path;
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path2, typeof(Transform), "m_LocalRotation.x"), cX);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path2, typeof(Transform), "m_LocalRotation.y"), cY);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path2, typeof(Transform), "m_LocalRotation.z"), cZ);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path2, typeof(Transform), "m_LocalRotation.w"), cW);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = isLooping;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, outPath);
            return clip;
        }

        private static void BuildPlayerController(AnimationClip idle, AnimationClip walk, AnimationClip sprint)
        {
            string path = "Assets/Animations/PlayerAnimatorController.controller";
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idle;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walk;

            var sprintState = rootStateMachine.AddState("Sprint");
            sprintState.motion = sprint;

            rootStateMachine.defaultState = idleState;

            // Idle -> Walk
            var tIdleWalk = idleState.AddTransition(walkState);
            tIdleWalk.hasExitTime = false;
            tIdleWalk.duration = 0.15f;
            tIdleWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            // Walk -> Idle
            var tWalkIdle = walkState.AddTransition(idleState);
            tWalkIdle.hasExitTime = false;
            tWalkIdle.duration = 0.15f;
            tWalkIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // Walk -> Sprint
            var tWalkSprint = walkState.AddTransition(sprintState);
            tWalkSprint.hasExitTime = false;
            tWalkSprint.duration = 0.15f;
            tWalkSprint.AddCondition(AnimatorConditionMode.Greater, 4.5f, "Speed");

            // Sprint -> Walk
            var tSprintWalk = sprintState.AddTransition(walkState);
            tSprintWalk.hasExitTime = false;
            tSprintWalk.duration = 0.15f;
            tSprintWalk.AddCondition(AnimatorConditionMode.Less, 4.5f, "Speed");

            EditorUtility.SetDirty(controller);
        }

        private static void BuildEnemyController(AnimationClip idle, AnimationClip walk, AnimationClip sprint, AnimationClip attack)
        {
            string path = "Assets/Animations/EnemyAnimatorController.controller";
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsChasing", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            var patrolState = rootStateMachine.AddState("Patrol");
            patrolState.motion = walk;

            var chaseState = rootStateMachine.AddState("Chase");
            chaseState.motion = sprint;

            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attack;

            rootStateMachine.defaultState = patrolState;

            // Patrol -> Chase
            var tPatrolChase = patrolState.AddTransition(chaseState);
            tPatrolChase.hasExitTime = false;
            tPatrolChase.duration = 0.15f;
            tPatrolChase.AddCondition(AnimatorConditionMode.If, 0, "IsChasing");

            // Chase -> Patrol
            var tChasePatrol = chaseState.AddTransition(patrolState);
            tChasePatrol.hasExitTime = false;
            tChasePatrol.duration = 0.2f;
            tChasePatrol.AddCondition(AnimatorConditionMode.IfNot, 0, "IsChasing");

            // Chase -> Attack
            var tChaseAttack = chaseState.AddTransition(attackState);
            tChaseAttack.hasExitTime = false;
            tChaseAttack.duration = 0.1f;
            tChaseAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            // Attack -> Patrol
            var tAttackPatrol = attackState.AddTransition(patrolState);
            tAttackPatrol.hasExitTime = true;
            tAttackPatrol.exitTime = 0.9f;
            tAttackPatrol.duration = 0.2f;

            EditorUtility.SetDirty(controller);
        }
    }
}
