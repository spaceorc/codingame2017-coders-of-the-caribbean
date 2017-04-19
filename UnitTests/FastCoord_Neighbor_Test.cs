using FluentAssertions;
using Game;
using Game.Geometry;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class FastCoord_Neighbor_Test
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			FastCoord.Init();
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		[TestCase(4)]
		[TestCase(5)]
		public void InsideMap_ReturnsValidFastCoord(int orientation)
		{
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			{
				var coord = new Coord(x, y);
				var fastCoord = FastCoord.Create(coord);
				var neighbor = FastCoord.Neighbor(fastCoord, orientation);
				var actual = FastCoord.ToCoord(neighbor);
				actual.Should().Be(coord.Neighbor(orientation));
			}
		}

		[TestCase(-1, -1, 0)]
		[TestCase(-1, -1, 5)]
		[TestCase(-1, -1, 4)]
		[TestCase(Constants.MAP_WIDTH, -1, 3)]
		[TestCase(Constants.MAP_WIDTH, -1, 4)]
		[TestCase(Constants.MAP_WIDTH, Constants.MAP_HEIGHT, 2)]
		[TestCase(Constants.MAP_WIDTH, Constants.MAP_HEIGHT, 3)]
		[TestCase(-1, Constants.MAP_HEIGHT, 0)]
		[TestCase(-1, Constants.MAP_HEIGHT, 1)]
		[TestCase(-1, Constants.MAP_HEIGHT, 2)]
		public void NearMap_GoIn_ReturnsValidFastCoord(int x, int y, int orientation)
		{
			var coord = new Coord(x, y);
			var fastCoord = FastCoord.Create(coord);
			var neighbor = FastCoord.Neighbor(fastCoord, orientation);
			var actual = FastCoord.ToCoord(neighbor);
			actual.Should().Be(coord.Neighbor(orientation));
		}

		[TestCase(-1, -1, 1)]
		[TestCase(-1, -1, 2)]
		[TestCase(-1, -1, 3)]
		[TestCase(Constants.MAP_WIDTH, -1, 0)]
		[TestCase(Constants.MAP_WIDTH, -1, 1)]
		[TestCase(Constants.MAP_WIDTH, -1, 2)]
		[TestCase(Constants.MAP_WIDTH, -1, 5)]
		[TestCase(Constants.MAP_WIDTH, Constants.MAP_HEIGHT, 0)]
		[TestCase(Constants.MAP_WIDTH, Constants.MAP_HEIGHT, 1)]
		[TestCase(Constants.MAP_WIDTH, Constants.MAP_HEIGHT, 4)]
		[TestCase(Constants.MAP_WIDTH, Constants.MAP_HEIGHT, 5)]
		[TestCase(-1, Constants.MAP_HEIGHT, 3)]
		[TestCase(-1, Constants.MAP_HEIGHT, 4)]
		[TestCase(-1, Constants.MAP_HEIGHT, 5)]
		public void NearMap_GoOut_ReturnsValidFastCoord(int x, int y, int orientation)
		{
			var coord = new Coord(x, y);
			var fastCoord = FastCoord.Create(coord);
			var neighbor = FastCoord.Neighbor(fastCoord, orientation);
			neighbor.Should().Be(-1);
		}
	}
}