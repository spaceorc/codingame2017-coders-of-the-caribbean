using FluentAssertions;
using Game;
using Game.Geometry;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class FastCoord_IsInsideMap_Test
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			FastCoord.Init();
		}

		[Test]
		public void ReturnsValidValue()
		{
			for (int x = -10; x < Constants.MAP_WIDTH + 10; x++)
			for (int y = -10; y < Constants.MAP_HEIGHT + 10; y++)
			{
				var coord = new Coord(x, y);
				var fastCoord = FastCoord.Create(coord);
				FastCoord.IsInsideMap(fastCoord).Should().Be(coord.IsInsideMap(), coord.ToString());
			}
		}
	}
}