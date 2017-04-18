using System;
using System.Collections.Generic;
using Game.Geometry;

namespace Game.Entities
{
	public class Ship : Entity
	{
		public readonly Coord bow;
		public readonly int orientation;
		public readonly int owner;
		public readonly int rum;
		public readonly int speed;
		public readonly Coord stern;

		public Ship(int id, Coord coord, int orientation, int speed, int rum, int owner) : this(id, coord.x, coord.y,
			orientation, speed, rum, owner)
		{
		}

		public Ship(int id, int x, int y, int orientation, int speed, int rum, int owner) : base(id, EntityType.Ship, x, y)
		{
			this.orientation = orientation;
			this.speed = speed;
			this.rum = rum;
			this.owner = owner;
			bow = coord.Neighbor(orientation);
			stern = coord.Neighbor((orientation + 3) % 6);
		}

		public void Wait()
		{
			Console.WriteLine("WAIT");
		}

		public void Move(Coord coord)
		{
			Console.WriteLine($"MOVE {coord.x} {coord.y}");
		}

		public void Fire(Coord coord)
		{
			Console.WriteLine($"FIRE {coord.x} {coord.y}");
		}

		public void Mine()
		{
			Console.WriteLine("MINE");
		}

		public void Faster()
		{
			Console.WriteLine("FASTER");
		}

		public void Slower()
		{
			Console.WriteLine("SLOWER");
		}

		public void Port()
		{
			Console.WriteLine("PORT");
		}

		public void Starboard()
		{
			Console.WriteLine("STARBOARD");
		}

		public void Move(ShipMoveCommand command)
		{
			switch (command)
			{
				case ShipMoveCommand.Faster:
					Faster();
					break;
				case ShipMoveCommand.Slower:
					Slower();
					break;
				case ShipMoveCommand.Port:
					Port();
					break;
				case ShipMoveCommand.Starboard:
					Starboard();
					break;
				default:
					Wait();
					break;
			}
		}

		public List<Ship> Apply(ShipMoveCommand moveCommand)
		{
			var result = new List<Ship>();
			var newSpeed = speed;
			switch (moveCommand)
			{
				case ShipMoveCommand.Faster:
					newSpeed++;
					break;
				case ShipMoveCommand.Slower:
					newSpeed--;
					break;
			}
			if (newSpeed > Constants.MAX_SHIP_SPEED)
				newSpeed = Constants.MAX_SHIP_SPEED;
			if (newSpeed < 0)
				newSpeed = 0;
			var movedShip = this;
			for (var sp = 1; sp <= newSpeed; sp++)
			{
				var newShip = new Ship(movedShip.id, movedShip.coord.Neighbor(orientation), orientation, sp, rum - 1, owner);
				if (!newShip.IsInsideMap())
					break;
				movedShip = newShip;
			}
			if (movedShip.speed != newSpeed)
				movedShip = new Ship(movedShip.id, movedShip.coord, orientation, 0, rum - 1, owner);
			result.Add(movedShip);
			switch (moveCommand)
			{
				case ShipMoveCommand.Port:
					movedShip = new Ship(movedShip.id, movedShip.coord, (orientation + 1) % 6, movedShip.speed, rum - 1, owner);
					break;
				case ShipMoveCommand.Starboard:
					movedShip = new Ship(movedShip.id, movedShip.coord, (orientation + 5) % 6, movedShip.speed, rum - 1, owner);
					break;
			}
			result.Add(movedShip);
			return result;
		}

		public bool IsInsideMap()
		{
			return coord.IsInsideMap();
		}

		public int DistanceTo(Coord target)
		{
			var dist = coord.DistanceTo(target);
			if (dist == 0)
				return 0;
			var bowDist = bow.DistanceTo(target);
			if (bowDist == 0)
				return 0;
			if (bowDist < dist)
				return bowDist;
			var sternDist = stern.DistanceTo(target);
			if (sternDist == 0)
				return 0;
			return dist;
		}

		public bool Collides(Coord target)
		{
			return coord.Equals(target) || bow.Equals(target) || stern.Equals(target);
		}

		public bool Collides(Entity target)
		{
			return Collides(target.coord);
		}

		public bool Collides(Ship target)
		{
			return Collides(target.coord) || Collides(target.bow) || Collides(target.stern);
		}

		public override string ToString()
		{
			return $"{base.ToString()}, {nameof(orientation)}: {orientation}, {nameof(speed)}: {speed}";
		}
	}
}