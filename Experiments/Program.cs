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
3
1 SHIP 18 5 2 1 96 1
0 SHIP 6 14 5 1 100 0
17 MINE 17 3 0 0 0 0
".Trim();

			//===
			var gameState = new GameState { currentTurn = 46 };
			gameState.cannoneers[1] = new Cannoneer(1, gameState) { cooldown = false };
			gameState.miners[1] = new Miner(1, gameState) { cooldown = 0 };
			gameState.navigators[1] = new Navigator(1, gameState);
			((Strateg)gameState.strateg).decisions[1] = new StrategicDecision { role = StrategicRole.RunAway, targetCoord = new Coord(18, 5).ToFastCoord() };

			//===

			gameState.Iteration(new StringReader(state));
		}
	}
}
