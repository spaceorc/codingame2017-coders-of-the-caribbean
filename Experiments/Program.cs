using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Cannons;
using Game.Entities;
using Game.Geometry;
using Game.Mining;
using Game.Navigation;
using Game.State;
using Game.Strategy;

namespace Experiments
{
	class Program
	{
		private static void Main(string[] args)
		{
			var state = @"
2
5
1 SHIP 17 16 1 2 42 1
3 SHIP 18 6 3 2 64 1
0 SHIP 15 12 0 2 72 0
2 SHIP 7 14 1 2 66 0
50 CANNONBALL 16 15 1 2 0 0
".Trim();

			//===
			var gameState = new GameState();
			gameState.cannoneers[1] = new Cannoneer(1, gameState) { fire = true };
			gameState.cannoneers[3] = new Cannoneer(3, gameState) { fire = true };
			gameState.miners[1] = new Miner(1, gameState) { cooldown = 0 };
			gameState.miners[3] = new Miner(3, gameState) { cooldown = 0 };
			gameState.navigators[1] = new Navigator(1, gameState);
			gameState.navigators[3] = new Navigator(3, gameState);
			gameState.admiral.strategies[1] = new CollectBarrelsStrategy(1, gameState.admiral.gameState) { currentTargetId = 28 };
			gameState.admiral.strategies[3] = new CollectBarrelsStrategy(3, gameState.admiral.gameState) { currentTargetId = 25 };
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
