using DBP.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace DBP.Tests
{
    public class LootPileMaterialTests
    {
        [Test]
        public void Spawn_UsesBrownUrpMaterialIncludedInPlayer()
        {
            var anchor = new GameObject("LootAnchor");
            try
            {
                var pile = LootPile.At(anchor.transform);
                var material = pile.GetComponent<Renderer>().sharedMaterial;
                Assert.AreEqual("UniversalPipeline", material.GetTag("RenderPipeline", false),
                    "Runtime loot must not use the built-in primitive material.");
                Assert.IsTrue(material.shader.isSupported);
                Assert.AreSame(Resources.Load<Material>("LootPileMaterial"), material,
                    "A Resources material keeps its shader dependency in player builds.");
                Assert.That(material.color.r, Is.EqualTo(.4f).Within(.0001f));
                Assert.That(material.color.g, Is.EqualTo(.3f).Within(.0001f));
                Assert.That(material.color.b, Is.EqualTo(.2f).Within(.0001f));
                Assert.AreSame(pile, LootPile.At(anchor.transform));
                Assert.IsNotNull(pile.GetComponent<Collider>());
            }
            finally { Object.DestroyImmediate(anchor); }
        }
    }
}
