using DBP.Core;
using NUnit.Framework;
using UnityEngine;

namespace DBP.Tests
{
    public class AudioImportTests
    {
        [TestCase("mat49_A", .5012f)]
        [TestCase("step_mud02", .1995f)]
        [TestCase("ui_click", .1259f)]
        public void Import_PreservesPreparedPeakLevel_AndMonoChannels(string name, float peak)
        {
            var clip = AudioCatalog.Clip(name);
            Assert.NotNull(clip);
            Assert.AreEqual(1, clip.channels);
            var samples = new float[clip.samples * clip.channels];
            Assert.IsTrue(clip.GetData(samples, 0));
            float actual = 0;
            foreach (float sample in samples) actual = Mathf.Max(actual, Mathf.Abs(sample));
            Assert.AreEqual(peak, actual, .002f, "Unity import must preserve the prepared sound balance");
        }
    }
}
