using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Entities;
using Game.FireTeam;
using Game.Geometry;
using Game.Navigation;
using Game.State;
using Game.Strategy;

namespace Experiments
{
	class Program
	{
		private static void Main22(string[] args)
		{
			FastCoord.Init();

			var coordsX = new List<Coord>();
			for (int x = -1; x < Constants.MAP_WIDTH + 1; x++)
			for (int y = -1; y < Constants.MAP_HEIGHT + 1; y++)
			{
				var coord = new Coord(x, y);
				coordsX.Add(coord);
			}

			var indexes = Enumerable.Range(0, coordsX.Count).ToArray();
			var seed = new Random().Next();
			Console.Out.WriteLine($"Seed: {seed}");
			var random = new Random(seed);
			for (int i = 0; i < indexes.Length; i++)
			{
				var r = random.Next(i, indexes.Length);
				var tmp = indexes[r];
				indexes[r] = indexes[i];
				indexes[i] = tmp;
			}

			var coords = indexes.Select(i => coordsX[i]).ToArray();
			var fastCoords = indexes.Select(i => FastCoord.Create(coords[i])).ToArray();

			var ships = coords.Select(c => new ShipPosition(c, random.Next(6), random.Next(3))).ToArray();
			var fastShips = ships.Select(FastShipPosition.Create).ToArray();

			var stopwatch = Stopwatch.StartNew();

			Console.Out.WriteLine("IsInsideMap");
			stopwatch.Restart();
			int ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				coords[ind++].IsInsideMap();
				if (ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				FastCoord.IsInsideMap(fastCoords[ind++]);
				if (ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			Console.Out.WriteLine("DistanceTo");
			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				coords[ind++].DistanceTo(coords[0]);
				if (ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				FastCoord.Distance(fastCoords[ind++], fastCoords[0]);
				if (ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			Console.Out.WriteLine("Neighbor");
			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				coords[ind].Neighbor(0);
				coords[ind].Neighbor(1);
				coords[ind].Neighbor(2);
				coords[ind].Neighbor(3);
				coords[ind].Neighbor(4);
				coords[ind].Neighbor(5);
				if (++ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				FastCoord.Neighbor(fastCoords[ind], 0);
				FastCoord.Neighbor(fastCoords[ind], 1);
				FastCoord.Neighbor(fastCoords[ind], 2);
				FastCoord.Neighbor(fastCoords[ind], 3);
				FastCoord.Neighbor(fastCoords[ind], 4);
				FastCoord.Neighbor(fastCoords[ind], 5);
				if (++ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);
			
			Console.Out.WriteLine("ShipDistanceTo");
			var shipPosition = new ShipPosition(coords[0], 0, 0);
			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				shipPosition.DistanceTo(coords[ind]);
				if (++ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			var fastShipPosition = FastShipPosition.Create(shipPosition);
			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				FastShipPosition.DistanceTo(fastShipPosition, fastCoords[ind++]);
				if (ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			Console.Out.WriteLine("Collides");
			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				shipPosition.Collides(coords[ind++]);
				if (ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				FastShipPosition.Collides(fastShipPosition, fastCoords[ind++]);
				if (ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			Console.Out.WriteLine("CollidesShip");
			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				shipPosition.CollidesShip(ships[ind++]);
				if (ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 10000000; i++)
			{
				FastShipPosition.CollidesShip(fastShipPosition, fastShips[ind++]);
				if (ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			Console.Out.WriteLine("Move");
			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 1_000_000; i++)
			{
				foreach (var moveCommand in ShipMoveCommands.all)
					ships[ind].Apply(moveCommand);
				if (++ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 1_000_000; i++)
			{
				foreach (var moveCommand in ShipMoveCommands.all)
				{
					var moved = FastShipPosition.Move(fastShips[ind], moveCommand);
					FastShipPosition.GetMovedPosition(moved);
					FastShipPosition.GetFinalPosition(moved);
				}
				if (++ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);

			stopwatch.Restart();
			ind = 0;
			for (int i = 0; i < 1_000_000; i++)
			{
				foreach (var moveCommand in ShipMoveCommands.all)
				{
					uint myMovement;
					uint otherMovement;
					CollisionChecker.Move(fastShips[ind], moveCommand, fastShips[(ind+1)%indexes.Length], moveCommand, out myMovement, out otherMovement);
					FastShipPosition.GetMovedPosition(myMovement);
					FastShipPosition.GetFinalPosition(myMovement);
				}
				if (++ind >= indexes.Length)
					ind = 0;
			}
			stopwatch.Stop();
			Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);
		}

		private static void Main(string[] args)
		{
			var state = @"
1
25
3 SHIP 14 10 1 2 100 1
0 SHIP 5 9 3 2 99 0
2 SHIP 14 7 4 1 90 0
5 MINE 16 11 0 0 0 0
4 MINE 16 9 0 0 0 0
9 MINE 16 14 0 0 0 0
8 MINE 16 6 0 0 0 0
33 CANNONBALL 17 11 2 1 0 0
34 CANNONBALL 14 10 0 2 0 0
35 CANNONBALL 19 10 2 3 0 0
36 CANNONBALL 13 9 1 2 0 0
37 CANNONBALL 13 9 3 2 0 0
17 BARREL 5 13 20 0 0 0
16 BARREL 5 7 20 0 0 0
19 BARREL 19 13 17 0 0 0
18 BARREL 19 7 17 0 0 0
21 BARREL 21 11 13 0 0 0
20 BARREL 21 9 13 0 0 0
23 BARREL 3 19 10 0 0 0
22 BARREL 3 1 10 0 0 0
24 BARREL 3 10 20 0 0 0
26 BARREL 6 11 13 0 0 0
29 BARREL 19 10 19 0 0 0
31 BARREL 2 15 16 0 0 0
30 BARREL 2 5 16 0 0 0
".Trim();

			//===
			var gameState = new GameState { currentTurn = 24 };
			gameState.cannoneers[1] = new Cannoneer(1, gameState) { cooldown = true };
			gameState.cannoneers[3] = new Cannoneer(3, gameState) { cooldown = true };
			gameState.miners[1] = new Miner(1, gameState) { cooldown = 0 };
			gameState.miners[3] = new Miner(3, gameState) { cooldown = 0 };
			gameState.navigators[1] = new Navigator(1, gameState);
			gameState.navigators[3] = new Navigator(3, gameState);
			gameState.admiral.strategies[1] = new CollectBarrelsStrategy(1, gameState.admiral.gameState) { currentTargetId = 29, currentTarget = 372 };
			gameState.admiral.strategies[3] = new CollectBarrelsStrategy(3, gameState.admiral.gameState) { currentTargetId = 14, currentTarget = 335 };

			//===

			gameState.Iteration(new StringReader(state));
		}
		/*
		private static void Main2(string[] args)
		{
			var ship = new Ship(1, new Coord(6, 15), owner: 1, rum: 100, orientation: 0, speed: 2);
			enemyShips = new List<Ship>
			{
				new Ship(666, new Coord(6, 20), owner: 0, rum: 100, orientation: 0, speed: 2)
			};
			myShips = new List<Ship> { ship };
			var fireTarget = SelectFireTarget(ship);
			Console.Out.WriteLine(fireTarget);
		}

		private static void Main3(string[] args)
		{
			var ship = new Ship(1, new Coord(6, 19), owner: 1, rum: 100, orientation: 0, speed: 0);
			shipsFired.Add(ship.id, true);
			mines = new List<Mine>();
			cannonballs = new List<Cannonball>();
			myShips = new List<Ship>
			{
				ship,
				new Ship(2, new Coord(8, 19), owner: 1, rum: 100, orientation: 4, speed: 0)
			};
			enemyShips = new List<Ship>();
			Preprocess();
			//ManualMove(ship, new Coord(2, 2));
		}*/
	}
}
