using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.Statistics;

namespace Game
{
	public class Player
	{
		private static Dictionary<int, Barrel> barrels;
		private static HashSet<Coord> usedBarrelCoords;
		private static List<Ship> myShips;
		private static List<Ship> enemyShips;
		private static List<Mine> mines;
		private static List<Cannonball> cannonballs;
		private static readonly Dictionary<int, IStrategy> strategies = new Dictionary<int, IStrategy>();
		private static readonly Dictionary<int, bool> shipsFired = new Dictionary<int, bool>();
		private static readonly Dictionary<int, int> shipsMined = new Dictionary<int, int>();
		private static List<List<Ship>> enemyShipsMoved;
		private static List<List<Ship>> myShipsMoved;
		private static List<TurnStat> stats = new List<TurnStat>();

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

		private static void Main22(string[] args)
		{
			var state = @"
2
6
1 SHIP 4 14 5 0 57 1
3 SHIP 5 14 5 2 28 1
0 SHIP 12 10 5 2 36 0
2 SHIP 19 9 2 2 45 0
5 MINE 5 16 0 0 0 0
7 MINE 1 16 0 0 0 0
".Trim();

			//===
			strategies[1] = new WalkAroundStrategy(2, true);
			strategies[3] = new WalkAroundStrategy(2, true);
			//===


			Iteration(int.MaxValue, new StringReader(state));
		}

		private static void Main2(string[] args)
		{
			var ship = new Ship(1, new Coord(6, 15), owner: 1, rum: 100, orientation: 0, speed: 2);
			enemyShips = new List<Ship>
			{
				new Ship(666, new Coord(6, 20), owner: 0, rum: 100, orientation: 0, speed: 2)
			};
			myShips = new List<Ship> {ship};
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
		}


		private static void Iteration(int currentTurn, TextReader input)
		{
			barrels = new Dictionary<int, Barrel>();
			usedBarrelCoords = new HashSet<Coord>();
			myShips = new List<Ship>();
			enemyShips = new List<Ship>();
			mines = new List<Mine>();
			cannonballs = new List<Cannonball>();
			var myShipCount = int.Parse(input.ReadLine()); // the number of remaining ships
			var entityCount = int.Parse(input.ReadLine()); // the number of entities (e.g. ships, mines or cannonballs)
			Console.Error.WriteLine("Current turn: " + currentTurn);
			if (currentTurn == Settings.DUMP_TURN)
			{
				Console.Error.WriteLine("---");
				Console.Error.WriteLine(myShipCount);
				Console.Error.WriteLine(entityCount);
			}
			for (var i = 0; i < entityCount; i++)
			{
				var line = input.ReadLine();
				if (currentTurn == Settings.DUMP_TURN)
				{
					Console.Error.WriteLine(line);
				}
				var inputs = line.Split(' ');
				var entityId = int.Parse(inputs[0]);
				var entityType = (EntityType) Enum.Parse(typeof(EntityType), inputs[1], true);
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
						if (usedBarrelCoords.Add(barrel.coord))
							barrels.Add(entityId, barrel);
						//Console.Error.WriteLine($"Barrel found: {barrel}");
						break;
					case EntityType.Ship:
						var ship = new Ship(entityId, x, y, arg1, arg2, arg3, arg4);
						if (ship.owner == 1)
							myShips.Add(ship);
						else
							enemyShips.Add(ship);
						break;
					case EntityType.Mine:
						mines.Add(new Mine(entityId, x, y));
						break;
					case EntityType.Cannonball:
						cannonballs.Add(new Cannonball(entityId, x, y, arg1, arg2));
						break;
				}
			}
			if (currentTurn == Settings.DUMP_TURN)
			{
				Console.Error.WriteLine("===");
				foreach (var kvp in strategies)
					Console.Error.WriteLine($"strategies[{kvp.Key}] = {kvp.Value.Dump()};");
				foreach (var kvp in shipsFired)
					Console.Error.WriteLine($"shipsFired[{kvp.Key}] = {kvp.Value.ToString().ToLower()};");
				foreach (var kvp in shipsMined)
					Console.Error.WriteLine($"shipsMined[{kvp.Key}] = {kvp.Value};");
			}
			var stopwatch = Stopwatch.StartNew();
			Preprocess();
			var moves = new List<ShipMoveCommand>();
			foreach (var ship in myShips)
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
			bool isDouble = Settings.USE_DOUBLE_PATHFINDING && stopwatch.ElapsedMilliseconds < 15;
			if (isDouble)
			{
				moves = new List<ShipMoveCommand>();
				foreach (var ship in myShips)
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
			for (var i = 0; i < myShips.Count; i++)
			{
				var ship = myShips[i];
				ManualMove(ship, moves[i]);
			}
			stopwatch.Stop();
			stats.Add(new TurnStat {isDouble = isDouble, time = stopwatch.ElapsedMilliseconds});
			Console.Error.WriteLine($"Decision made in {stopwatch.ElapsedMilliseconds} ms");
			if (currentTurn == Settings.DUMP_STAT_TURN)
			{
				DumpStats();
			}
		}

