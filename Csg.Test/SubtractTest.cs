﻿using NUnit.Framework;
using System;

namespace Csg.Test
{
	[TestFixture]
	public class SubtractTest : SolidTest
	{
		[Test]
		public void UnitSphere_UnitSphere()
		{
			var sphere1 = Script.Sphere(1, new Vector3D(-0.5, 0, 0));
			var sphere2 = Script.Sphere(1, new Vector3D(0.5, 0, 0));
			var r = sphere1.Substract(sphere2);
			Assert.AreEqual(84, r.Polygons.Count);
			Assert.IsTrue(r.IsCanonicalized);
			Assert.IsTrue(r.IsRetesselated);
			AssertAcceptedStl(r, "SubtractTest");
		}

		[Test]
		public void UnitSphere_NoOverlap_UnitSphere()
		{
			var sphere1 = Script.Sphere(1, new Vector3D(-50, 0, 0));
			var sphere2 = Script.Sphere(1, new Vector3D(50, 0, 0));
			var r = sphere1.Substract(sphere2);
			Assert.AreEqual(72, r.Polygons.Count);
			Assert.IsTrue(r.IsCanonicalized);
			Assert.IsTrue(r.IsRetesselated);
			AssertAcceptedStl(r, "SubtractTest");
		}
	}
}

