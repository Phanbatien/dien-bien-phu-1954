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
        [Tooltip("Dời bộ tay + súng so với điểm cầm (không gian súng): thấp, gần hơn để thấy tay rõ")]
        public Vector3 modelOffset = new Vector3(0.03f, -0.045f, -0.08f);
        [Tooltip("Nghiêng súng vào giữa màn hình để thấy thân súng, như các game FPS")]
        public Vector3 modelCant = new Vector3(-1.5f, -4f, -2f);

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

        readonly System.Collections.Generic.Dictionary<string, GameObject> models = new System.Collections.Generic.Dictionary<string, GameObject>();
        string currentModel;

        /// Thân lính dựng sẵn bị nhàu khi nhìn sát camera: ẩn cả thân và khẩu Mosin khối, thay bằng bộ tay + súng
        /// dựng sẵn (Resources/ViewModels): súng thật, bàn tay ôm báng, ống tay áo kaki.
        void BuildSimpleArms()
        {
            if (models.Count > 0 || !rifle) return;
            foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.enabled = false;
            foreach (var r in rifle.GetComponentsInChildren<MeshRenderer>(true)) r.enabled = false;
            foreach (var name in new[] { "VM_Rifle", "VM_Scoped", "VM_Smg" })
            {
                var prefab = Resources.Load<GameObject>("ViewModels/" + name);
                if (!prefab) continue;
                var vm = Instantiate(prefab, rifle, false); // gốc prefab = điểm tay phải cầm báng = gốc súng cũ
                vm.name = name;
                vm.transform.localPosition = modelOffset;
                vm.transform.localRotation = Quaternion.Euler(modelCant);
                vm.SetActive(false);
                models[name] = vm;
            }
            ShowModel("VM_Rifle");
        }

        /// Đổi bộ tay + súng hiển thị: "VM_Rifle", "VM_Scoped" hoặc "VM_Smg".
        public void ShowModel(string name)
        {
            if (name == currentModel || !models.ContainsKey(name)) return;
            foreach (var pair in models) pair.Value.SetActive(pair.Key == name);
            currentModel = name;
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
