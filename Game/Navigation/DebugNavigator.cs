using System;
using System.Collections.Generic;
using System.Linq;
using Game.Geometry;
using Game.State;

namespace Game.Navigation
{
	public class DebugNavigator : ITeamMember
	{
		public readonly GameState gameState;
		public readonly int shipId;

		public DebugNavigator(int shipId, GameState gameState)
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

		public List<ShipMoveCommand> FindPath(TurnState turnState, int ftargetPos)
		{
			var ship = turnState.myShipsById[shipId];

			if (ship.fposition == ftargetPos)
				return new List<ShipMoveCommand>();

			var queue = new Queue<ShipPathChainItem>();
			queue.Enqueue(ShipPathChainItem.Start(ship.fposition, ftargetPos));

			var used = new Dictionary<ShipMovementState, ShipPathChainItem>();

			while (queue.Any())
			{
				var current = queue.Dequeue();
				if (current.depth != Settings.NAVIGATION_PATH_DEPTH)
				{
					var turnForecast = gameState.forecaster.GetTurnForecast(current.depth);
					foreach (var moveCommand in Enum.GetValues(typeof(ShipMoveCommand)).Cast<ShipMoveCommand>())
					{
						var newShipMovement = FastShipPosition.Move(current.fposition, moveCommand);
						var newMovedPos = FastShipPosition.GetMovedPosition(newShipMovement);
						var newPos = FastShipPosition.GetFinalPosition(newShipMovement);
						var newMovementState = new ShipMovementState(newPos, current.depth + 1);
						if (!used.ContainsKey(newMovementState))
						{
							var onMyShip = current.depth == 0 && turnState.myShips.Where(m => m.id != shipId).Any(m => FastShipPosition.CollidesShip(newPos, m.fposition) || FastShipPosition.CollidesShip(newMovedPos, m.fposition))
											|| turnForecast.myShipsPositions
												.Where((_, i) => i != ship.index)
												.Any(m => FastShipPosition.CollidesShip(newPos, m) || FastShipPosition.CollidesShip(newMovedPos, m));



							var onEnemyShip = current.depth == 0 && turnState.enemyShips.Any(m => FastShipPosition.CollidesShip(newPos, m.fposition) || FastShipPosition.CollidesShip(newMovedPos, m.fposition))
											|| turnForecast.enemyShipsFinalPositions
												.Any(m => FastShipPosition.CollidesShip(newPos, m) || FastShipPosition.CollidesShip(newMovedPos, m));

							if (!onMyShip && !onEnemyShip)
							{
								var next = current.Next(newPos, moveCommand, ftargetPos);
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
			}

			ShipPathChainItem bestChainItem = null;
			foreach (var chainItem in used.Values.Where(v => v != null))
			{
				if (chainItem.prev != null)
				{
					if (bestChainItem == null || chainItem.dist < bestChainItem.dist || chainItem.dist == bestChainItem.dist
						&& (chainItem.speedScore < bestChainItem.speedScore || chainItem.speedScore == bestChainItem.speedScore
						&& (chainItem.orientationScore < bestChainItem.orientationScore || chainItem.orientationScore == bestChainItem.orientationScore
							&& chainItem.depth < bestChainItem.depth)))
					{
						bestChainItem = chainItem;
					}
				}
			}

			if (bestChainItem == null)
				return new List<ShipMoveCommand>();

			var chainDump = new List<ShipPathChainItem>();
			var chain = new List<ShipMoveCommand>();
			while (bestChainItem.prev != null)
			{
				chain.Add(bestChainItem.command);
				chainDump.Add(bestChainItem);
				bestChainItem = bestChainItem.prev;
			}
			chainDump.Reverse();
			if (Settings.DUMP_BEST_PATH)
			{
				Console.Error.WriteLine($"Best path for ship {shipId}");
				foreach (var item in chainDump)
				{
					Console.Error.WriteLine($"{item.command} - {FastShipPosition.ToShipPosition(item.fposition)}");
				}
			}
			chain.Reverse();
			return chain;
		}

		public string Dump(string gameStateRef)
		{
			return $"new {nameof(DebugNavigator)}({shipId}, {gameStateRef})";
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

			public override string ToString()
			{
				return $"{nameof(depth)}: {depth}, {nameof(fposition)}: {FastShipPosition.ToShipPosition(fposition)}";
			}
		}

		private class ShipPathChainItem
		{
			public static ShipPathChainItem Start(int fposition, int ftargetPos)
			{
				return new ShipPathChainItem(null, ShipMoveCommand.Wait, fposition, 0, ShipMoveCommand.Wait, ftargetPos);
			}

			public readonly ShipMoveCommand command;
			public readonly int depth;
			public readonly int dist;
			public readonly int speedScore;
			public readonly int orientationScore;
			public readonly int fposition;
			public readonly ShipPathChainItem prev;
			public readonly ShipMoveCommand startCommand;

			private ShipPathChainItem(
				ShipPathChainItem prev,
				ShipMoveCommand command,
				int fposition,
				int depth,
				ShipMoveCommand startCommand,
				int ftargetPos)
			{
				this.prev = prev;
				this.command = command;
				this.fposition = fposition;
				this.depth = depth;
				this.startCommand = startCommand;
				dist = FastCoord.Distance(FastShipPosition.Coord(fposition), FastShipPosition.Coord(ftargetPos));
				speedScore = Math.Abs(FastShipPosition.Speed(fposition) - FastShipPosition.Speed(ftargetPos));
				orientationScore = Math.Abs(FastShipPosition.Orientation(fposition) - FastShipPosition.Orientation(ftargetPos));
			}

			public ShipPathChainItem Next(int nextPosition, ShipMoveCommand moveCommand, int ftargetPos)
			{
				return new ShipPathChainItem(
					this,
					moveCommand,
					nextPosition,
					depth + 1,
					prev == null ? moveCommand : startCommand,
					ftargetPos);
			}

			public override string ToString()
			{
				return $"{(prev == null ? "ROOT: " : "")}{nameof(command)}: {command}, {nameof(depth)}: {depth}, {nameof(startCommand)}: {startCommand}, {nameof(fposition)}: {FastShipPosition.ToShipPosition(fposition)}, {nameof(dist)}: {dist}";
			}
		}
	}
}