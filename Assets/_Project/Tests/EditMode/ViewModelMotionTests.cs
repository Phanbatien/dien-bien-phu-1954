using DBP.Player;
using NUnit.Framework;
using UnityEngine;

namespace DBP.Tests
{
    public class ViewModelMotionTests
    {
        [Test]
        public void ReloadBolt_RestsAtBothEnds_AndPeaksMidway()
        {
            Assert.AreEqual(0f, ViewModelMotions.ReloadBolt(0f), 0.001f);
            Assert.AreEqual(0f, ViewModelMotions.ReloadBolt(1f), 0.001f);
            Assert.AreEqual(ViewModelMotions.ReloadBoltDegrees, ViewModelMotions.ReloadBolt(0.5f), 0.001f);
            Assert.Greater(ViewModelMotions.ReloadBolt(0.25f), 0f);
        }
    }
}
