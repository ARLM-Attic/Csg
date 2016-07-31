﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Csg
{
	public class Csg
	{
		public List<Polygon> Polygons;
		public Properties Properties;

		public bool IsCanonicalized;
		public bool IsRetesselated;

		public const int DefaultResolution2D = 32;
		public const int DefaultResolution3D = 12;

		public static Csg FromPolygons(List<Polygon> polygons)
		{
			var csg = new Csg();
			csg.Polygons = polygons;
			csg.IsCanonicalized = false;
			csg.IsRetesselated = false;
			return csg;
		}

		public Csg Union(params Csg[] others)
		{
			var csgs = new List<Csg>();
			csgs.Add(this);
			csgs.AddRange(others);
			var i = 1;
			for (; i < csgs.Count; i += 2)
			{
				var n = csgs[i - 1].UnionSub(csgs[i], false, false);
				csgs.Add(n);
			}
			return csgs[i - 1].Retesselated().Canonicalized();
		}

		Csg UnionSub(Csg csg, bool retesselate, bool canonicalize)
		{
			if (!MayOverlap(csg))
			{
				return UnionForNonIntersecting(csg);
			}
			else {
				var a = new Tree(Polygons);
				var b = new Tree(csg.Polygons);

				a.ClipTo(b, false);
				b.ClipTo(a);
				b.Invert();
				b.ClipTo(a);
				b.Invert();

				var newpolygons = new List<Polygon>(a.AllPolygons());
				newpolygons.AddRange(b.AllPolygons());
				var result = Csg.FromPolygons(newpolygons);
				result.Properties = Properties.Merge(csg.Properties);
				if (retesselate) result = result.Retesselated();
				if (canonicalize) result = result.Canonicalized();
				return result;
			}
		}

		Csg UnionForNonIntersecting(Csg csg)
		{
			var newpolygons = new List<Polygon>(Polygons);
			newpolygons.AddRange(csg.Polygons);
			var result = Csg.FromPolygons(newpolygons);
			result.Properties = Properties.Merge(csg.Properties);
			result.IsCanonicalized = IsCanonicalized && csg.IsCanonicalized;
			result.IsRetesselated = IsRetesselated && csg.IsRetesselated;
			return result;
		}

		public Csg Substract(params Csg[] csgs)
		{
			Csg result = this;
			for (var i = 0; i < csgs.Length; i++)
			{
				var islast = (i == (csgs.Length - 1));
				result = result.SubtractSub(csgs[i], islast, islast);
			}
			return result;
		}

		Csg SubtractSub(Csg csg, bool retesselate, bool canonicalize)
		{
			var a = new Tree(Polygons);
			var b = new Tree(csg.Polygons);

			a.Invert();
			a.ClipTo(b);
			b.ClipTo(a, true);
			a.AddPolygons(b.AllPolygons());
			a.Invert();

			var result = Csg.FromPolygons(a.AllPolygons());
			result.Properties = Properties.Merge(csg.Properties);
			if (retesselate) result = result.Retesselated();
			if (canonicalize) result = result.Canonicalized();
			return result;
		}

		public Csg Intersect(params Csg[] csgs)
		{
			var result = this;
			for (var i = 0; i < csgs.Length; i++)
			{
				var islast = (i == (csgs.Length - 1));
				result = result.IntersectSub(csgs[i], islast, islast);
			}
			return result;
		}

		Csg IntersectSub(Csg csg, bool retesselate, bool canonicalize)
		{
			var a = new Tree(Polygons);
			var b = new Tree(csg.Polygons);

			a.Invert();
			b.ClipTo(a);
			b.Invert();
			a.ClipTo(b);
			b.ClipTo(a);
			a.AddPolygons(b.AllPolygons());
			a.Invert();

			var result = Csg.FromPolygons(a.AllPolygons());
			result.Properties = Properties.Merge(csg.Properties);
			if (retesselate) result = result.Retesselated();
			if (canonicalize) result = result.Canonicalized();
			return result;
		}

		bool MayOverlap(Csg other)
		{
			throw new NotImplementedException();
		}

		Csg Canonicalized()
		{
			if (IsCanonicalized)
			{
				return this;
			}
			else {
				var factory = new FuzzyCsgFactory();
				var result = factory.GetCsg(this);
				result.IsCanonicalized = true;
				result.IsRetesselated = IsRetesselated;
				result.Properties = Properties;
				return result;
			}
		}

		Csg Retesselated()
		{
			if (IsRetesselated)
			{
				return this;
			}
			else {
				var csg = this;
				var polygonsPerPlane = new Dictionary<string, List<Polygon>>();
				var isCanonicalized = csg.IsCanonicalized;
				var fuzzyFactory = new FuzzyCsgFactory();
				foreach (var polygon in csg.Polygons)
				{
					var plane = polygon.Plane;
					var shared = polygon.Shared;
					if (!isCanonicalized)
					{
						plane = fuzzyFactory.GetPlane(plane);
						shared = fuzzyFactory.GetPolygonShared(shared);
					}
					var tag = plane.Tag + "/" + shared.Tag;
					List<Polygon> ppp;
					if (polygonsPerPlane.TryGetValue(tag, out ppp))
					{
						ppp.Add(polygon);
					}
					else {
						ppp = new List<Polygon>();
						ppp.Add(polygon);
						polygonsPerPlane.Add(tag, ppp);
					}
				}
				var destpolygons = new List<Polygon>();
				foreach (var planetag in polygonsPerPlane)
				{
					var sourcepolygons = planetag.Value;
					if (sourcepolygons.Count < 2)
					{
						destpolygons.AddRange(sourcepolygons);
					}
					else {
						var retesselatedpolygons = new List<Polygon>();
						Csg.RetesselateCoplanarPolygons(sourcepolygons, retesselatedpolygons);
						destpolygons.AddRange(retesselatedpolygons);
					}
				}
				var result = Csg.FromPolygons(destpolygons);
				result.IsRetesselated = true;
				result.Properties = Properties;
				return result;
			}

		}

		static void RetesselateCoplanarPolygons(List<Polygon> s, List<Polygon> r)
		{
			throw new NotImplementedException();
		}

		static int staticTag = 1;
		public static int GetTag()
		{
			return staticTag++;
		}
	}

	public class Properties
	{
		public Properties Merge(Properties others)
		{
			throw new NotImplementedException();
		}
	}

	class FuzzyCsgFactory
	{
		public Csg GetCsg(Csg start)
		{
			throw new NotImplementedException();
		}

		public Plane GetPlane(Plane plane)
		{
			throw new NotImplementedException();
		}
		public PolygonShared GetPolygonShared(PolygonShared shared)
		{
			throw new NotImplementedException();
		}
	}
}