		private static void DumpStats()
		{
			Console.Error.WriteLine("--- STATISTICS ---");
			Console.Error.WriteLine($"TotalCount: {stats.Count}");
			Console.Error.WriteLine($"DoublePathCount: {stats.Count(t => t.isDouble)}");
			Console.Error.WriteLine($"Time_Max: {stats.Max(t => t.time)}");
			Console.Error.WriteLine($"Time_Avg: {stats.Average(t => t.time)}");
			Console.Error.WriteLine($"Time_95: {stats.Percentile(t => t.time, 95)}");
			Console.Error.WriteLine($"Time_50: {stats.Percentile(t => t.time, 50)}");
			Console.Error.WriteLine($"TimeCorrected_Avg: {stats.Average(t => t.CorrectedTime())}");
			Console.Error.WriteLine($"TimeCorrected_95: {stats.Percentile(t => t.CorrectedTime(), 95)}");
			Console.Error.WriteLine($"TimeCorrected_50: {stats.Percentile(t => t.CorrectedTime(), 50)}");
			Console.Error.WriteLine("---");
		}

		private static void Preprocess()
		{
			enemyShipsMoved = new List<List<Ship>>();
			var prevShips = enemyShips;
			for (int i = 0; i < Settings.MANUAL_MOVE_DEPTH; i++)
			{
				var ships = new List<Ship>();
				enemyShipsMoved.Add(ships);
				foreach (var ship in prevShips)
					ships.Add(ship.Apply(ShipMoveCommand.Wait)[1]);
				prevShips = ships;
			}
			myShipsMoved = new List<List<Ship>>();
			prevShips = myShips;
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
							var onMine = mines.Any(m => newShip.Collides(m) || newMovedShip.Collides(m));
							if (onMine)
								damage = Math.Max(damage, Constants.MINE_DAMAGE);
							var cannonedBowOrStern = cannonballs.Any(b => b.turns == current.depth + 1 &&
							                                              (newShip.bow.Equals(b.coord) || newShip.stern.Equals(b.coord)));
							if (cannonedBowOrStern)
								damage = Math.Max(damage, Constants.LOW_DAMAGE);
							var cannonedCenter = cannonballs.Any(b => b.turns == current.depth + 1 && newShip.coord.Equals(b.coord));
							if (cannonedCenter)
								damage = Math.Max(damage, Constants.HIGH_DAMAGE);
							var nearEnemyShip = enemyShips.Any(m => newShip.DistanceTo(m.coord) < Settings.SHIP_MIN_DIST);
							if (nearEnemyShip)
								damage = Math.Max(damage, Settings.NEAR_SHIP_DAMAGE); // virtual

							var onMyShip = current.depth == 0 && myShips.Where(m => m.id != newShip.id)
								               .Any(m => newShip.Collides(m) || newMovedShip.Collides(m))
							               || myShipsMoved[current.depth]
								               .Where(m => m.id != newShip.id)
								               .Any(m => newShip.Collides(m) || newMovedShip.Collides(m));
							var onEnemyShipMoved = current.depth == 0 && enemyShips.Any(m => newShip.Collides(m) || newMovedShip.Collides(m))
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
			bool fired;
			shipsFired.TryGetValue(ship.id, out fired);
			shipsFired.Remove(ship.id);
			int mined;
			shipsMined.TryGetValue(ship.id, out mined);
			if (mined - 1 <= 0)
				shipsMined.Remove(ship.id);
			else
				shipsMined[ship.id] = mined - 1;
			if (moveCommand == ShipMoveCommand.Wait)
			{
				if (!fired)
				{
					var fireTarget = SelectFireTarget(ship);
					if (fireTarget != null)
					{
						shipsFired[ship.id] = true;
						ship.Fire(fireTarget.target);
						return;
					}
				}
			}
			if (Settings.USE_MINING)
			{
				if (moveCommand == ShipMoveCommand.Wait)
				{
					if (mined <= 0)
					{
						shipsMined[ship.id] = 4;
						ship.Mine();
						return;
					}
				}
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

		#region FireTarget

		private static FireTarget SelectFireTarget(Ship ship)
		{
			FireTarget bestFireTarget = null;
			for (var i = 0; i < enemyShips.Count; i++)
			{
				var fireTargets = GetFireTargets(ship, enemyShips[i]);
				foreach (var fireTarget in fireTargets)
				{
					if (bestFireTarget == null ||
					    fireTarget.priority < bestFireTarget.priority ||
					    fireTarget.priority == bestFireTarget.priority && fireTarget.turns < bestFireTarget.turns)
						bestFireTarget = fireTarget;
				}
			}
			return bestFireTarget;
		}

		private static IEnumerable<FireTarget> GetFireTargets(Ship ship, Ship enemyShip)
		{
			var currentMyShips = myShips;
			for (var turns = 0; turns < 5; turns++)
			{
				enemyShip = enemyShip.Apply(ShipMoveCommand.Wait)[1];
				var coords = new[] {enemyShip.coord, enemyShip.bow, enemyShip.stern};
				for (var i = 0; i < coords.Length; i++)
				{
					var target = coords[i];
					if (target.IsInsideMap())
					{
						if (currentMyShips.Any(m => m.DistanceTo(target) == 0))
							continue;
						var distanceTo = ship.bow.DistanceTo(target);
						if (distanceTo <= 10)
						{
							var travelTime = (int) (1 + Math.Round(distanceTo / 3.0));
							if (travelTime == turns)
								yield return new FireTarget(target, turns, i == 0 ? Constants.HIGH_DAMAGE : Constants.LOW_DAMAGE, i);
						}
					}
				}
				currentMyShips = currentMyShips.Select(c => c.Apply(ShipMoveCommand.Wait)[1]).ToList();
			}
		}

		private class FireTarget
		{
			public readonly int rum;
			public readonly Coord target;
			public readonly int turns;
			public readonly int priority;

			public FireTarget(Coord target, int turns, int rum, int priority)
			{
				this.target = target;
				this.turns = turns;
				this.rum = rum;
				this.priority = priority;
			}

			public override string ToString()
			{
				return
					$"{nameof(target)}: {target}, {nameof(rum)}: {rum}, {nameof(turns)}: {turns}, {nameof(priority)}: {priority}";
			}
		}

		#endregion

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
				if (!barrels.Any())
					return Decision.Unknown();

				var used = new HashSet<int>();
				foreach (var myShip in myShips)
				{
					IStrategy otherStrategy;
					if (myShip.id != ship.id && strategies.TryGetValue(myShip.id, out otherStrategy))
					{
						var otherBarrelId = (otherStrategy as CollectBarrelsStrategy)?.currentTarget?.id;
						if (otherBarrelId.HasValue)
							used.Add(otherBarrelId.Value);
					}
				}

				if (currentTarget == null || !barrels.ContainsKey(currentTarget.id))
				{
					var bestDist = int.MaxValue;
					Barrel bestBarrel = null;
					foreach (var barrel in barrels.Values)
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