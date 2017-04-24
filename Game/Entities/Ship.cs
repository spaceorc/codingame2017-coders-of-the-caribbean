using System;
using Game.Geometry;

namespace Game.Entities
{
	public class Ship : Entity
	{
		public readonly Coord bow;
		public readonly int fbow;
		public readonly int orientation;
		public readonly int owner;
		public readonly int rum;
		public readonly int speed;
		public readonly Coord stern;
		public readonly int fstern;
		public readonly int fposition;
		public readonly int index;

		public Ship(int index, int id, Coord coord, int orientation, int speed, int rum, int owner) : this(index, id, coord.x, coord.y,
			orientation, speed, rum, owner)
		{
		}

		public Ship(int index, int id, int x, int y, int orientation, int speed, int rum, int owner) : base(id, EntityType.Ship, x, y)
		{
			this.index = index;
			this.orientation = orientation;
			this.speed = speed;
			this.rum = rum;
			this.owner = owner;
			bow = coord.Neighbor(orientation);
			stern = coord.Neighbor((orientation + 3) % 6);
			fposition = FastShipPosition.Create(x, y, orientation, speed);
			fbow = FastShipPosition.Bow(fposition);
			fstern = FastShipPosition.Stern(fposition);
		}

		public void Wait()
		{
			Console.WriteLine("WAIT WAIT");
		}
		
		public void Fire(int fastCoord)
		{
			Console.WriteLine($"FIRE {FastCoord.GetX(fastCoord)} {FastCoord.GetY(fastCoord)} FIRE {FastCoord.GetX(fastCoord)} {FastCoord.GetY(fastCoord)}");
		}

		public void Mine()
		{
			Console.WriteLine("MINE MINE");
		}

		public void Faster()
		{
			Console.WriteLine("FASTER FASTER");
		}

		public void Slower()
		{
			Console.WriteLine("SLOWER SLOWER");
		}

		public void Port()
		{
			Console.WriteLine("PORT PORT");
		}

		public void Starboard()
		{
			Console.WriteLine("STARBOARD STARBOARD");
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
			return $"{base.ToString()}, {nameof(orientation)}: {orientation}, {nameof(speed)}: {speed}";
		}
	}
}