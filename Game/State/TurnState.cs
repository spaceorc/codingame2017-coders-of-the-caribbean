using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Entities;

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
		private readonly List<Ship> myShipsById = new List<Ship>();
		private readonly List<Ship> enemyShipsById = new List<Ship>();
		public readonly Dictionary<int, Mine> minesById = new Dictionary<int, Mine>();
		public readonly Dictionary<int, Cannonball> cannonballsById = new Dictionary<int, Cannonball>();

		public readonly Stopwatch stopwatch = Stopwatch.StartNew();

		public readonly List<string> lines = new List<string>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Ship FindMyShip(int shipId)
		{
			return shipId >= myShipsById.Count ? null : myShipsById[shipId];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Ship FindEnemyShip(int shipId)
		{
			return shipId >= enemyShipsById.Count ? null : enemyShipsById[shipId];
		}

		private TurnState(TextReader input)
		{
			Dictionary<int, Barrel> usedBarrelCoords = new Dictionary<int, Barrel>();
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
						if (!usedBarrelCoords.TryGetValue(barrel.fcoord, out prevBarrel))
						{
							usedBarrelCoords.Add(barrel.fcoord, barrel);
							barrels.Add(barrel);
							barrelsById.Add(entityId, barrel);
						}
						else
							prevBarrel.rum += barrel.rum;
						break;
					case EntityType.Ship:
						if (arg4 == 1)
						{
							var ship = new Ship(myShips.Count, entityId, x, y, arg1, arg2, arg3, arg4);
							myShips.Add(ship);
							while (myShipsById.Count <= entityId)
								myShipsById.Add(null);
							myShipsById[entityId] = ship;
						}
						else
						{
							var ship = new Ship(enemyShips.Count, entityId, x, y, arg1, arg2, arg3, arg4);
							enemyShips.Add(ship);
							while (enemyShipsById.Count <= entityId)
								enemyShipsById.Add(null);
							enemyShipsById[entityId] = ship;
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
					output.WriteLine($"{entity.id} {entity.type.ToString().ToUpper()} {entity._coord.x} {entity._coord.y} {entity._orientation} {entity._speed} {entity.rum} {entity.owner}");
				foreach (var entity in enemyShips)
					output.WriteLine($"{entity.id} {entity.type.ToString().ToUpper()} {entity._coord.x} {entity._coord.y} {entity._orientation} {entity._speed} {entity.rum} {entity.owner}");
				foreach (var entity in barrels)
					output.WriteLine($"{entity.id} {entity.type.ToString().ToUpper()} {entity._coord.x} {entity._coord.y} {entity.rum} 0 0 0");
				foreach (var entity in mines)
					output.WriteLine($"{entity.id} {entity.type.ToString().ToUpper()} {entity._coord.x} {entity._coord.y} 0 0 0 0");
				foreach (var entity in cannonballs)
					output.WriteLine($"{entity.id} {entity.type.ToString().ToUpper()} {entity._coord.x} {entity._coord.y} {entity.firedBy} {entity.turns} 0 0");
			}
		}
	}
}