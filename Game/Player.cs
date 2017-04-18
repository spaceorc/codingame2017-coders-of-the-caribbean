using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;
using Game.Statistics;

namespace Game
{
	public class Player
	{
		private static readonly Dictionary<int, IStrategy> strategies = new Dictionary<int, IStrategy>();
		private static TurnState turnState;
		private static GameState gameState = new GameState();

		private static void Main(string[] args)
		{
			// game loop
			int currentTurn = 0;
			while (true)
			{
				currentTurn += 2;
				Iteration(currentTurn, Console.In);
			}
		}

		private static void Iteration(int currentTurn, TextReader input)
		{
			turnState = TurnState.ReadFrom(input);
			Console.Error.WriteLine("Current turn: " + currentTurn);
			if (currentTurn == Settings.DUMP_TURN)
			{
				turnState.WriteTo(Console.Error);
				Console.Error.WriteLine("===");
				gameState.Dump();
			}
			turnState.stopwatch.Restart();
			gameState.forecaster.BuildForecast(turnState);
			var moves = new List<List<ShipMoveCommand>>();
			foreach (var ship in turnState.myShips)
			{
				var action = Decide(ship);
				var navigator = gameState.GetNavigator(ship);
				switch (action.type)
				{
					case DecisionType.Goto:
						var path = navigator.FindPath(turnState, action.coord);
						moves.Add(path);
						gameState.forecaster.ApplyPath(ship, path);
						break;
					default:
						moves.Add(null);
						break;
				}
			}

			bool isDouble = Settings.USE_DOUBLE_PATHFINDING && turnState.stopwatch.ElapsedMilliseconds < Settings.DOUBLE_PATHFINDING_TIMELIMIT;
			if (isDouble)
			{
				moves = new List<List<ShipMoveCommand>>();
				foreach (var ship in turnState.myShips)
				{
					var action = Decide(ship);
					var navigator = gameState.GetNavigator(ship);
					switch (action.type)
					{
						case DecisionType.Goto:
							var path = navigator.FindPath(turnState, action.coord);
							moves.Add(path);
							gameState.forecaster.ApplyPath(ship, path);
							break;
						default:
							moves.Add(null);
							break;
					}
				}
			}
			for (var i = 0; i < turnState.myShips.Count; i++)
			{
				var ship = turnState.myShips[i];
				ManualMove(ship, moves[i]?.FirstOrDefault() ?? ShipMoveCommand.Wait);
			}
			turnState.stopwatch.Stop();
			gameState.stats.Add(new TurnStat {isDouble = isDouble, time = turnState.stopwatch.ElapsedMilliseconds});
			Console.Error.WriteLine($"Decision made in {turnState.stopwatch.ElapsedMilliseconds} ms (isDouble = {isDouble})");
			if (currentTurn == Settings.DUMP_STAT_TURN)
				gameState.DumpStats();
		}

		private static void ManualMove(Ship ship, ShipMoveCommand moveCommand)
		{
			var cannoneer = gameState.GetCannoneer(ship);
			cannoneer.PrepareToFire(turnState);
			var miner = gameState.GetMiner(ship);
			miner.PrepareToMine(turnState);
			if (moveCommand == ShipMoveCommand.Wait)
			{
				if (cannoneer.Fire(turnState))
					return;
				if (miner.Mine(turnState))
					return;
			}
			ship.Move(moveCommand);
		}


		#region Strategies

		private static Decision Decide(Ship ship)
		{
			IStrategy strategy;
			if (!strategies.TryGetValue(ship.id, out strategy))
				strategies[ship.id] = strategy = new CollectBarrelsStrategy();
			if (strategy is WalkAroundStrategy)
			{
				var switchStrategy = new CollectBarrelsStrategy();
				var switchAction = switchStrategy.Decide(ship);
				if (switchAction.type == DecisionType.Goto)
				{
					strategies[ship.id] = switchStrategy;
					return switchAction;
				}
			}
			var action = strategy.Decide(ship);
			if (action.type == DecisionType.Unknown)
			{
				strategies[ship.id] = strategy = new WalkAroundStrategy();
				action = strategy.Decide(ship);
			}
			return action;
		}

		private enum DecisionType
		{
			Unknown,
			Goto
		}

		private class Decision
		{
			public readonly Coord coord;
			public readonly DecisionType type;

			private Decision(DecisionType type, Coord coord)
			{
				this.type = type;
				this.coord = coord;
			}

			public static Decision Unknown()
			{
				return new Decision(DecisionType.Unknown, default(Coord));
			}

			public static Decision Goto(Coord coord)
			{
				return new Decision(DecisionType.Goto, coord);
			}
		}

		private interface IStrategy
		{
			Decision Decide(Ship ship);
			string Dump();
		}

		private class CollectBarrelsStrategy : IStrategy
		{
			public Barrel currentTarget;

			public CollectBarrelsStrategy()
			{
			}

			public CollectBarrelsStrategy(Barrel currentTarget)
			{
				this.currentTarget = currentTarget;
			}

			public Decision Decide(Ship ship)
			{
				if (!turnState.barrels.Any())
					return Decision.Unknown();

				var used = new HashSet<int>();
				foreach (var myShip in turnState.myShips)
				{
					IStrategy otherStrategy;
					if (myShip.id != ship.id && strategies.TryGetValue(myShip.id, out otherStrategy))
					{
						var otherBarrelId = (otherStrategy as CollectBarrelsStrategy)?.currentTarget?.id;
						if (otherBarrelId.HasValue)
							used.Add(otherBarrelId.Value);
					}
				}

				if (currentTarget == null || !turnState.barrelsById.ContainsKey(currentTarget.id))
				{
					var bestDist = int.MaxValue;
					Barrel bestBarrel = null;
					foreach (var barrel in turnState.barrels)
						if (!used.Contains(barrel.id))
						{
							var dist = ship.DistanceTo(barrel.coord);
							if (dist < bestDist)
							{
								bestBarrel = barrel;
								bestDist = dist;
							}
						}
					currentTarget = bestBarrel;
					Console.Error.WriteLine($"New target for {ship.id}: {currentTarget}");
				}

				return currentTarget == null ? Decision.Unknown() : Decision.Goto(currentTarget.coord);
			}

			public string Dump()
			{
				return $"new CollectBarrelsStrategy({currentTarget.Dump()})";
			}
		}

		private class WalkAroundStrategy : IStrategy
		{
			private static readonly Coord[] targets =
			{
				new Coord(5, 5),
				new Coord(5, Constants.MAP_HEIGHT - 5),
				new Coord(Constants.MAP_WIDTH - 5, Constants.MAP_HEIGHT - 5),
				new Coord(Constants.MAP_WIDTH - 5, 5)
			};

			private int currentTarget;
			private bool started;

			public WalkAroundStrategy()
			{
			}

			public WalkAroundStrategy(int currentTarget, bool started)
			{
				this.currentTarget = currentTarget;
				this.started = started;
			}

			public Decision Decide(Ship ship)
			{
				if (ship.DistanceTo(targets[currentTarget]) < Settings.FREE_WALK_TARGET_REACH_DIST)
				{
					currentTarget = (currentTarget + 1) % targets.Length;
					Console.Error.WriteLine($"New target for {ship.id}: {targets[currentTarget]}");
				}
				if (!started)
				{
					started = true;
					Console.Error.WriteLine($"New target for {ship.id}: {targets[currentTarget]}");
				}
				return Decision.Goto(targets[currentTarget]);
			}

			public string Dump()
			{
				return $"new WalkAroundStrategy({currentTarget}, {started.ToString().ToLower()})";
			}
		}

		#endregion

		
	}
}