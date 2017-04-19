using FluentAssertions;
using Game;
using Game.Geometry;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class FastCoord_Create_Test
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			FastCoord.Init();
		}

		[Test]
		public void InsideMap_ReturnsValidFastCoord()
		{
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			{
				var coord = new Coord(x, y);
				var fastCoord = FastCoord.Create(coord);
				var actual = FastCoord.ToCoord(fastCoord);
				actual.Should().Be(coord);
			}
		}

		[Test]
		public void NearMap_ReturnsValidFastCoord()
		{
			for (int x = -1; x < Constants.MAP_WIDTH + 1; x++)
			{
				{
					var coord = new Coord(x, -1);
					var fastCoord = FastCoord.Create(coord);
					fastCoord.Should().BeGreaterOrEqualTo(0);
					var actual = FastCoord.ToCoord(fastCoord);
					actual.Should().Be(coord);
				}
				{
					var coord = new Coord(x, Constants.MAP_HEIGHT);
					var fastCoord = FastCoord.Create(coord);
					fastCoord.Should().BeGreaterOrEqualTo(0);
					var actual = FastCoord.ToCoord(fastCoord);
					actual.Should().Be(coord);
				}
			}
			for (int y = -1; y < Constants.MAP_HEIGHT + 1; y++)
			{
				{
					var coord = new Coord(-1, y);
					var fastCoord = FastCoord.Create(coord);
					fastCoord.Should().BeGreaterOrEqualTo(0);
					var actual = FastCoord.ToCoord(fastCoord);
					actual.Should().Be(coord);
				}
				{
					var coord = new Coord(Constants.MAP_WIDTH, y);
					var fastCoord = FastCoord.Create(coord);
					fastCoord.Should().BeGreaterOrEqualTo(0);
					var actual = FastCoord.ToCoord(fastCoord);
					actual.Should().Be(coord);
				}
			}
		}

		[TestCase(int.MinValue, int.MinValue)]
		[TestCase(int.MaxValue, int.MaxValue)]
		[TestCase(-2, -2)]
		[TestCase(-2, 2)]
		[TestCase(-2, Constants.MAP_HEIGHT + 1)]
		[TestCase(2, -2)]
		[TestCase(2, Constants.MAP_HEIGHT + 1)]
		[TestCase(Constants.MAP_WIDTH + 1, -2)]
		[TestCase(Constants.MAP_WIDTH + 1, 2)]
		[TestCase(Constants.MAP_WIDTH + 1, Constants.MAP_HEIGHT + 1)]
		public void FarFromMap_ReturnsValidFastCoord(int x, int y)
		{
			var coord = new Coord(x, y);
			var fastCoord = FastCoord.Create(coord);
			fastCoord.Should().Be(-1);
			var actual = FastCoord.ToCoord(fastCoord);
			actual.Should().Be(new Coord(-1000, -1000));
		}
	}
}