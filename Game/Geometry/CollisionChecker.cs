namespace Game.Geometry
{
	public static class CollisionChecker
	{
		public static CollisionType Move(int myPosition, ShipMoveCommand myCommand, int otherPosition, ShipMoveCommand otherCommand, out uint myMovement, out uint otherMovement)
		{
			var result = CollisionType.None;

			int myCoord, myBow, myStern, myOrientation, mySpeed, nmyCoord, nmyBow, nmyStern, nmyOrientation;
			Split(myPosition, myCommand, out myCoord, out myBow, out myStern, out myOrientation, out mySpeed, out nmyCoord, out nmyBow, out nmyStern, out nmyOrientation);

			int otherCoord, otherBow, otherStern, otherOrientation, otherSpeed, notherCoord, notherBow, notherStern, notherOrientation;
			Split(otherPosition, otherCommand, out otherCoord, out otherBow, out otherStern, out otherOrientation, out otherSpeed, out notherCoord, out notherBow, out notherStern, out notherOrientation);

			// move ships

			for (int i = 1; i <= Constants.MAX_SHIP_SPEED; i++)
			{
				if (!GoForward(i, ref myCoord, ref myBow, ref myStern, ref myOrientation, ref mySpeed, ref nmyCoord, ref nmyBow, ref nmyStern))
					result |= CollisionType.MyWall;
				if (!GoForward(i, ref otherCoord, ref otherBow, ref otherStern, ref otherOrientation, ref otherSpeed, ref notherCoord, ref notherBow, ref notherStern))
					result |= CollisionType.OtherWall;

				var myCollides = mySpeed > 0 && (nmyBow == notherBow || nmyBow == notherCoord || nmyBow == notherStern);
				var otherCollides = otherSpeed > 0 && (notherBow == nmyBow || notherBow == nmyCoord || notherBow == nmyStern);

				if (myCollides)
				{
					nmyCoord = myCoord;
					nmyBow = myBow;
					nmyStern = myStern;
					mySpeed = 0;
					result |= CollisionType.MyMove;
				}
				if (otherCollides)
				{
					notherCoord = otherCoord;
					notherBow = otherBow;
					notherStern = otherStern;
					otherSpeed = 0;
					result |= CollisionType.OtherMove;
				}

				myCoord = nmyCoord;
				myBow = nmyBow;
				myStern = nmyStern;

				otherCoord = notherCoord;
				otherBow = notherBow;
				otherStern = notherStern;
			}

			var myMovedPosition = FastShipPosition.Create(myCoord, myOrientation, mySpeed);
			var otherMovedPosition = FastShipPosition.Create(otherCoord, otherOrientation, otherSpeed);

			// rotate ships
			nmyBow = FastCoord.Neighbor(myCoord, nmyOrientation);
			nmyStern = FastCoord.Neighbor(myCoord, (nmyOrientation + 3) % 6);

			notherBow = FastCoord.Neighbor(otherCoord, notherOrientation);
			notherStern = FastCoord.Neighbor(otherCoord, (notherOrientation + 3) % 6);

			var rotationCollides = 
				nmyBow == notherBow || nmyBow == notherCoord || nmyBow == notherStern ||
				nmyCoord == notherBow || nmyCoord == notherCoord || nmyCoord == notherStern ||
				nmyStern == notherBow || nmyStern == notherCoord || nmyStern == notherStern;

			if (rotationCollides)
			{
				if (myCommand == ShipMoveCommand.Port || myCommand == ShipMoveCommand.Starboard)
					result |= CollisionType.MyRotation;
				if (otherCommand == ShipMoveCommand.Port || otherCommand == ShipMoveCommand.Starboard)
					result |= CollisionType.OtherRotation;
			}
			else
			{
				myBow = nmyBow;
				myStern = nmyStern;
				myOrientation = nmyOrientation;

				otherBow = notherBow;
				otherStern = notherStern;
				otherOrientation = notherOrientation;
			}

			var myFinalPosition = FastShipPosition.Create(myCoord, myOrientation, mySpeed);
			myMovement = FastShipPosition.Move(myMovedPosition, myFinalPosition);

			var otherFinalPosition = FastShipPosition.Create(otherCoord, otherOrientation, otherSpeed);
			otherMovement = FastShipPosition.Move(otherMovedPosition, otherFinalPosition);

			return result;
		}

		private static bool GoForward(int phaseSpeed, ref int coord, ref int bow, ref int stern, ref int orientation, ref int speed, ref int ncoord, ref int nbow, ref int nstern)
		{
			ncoord = coord;
			nbow = bow;
			nstern = stern;

			if (phaseSpeed > speed)
				return true;

			var newCoord = bow;
			if (FastCoord.IsInsideMap(newCoord))
			{
				ncoord = newCoord;
				nbow = FastCoord.Neighbor(ncoord, orientation);
				nstern = FastCoord.Neighbor(ncoord, (orientation + 3) % 6);
				return true;
			}

			speed = 0;
			return false;
		}

		private static void Split(int position, ShipMoveCommand command, out int coord, out int bow, out int stern, out int orientation, out int speed, out int ncoord, out int nbow, out int nstern, out int norientation)
		{
			coord = FastShipPosition.Coord(position);
			orientation = FastShipPosition.Orientation(position);
			speed = FastShipPosition.Speed(position);
			bow = FastCoord.Neighbor(coord, orientation);
			stern = FastCoord.Neighbor(coord, (orientation + 3) % 6);
			ncoord = coord;
			nbow = bow;
			nstern = stern;
			norientation = orientation;
			switch (command)
			{
				case ShipMoveCommand.Faster:
					if (speed < Constants.MAX_SHIP_SPEED)
						speed++;
					break;
				case ShipMoveCommand.Slower:
					if (speed > 0)
						speed--;
					break;
				case ShipMoveCommand.Port:
					norientation = (orientation + 1) % 6;
					break;
				case ShipMoveCommand.Starboard:
					norientation = (orientation + 5) % 6;
					break;
			}
		}
	}
}