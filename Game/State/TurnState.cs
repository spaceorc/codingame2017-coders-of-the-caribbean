using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Game.Entities;
using Game.Geometry;

namespace Game.State
{
	public class TurnState
	{
		public readonly List<Barrel> barrels = new List<Barrel>();
		public readonly List<Ship> myShips = new List<Ship>();
		public readonly List<Ship> enemyShips = new List<Ship>();
		public readonly List<Mine> mines = new List<Mine>();
		public readonly List<Cannonball> cannonballs = new List<Cannonball>();

		public readonly Dictionary<int, Barrel> barrelsById = new Dictionary<int, Barrel>();
		public readonly Dictionary<int, Ship> myShipsById = new Dictionary<int, Ship>();
		public readonly Dictionary<int, Ship> enemyShipsById = new Dictionary<int, Ship>();
		public readonly Dictionary<int, Mine> minesById = new Dictionary<int, Mine>();
		public readonly Dictionary<int, Cannonball> cannonballsById = new Dictionary<int, Cannonball>();

		public readonly Stopwatch stopwatch = Stopwatch.StartNew();

		public readonly List<string> lines = new List<string>();

		private TurnState(TextReader input)
		{
			Dictionary<Coord, Barrel> usedBarrelCoords = new Dictionary<Coord, Barrel>();
			string line;
			var myShipCount = int.Parse(line = input.ReadLine()); // the number of remaining ships
			lines.Add(line);
			var entityCount = int.Parse(line = input.ReadLine()); // the number of entities (e.g. ships, mines or cannonballs)
			lines.Add(line);
			for (var i = 0; i < entityCount; i++)
			{
				line = input.ReadLine();
				lines.Add(line);
				var inputs = line.Split(' ');
				var entityId = int.Parse(inputs[0]);
				var entityType = (EntityType)Enum.Parse(typeof(EntityType), inputs[1], true);
				var x = int.Parse(inputs[2]);
				var y = int.Parse(inputs[3]);
				var arg1 = int.Parse(inputs[4]);
				var arg2 = int.Parse(inputs[5]);
				var arg3 = int.Parse(inputs[6]);
				var arg4 = int.Parse(inputs[7]);
				switch (entityType)
				{
					case EntityType.Barrel:
						var barrel = new Barrel(entityId, x, y, arg1);
						Barrel prevBarrel;
						if (!usedBarrelCoords.TryGetValue(barrel.coord, out prevBarrel))
						{
							usedBarrelCoords.Add(barrel.coord, barrel);
							barrels.Add(barrel);
							barrelsById.Add(entityId, barrel);
						}
						else
							prevBarrel.rum += barrel.rum;
						break;
					case EntityType.Ship:
						var ship = new Ship(entityId, x, y, arg1, arg2, arg3, arg4);
						if (ship.owner == 1)
						{
							myShips.Add(ship);
							myShipsById.Add(entityId, ship);
						}
						else
						{
							enemyShips.Add(ship);
							enemyShipsById.Add(entityId, ship);
						}
						break;
					case EntityType.Mine:
						mines.Add(new Mine(entityId, x, y));
						minesById.Add(entityId, new Mine(entityId, x, y));
						break;
					case EntityType.Cannonball:
						cannonballs.Add(new Cannonball(entityId, x, y, arg1, arg2));
						cannonballsById.Add(entityId, new Cannonball(entityId, x, y, arg1, arg2));
						break;
				}
			}
		}
		
		public static TurnState ReadFrom(TextReader input)
		{
			return new TurnState(input);
		}

		public void WriteTo(TextWriter output)
		{
			if (lines.Any())
			{
				foreach (var line in lines)
					output.WriteLine(line);
			}
			else
			{
				output.WriteLine(myShips.Count);
				output.WriteLine(myShips.Count + enemyShips.Count + barrels.Count + mines.Count + cannonballs.Count);
				foreach (var entity in myShips)
					output.WriteLine($"{entity.id} {entity.type.ToString().ToUpper()} {entity.coord.x} {entity.coord.y} {entity.orientation} {entity.speed} {entity.rum} {entity.owner}");
				foreach (var entity in enemyShips)
					output.WriteLine($"{entity.id} {entity.type.ToString().ToUpper()} {entity.coord.x} {entity.coord.y} {entity.orientation} {entity.speed} {entity.rum} {entity.owner}");
				foreach (var entity in barrels)
					output.WriteLine($"{entity.id} {entity.type.ToString().ToUpper()} {entity.coord.x} {entity.coord.y} {entity.rum} 0 0 0");
				foreach (var entity in mines)
					output.WriteLine($"{entity.id} {entity.type.ToString().ToUpper()} {entity.coord.x} {entity.coord.y} 0 0 0 0");
				foreach (var entity in cannonballs)
					output.WriteLine($"{entity.id} {entity.type.ToString().ToUpper()} {entity.coord.x} {entity.coord.y} {entity.firedBy} {entity.turns} 0 0");
			}
		}
	}
}