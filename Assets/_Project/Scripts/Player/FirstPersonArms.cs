using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DBP.Player
{
    /// Tay góc nhìn thứ nhất trên Infantry: tư thế cầm súng tĩnh, đầu và chân thu lại.
    /// Súng là con của hand.R; cả cụm nằm dưới camera nên ngắm đã kéo theo nòng.
    public class FirstPersonArms : MonoBehaviour
    {
        [Serializable]
        public struct BoneOffset
        {
            public string boneName;
            public Quaternion localOffset;
        }

        [Header("Súng")]
        public Transform rifle;
        public Vector3 gripLocalPosition;
        public Quaternion gripLocalRotation = Quaternion.identity;

        [Header("Tư thế")]
        public BoneOffset[] pose = Array.Empty<BoneOffset>();

        [Header("Ẩn đầu và chân")]
        public string[] collapsedBones = { "head", "upper_leg.L", "upper_leg.R" };
        public float collapsedScale = 0.001f;

        [Header("Tay gọn: ẩn thân lính, chỉ vẽ cẳng tay + bàn tay ôm súng")]
        public bool simpleArms = true;
        public Color sleeveColor = new Color(0.33f, 0.36f, 0.24f);
        public Color skinColor = new Color(0.72f, 0.55f, 0.42f);

        [Header("Vị trí trên màn hình (không gian camera)")]
        public Vector3 rifleInCamera = new Vector3(0.16f, -0.2f, 0.62f);

        static Material hidden;

        Transform[] poseBones;
        Quaternion[] bindLocal;
        Transform[] collapsed;
        bool ready;

        void Awake()
        {
            PrepareRig();
            Apply();
        }

        void OnValidate()
        {
            if (!Application.isPlaying || !ready) return;
            Apply();
        }

        public void PrepareRig()
        {
            if (ready) return;

            poseBones = new Transform[pose.Length];
            bindLocal = new Quaternion[pose.Length];
            for (int i = 0; i < pose.Length; i++)
            {
                poseBones[i] = FindNamed(pose[i].boneName);
                if (!poseBones[i])
                {
                    Debug.LogError($"[FirstPersonArms] Không thấy xương '{pose[i].boneName}'", this);
                    continue;
                }
                bindLocal[i] = poseBones[i].localRotation;
            }

            collapsed = new Transform[collapsedBones.Length];
            for (int i = 0; i < collapsedBones.Length; i++)
            {
                collapsed[i] = FindNamed(collapsedBones[i]);
                if (!collapsed[i])
                    Debug.LogError($"[FirstPersonArms] Không thấy xương '{collapsedBones[i]}'", this);
            }

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (renderer is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen = true;
                    // Khung bao gốc nằm dưới camera nên Unity loại cả tay và súng.
                    skin.localBounds = new Bounds(new Vector3(0f, 1.2f, 0.45f), new Vector3(3f, 3f, 3f));
                }
            }
            foreach (var collider in GetComponentsInChildren<Collider>(true))
                Destroy(collider);

            ready = true;
        }

        public void Apply()
        {
            if (!ready) PrepareRig();

            for (int i = 0; i < poseBones.Length; i++)
            {
                if (!poseBones[i]) continue;
                var offset = pose[i].localOffset;
                if (offset.x == 0f && offset.y == 0f && offset.z == 0f && offset.w == 0f)
                    offset = Quaternion.identity;
                poseBones[i].localRotation = bindLocal[i] * offset;
            }

            for (int i = 0; i < collapsed.Length; i++)
            {
                if (!collapsed[i]) continue;
                collapsed[i].localScale = Vector3.one * collapsedScale;
            }

            if (!rifle) return;
            var grip = gripLocalRotation;
            if (grip.x == 0f && grip.y == 0f && grip.z == 0f && grip.w == 0f)
                grip = Quaternion.identity;
            rifle.localPosition = gripLocalPosition;
            rifle.localRotation = grip;

            HideTorsoAndLegs();
            PlaceRifleInView();
            if (simpleArms) BuildSimpleArms();
        }

        Transform simpleRoot;

        /// Thân lính dựng sẵn bị nhàu khi nhìn sát camera. Ẩn cả thân, dựng hai cẳng tay đơn giản:
        /// tay phải ở báng (chỗ cầm cò), tay trái đỡ ốp lót phía trước, cùi chỏ chĩa về hai góc dưới màn hình.
        void BuildSimpleArms()
        {
            if (simpleRoot || !rifle || !transform.parent) return;
            foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.enabled = false;
            var cam = transform.parent;
            simpleRoot = new GameObject("SimpleArms").transform;
            simpleRoot.SetParent(rifle.parent ? rifle.parent : transform, false);
            var sleeve = ArmMaterial("ViewModelSleeve", sleeveColor);
            var skinMat = ArmMaterial("ViewModelSkin", skinColor);
            Vector3 rightHand = rifle.position;
            Vector3 leftHand = rifle.position + cam.forward * 0.32f + cam.up * 0.02f;
            Limb(rightHand, cam.TransformPoint(new Vector3(0.34f, -0.46f, 0.18f)), sleeve, skinMat);
            Limb(leftHand, cam.TransformPoint(new Vector3(-0.2f, -0.5f, 0.42f)), sleeve, skinMat);
        }

        void Limb(Vector3 hand, Vector3 elbow, Material sleeve, Material skinMat)
        {
            var dir = elbow - hand;
            var arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            arm.name = "CangTay";
            Destroy(arm.GetComponent<Collider>());
            arm.transform.SetParent(simpleRoot, false);
            arm.transform.position = hand + dir * 0.5f;
            arm.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            arm.transform.localScale = new Vector3(0.085f, dir.magnitude * 0.5f + 0.04f, 0.085f);
            arm.GetComponent<Renderer>().sharedMaterial = sleeve;
            var palm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            palm.name = "BanTay";
            Destroy(palm.GetComponent<Collider>());
            palm.transform.SetParent(simpleRoot, false);
            palm.transform.position = hand;
            palm.transform.rotation = arm.transform.rotation;
            palm.transform.localScale = new Vector3(0.075f, 0.1f, 0.065f);
            palm.GetComponent<Renderer>().sharedMaterial = skinMat;
            foreach (var r in new[] { arm.GetComponent<Renderer>(), palm.GetComponent<Renderer>() })
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        static Material ArmMaterial(string name, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.15f);
            return mat;
        }

        /// Thân và chân cùng một mesh với tay. Camera nằm trong đầu, nên nếu vẽ thân thì cả màn hình bị che.
        void HideTorsoAndLegs()
        {
            var skin = GetComponentInChildren<SkinnedMeshRenderer>();
            if (!skin || skin.sharedMaterials.Length < 3) return;
            var mats = skin.sharedMaterials;
            var blank = HiddenMaterial();
            if (!blank) return;
            mats[0] = blank;
            mats[2] = blank;
            skin.sharedMaterials = mats;
        }

        void PlaceRifleInView()
        {
            if (!rifle || !transform.parent) return;
            Vector3 desired = transform.parent.TransformPoint(rifleInCamera);
            transform.position += desired - rifle.position;
        }

        static Material HiddenMaterial()
        {
            if (hidden) return hidden;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) return null;
            hidden = new Material(shader) { name = "ViewModelHidden" };
            hidden.SetFloat("_Surface", 1f);
            hidden.SetFloat("_Blend", 0f);
            hidden.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            hidden.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            hidden.SetInt("_ZWrite", 0);
            hidden.SetColor("_BaseColor", Color.clear);
            hidden.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            hidden.SetOverrideTag("RenderType", "Transparent");
            hidden.renderQueue = (int)RenderQueue.Transparent;
            return hidden;
        }

        Transform FindNamed(string boneName)
        {
            if (string.IsNullOrEmpty(boneName)) return null;
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == boneName) return all[i];
            return null;
        }
    }
}
