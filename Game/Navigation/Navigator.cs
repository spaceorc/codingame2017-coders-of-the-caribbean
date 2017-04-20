using System;
using System.Collections.Generic;
using System.Linq;
using Game.Geometry;
using Game.State;

namespace Game.Navigation
{
	public class Navigator : ITeamMember
	{
		public readonly GameState gameState;
		public readonly int shipId;

		public Navigator(int shipId, GameState gameState)
		{
			this.shipId = shipId;
			this.gameState = gameState;
		}

		public void StartTurn(TurnState turnState)
		{
		}

		public void EndTurn(TurnState turnState)
		{
		}

		public List<ShipMoveCommand> FindPath(TurnState turnState, int ftarget)
		{
			var ship = turnState.myShipsById[shipId];

			// todo fix navigation here - iterate anyway!
			if (FastShipPosition.Collides(ship.fposition, ftarget))
				return new List<ShipMoveCommand>();

			var queue = new Queue<ShipPathChainItem>();
			queue.Enqueue(ShipPathChainItem.Start(ship.fposition, ftarget));

			var used = new Dictionary<ShipMovementState, ShipPathChainItem>();

			while (queue.Any())
			{
				var current = queue.Dequeue();
				if (current.depth != Settings.NAVIGATION_PATH_DEPTH)
					foreach (var moveCommand in Enum.GetValues(typeof(ShipMoveCommand)).Cast<ShipMoveCommand>())
					{
						var newShipMovement = FastShipPosition.Move(current.fposition, moveCommand);
						var newMovedPos = FastShipPosition.GetMovedPosition(newShipMovement);
						var newPos = FastShipPosition.GetFinalPosition(newShipMovement);
						var newMovementState = new ShipMovementState(newPos, current.depth + 1);
						if (!used.ContainsKey(newMovementState))
						{
							var damage = 0;
							var onMine = turnState.mines.Any(m => FastShipPosition.Collides(newPos, m.fcoord) || FastShipPosition.Collides(newMovedPos, m.fcoord));
							if (onMine)
								damage = Math.Max(damage, Constants.MINE_DAMAGE);
							var cannonedBowOrStern = turnState.cannonballs.Any(b => b.turns == current.depth + 1 && (FastShipPosition.BowCollides(newPos, b.fcoord) || FastShipPosition.SternCollides(newPos, b.fcoord)));
							if (cannonedBowOrStern)
								damage = Math.Max(damage, Constants.LOW_DAMAGE);
							var cannonedCenter = turnState.cannonballs.Any(b => b.turns == current.depth + 1 && FastShipPosition.CenterCollides(newPos, b.fcoord));
							if (cannonedCenter)
								damage = Math.Max(damage, Constants.HIGH_DAMAGE);
							var nearEnemyShip = turnState.enemyShips.Any(m => FastShipPosition.DistanceTo(newPos, m.fcoord) < Settings.NEAR_ENEMY_SHIP_MIN_DIST);
							if (nearEnemyShip)
								damage = Math.Max(damage, Settings.NEAR_ENEMYSHIP_VIRTUAL_DAMAGE);

							var onMyShip = current.depth == 0 && turnState.myShips.Where(m => m.id != shipId).Any(m => FastShipPosition.CollidesShip(newPos, m.fposition) || FastShipPosition.CollidesShip(newMovedPos, m.fposition))
											|| gameState.forecaster.GetTurnForecast(current.depth).myShipsPositions
												.Where((_, i) => i != ship.index)
												.Any(m => FastShipPosition.CollidesShip(newPos, m) || FastShipPosition.CollidesShip(newMovedPos, m));

							var onEnemyShip = current.depth == 0 && turnState.enemyShips.Any(m => FastShipPosition.CollidesShip(newPos, m.fposition) || FastShipPosition.CollidesShip(newMovedPos, m.fposition))
											|| gameState.forecaster.GetTurnForecast(current.depth).enemyShipsPositions
												.Any(m => FastShipPosition.CollidesShip(newPos, m) || FastShipPosition.CollidesShip(newMovedPos, m));

							if (!onMyShip && !onEnemyShip)
							{
								var next = current.Next(newPos, moveCommand, ftarget, damage);
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
			public readonly int depth;
			public readonly int fposition;

			public ShipMovementState(int fposition, int depth)
			{
				this.depth = depth;
				this.fposition = fposition;
			}

			public bool Equals(ShipMovementState other)
			{
				if (ReferenceEquals(null, other))
					return false;
				if (ReferenceEquals(this, other))
					return true;
				return fposition == other.fposition && depth == other.depth;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
					return false;
				if (ReferenceEquals(this, obj))
					return true;
				if (obj.GetType() != this.GetType())
					return false;
				return Equals((ShipMovementState)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (fposition * 397) ^ depth;
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
			public static ShipPathChainItem Start(int fposition, int ftarget)
			{
				return new ShipPathChainItem(null, ShipMoveCommand.Wait, fposition, 0, ShipMoveCommand.Wait, ftarget, 0);
			}

			public readonly ShipMoveCommand command;
			public readonly int depth;
			public readonly int dist;
			public readonly int fposition;
			public readonly int pathDamage;
			public readonly ShipPathChainItem prev;
			public readonly ShipMoveCommand startCommand;
			public int damage = int.MaxValue;

			private ShipPathChainItem(
				ShipPathChainItem prev,
				ShipMoveCommand command,
				int fposition,
				int depth,
				ShipMoveCommand startCommand,
				int ftarget,
				int pathDamage)
			{
				this.prev = prev;
				this.command = command;
				this.fposition = fposition;
				this.depth = depth;
				this.startCommand = startCommand;
				dist = FastShipPosition.DistanceTo(fposition, ftarget);
				this.pathDamage = pathDamage;
				if (depth == Settings.NAVIGATION_PATH_DEPTH)
					SetDamage(pathDamage);
			}

			public ShipPathChainItem Next(int nextPosition, ShipMoveCommand moveCommand, int ftarget, int nextDamage)
			{
				return new ShipPathChainItem(
					this,
					moveCommand,
					nextPosition,
					depth + 1,
					prev == null ? moveCommand : startCommand,
					ftarget,
					pathDamage + nextDamage);
			}

			public override string ToString()
			{
				return $"{(prev == null ? "ROOT: " : "")}{nameof(command)}: {command}, {nameof(depth)}: {depth}, {nameof(startCommand)}: {startCommand}, {nameof(fposition)}: {FastShipPosition.ToShipPosition(fposition)}, {nameof(damage)}: {damage}, {nameof(pathDamage)}: {pathDamage}";
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
		}
	}
}