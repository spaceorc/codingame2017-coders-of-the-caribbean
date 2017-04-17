using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
internal class Player
{
	private static readonly int MAP_WIDTH = 23;
	private static readonly int MAP_HEIGHT = 21;
	private static readonly int MAX_SHIP_SPEED = 2;
	private static readonly int LOW_DAMAGE = 25;
	private static readonly int HIGH_DAMAGE = 50;
	private static readonly int MINE_DAMAGE = 25;
	private static readonly int NEAR_MINE_DAMAGE = 10;
	private static readonly int NEAR_SHIP_DAMAGE = 5;
	private static readonly int MANUAL_MOVE_DEPTH = 5;
	private static readonly int FREE_REACH_DIST = 5;
	private static readonly int SHIP_MIN_DIST = 4;
	private static readonly bool USE_MINING = false;
	private static readonly bool USE_DOUBLE_PATHFINDING = true;

	private static readonly int DUMP_TURN = -1;

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
3
17
0 SHIP 16 18 5 2 91 1
2 SHIP 6 2 4 2 91 1
4 SHIP 2 8 4 2 89 1
1 SHIP 8 20 0 2 91 0
3 SHIP 20 17 5 0 88 0
5 SHIP 6 14 3 2 90 0
8 MINE 11 19 0 0 0 0
9 MINE 4 1 0 0 0 0
11 MINE 2 10 0 0 0 0
13 MINE 15 15 0 0 0 0
15 MINE 18 13 0 0 0 0
32 CANNONBALL 21 17 0 2 0 0
33 CANNONBALL 5 8 2 2 0 0
34 CANNONBALL 7 12 4 0 0 0
35 CANNONBALL 20 17 0 3 0 0
36 CANNONBALL 17 20 3 3 0 0
21 BARREL 21 19 20 0 0 0
".Trim();
		
		//===
		strategies[0] = new CollectBarrelsStrategy(new Barrel(21, 21, 19, 20));
		strategies[2] = new WalkAroundStrategy(1, true);
		strategies[4] = new WalkAroundStrategy(1, true);
		shipsFired[0] = true;
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
		myShips = new List<Ship>{ship};
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
		if (currentTurn == DUMP_TURN)
		{
			Console.Error.WriteLine("---");
			Console.Error.WriteLine(myShipCount);
			Console.Error.WriteLine(entityCount);
		}
		for (var i = 0; i < entityCount; i++)
		{
			var line = input.ReadLine();
			if (currentTurn == DUMP_TURN)
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
		if (currentTurn == DUMP_TURN)
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
		if (USE_DOUBLE_PATHFINDING && stopwatch.ElapsedMilliseconds < 20)
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
		Console.Error.WriteLine($"Decision made in {stopwatch.ElapsedMilliseconds} ms");
	}

