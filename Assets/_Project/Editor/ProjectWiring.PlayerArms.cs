using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DBP.EditorTools
{
    public static partial class ProjectWiring
    {
        static void AddAlliedPlayerArms(GameObject view, Vector3 support, Scene preview)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(AlliedAnimatedModelPath);
            if (!source) throw new InvalidOperationException("Import the allied soldier before building player arms.");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
            try
            {
                rig.transform.localScale *= 1.72f / WorldBounds(rig).size.y;
                var animator = rig.GetComponent<Animator>();
                var clip = AssetDatabase.LoadAllAssetsAtPath(AlliedAnimatedModelPath).OfType<AnimationClip>()
                    .First(c => !c.name.StartsWith("__preview__"));
                clip.SampleAnimation(rig, .25f);
                var rightUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                var rightLower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var leftUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                var leftLower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                var aim = leftHand.position - rightHand.position;
                rig.transform.rotation = Quaternion.Inverse(Quaternion.LookRotation(aim, Vector3.up));
                rig.transform.position -= rightHand.position;
                Object.DestroyImmediate(animator);

                // Bring the upper arms below the camera; preserve the source's
                // hand rotations and finger curls while solving each grip.
                var rightRotation = rightHand.rotation;
                var leftRotation = leftHand.rotation;
                var rightGrip = new Vector3(.06f, .01f, -.01f);
                var leftGrip = support + new Vector3(.015f, -.01f, 0f);
                float rightReach = Vector3.Distance(rightUpper.position, rightLower.position) + Vector3.Distance(rightLower.position, rightHand.position);
                float leftReach = Vector3.Distance(leftUpper.position, leftLower.position) + Vector3.Distance(leftLower.position, leftHand.position);
                rightUpper.position = rightGrip + new Vector3(.24f, -.32f, -.25f).normalized * rightReach * .82f;
                leftUpper.position = leftGrip + new Vector3(-.3f, -.30f, -.25f).normalized * leftReach * .82f;
                PosePlayerArm(rightUpper, rightLower, rightHand, rightGrip, new Vector3(.45f, -.3f, -.2f));
                PosePlayerArm(leftUpper, leftLower, leftHand, leftGrip, support + new Vector3(-.4f, -.3f, -.2f));
                rightHand.rotation = rightRotation;
                leftHand.rotation = leftRotation;
                if (Vector3.Distance(rightHand.position, rightGrip) > .015f || Vector3.Distance(leftHand.position, leftGrip) > .015f)
                    throw new InvalidOperationException("The player arms cannot reach the weapon grips.");

                var skin = rig.GetComponentInChildren<SkinnedMeshRenderer>();
                foreach (bool left in new[] { false, true })
                {
                    string suffix = left ? "Left" : "Right";
                    var mesh = SaveMesh(view.name + "_SentinelArms_" + suffix, ExtractPosedPlayerArm(skin, view.transform, left,
                        left ? leftUpper : rightUpper, left ? leftLower : rightLower));
                    var arm = new GameObject("SentinelArm" + suffix);
                    arm.transform.SetParent(view.transform, false);
                    arm.AddComponent<MeshFilter>().sharedMesh = mesh;
                    arm.AddComponent<MeshRenderer>().sharedMaterial = AlliedMaterial();
                }
            }
            finally { Object.DestroyImmediate(rig); }
        }

        static void PosePlayerArm(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 pole)
        {
            for (int i = 0; i < 40; i++)
            {
                lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
                upper.rotation = Quaternion.FromToRotation(hand.position - upper.position, target - upper.position) * upper.rotation;
                var axis = hand.position - upper.position;
                var current = Vector3.ProjectOnPlane(lower.position - upper.position, axis);
                var desired = Vector3.ProjectOnPlane(pole - upper.position, axis);
                if (current.sqrMagnitude > 1e-8f && desired.sqrMagnitude > 1e-8f)
                    upper.rotation = Quaternion.FromToRotation(current, desired) * upper.rotation;
            }
        }

        static Mesh ExtractPosedPlayerArm(SkinnedMeshRenderer skin, Transform space, bool left, Transform shoulder, Transform elbow)
        {
            var source = skin.sharedMesh;
            if (!source.isReadable) throw new InvalidOperationException("Enable Read/Write on the allied soldier model.");
            string side = left ? "Left" : "Right";
            var selected = skin.bones.Select(b => b.name.Contains(side + "Arm") || b.name.Contains(side + "ForeArm") || b.name.Contains(side + "Hand")).ToArray();
            var weights = source.boneWeights;
            var matrices = skin.bones.Select((bone, i) => space.worldToLocalMatrix * bone.localToWorldMatrix * source.bindposes[i]).ToArray();
            var normalMatrices = matrices.Select(m => m.inverse.transpose).ToArray();
            var original = source.vertices;
            var originalNormals = source.normals;
            var originalUv = source.uv;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var remap = Enumerable.Repeat(-1, original.Length).ToArray();
            var elbowPoint = space.InverseTransformPoint(elbow.position);
            var upperAxis = space.InverseTransformPoint(shoulder.position) - elbowPoint;
            var projection = upperAxis / upperAxis.sqrMagnitude;
            var sleeveExtension = new Vector3(0f, -.4f, -.12f);
            for (int i = 0; i < original.Length; i++)
            {
                var w = weights[i];
                float armWeight = (selected[w.boneIndex0] ? w.weight0 : 0) + (selected[w.boneIndex1] ? w.weight1 : 0)
                    + (selected[w.boneIndex2] ? w.weight2 : 0) + (selected[w.boneIndex3] ? w.weight3 : 0);
                if (armWeight < .6f) continue;
                var p = matrices[w.boneIndex0].MultiplyPoint3x4(original[i]) * w.weight0
                    + matrices[w.boneIndex1].MultiplyPoint3x4(original[i]) * w.weight1
                    + matrices[w.boneIndex2].MultiplyPoint3x4(original[i]) * w.weight2
                    + matrices[w.boneIndex3].MultiplyPoint3x4(original[i]) * w.weight3;
                var n = normalMatrices[w.boneIndex0].MultiplyVector(originalNormals[i]) * w.weight0
                    + normalMatrices[w.boneIndex1].MultiplyVector(originalNormals[i]) * w.weight1
                    + normalMatrices[w.boneIndex2].MultiplyVector(originalNormals[i]) * w.weight2
                    + normalMatrices[w.boneIndex3].MultiplyVector(originalNormals[i]) * w.weight3;
                // The full character's shoulder is an open cut in a first-person
                // mesh. Continue that sleeve below the screen, keeping the hand
                // and elbow unchanged and correcting normals for the extension.
                float t = Mathf.Clamp01(Vector3.Dot(p - elbowPoint, projection));
                p += sleeveExtension * (t * t);
                if (t > 0f && t < 1f)
                {
                    var deformation = Matrix4x4.identity;
                    for (int row = 0; row < 3; row++)
                        for (int column = 0; column < 3; column++)
                            deformation[row, column] += sleeveExtension[row] * (2f * t) * projection[column];
                    n = deformation.inverse.transpose.MultiplyVector(n);
                }
                remap[i] = vertices.Count;
                vertices.Add(p); normals.Add(n.normalized); uv.Add(originalUv[i]);
            }
            var triangles = new List<int>();
            var originalTriangles = source.triangles;
            for (int i = 0; i < originalTriangles.Length; i += 3)
            {
                int a = remap[originalTriangles[i]], b = remap[originalTriangles[i + 1]], c = remap[originalTriangles[i + 2]];
                if (a >= 0 && b >= 0 && c >= 0) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
            }
            if (triangles.Count < 300) throw new InvalidOperationException("The new player arm mesh is incomplete.");
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            return mesh;
        }
    }
}
