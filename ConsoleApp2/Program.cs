using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	private static readonly int MANUAL_MOVE_DEPTH = 6;
	private static readonly int FREE_REACH_DIST = 5;
	private static readonly int SHIP_MIN_DIST = 3;

	private static Dictionary<int, Barrel> barrels;
	private static HashSet<Coord> usedBarrelCoords;
	private static List<Ship> myShips;
	private static List<Mine> mines;
	private static List<Cannonball> cannonballs;
	private static readonly Dictionary<int, IStrategy> strategies = new Dictionary<int, IStrategy>();
	private static readonly Dictionary<int, bool> shipsFired = new Dictionary<int, bool>();
	private static List<Ship> enemyShips;
	private static List<Ship> enemyShipsMoved;

	private static void Main2(string[] args)
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
		ManualMove(ship, new Coord(2, 2));
	}

	private static void Main(string[] args)
	{
		// game loop
		while (true)
		{
			var stopwatch = Stopwatch.StartNew();
			barrels = new Dictionary<int, Barrel>();
			usedBarrelCoords = new HashSet<Coord>();
			myShips = new List<Ship>();
			enemyShips = new List<Ship>();
			mines = new List<Mine>();
			cannonballs = new List<Cannonball>();
			var myShipCount = int.Parse(Console.ReadLine()); // the number of remaining ships
			var entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. ships, mines or cannonballs)
			for (var i = 0; i < entityCount; i++)
			{
				var inputs = Console.ReadLine().Split(' ');
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
			Preprocess();
			foreach (var ship in myShips)
			{
				var action = Decide(ship);
				switch (action.type)
				{
					case DecisionType.Goto:
						ManualMove(ship, action.coord);
						break;
					default:
						ship.Wait();
						break;
				}
			}
			stopwatch.Stop();
			Console.Error.WriteLine($"Decision made in {stopwatch.ElapsedMilliseconds} ms");
		}
	}

	private static void Preprocess()
	{
		enemyShipsMoved = new List<Ship>();
		foreach (var enemyShip in enemyShips)
			enemyShipsMoved.Add(enemyShip.Apply(ShipMoveCommand.Wait)[1]);
	}

	private static ShipMoveCommand SelectMoveCommand(Ship ship, Coord target)
	{
		if (ship.DistanceTo(target) == 0)
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
						var onMine = mines.Any(m => newShip.DistanceTo(m.coord) == 0 || newMovedShip.DistanceTo(m.coord) == 0);
						if (onMine)
							damage = MINE_DAMAGE;
						var cannonedBowOrStern = cannonballs.Any(b => b.turns == current.depth + 1 && (newShip.bow.DistanceTo(b.coord) == 0 || newShip.stern.DistanceTo(b.coord) == 0));
						if (cannonedBowOrStern)
							damage = LOW_DAMAGE;
						var cannonedCenter = cannonballs.Any(b => b.turns == current.depth + 1 && newShip.coord.DistanceTo(b.coord) == 0);
						if (cannonedCenter)
							damage = HIGH_DAMAGE;
						var nearMyShip = myShips.Where(m => m.id != newShip.id).Any(m => newShip.DistanceTo(m.coord) < SHIP_MIN_DIST);
						var nearEnemyShip = enemyShips.Any(m => newShip.DistanceTo(m.coord) < SHIP_MIN_DIST);
						if (nearMyShip || nearEnemyShip)
							damage = HIGH_DAMAGE * 2; // virtual

						if (that) Console.Error.WriteLine(damage);

						var onMyShip = myShips.Where(m => m.id != newShip.id).Any(m => newShip.DistanceTo(m.coord) == 0 || newShip.DistanceTo(m.bow) == 0 || newShip.DistanceTo(m.stern) == 0);
						var onEnemyShip = enemyShips.Any(m => newShip.DistanceTo(m.coord) == 0 || newShip.DistanceTo(m.bow) == 0 || newShip.DistanceTo(m.stern) == 0);
						var onEnemyShipMoved = enemyShipsMoved.Any(m => newShip.DistanceTo(m.coord) == 0 || newShip.DistanceTo(m.bow) == 0 || newShip.DistanceTo(m.stern) == 0);
						if (!onMyShip && !onEnemyShip && !onEnemyShipMoved)
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
		return bestChainItem?.startCommand ?? ShipMoveCommand.Wait;
	}

	private static void ManualMove(Ship ship, Coord target)
	{
		var moveCommand = SelectMoveCommand(ship, target);
		if (moveCommand == ShipMoveCommand.Wait)
		{
			bool fired;
			shipsFired.TryGetValue(ship.id, out fired);
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
		shipsFired[ship.id] = false;
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
				if (bestFireTarget == null || bestFireTarget.rum < fireTarget.rum || bestFireTarget.turns > fireTarget.turns)
					bestFireTarget = fireTarget;
			}
		}
		return bestFireTarget;
	}

	private static IEnumerable<FireTarget> GetFireTargets(Ship ship, Ship enemyShip)
	{
		var targets = new List<Tuple<Coord, int>>
		{
			Tuple.Create(enemyShip.coord, 50),
			Tuple.Create(enemyShip.bow, 25),
			Tuple.Create(enemyShip.stern, 25)
		};
		var currentMyShips = myShips;
		for (var turns = 0; turns < 4; turns++)
		{
			foreach (var target in targets)
			{
				if (!target.Item1.IsInsideMap() || currentMyShips.Any(m => m.DistanceTo(target.Item1) == 0))
					continue;
				var distanceTo = ship.bow.DistanceTo(target.Item1);
				if (distanceTo <= 10)
				{
					var travelTime = (int)(1 + Math.Round(distanceTo / 3.0));
					if (travelTime == turns)
						yield return new FireTarget(target.Item1, turns, target.Item2);
				}
			}
			if (enemyShip.speed != 0)
				for (var i = 0; i < targets.Count; i++)
				for (var j = 0; j < enemyShip.speed; j++)
					targets[i] = Tuple.Create(targets[i].Item1.Neighbor(enemyShip.orientation), targets[i].Item2);
			currentMyShips = currentMyShips.Select(c => c.Apply(ShipMoveCommand.Wait)[1]).ToList();
		}
	}

	private class FireTarget
	{
		public readonly int rum;
		public readonly Coord target;
		public readonly int turns;

		public FireTarget(Coord target, int turns, int rum)
		{
			this.target = target;
			this.turns = turns;
			this.rum = rum;
		}
	}

	#endregion

	#region Strategies

	private static Decision Decide(Ship ship)
	{
		IStrategy strategy;
		if (!strategies.TryGetValue(ship.id, out strategy))
			strategies[ship.id] = strategy = new CollectBarrelsStrategy();
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
	}

	private class CollectBarrelsStrategy : IStrategy
	{
		public Barrel currentTarget;

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