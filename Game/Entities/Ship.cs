using System;
using Game.Geometry;

namespace Game.Entities
{
	public class Ship : Entity
	{
		public readonly Coord _bow;
		public readonly int fbow;
		public readonly int _orientation;
		public readonly int owner;
		public readonly int rum;
		public readonly int _speed;
		public readonly Coord _stern;
		public readonly int fstern;
		public readonly int fposition;

		public Ship(int id, Coord coord, int orientation, int speed, int rum, int owner) : this(id, coord.x, coord.y,
			orientation, speed, rum, owner)
		{
		}

		public Ship(int id, int x, int y, int orientation, int speed, int rum, int owner) : base(id, EntityType.Ship, x, y)
		{
			this._orientation = orientation;
			this._speed = speed;
			this.rum = rum;
			this.owner = owner;
			_bow = _coord.Neighbor(orientation);
			_stern = _coord.Neighbor((orientation + 3) % 6);
			fposition = FastShipPosition.Create(x, y, orientation, speed);
			fbow = FastShipPosition.Bow(fposition);
			fstern = FastShipPosition.Stern(fposition);
		}

		public void Wait()
		{
			Console.WriteLine("WAIT");
		}

		public void Move(Coord coord)
		{
			Console.WriteLine($"MOVE {coord.x} {coord.y}");
		}

		public void Move(int fastCoord)
		{
			Console.WriteLine($"MOVE {FastCoord.GetX(fastCoord)} {FastCoord.GetY(fastCoord)}");
		}

		public void Fire(Coord coord)
		{
			Console.WriteLine($"FIRE {coord.x} {coord.y}");
		}

		public void Fire(int fastCoord)
		{
			Console.WriteLine($"FIRE {FastCoord.GetX(fastCoord)} {FastCoord.GetY(fastCoord)}");
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

		public override string ToString()
		{
			return $"{base.ToString()}, {nameof(_orientation)}: {_orientation}, {nameof(_speed)}: {_speed}";
		}
	}
}