using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.Navigation
{
	public class Navigator
	{
		public readonly GameState gameState;
		public readonly int shipId;

		public Navigator(int shipId, GameState gameState)
		{
			this.shipId = shipId;
			this.gameState = gameState;
		}

		// todo move it to precalc logic
		public void ApplyPath(TurnState turnState, List<ShipMoveCommand> path)
		{
			var ship = turnState.myShipsById[shipId];

			var index = Player.myShipsMoved[0].FindIndex(s => s.id == shipId);
			var movedShip = ship;
			for (var i = 0; i < path.Count; i++)
			{
				var moveCommand = path[i];
				movedShip = movedShip.Apply(moveCommand)[0];
				Player.myShipsMoved[i][index] = movedShip;
			}
			for (var i = path.Count; i < Settings.NAVIGATION_PATH_DEPTH; i++)
			{
				movedShip = movedShip.Apply(ShipMoveCommand.Wait)[0];
				Player.myShipsMoved[i][index] = movedShip;
			}
		}

		public List<ShipMoveCommand> FindPath(TurnState turnState, Coord target)
		{
			var ship = turnState.myShipsById[shipId];

			// todo fix navigation here - iterate anyway!
			if (ship.Collides(target))
				return new List<ShipMoveCommand>();

			var queue = new Queue<ShipPathChainItem>();
			queue.Enqueue(ShipPathChainItem.Start(ship, target));

			var used = new Dictionary<ShipMovementState, ShipPathChainItem>();

			while (queue.Any())
			{
				var current = queue.Dequeue();
				if (current.depth != Settings.NAVIGATION_PATH_DEPTH)
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
							var nearEnemyShip = turnState.enemyShips.Any(m => newShip.DistanceTo(m.coord) < Settings.NEAR_ENEMY_SHIP_MIN_DIST);
							if (nearEnemyShip)
								damage = Math.Max(damage, Settings.NEAR_ENEMYSHIP_VIRTUAL_DAMAGE); // virtual

							var onMyShip = current.depth == 0 && turnState.myShips.Where(m => m.id != newShip.id)
								               .Any(m => newShip.Collides(m) || newMovedShip.Collides(m))
							               || Player.myShipsMoved[current.depth]
								               .Where(m => m.id != newShip.id)
								               .Any(m => newShip.Collides(m) || newMovedShip.Collides(m));
							var onEnemyShipMoved = current.depth == 0 && turnState.enemyShips.Any(m => newShip.Collides(m) || newMovedShip.Collides(m))
							                       || Player.enemyShipsMoved[current.depth]
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

			if (bestChainItem == null)
				return new List<ShipMoveCommand>();

			var chain = new List<ShipMoveCommand>();
			while (bestChainItem.prev != null)
			{
				chain.Add(bestChainItem.command);
				bestChainItem = bestChainItem.prev;
			}
			chain.Reverse();
			return chain;
		}

		public string Dump(string gameStateRef)
		{
			return $"new {nameof(Navigator)}({shipId}, {gameStateRef})";
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
				return Equals((ShipMovementState)obj);
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
				if (depth == Settings.NAVIGATION_PATH_DEPTH)
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
				return $"{(prev == null ? "ROOT: " : "")}{nameof(command)}: {command}, {nameof(depth)}: {depth}, {nameof(startCommand)}: {startCommand}, {nameof(ship)}: {ship}, {nameof(damage)}: {damage}, {nameof(pathDamage)}: {pathDamage}";
			}
		}
	}
}