	private static void Preprocess()
	{
		enemyShipsMoved = new List<List<Ship>>();
		var prevShips = enemyShips;
		for (int i = 0; i < MANUAL_MOVE_DEPTH; i++)
		{
			var ships = new List<Ship>();
			enemyShipsMoved.Add(ships);
			foreach (var ship in prevShips)
				ships.Add(ship.Apply(ShipMoveCommand.Wait)[1]);
			prevShips = ships;
		}
		myShipsMoved = new List<List<Ship>>();
		prevShips = myShips;
		for (int i = 0; i < MANUAL_MOVE_DEPTH; i++)
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
		var item = ShipPathChainItem.Start(ship, target);
		queue.Enqueue(item);

		var used = new Dictionary<ShipMovementState, ShipPathChainItem>();
		used.Add(new ShipMovementState(ship), item);

		while (queue.Any())
		{
			var current = queue.Dequeue();
			if (current.depth != MANUAL_MOVE_DEPTH)
				foreach (var moveCommand in Enum.GetValues(typeof(ShipMoveCommand)).Cast<ShipMoveCommand>())
				{
					bool that = false;
					//if (current.ship.id == 1 && current.ship.coord.Equals(new Coord(6, 19)))
					//{
					//	that = true;
					//}

					if (that) Console.Error.WriteLine(moveCommand);
					if (that) Console.Error.WriteLine(current.ship);

					var newShips = current.ship.Apply(moveCommand);
					var newMovedShip = newShips[0];
					var newShip = newShips[1];
					var newMovementState = new ShipMovementState(newShip);
					if (!used.ContainsKey(newMovementState))
					{
						var damage = 0;
						var onMine = mines.Any(m => newShip.Collides(m) || newMovedShip.Collides(m));
						if (onMine)
							damage = Math.Max(damage, MINE_DAMAGE);
						var cannonedBowOrStern = cannonballs.Any(b => b.turns == current.depth + 1 && (newShip.bow.Equals(b.coord) || newShip.stern.Equals(b.coord)));
						if (cannonedBowOrStern)
							damage = Math.Max(damage, LOW_DAMAGE);
						var cannonedCenter = cannonballs.Any(b => b.turns == current.depth + 1 && newShip.coord.Equals(b.coord));
						if (cannonedCenter)
							damage = Math.Max(damage, HIGH_DAMAGE);
						//var nearMyShip = myShips.Where(m => m.id != newShip.id).Any(m => newShip.DistanceTo(m.coord) < SHIP_MIN_DIST);
						var nearEnemyShip = enemyShips.Any(m => newShip.DistanceTo(m.coord) < SHIP_MIN_DIST);
						if (/*nearMyShip || */nearEnemyShip)
							damage = Math.Max(damage, NEAR_SHIP_DAMAGE); // virtual

						if (that) Console.Error.WriteLine(damage);

						var onMyShip = myShips.Where(m => m.id != newShip.id).Any(m => newShip.Collides(m))
							|| myShipsMoved[current.depth].Where(m => m.id != newShip.id).Any(m => newShip.Collides(m));
						//var onEnemyShip = enemyShips.Any(m => newShip.DistanceTo(m.coord) == 0 || newShip.DistanceTo(m.bow) == 0 || newShip.DistanceTo(m.stern) == 0);
						var onEnemyShipMoved = enemyShips.Any(m => newShip.Collides(m))
							|| enemyShipsMoved[current.depth].Any(m => newShip.Collides(m));
						if (!onMyShip /*&& !onEnemyShip */&& !onEnemyShipMoved)
						{
							var next = current.Next(newShip, moveCommand, target, damage);
							queue.Enqueue(next);
							used.Add(newMovementState, next);
							if (that) Console.Error.WriteLine(next);
						}
						else
						{
							used.Add(newMovementState, null);
							if (that) Console.Error.WriteLine("NO");
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
		if (USE_MINING)
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

		public ShipMovementState(Ship ship)
		{
			coord = ship.coord;
			orientation = ship.orientation;
			speed = ship.speed;
		}

		public bool Equals(ShipMovementState other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return coord.Equals(other.coord) && orientation == other.orientation && speed == other.speed;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((ShipMovementState)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = coord.GetHashCode();
				hashCode = (hashCode * 397) ^ orientation;
				hashCode = (hashCode * 397) ^ speed;
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
			if (depth == MANUAL_MOVE_DEPTH)
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
			return new ShipPathChainItem(this, moveCommand, nextShip, depth + 1, prev == null ? moveCommand : startCommand, target, pathDamage + nextDamage);
		}

		public override string ToString()
		{
			return
				$"{(prev == null ? "ROOT: " : "")}{nameof(command)}: {command}, {nameof(depth)}: {depth}, {nameof(startCommand)}: {startCommand}, {nameof(ship)}: {ship}";
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
							yield return new FireTarget(target, turns, i == 0 ? HIGH_DAMAGE : LOW_DAMAGE, i);
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
			return $"{nameof(target)}: {target}, {nameof(rum)}: {rum}, {nameof(turns)}: {turns}, {nameof(priority)}: {priority}";
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
			new Coord(5, MAP_HEIGHT - 5),
			new Coord(MAP_WIDTH - 5, MAP_HEIGHT - 5),
			new Coord(MAP_WIDTH - 5, 5)
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
			if (ship.DistanceTo(targets[currentTarget]) < FREE_REACH_DIST)
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

	#region Entities

	private enum ShipMoveCommand
	{
		Wait,
		Slower,
		Faster,
		Port, // to the left
		Starboard // to the right
	}

	private class Ship : Entity
	{
		public readonly Coord bow;
		public readonly int orientation;
		public readonly int owner;
		public readonly int rum;
		public readonly int speed;
		public readonly Coord stern;

		public Ship(int id, Coord coord, int orientation, int speed, int rum, int owner) : this(id, coord.x, coord.y,
			orientation, speed, rum, owner)
		{
		}

		public Ship(int id, int x, int y, int orientation, int speed, int rum, int owner) : base(id, EntityType.Ship, x, y)
		{
			this.orientation = orientation;
			this.speed = speed;
			this.rum = rum;
			this.owner = owner;
			bow = coord.Neighbor(orientation);
			stern = coord.Neighbor((orientation + 3) % 6);
		}

		public void Wait()
		{
			Console.WriteLine("WAIT");
		}

		public void Move(Coord coord)
		{
			Console.WriteLine($"MOVE {coord.x} {coord.y}");
		}

		public void Fire(Coord coord)
		{
			Console.WriteLine($"FIRE {coord.x} {coord.y}");
		}

		public void Mine()
		{
			Console.WriteLine("MINE");
		}

		public void Faster()
		{
			Console.WriteLine("FASTER");
		}

		public void Slower()
		{
			Console.WriteLine("SLOWER");
		}

		public void Port()
		{
			Console.WriteLine("PORT");
		}

		public void Starboard()
		{
			Console.WriteLine("STARBOARD");
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

		public List<Ship> Apply(ShipMoveCommand moveCommand)
		{
			var result = new List<Ship>();
			var newSpeed = speed;
			switch (moveCommand)
			{
				case ShipMoveCommand.Faster:
					newSpeed++;
					break;
				case ShipMoveCommand.Slower:
					newSpeed--;
					break;
			}
			if (newSpeed > MAX_SHIP_SPEED)
				newSpeed = MAX_SHIP_SPEED;
			if (newSpeed < 0)
				newSpeed = 0;
			var movedShip = this;
			for (var sp = 1; sp <= newSpeed; sp++)
			{
				var newShip = new Ship(movedShip.id, movedShip.coord.Neighbor(orientation), orientation, sp, rum - 1, owner);
				if (!newShip.IsInsideMap())
					break;
				movedShip = newShip;
			}
			if (movedShip.speed != newSpeed)
				movedShip = new Ship(movedShip.id, movedShip.coord, orientation, 0, rum - 1, owner);
			result.Add(movedShip);
			switch (moveCommand)
			{
				case ShipMoveCommand.Port:
					movedShip = new Ship(movedShip.id, movedShip.coord, (orientation + 1) % 6, movedShip.speed, rum - 1, owner);
					break;
				case ShipMoveCommand.Starboard:
					movedShip = new Ship(movedShip.id, movedShip.coord, (orientation + 5) % 6, movedShip.speed, rum - 1, owner);
					break;
			}
			result.Add(movedShip);
			return result;
		}

		public bool IsInsideMap()
		{
			return coord.IsInsideMap();
		}

		public int DistanceTo(Coord target)
		{
			var dist = coord.DistanceTo(target);
			if (dist == 0)
				return 0;
			var bowDist = bow.DistanceTo(target);
			if (bowDist == 0)
				return 0;
			if (bowDist < dist)
				return bowDist;
			var sternDist = stern.DistanceTo(target);
			if (sternDist == 0)
				return 0;
			return dist;
		}

		public bool Collides(Coord target)
		{
			return coord.Equals(target) || bow.Equals(target) || stern.Equals(target);
		}

		public bool Collides(Entity target)
		{
			return Collides(target.coord);
		}

		public bool Collides(Ship target)
		{
			return Collides(target.coord) || Collides(target.bow) || Collides(target.stern);
		}

		public override string ToString()
		{
			return $"{base.ToString()}, {nameof(orientation)}: {orientation}, {nameof(speed)}: {speed}";
		}
	}

	private class Barrel : Entity
	{
		public readonly int rum;

		public Barrel(int id, int x, int y, int rum) : base(id, EntityType.Barrel, x, y)
		{
			this.rum = rum;
		}

		public string Dump()
		{
			return $"new Barrel({id}, {coord.x}, {coord.y}, {rum})";
		}
	}

	private class Mine : Entity
	{
		public Mine(int id, int x, int y) : base(id, EntityType.Mine, x, y)
		{
		}
	}

	private class Cannonball : Entity
	{
		public readonly int firedBy;
		public readonly int turns;

		public Cannonball(int id, int x, int y, int firedBy, int turns) : base(id, EntityType.Cannonball, x, y)
		{
			this.firedBy = firedBy;
			this.turns = turns;
		}
	}

	private enum EntityType
	{
		Ship,
		Barrel,
		Mine,
		Cannonball
	}

	private abstract class Entity
	{
		public readonly Coord coord;
		public readonly int id;
		public readonly EntityType type;

		protected Entity(int id, EntityType type, int x, int y)
		{
			this.id = id;
			this.type = type;
			coord = new Coord(x, y);
		}

		public override string ToString()
		{
			return $"{type}[{id}] at ({coord})";
		}
	}

	#endregion

	#region Coords

	private struct Coord
	{
		private static readonly int[][] DIRECTIONS_EVEN =
			{new[] {1, 0}, new[] {0, -1}, new[] {-1, -1}, new[] {-1, 0}, new[] {-1, 1}, new[] {0, 1}};

		private static readonly int[][] DIRECTIONS_ODD =
			{new[] {1, 0}, new[] {1, -1}, new[] {0, -1}, new[] {-1, 0}, new[] {0, 1}, new[] {1, 1}};

		public Coord(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public readonly int x;
		public readonly int y;

		public int DistanceTo(Coord dst)
		{
			return ToCubeCoord().DistanceTo(dst.ToCubeCoord());
		}

		public Coord Neighbor(int orientation)
		{
			int newY, newX;
			if (y % 2 == 1)
			{
				newY = y + DIRECTIONS_ODD[orientation][1];
				newX = x + DIRECTIONS_ODD[orientation][0];
			}
			else
			{
				newY = y + DIRECTIONS_EVEN[orientation][1];
				newX = x + DIRECTIONS_EVEN[orientation][0];
			}

			return new Coord(newX, newY);
		}

		public override string ToString()
		{
			return $"{x}, {y}";
		}

		public CubeCoord ToCubeCoord()
		{
			var xp = x - (y - (y & 1)) / 2;
			var zp = y;
			var yp = -(xp + zp);
			return new CubeCoord(xp, yp, zp);
		}

		public bool IsInsideMap()
		{
			return x >= 0 && x < MAP_WIDTH && y >= 0 && y < MAP_HEIGHT;
		}
	}

	private struct CubeCoord
	{
		private static readonly int[][] directions =
		{
			new[] {1, -1, 0},
			new[] {+1, 0, -1},
			new[] {0, +1, -1},
			new[] {-1, +1, 0},
			new[] {-1, 0, +1},
			new[] {0, -1, +1}
		};

		public CubeCoord(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public readonly int x;
		public readonly int y;
		public readonly int z;

		public int DistanceTo(CubeCoord dst)
		{
			return (Math.Abs(x - dst.x) + Math.Abs(y - dst.y) + Math.Abs(z - dst.z)) / 2;
		}

		public CubeCoord Neighbor(int orientation)
		{
			var nx = x + directions[orientation][0];
			var ny = y + directions[orientation][1];
			var nz = z + directions[orientation][2];

			return new CubeCoord(nx, ny, nz);
		}

		public Coord ToCoord()
		{
			var newX = x + (z - (z & 1)) / 2;
			var newY = z;
			return new Coord(newX, newY);
		}
	}

	#endregion
}