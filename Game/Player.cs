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
		private static List<List<Ship>> enemyShipsMoved;
		private static List<List<Ship>> myShipsMoved;
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
			Preprocess();
			var moves = new List<ShipMoveCommand>();
			foreach (var ship in turnState.myShips)
			{
				var action = Decide(ship);
				switch (action.type)
				{
					case DecisionType.Goto:
						moves.Add(SelectMoveCommand(ship, action.coord));
						break;
					default:
						moves.Add(ShipMoveCommand.Wait);
						break;
				}
			}
			bool isDouble = Settings.USE_DOUBLE_PATHFINDING && turnState.stopwatch.ElapsedMilliseconds < Settings.DOUBLE_PATHFINDING_TIMELIMIT;
			if (isDouble)
			{
				moves = new List<ShipMoveCommand>();
				foreach (var ship in turnState.myShips)
				{
					var action = Decide(ship);
					switch (action.type)
					{
						case DecisionType.Goto:
							moves.Add(SelectMoveCommand(ship, action.coord));
							break;
						default:
							moves.Add(ShipMoveCommand.Wait);
							break;
					}
				}
			}
			for (var i = 0; i < turnState.myShips.Count; i++)
			{
				var ship = turnState.myShips[i];
				ManualMove(ship, moves[i]);
			}
			turnState.stopwatch.Stop();
			gameState.stats.Add(new TurnStat {isDouble = isDouble, time = turnState.stopwatch.ElapsedMilliseconds});
			Console.Error.WriteLine($"Decision made in {turnState.stopwatch.ElapsedMilliseconds} ms");
			if (currentTurn == Settings.DUMP_STAT_TURN)
				gameState.DumpStats();
		}

		private static void Preprocess()
		{
			enemyShipsMoved = new List<List<Ship>>();
			var prevShips = turnState.enemyShips;
			for (int i = 0; i < Settings.MANUAL_MOVE_DEPTH; i++)
			{
				var ships = new List<Ship>();
				enemyShipsMoved.Add(ships);
				foreach (var ship in prevShips)
					ships.Add(ship.Apply(ShipMoveCommand.Wait)[1]);
				prevShips = ships;
			}
			myShipsMoved = new List<List<Ship>>();
			prevShips = turnState.myShips;
			for (int i = 0; i < Settings.MANUAL_MOVE_DEPTH; i++)
			{
				var ships = new List<Ship>();
				myShipsMoved.Add(ships);
				foreach (var ship in prevShips)
					ships.Add(ship.Apply(ShipMoveCommand.Wait)[1]);
				prevShips = ships;
			}
		}

		private static ShipMoveCommand SelectMoveCommand(Ship ship, Coord target)
		{
			if (ship.Collides(target))
				return ShipMoveCommand.Wait;

			var queue = new Queue<ShipPathChainItem>();
			queue.Enqueue(ShipPathChainItem.Start(ship, target));

			var used = new Dictionary<ShipMovementState, ShipPathChainItem>();

			while (queue.Any())
			{
				var current = queue.Dequeue();
				if (current.depth != Settings.MANUAL_MOVE_DEPTH)
					foreach (var moveCommand in Enum.GetValues(typeof(ShipMoveCommand)).Cast<ShipMoveCommand>())
					{
						var newShips = current.ship.Apply(moveCommand);
						var newMovedShip = newShips[0];
						var newShip = newShips[1];
						var newMovementState = new ShipMovementState(newShip, current.depth + 1);
						if (!used.ContainsKey(newMovementState))
						{
							var damage = 0;
							var onMine = turnState.mines.Any(m => newShip.Collides(m) || newMovedShip.Collides(m));
							if (onMine)
								damage = Math.Max(damage, Constants.MINE_DAMAGE);
							var cannonedBowOrStern = turnState.cannonballs.Any(b => b.turns == current.depth + 1 &&
							                                              (newShip.bow.Equals(b.coord) || newShip.stern.Equals(b.coord)));
							if (cannonedBowOrStern)
								damage = Math.Max(damage, Constants.LOW_DAMAGE);
							var cannonedCenter = turnState.cannonballs.Any(b => b.turns == current.depth + 1 && newShip.coord.Equals(b.coord));
							if (cannonedCenter)
								damage = Math.Max(damage, Constants.HIGH_DAMAGE);
							var nearEnemyShip = turnState.enemyShips.Any(m => newShip.DistanceTo(m.coord) < Settings.SHIP_MIN_DIST);
							if (nearEnemyShip)
								damage = Math.Max(damage, Settings.NEAR_SHIP_DAMAGE); // virtual

							var onMyShip = current.depth == 0 && turnState.myShips.Where(m => m.id != newShip.id)
								               .Any(m => newShip.Collides(m) || newMovedShip.Collides(m))
							               || myShipsMoved[current.depth]
								               .Where(m => m.id != newShip.id)
								               .Any(m => newShip.Collides(m) || newMovedShip.Collides(m));
							var onEnemyShipMoved = current.depth == 0 && turnState.enemyShips.Any(m => newShip.Collides(m) || newMovedShip.Collides(m))
							                       || enemyShipsMoved[current.depth]
								                       .Any(m => newShip.Collides(m) || newMovedShip.Collides(m));
							if (!onMyShip && !onEnemyShipMoved)
							{
								var next = current.Next(newShip, moveCommand, target, damage);
								queue.Enqueue(next);
								used.Add(newMovementState, next);
							}
							else
							{
								used.Add(newMovementState, null);
							}
						}
					}
			}

			ShipPathChainItem bestChainItem = null;
			foreach (var chainItem in used.Values.Where(v => v != null))
			{
				if (chainItem.prev != null)
				{
					if (bestChainItem == null || chainItem.damage < bestChainItem.damage || chainItem.damage == bestChainItem.damage
					    && (chainItem.dist < bestChainItem.dist || chainItem.dist == bestChainItem.dist
					        && (chainItem.depth < bestChainItem.depth || chainItem.depth == bestChainItem.depth
					            && chainItem.startCommand == ShipMoveCommand.Wait)))
					{
						bestChainItem = chainItem;
					}
				}
			}

			if (bestChainItem != null)
			{
				var chain = new List<ShipMoveCommand>();
				var chainItem = bestChainItem;
				while (chainItem.prev != null)
				{
					chain.Add(chainItem.command);
					chainItem = chainItem.prev;
				}
				chain.Reverse();

				var index = myShipsMoved[0].FindIndex(s => s.id == ship.id);
				var movedShip = ship;
				for (var i = 0; i < chain.Count; i++)
				{
					var moveCommand = chain[i];
					movedShip = movedShip.Apply(moveCommand)[0];
					myShipsMoved[i][index] = movedShip;
				}
			}

			return bestChainItem?.startCommand ?? ShipMoveCommand.Wait;
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

		private class ShipMovementState : IEquatable<ShipMovementState>
		{
			public readonly Coord coord;
			public readonly int orientation;
			public readonly int speed;
			public readonly int depth;

			public ShipMovementState(Ship ship, int depth)
			{
				this.depth = depth;
				coord = ship.coord;
				orientation = ship.orientation;
				speed = ship.speed;
			}

			public bool Equals(ShipMovementState other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return coord.Equals(other.coord) && orientation == other.orientation && speed == other.speed &&
				       depth == other.depth;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((ShipMovementState) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = coord.GetHashCode();
					hashCode = (hashCode * 397) ^ orientation;
					hashCode = (hashCode * 397) ^ speed;
					hashCode = (hashCode * 397) ^ depth;
					return hashCode;
				}
			}

			public static bool operator ==(ShipMovementState left, ShipMovementState right)
			{
				return Equals(left, right);
			}

			public static bool operator !=(ShipMovementState left, ShipMovementState right)
			{
				return !Equals(left, right);
			}
		}

		private class ShipPathChainItem
		{
			public readonly ShipMoveCommand command;
			public readonly int depth;
			public readonly int dist;
			public readonly int pathDamage;
			public readonly ShipPathChainItem prev;
			public readonly Ship ship;
			public readonly ShipMoveCommand startCommand;
			public int damage = int.MaxValue;

			private ShipPathChainItem(ShipPathChainItem prev, ShipMoveCommand command, Ship ship, int depth,
				ShipMoveCommand startCommand, Coord target, int pathDamage)
			{
				this.prev = prev;
				this.command = command;
				this.ship = ship;
				this.depth = depth;
				this.startCommand = startCommand;
				dist = ship.DistanceTo(target);
				this.pathDamage = pathDamage;
				if (depth == Settings.MANUAL_MOVE_DEPTH)
					SetDamage(pathDamage);
			}

			private void SetDamage(int newDamage)
			{
				var t = this;
				while (t != null && t.damage > newDamage)
				{
					t.damage = newDamage;
					t = t.prev;
				}
			}

			public static ShipPathChainItem Start(Ship ship, Coord target)
			{
				return new ShipPathChainItem(null, ShipMoveCommand.Wait, ship, 0, ShipMoveCommand.Wait, target, 0);
			}

			public ShipPathChainItem Next(Ship nextShip, ShipMoveCommand moveCommand, Coord target, int nextDamage)
			{
				return new ShipPathChainItem(this, moveCommand, nextShip, depth + 1, prev == null ? moveCommand : startCommand,
					target, pathDamage + nextDamage);
			}

			public override string ToString()
			{
				return
					$"{(prev == null ? "ROOT: " : "")}{nameof(command)}: {command}, {nameof(depth)}: {depth}, {nameof(startCommand)}: {startCommand}, {nameof(ship)}: {ship}, {nameof(damage)}: {damage}, {nameof(pathDamage)}: {pathDamage}";
			}
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
				if (ship.DistanceTo(targets[currentTarget]) < Settings.FREE_REACH_DIST)
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