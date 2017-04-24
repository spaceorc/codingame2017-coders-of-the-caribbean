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
3
8
1 SHIP 21 13 4 2 64 1
3 SHIP 16 17 0 2 61 1
5 SHIP 18 14 4 0 20 1
0 SHIP 2 1 0 2 54 0
4 SHIP 13 16 4 2 85 0
13 MINE 18 18 0 0 0 0
80 CANNONBALL 15 15 4 2 0 0
81 CANNONBALL 14 18 3 1 0 0
".Trim();

			//===
			var gameState = new GameState { currentTurn = 110 };
			gameState.cannoneers[1] = new Cannoneer(1, gameState) { cooldown = false };
			gameState.cannoneers[3] = new Cannoneer(3, gameState) { cooldown = false };
			gameState.cannoneers[5] = new Cannoneer(5, gameState) { cooldown = false };
			gameState.miners[1] = new Miner(1, gameState) { cooldown = 0 };
			gameState.miners[3] = new Miner(3, gameState) { cooldown = 0 };
			gameState.miners[5] = new Miner(5, gameState) { cooldown = 0 };
			gameState.navigators[1] = new Navigator(1, gameState);
			gameState.navigators[3] = new Navigator(3, gameState);
			gameState.navigators[5] = new Navigator(5, gameState);
			((Strateg)gameState.strateg).decisions[5] = new StrategicDecision { role = StrategicRole.Suicide, targetBarrelId = null, fireToCoord = null, explicitCommand = null, targetCoord = 563 };
			((Strateg)gameState.strateg).decisions[3] = new StrategicDecision { role = StrategicRole.RunAway, targetBarrelId = null, fireToCoord = null, explicitCommand = null, targetCoord = 563 };
			((Strateg)gameState.strateg).decisions[1] = new StrategicDecision { role = StrategicRole.Approach, targetBarrelId = null, fireToCoord = null, explicitCommand = null, targetCoord = 530 };

			//===

			gameState.Iteration(new StringReader(state));
		}
	}
}
