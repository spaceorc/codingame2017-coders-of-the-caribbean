using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Entities;
using Game.Geometry;
using Game.Navigation;
using Game.State;

namespace Game.Prediction
{
	public class Forecaster : ITeamMember
	{
		public readonly GameState gameState;
		private TurnForecast[] turnForecasts;
		private readonly HashSet<int> knownCannonballs = new HashSet<int>();
		private readonly HashSet<int> enemiesCanFire = new HashSet<int>();
		private readonly Dictionary<int, int> enemiesMineCooldown = new Dictionary<int, int>();
		private readonly Dictionary<int, int> lastEnemiesPositions = new Dictionary<int, int>();
		private readonly HashSet<int> knownMines = new HashSet<int>();

		public Forecaster(GameState gameState)
		{
			this.gameState = gameState;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TurnForecast GetTurnForecast(int turn)
		{
			return turnForecasts[turn];
		}

		public void StartTurn(TurnState turnState)
		{
			PredictEnemiesCanFire(turnState);
			PredictEnemiesCanMine(turnState);
		}

		private void PredictEnemiesCanFire(TurnState turnState)
		{
			enemiesCanFire.Clear();
			foreach (var enemyShip in turnState.enemyShips)
				enemiesCanFire.Add(enemyShip.id);
			foreach (var cannonball in turnState.cannonballs)
			{
				if (knownCannonballs.Add(cannonball.id))
					enemiesCanFire.Remove(cannonball.firedBy);
			}
		}

		private void PredictEnemiesCanMine(TurnState turnState)
		{
			foreach (var kvp in enemiesMineCooldown.ToList())
			{
				var cooldown = kvp.Value - 1;
				if (cooldown == 0)
					enemiesMineCooldown.Remove(kvp.Key);
				else
					enemiesMineCooldown[kvp.Key] = cooldown;
			}
			foreach (var mine in turnState.mines)
			{
				if (knownMines.Add(mine.id))
				{
					foreach (var kvp in lastEnemiesPositions)
					{
						if (FastShipPosition.Mine(kvp.Value) == mine.fcoord)
							enemiesMineCooldown[kvp.Key] = Constants.MINING_COOLDOWN;
					}
				}
			}
		}

		public void EndTurn(TurnState turnState)
		{
			lastEnemiesPositions.Clear();
			foreach (var enemyShip in turnState.enemyShips)
				lastEnemiesPositions[enemyShip.id] = enemyShip.fposition;
		}

		public void BuildForecast(TurnState turnState)
		{
			turnForecasts = new TurnForecast[Settings.NAVIGATION_PATH_DEPTH];
			for (var depth = 0; depth < Settings.NAVIGATION_PATH_DEPTH; depth++)
				turnForecasts[depth] = new TurnForecast();

			BuildEnemyShipsForecast(turnState);
			BuildMyShipsForecast(turnState);
			BuildCannonballsForecast(turnState);
			BuildMinesForecast(turnState);
		}

		private void BuildCannonballsForecast(TurnState turnState)
		{
			for (var i = 0; i < turnState.cannonballs.Count; i++)
			{
				if (turnState.cannonballs[i].turns - 1 >= 0 && turnState.cannonballs[i].turns - 1 < Settings.NAVIGATION_PATH_DEPTH)
					GetTurnForecast(turnState.cannonballs[i].turns - 1).cannonballCoordsMap[turnState.cannonballs[i].fcoord] = true;
			}

			if (Settings.CANNONBALLS_FORECAST_TRAVEL_TIME_LIMIT > 0)
			{
				foreach (var enemyShip in turnState.enemyShips)
				{
					if (!enemiesCanFire.Contains(enemyShip.id))
						continue;
					var targets = Enumerable.Range(0, 6).Select(o => enemyShip.fbow).ToArray();
					for (var dist = 1; dist <= 10; dist++)
					{
						var travelTime = (int)(1 + Math.Round(dist / 3.0));
						if (travelTime > Settings.CANNONBALLS_FORECAST_TRAVEL_TIME_LIMIT)
							break;
						if (travelTime >= Settings.NAVIGATION_PATH_DEPTH)
							break;
						for (var orientation = 0; orientation < 6; orientation++)
							targets[orientation] = FastCoord.Neighbor(targets[orientation], orientation);
						for (var orientation = 0; orientation < 6; orientation++)
							if (FastCoord.IsInsideMap(targets[orientation]))
								GetTurnForecast(travelTime).cannonballCoordsMap[targets[orientation]] = true;
					}
				}

			}
		}

		private void BuildMinesForecast(TurnState turnState)
		{
			for (var i = 0; i < turnState.mines.Count; i++)
			{
				var mineCoord = turnState.mines[i].fcoord;
				BuildMineForecast(mineCoord);
			}
			foreach (var enemyShip in turnState.enemyShips)
			{
				if (!enemiesMineCooldown.ContainsKey(enemyShip.id))
				{
					var mineCoord = FastShipPosition.Mine(enemyShip.fposition);
					BuildMineForecast(mineCoord);
				}
			}
		}

		private void BuildMineForecast(int mineCoord)
		{
			if (!FastCoord.IsInsideMap(mineCoord))
				return;
			for (var depth = 0; depth < Settings.NAVIGATION_PATH_DEPTH; depth++)
			{
				var turnForecast = GetTurnForecast(depth);
				turnForecast.mineDamageCoordMap[mineCoord] += Constants.MINE_DAMAGE;
				if (turnForecast.cannonballCoordsMap[mineCoord])
				{
					for (int orientation = 0; orientation < 6; orientation++)
					{
						var neighbor = FastCoord.Neighbor(mineCoord, orientation);
						turnForecast.nearMineDamageCoordMap[neighbor] += Constants.NEAR_MINE_DAMAGE;
					}
				}
			}
		}

		private void BuildMyShipsForecast(TurnState turnState)
		{
			var prevPositions = new int[turnState.myShips.Count];
			for (var i = 0; i < turnState.myShips.Count; i++)
				prevPositions[i] = turnState.myShips[i].fposition;
			for (int depth = 0; depth < Settings.NAVIGATION_PATH_DEPTH; depth++)
			{
				turnForecasts[depth].myShipsSourcePositions = prevPositions;
				var commands = new ShipMoveCommand[prevPositions.Length];
				turnForecasts[depth].myShipsMoveCommands = commands;
				var nextPositions = new int[prevPositions.Length];
				for (var i = 0; i < prevPositions.Length; i++)
				{
					var position = prevPositions[i];
					var movement = FastShipPosition.Move(position, ShipMoveCommand.Wait);
					nextPositions[i] = FastShipPosition.GetFinalPosition(movement);
				}
				prevPositions = turnForecasts[depth].myShipsPositions = nextPositions;
			}
		}

		private void BuildEnemyShipsForecast(TurnState turnState)
		{
			var prevPositions = new int[turnState.enemyShips.Count];
			for (var i = 0; i < turnState.enemyShips.Count; i++)
				prevPositions[i] = turnState.enemyShips[i].fposition;

			for (var depth = 0; depth < Settings.NAVIGATION_PATH_DEPTH; depth++)
			{
				var nextMovedPositions = new int[prevPositions.Length];
				var nextFinalPositions = new int[prevPositions.Length];
				for (var i = 0; i < prevPositions.Length; i++)
				{
					var position = prevPositions[i];
					var movement = FastShipPosition.Move(position, ShipMoveCommand.Wait);
					nextMovedPositions[i] = FastShipPosition.GetMovedPosition(movement);
					nextFinalPositions[i] = FastShipPosition.GetFinalPosition(movement);
				}
				turnForecasts[depth].enemyShipsMovedPositions = nextMovedPositions;
				prevPositions = turnForecasts[depth].enemyShipsFinalPositions = nextFinalPositions;
			}
		}

		public void ApplyPath(Ship ship, List<PathItem> path)
		{
			var index = ship.index;
			var movedShip = ship.fposition;
			for (var i = 0; i < path.Count; i++)
			{
				var pathItem = path[i];
				GetTurnForecast(i).myShipsSourcePositions[index] = pathItem.sourcePosition;
				GetTurnForecast(i).myShipsPositions[index] = pathItem.targetPosition;
				GetTurnForecast(i).myShipsMoveCommands[index] = pathItem.command;
				movedShip = pathItem.targetPosition;
			}
			for (var i = path.Count; i < Settings.NAVIGATION_PATH_DEPTH; i++)
			{
				movedShip = FastShipPosition.GetFinalPosition(FastShipPosition.Move(movedShip, ShipMoveCommand.Wait));
				GetTurnForecast(i).myShipsPositions[index] = movedShip;
			}
		}

		public class TurnForecast
		{
			public int[] enemyShipsMovedPositions;
			public int[] enemyShipsFinalPositions;
			public int[] myShipsPositions;
			public int[] myShipsSourcePositions;
			public ShipMoveCommand[] myShipsMoveCommands;
			public bool[] cannonballCoordsMap = new bool[FastCoord.count];
			public int[] mineDamageCoordMap = new int[FastCoord.count];
			public int[] nearMineDamageCoordMap = new int[FastCoord.count];

			public override string ToString()
			{
				return $"enemiesMov: {string.Join("; ", enemyShipsMovedPositions.Select(FastShipPosition.ToShipPosition))} | " +
						$"enemiesFin: {string.Join("; ", enemyShipsFinalPositions.Select(FastShipPosition.ToShipPosition))} | " +
						$"myships: {string.Join("; ", myShipsPositions.Select(FastShipPosition.ToShipPosition))} | " +
						$"cannonballs: {string.Join("; ", cannonballCoordsMap.Select((b, i) => new { b, i }).Where(x => x.b).Select(x => FastCoord.ToCoord(x.i)))} | " +
						$"mines: {string.Join("; ", mineDamageCoordMap.Select((d, i) => new { d, i }).Where(x => x.d != 0).Select(x => FastCoord.ToCoord(x.i)))} | " +
						$"nearMines: {string.Join("; ", nearMineDamageCoordMap.Select((d, i) => new { d, i }).Where(x => x.d != 0).Select(x => FastCoord.ToCoord(x.i)))}";
			}
		}
	}
}