using System;
using FluentAssertions;
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
}