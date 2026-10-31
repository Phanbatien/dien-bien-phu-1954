using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Hoạt ảnh nhân vật (29/10): dự án chỉ có đứng/đi/chạy (Starter Assets) và bắn súng (Long). Các động tác còn lại
    /// được dựng bằng đường cong cơ humanoid nên dùng chung cho mọi model có rig Human (bộ đội, lính Pháp, sĩ quan):
    /// ngồi nấp, trèo thang, đâm lê, đấm, ném lựu đạn, trúng đạn, gục ngã, chỉ tay ra lệnh (chỉ huy), hô hào (trùm).
    /// Hai controller (bộ đội, lính địch) được dựng lại cùng cấu trúc, giữ nguyên GUID để scene/prefab không mất tham chiếu:
    ///   Lớp gốc: Locomotion (Speed) ↔ Crouch (bool) ↔ Climb (bool); AnyState → Death (Dead).
    ///   Lớp thân trên (mask nửa người trên): Shoot, Stab, Punch, Throw, Hit, Command, Rally — xong tự về trạng thái rỗng.
    public static partial class ProjectWiring
    {
        const string GeneratedAnimDir = "Assets/_Project/Art/Animations/Generated/";
        const string UpperMaskPath = GeneratedAnimDir + "ThanTren.mask";

        [MenuItem("DBP/Animations/Dựng lại hoạt ảnh nhân vật")]
        public static void BuildCharacterAnimations()
        {
            Directory.CreateDirectory(GeneratedAnimDir);
            var clips = BuildClips();
            var mask = UpperBodyMask();
            if (File.Exists(AlliedAnimatedModelPath))
            {
                var fire = AssetDatabase.LoadAllAssetsAtPath(AlliedAnimatedModelPath).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
                RebuildController(AlliedControllerPath, clips, fire, mask);
                RebuildController(SoldierControllerPath, clips, fire, mask);
            }
            else RebuildController(SoldierControllerPath, clips, null, mask);
            ApplyAlliedAnimationClips();
            ApplyActionClips();
            AssetDatabase.SaveAssets();
        }

        // ───────── Clip từ đường cong cơ ─────────

        /// Một khóa: thời điểm (giây) và giá trị cơ (−1…1).
        sealed class Pose : Dictionary<string, (float t, float v)[]> { }

        static Dictionary<string, AnimationClip> BuildClips()
        {
            var clips = new Dictionary<string, AnimationClip>();
            // Tư thế ôm súng dùng chung cho nhiều động tác thân trên
            (float, float)[] Hold(float v) => new[] { (0f, v) };

            clips["Crouch"] = Clip("NgoiNap", true, 1f, new Pose
            {
                ["RootT.y"] = new[] { (0f, .66f), (.5f, .64f), (1f, .66f) },
                ["Spine Front-Back"] = Hold(.35f), ["Chest Front-Back"] = Hold(.2f), ["Head Nod Down-Up"] = Hold(-.15f),
                ["Left Upper Leg Front-Back"] = Hold(.8f), ["Right Upper Leg Front-Back"] = Hold(.65f),
                ["Left Lower Leg Stretch"] = Hold(-.95f), ["Right Lower Leg Stretch"] = Hold(-.9f),
                ["Left Foot Up-Down"] = Hold(.45f), ["Right Foot Up-Down"] = Hold(.4f),
                ["Left Upper Leg In-Out"] = Hold(.25f), ["Right Upper Leg In-Out"] = Hold(.25f),
                ["Left Arm Down-Up"] = Hold(-.45f), ["Right Arm Down-Up"] = Hold(-.5f),
                ["Left Arm Front-Back"] = Hold(.6f), ["Right Arm Front-Back"] = Hold(.45f),
                ["Left Forearm Stretch"] = Hold(-.55f), ["Right Forearm Stretch"] = Hold(-.7f),
            });

            clips["Climb"] = Clip("TreoThang", true, .9f, new Pose
            {
                ["RootT.y"] = Hold(1f),
                ["Spine Front-Back"] = Hold(.15f), ["Head Nod Down-Up"] = Hold(.25f),
                ["Left Arm Down-Up"] = new[] { (0f, .85f), (.45f, .35f), (.9f, .85f) },
                ["Right Arm Down-Up"] = new[] { (0f, .35f), (.45f, .85f), (.9f, .35f) },
                ["Left Arm Front-Back"] = Hold(.65f), ["Right Arm Front-Back"] = Hold(.65f),
                ["Left Forearm Stretch"] = new[] { (0f, -.25f), (.45f, -.8f), (.9f, -.25f) },
                ["Right Forearm Stretch"] = new[] { (0f, -.8f), (.45f, -.25f), (.9f, -.8f) },
                ["Left Upper Leg Front-Back"] = new[] { (0f, .1f), (.45f, .8f), (.9f, .1f) },
                ["Right Upper Leg Front-Back"] = new[] { (0f, .8f), (.45f, .1f), (.9f, .8f) },
                ["Left Lower Leg Stretch"] = new[] { (0f, -.2f), (.45f, -.9f), (.9f, -.2f) },
                ["Right Lower Leg Stretch"] = new[] { (0f, -.9f), (.45f, -.2f), (.9f, -.9f) },
            });

            clips["Death"] = Clip("GucNga", false, 1f, new Pose
            {
                ["RootT.y"] = new[] { (0f, 1f), (.6f, .7f), (1f, .55f) },
                ["Spine Front-Back"] = new[] { (0f, 0f), (.15f, -.4f), (1f, .6f) },
                ["Chest Front-Back"] = new[] { (0f, 0f), (.15f, -.3f), (1f, .4f) },
                ["Head Nod Down-Up"] = new[] { (0f, 0f), (.15f, .5f), (1f, -.7f) },
                ["Left Upper Leg Front-Back"] = new[] { (0f, 0f), (1f, .6f) }, ["Right Upper Leg Front-Back"] = new[] { (0f, 0f), (1f, .45f) },
                ["Left Lower Leg Stretch"] = new[] { (0f, 0f), (1f, -.9f) }, ["Right Lower Leg Stretch"] = new[] { (0f, 0f), (1f, -.75f) },
                ["Left Arm Down-Up"] = new[] { (0f, -.4f), (.15f, .3f), (1f, -.9f) }, ["Right Arm Down-Up"] = new[] { (0f, -.4f), (.15f, .4f), (1f, -.85f) },
                ["Left Forearm Stretch"] = new[] { (0f, -.4f), (1f, -.1f) }, ["Right Forearm Stretch"] = new[] { (0f, -.4f), (1f, -.15f) },
            });

            clips["Stab"] = Clip("DamLe", false, .55f, new Pose
            {
                ["Spine Front-Back"] = new[] { (0f, .05f), (.18f, -.15f), (.3f, .5f), (.55f, .05f) },
                ["Spine Twist Left-Right"] = new[] { (0f, 0f), (.18f, .25f), (.3f, -.2f), (.55f, 0f) },
                ["Right Arm Front-Back"] = new[] { (0f, .35f), (.18f, .1f), (.3f, 1f), (.55f, .35f) },
                ["Left Arm Front-Back"] = new[] { (0f, .5f), (.18f, .3f), (.3f, 1f), (.55f, .5f) },
                ["Right Arm Down-Up"] = new[] { (0f, -.4f), (.3f, -.1f), (.55f, -.4f) },
                ["Left Arm Down-Up"] = new[] { (0f, -.3f), (.3f, 0f), (.55f, -.3f) },
                ["Right Forearm Stretch"] = new[] { (0f, -.75f), (.18f, -.9f), (.3f, .1f), (.55f, -.75f) },
                ["Left Forearm Stretch"] = new[] { (0f, -.55f), (.18f, -.8f), (.3f, .15f), (.55f, -.55f) },
            });

            clips["Punch"] = Clip("Dam", false, .45f, new Pose
            {
                ["Spine Twist Left-Right"] = new[] { (0f, .15f), (.2f, -.45f), (.45f, .15f) },
                ["Spine Front-Back"] = new[] { (0f, .1f), (.2f, .3f), (.45f, .1f) },
                ["Right Arm Front-Back"] = new[] { (0f, .2f), (.2f, 1f), (.45f, .2f) },
                ["Right Arm Down-Up"] = new[] { (0f, -.5f), (.2f, .05f), (.45f, -.5f) },
                ["Right Forearm Stretch"] = new[] { (0f, -1f), (.2f, .1f), (.45f, -1f) },
                ["Left Arm Front-Back"] = Hold(.6f), ["Left Arm Down-Up"] = Hold(-.3f), ["Left Forearm Stretch"] = Hold(-1f),
            });

            clips["Throw"] = Clip("NemLuuDan", false, .8f, new Pose
            {
                ["Spine Front-Back"] = new[] { (0f, 0f), (.3f, -.35f), (.5f, .45f), (.8f, 0f) },
                ["Spine Twist Left-Right"] = new[] { (0f, 0f), (.3f, .45f), (.5f, -.4f), (.8f, 0f) },
                ["Right Arm Down-Up"] = new[] { (0f, -.4f), (.3f, 1f), (.5f, .6f), (.8f, -.4f) },
                ["Right Arm Front-Back"] = new[] { (0f, .2f), (.3f, -.7f), (.5f, .9f), (.8f, .2f) },
                ["Right Forearm Stretch"] = new[] { (0f, -.6f), (.3f, -.95f), (.5f, -.1f), (.8f, -.6f) },
                ["Left Arm Down-Up"] = new[] { (0f, -.4f), (.3f, .3f), (.5f, -.2f), (.8f, -.4f) },
                ["Left Arm Front-Back"] = new[] { (0f, .3f), (.3f, .9f), (.5f, .2f), (.8f, .3f) },
                ["Left Forearm Stretch"] = Hold(-.2f),
            });

            clips["Hit"] = Clip("TrungDan", false, .4f, new Pose
            {
                ["Spine Front-Back"] = new[] { (0f, 0f), (.08f, -.55f), (.4f, 0f) },
                ["Chest Front-Back"] = new[] { (0f, 0f), (.08f, -.4f), (.4f, 0f) },
                ["Spine Twist Left-Right"] = new[] { (0f, 0f), (.08f, .3f), (.4f, 0f) },
                ["Head Nod Down-Up"] = new[] { (0f, 0f), (.08f, .45f), (.4f, 0f) },
                ["Left Arm Down-Up"] = new[] { (0f, -.4f), (.1f, .1f), (.4f, -.4f) },
                ["Right Arm Down-Up"] = new[] { (0f, -.4f), (.1f, 0f), (.4f, -.4f) },
            });

            clips["Command"] = Clip("ChiHuy", false, 1.1f, new Pose
            {
                ["Right Arm Down-Up"] = new[] { (0f, -.5f), (.3f, .45f), (.85f, .45f), (1.1f, -.5f) },
                ["Right Arm Front-Back"] = new[] { (0f, .2f), (.3f, .95f), (.85f, .95f), (1.1f, .2f) },
                ["Right Forearm Stretch"] = new[] { (0f, -.6f), (.3f, .1f), (.85f, .1f), (1.1f, -.6f) },
                ["Right Hand Down-Up"] = Hold(.2f),
                ["Spine Twist Left-Right"] = new[] { (0f, 0f), (.3f, -.2f), (1.1f, 0f) },
                ["Head Nod Down-Up"] = new[] { (0f, 0f), (.3f, .15f), (1.1f, 0f) },
                ["Left Arm Down-Up"] = Hold(-.7f), ["Left Forearm Stretch"] = Hold(-.3f),
            });

            clips["Rally"] = Clip("HoHao", false, 1.4f, new Pose
            {
                ["Spine Front-Back"] = new[] { (0f, 0f), (.35f, -.4f), (1.1f, -.4f), (1.4f, 0f) },
                ["Head Nod Down-Up"] = new[] { (0f, 0f), (.35f, .5f), (1.1f, .5f), (1.4f, 0f) },
                ["Left Arm Down-Up"] = new[] { (0f, -.5f), (.35f, 1f), (.7f, .8f), (1.1f, 1f), (1.4f, -.5f) },
                ["Right Arm Down-Up"] = new[] { (0f, -.5f), (.35f, 1f), (.7f, .8f), (1.1f, 1f), (1.4f, -.5f) },
                ["Left Arm Front-Back"] = Hold(.3f), ["Right Arm Front-Back"] = Hold(.3f),
                ["Left Forearm Stretch"] = new[] { (0f, -.4f), (.35f, -.6f), (.7f, -.3f), (1.1f, -.6f), (1.4f, -.4f) },
                ["Right Forearm Stretch"] = new[] { (0f, -.4f), (.35f, -.6f), (.7f, -.3f), (1.1f, -.6f), (1.4f, -.4f) },
            });
            return clips;
        }

        static AnimationClip Clip(string name, bool loop, float length, Pose pose)
        {
            var valid = new HashSet<string>(HumanTrait.MuscleName) { "RootT.x", "RootT.y", "RootT.z" };
            string path = GeneratedAnimDir + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!clip) { clip = new AnimationClip { name = name }; AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves();
            foreach (var (muscle, keys) in pose)
            {
                if (!valid.Contains(muscle)) throw new ArgumentException($"Không có cơ humanoid tên '{muscle}'");
                var curve = new AnimationCurve(keys.Select(k => new Keyframe(k.t, k.v)).ToArray());
                if (keys.Length == 1) curve.AddKey(length, keys[0].v);
                for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), curve);
            }
            // Thân đứng thẳng, giữ nguyên hướng (động tác không xoay cả người; ngã do EnemyAnimatorDriver xoay model)
            if (pose.ContainsKey("RootT.y"))
            {
                foreach (var (axis, v) in new[] { ("RootT.x", 0f), ("RootT.z", 0f), ("RootQ.x", 0f), ("RootQ.y", 0f), ("RootQ.z", 0f), ("RootQ.w", 1f) })
                    if (!pose.ContainsKey(axis))
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), axis), AnimationCurve.Constant(0f, length, v));
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = length;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static AvatarMask UpperBodyMask()
        {
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperMaskPath);
            if (!mask) { mask = new AvatarMask(); AssetDatabase.CreateAsset(mask, UpperMaskPath); }
            for (var part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++)
                mask.SetHumanoidBodyPartActive(part, part != AvatarMaskBodyPart.Root && part != AvatarMaskBodyPart.LeftLeg && part != AvatarMaskBodyPart.RightLeg
                    && part != AvatarMaskBodyPart.LeftFootIK && part != AvatarMaskBodyPart.RightFootIK);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        // ───────── Controller ─────────

        /// Dựng controller ở file tạm rồi chép đè nội dung lên file cũ: giữ GUID (file .meta) nên mọi scene/prefab vẫn trỏ đúng.
        static void RebuildController(string path, Dictionary<string, AnimationClip> clips, AnimationClip fire, AvatarMask mask)
        {
            string temp = GeneratedAnimDir + "_tam.controller";
            AssetDatabase.DeleteAsset(temp);
            var c = AnimatorController.CreateAnimatorControllerAtPath(temp);
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            foreach (var b in new[] { "Crouch", "Climb" }) c.AddParameter(b, AnimatorControllerParameterType.Bool);
            foreach (var t in new[] { "Dead", "Shoot", "Stab", "Punch", "Throw", "Hit", "Command", "Rally" }) c.AddParameter(t, AnimatorControllerParameterType.Trigger);

            // Lớp gốc
            var sm = c.layers[0].stateMachine;
            var loco = c.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            AnimationClip Starter(string file) => AssetDatabase.LoadAllAssetsAtPath(StarterAnims + file).OfType<AnimationClip>().FirstOrDefault(x => !x.name.StartsWith("__preview__"));
            tree.AddChild(Starter("Stand--Idle.anim.fbx"), 0f);
            tree.AddChild(Starter("Locomotion--Walk_N.anim.fbx"), 1.6f);
            tree.AddChild(Starter("Locomotion--Run_N.anim.fbx"), 4.5f);
            sm.defaultState = loco;
            var crouch = sm.AddState("Crouch"); crouch.motion = clips["Crouch"];
            var climb = sm.AddState("Climb"); climb.motion = clips["Climb"];
            var death = sm.AddState("Death"); death.motion = clips["Death"];
            Link(loco, crouch, "Crouch", true); Link(crouch, loco, "Crouch", false);
            Link(loco, climb, "Climb", true); Link(crouch, climb, "Climb", true); Link(climb, loco, "Climb", false);
            var toDeath = sm.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            toDeath.duration = .1f; toDeath.canTransitionToSelf = false;

            // Lớp thân trên
            var upperSm = new AnimatorStateMachine { name = "ThanTren", hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(upperSm, c);
            c.AddLayer(new AnimatorControllerLayer { name = "ThanTren", stateMachine = upperSm, avatarMask = mask, defaultWeight = 1f, blendingMode = AnimatorLayerBlendingMode.Override });
            var empty = upperSm.AddState("Trong");
            upperSm.defaultState = empty;
            var actions = new List<(string trigger, Motion motion)> { ("Stab", clips["Stab"]), ("Punch", clips["Punch"]), ("Throw", clips["Throw"]), ("Hit", clips["Hit"]), ("Command", clips["Command"]), ("Rally", clips["Rally"]) };
            if (fire) actions.Insert(0, ("Shoot", fire));
            foreach (var (trigger, motion) in actions)
            {
                var s = upperSm.AddState(trigger); s.motion = motion;
                var t = upperSm.AddAnyStateTransition(s);
                t.AddCondition(AnimatorConditionMode.If, 0, trigger);
                t.duration = .08f; t.canTransitionToSelf = trigger == "Shoot";
                var back = s.AddTransition(empty);
                back.hasExitTime = true; back.exitTime = .9f; back.duration = .15f;
            }
            AssetDatabase.SaveAssets();
            File.Copy(temp, path, true);
            AssetDatabase.DeleteAsset(temp);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        static void Link(AnimatorState from, AnimatorState to, string param, bool on)
        {
            var t = from.AddTransition(to);
            t.AddCondition(on ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
            t.hasExitTime = false; t.duration = .2f;
        }
    }
}
