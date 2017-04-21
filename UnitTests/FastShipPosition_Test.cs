using System;
using System.Linq;
using FluentAssertions;
using Game;
using Game.Geometry;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class CollisionChecker_Test
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			FastShipPosition.Init();
		}

		[TestCase(13, 11, 2, 0, ShipMoveCommand.Wait, 13, 7, 1, 0, ShipMoveCommand.Wait, CollisionType.None)]
		[TestCase(13, 11, 2, 0, ShipMoveCommand.Wait, 13, 8, 1, 0, ShipMoveCommand.Wait, CollisionType.None)]
		[TestCase(13, 11, 2, 0, ShipMoveCommand.Wait, 12, 9, 1, 0, ShipMoveCommand.Wait, CollisionType.None)]
		[TestCase(13, 11, 2, 0, ShipMoveCommand.Wait, 12, 10, 1, 0, ShipMoveCommand.Wait, CollisionType.None)]
		[TestCase(13, 11, 2, 0, ShipMoveCommand.Wait, 11, 11, 1, 0, ShipMoveCommand.Wait, CollisionType.None)]

		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 13, 7, 1, 0, ShipMoveCommand.Wait, CollisionType.None)]
		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 13, 8, 1, 0, ShipMoveCommand.Wait, CollisionType.MyMove)]
		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 12, 9, 1, 0, ShipMoveCommand.Wait, CollisionType.MyMove)]
		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 12, 10, 1, 0, ShipMoveCommand.Wait, CollisionType.MyMove)]
		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 11, 11, 1, 0, ShipMoveCommand.Wait, CollisionType.None)]

		[TestCase(13, 11, 2, 2, ShipMoveCommand.Wait, 13, 7, 1, 0, ShipMoveCommand.Wait, CollisionType.None)]
		[TestCase(13, 11, 2, 2, ShipMoveCommand.Wait, 13, 8, 1, 0, ShipMoveCommand.Wait, CollisionType.MyMove)]
		[TestCase(13, 11, 2, 2, ShipMoveCommand.Wait, 12, 9, 1, 0, ShipMoveCommand.Wait, CollisionType.MyMove)]
		[TestCase(13, 11, 2, 2, ShipMoveCommand.Wait, 12, 10, 1, 0, ShipMoveCommand.Wait, CollisionType.MyMove)]
		[TestCase(13, 11, 2, 2, ShipMoveCommand.Wait, 11, 11, 1, 0, ShipMoveCommand.Wait, CollisionType.None)]

		[TestCase(13, 11, 2, 0, ShipMoveCommand.Faster, 13, 7, 1, 0, ShipMoveCommand.Wait, CollisionType.None)]
		[TestCase(13, 11, 2, 0, ShipMoveCommand.Faster, 13, 8, 1, 0, ShipMoveCommand.Wait, CollisionType.MyMove)]
		[TestCase(13, 11, 2, 0, ShipMoveCommand.Faster, 12, 9, 1, 0, ShipMoveCommand.Wait, CollisionType.MyMove)]
		[TestCase(13, 11, 2, 0, ShipMoveCommand.Faster, 12, 10, 1, 0, ShipMoveCommand.Wait, CollisionType.MyMove)]
		[TestCase(13, 11, 2, 0, ShipMoveCommand.Faster, 11, 11, 1, 0, ShipMoveCommand.Wait, CollisionType.None)]

		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 13, 7, 1, 1, ShipMoveCommand.Wait, CollisionType.None)]
		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 13, 8, 1, 1, ShipMoveCommand.Wait, CollisionType.None)]
		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 12, 9, 1, 1, ShipMoveCommand.Wait, CollisionType.MyMove)]
		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 12, 10, 1, 1, ShipMoveCommand.Wait, CollisionType.MyMove)]
		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 11, 11, 1, 1, ShipMoveCommand.Wait, CollisionType.MyMove | CollisionType.OtherMove)]
		public void GetCollisionType_NoRotation_SimpleIntersectionCases_ReturnsValidResult(int myx, int myy, int myor, int mysp, ShipMoveCommand myc, int ox, int oy, int oor, int osp, ShipMoveCommand oc, CollisionType expected)
		{
			var my = FastShipPosition.Create(myx, myy, myor, mysp);
			var other = FastShipPosition.Create(ox, oy, oor, osp);
			GetCollisionType(my, myc, other, oc).Should().Be(expected);
		}

		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 12, 12, 1, 1, ShipMoveCommand.Wait, CollisionType.OtherMove)]
		[TestCase(13, 11, 2, 1, ShipMoveCommand.Wait, 12, 12, 1, 1, ShipMoveCommand.Port, CollisionType.OtherMove)]
		[TestCase(13, 11, 2, 0, ShipMoveCommand.Port, 12, 12, 1, 1, ShipMoveCommand.Faster, CollisionType.OtherMove | CollisionType.MyRotation)]
		public void GetCollisionType_ComplexCases_ReturnsValidResult(int myx, int myy, int myor, int mysp, ShipMoveCommand myc, int ox, int oy, int oor, int osp, ShipMoveCommand oc, CollisionType expected)
		{
			var my = FastShipPosition.Create(myx, myy, myor, mysp);
			var other = FastShipPosition.Create(ox, oy, oor, osp);
			GetCollisionType(my, myc, other, oc).Should().Be(expected);
		}

		private CollisionType GetCollisionType(int my, ShipMoveCommand myc, int other, ShipMoveCommand oc)
		{
			uint myMovement;
			uint otherMovement;
			var collisionType = CollisionChecker.Move(my, myc, other, oc, out myMovement, out otherMovement);
			Console.Out.WriteLine(FastShipPosition.ToShipPosition(FastShipPosition.GetMovedPosition(myMovement)) + " - " + FastShipPosition.ToShipPosition(FastShipPosition.GetFinalPosition(myMovement)));
			Console.Out.WriteLine(FastShipPosition.ToShipPosition(FastShipPosition.GetMovedPosition(otherMovement)) + " - " + FastShipPosition.ToShipPosition(FastShipPosition.GetFinalPosition(otherMovement)));
			return collisionType;
		}
	}

	[TestFixture]
	public class FastShipPosition_Test
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			FastShipPosition.Init();
		}

		[TestCase(-2, -2)]
		[TestCase(-2, Constants.MAP_HEIGHT + 1)]
		[TestCase(Constants.MAP_WIDTH + 1, Constants.MAP_HEIGHT + 1)]
		[TestCase(Constants.MAP_WIDTH + 1, -2)]
		public void Create_OutsideMap_ReturnsValidPosition(int x, int y)
		{
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			{
				var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
				var fastShipPosition = FastShipPosition.Create(shipPosition);
				var actual = FastShipPosition.ToShipPosition(fastShipPosition);
				actual.coord.Should().Be(new Coord(-1000, -1000), $"it's coord of [{shipPosition}]");
				actual.speed.Should().Be(shipPosition.speed, $"it's speed of [{shipPosition}]");
				actual.orientation.Should().Be(shipPosition.orientation, $"it's orientation of [{shipPosition}]");
			}
		}

		[Test]
		public void Move_ReturnsValidPosition()
		{
			int i=0;
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			{
				foreach (var moveCommand in Enum.GetValues(typeof(ShipMoveCommand)).Cast<ShipMoveCommand>())
				{
					var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
					var nextPositions = shipPosition.Apply(moveCommand);
					nextPositions.Count.Should().Be(2);
					var fastShipPosition = FastShipPosition.Create(shipPosition);

					for (int phase = 0; phase < nextPositions.Count; phase++)
					{
						if ((i++)%99 == 0)
							Console.Out.WriteLine($"shipPosition: {shipPosition}; moveCommand: {moveCommand}; phase: {phase}; nextPosition: {nextPositions[phase]}");
						var actual = FastShipPosition.ToShipPosition(FastShipPosition.GetPositionAtPhase(FastShipPosition.Move(fastShipPosition, moveCommand), phase));
						actual.Should().Be(nextPositions[phase], $"shipPosition: {shipPosition}; moveCommand: {moveCommand}; phase: {phase}; nextPosition: {nextPositions[phase]}");
					}
				}
			}
		}

		[Test]
		public void Bow_ReturnsValidCoord()
		{
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			{
				var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
				var fastShipPosition = FastShipPosition.Create(shipPosition);
				FastCoord.ToCoord(FastShipPosition.Bow(fastShipPosition)).Should().Be(shipPosition.bow, shipPosition.ToString());
			}
		}

		[Test]
		public void Collides_ReturnsValidValue()
		{
			for (int xt = 0; xt < Constants.MAP_WIDTH; xt++)
			for (int yt = 0; yt < Constants.MAP_HEIGHT; yt++)
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			{
				var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
				var target = new Coord(xt, yt);
				var fastShipPosition = FastShipPosition.Create(shipPosition);
				FastShipPosition.Collides(fastShipPosition, FastCoord.Create(target)).Should().Be(shipPosition.Collides(target), $"shipPosition: {shipPosition}; target: {target}");
			}
		}

		[Test]
		public void CollidesShip_ReturnsValidValue()
		{
			for (int xt = 0; xt < Constants.MAP_WIDTH; xt += 3)
			for (int yt = 0; yt < Constants.MAP_HEIGHT; yt += 3)
			for (int speedT = 0; speedT <= Constants.MAX_SHIP_SPEED; speedT++)
			for (int orientationT = 0; orientationT < 6; orientationT++)
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			{
				if (!(Math.Abs(x - xt) < 6 && Math.Abs(y - yt) < 6))
					continue;
				var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
				var targetPosition = new ShipPosition(new Coord(xt, yt), orientationT, speedT);
				var fastShipPosition = FastShipPosition.Create(shipPosition);
				var fastTargetPosition = FastShipPosition.Create(targetPosition);
				FastShipPosition.CollidesShip(fastShipPosition, fastTargetPosition).Should().Be(shipPosition.CollidesShip(targetPosition), $"shipPosition: {shipPosition}; targetPosition: {targetPosition}");
			}
		}

		[Test]
		public void Coord_ReturnsValidCoord()
		{
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			{
				var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
				var fastShipPosition = FastShipPosition.Create(shipPosition);
				FastCoord.ToCoord(FastShipPosition.Coord(fastShipPosition)).Should().Be(shipPosition.coord, shipPosition.ToString());
			}
		}

		[Test]
		public void Create_InsideMap_ReturnsValidPosition()
		{
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			{
				var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
				var fastShipPosition = FastShipPosition.Create(shipPosition);
				var actual = FastShipPosition.ToShipPosition(fastShipPosition);
				actual.coord.Should().Be(shipPosition.coord, $"it's coord of [{shipPosition}]");
				actual.speed.Should().Be(shipPosition.speed, $"it's speed of [{shipPosition}]");
				actual.orientation.Should().Be(shipPosition.orientation, $"it's orientation of [{shipPosition}]");
			}
		}

		[Test]
		public void DistanceTo_ReturnsValidValue()
		{
			for (int xt = 0; xt < Constants.MAP_WIDTH; xt++)
			for (int yt = 0; yt < Constants.MAP_HEIGHT; yt++)
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			{
				var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
				var target = new Coord(xt, yt);
				var fastShipPosition = FastShipPosition.Create(shipPosition);
				FastShipPosition.DistanceTo(fastShipPosition, FastCoord.Create(target)).Should().Be(shipPosition.DistanceTo(target), $"shipPosition: {shipPosition}; target: {target}");
			}
		}

		[Test]
		public void DistanceTo_StrangeCase_ReturnsValidValue()
		{
			//coord: 23, 5, orientation: 0, speed: 0 23, 5
			var shipPosition = new ShipPosition(new Coord(23, 5), 0, 0);
			var target = new Coord(20, 5);
			var fastShipPosition = FastShipPosition.Create(shipPosition);
			var fastTarget = FastCoord.Create(target);
			FastShipPosition.DistanceTo(fastShipPosition, fastTarget).Should().Be(1000);
		}

		[Test]
		public void IsInsideMap_ReturnsValidValue()
		{
			for (int x = -2; x < Constants.MAP_WIDTH + 2; x++)
			for (int y = -2; y < Constants.MAP_HEIGHT + 2; y++)
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			{
				var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
				var fastShipPosition = FastShipPosition.Create(shipPosition);
				FastShipPosition.IsInsideMap(fastShipPosition).Should().Be(shipPosition.IsInsideMap(), shipPosition.ToString());
			}
		}

		[Test]
		public void Orientation_ReturnsValidOrientation()
		{
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			{
				var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
				var fastShipPosition = FastShipPosition.Create(shipPosition);
				FastShipPosition.Orientation(fastShipPosition).Should().Be(shipPosition.orientation, shipPosition.ToString());
			}
		}

		[Test]
		public void Speed_ReturnsValidSpeed()
		{
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			{
				var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
				var fastShipPosition = FastShipPosition.Create(shipPosition);
				FastShipPosition.Speed(fastShipPosition).Should().Be(shipPosition.speed, shipPosition.ToString());
			}
		}

		[Test]
		public void Stern_ReturnsValidCoord()
		{
			for (int x = 0; x < Constants.MAP_WIDTH; x++)
			for (int y = 0; y < Constants.MAP_HEIGHT; y++)
			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
			for (int orientation = 0; orientation < 6; orientation++)
			{
				var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
				var fastShipPosition = FastShipPosition.Create(shipPosition);
				FastCoord.ToCoord(FastShipPosition.Stern(fastShipPosition)).Should().Be(shipPosition.stern, shipPosition.ToString());
			}
		}
	}
}