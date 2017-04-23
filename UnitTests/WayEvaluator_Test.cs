using System;
using Game.Geometry;
using Game.Strategy;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class WayEvaluator_Test
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			FastCoord.Init();
		}

		[Test]
		public void Test()
		{
			Console.Out.WriteLine(WayEvaluator.CalcCost(FastCoord.Create(19, 16), FastCoord.Create(13, 5), FastCoord.Create(17, 8)));
			Console.Out.WriteLine(WayEvaluator.CalcCost(FastCoord.Create(19, 16), FastCoord.Create(13, 5), FastCoord.Create(16, 8)));
			Console.Out.WriteLine(WayEvaluator.CalcCost(FastCoord.Create(19, 16), FastCoord.Create(13, 5), FastCoord.Create(18, 8)));
		}
	}
}