using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.Prediction
{
	public class Forecaster : ITeamMember
	{
		public readonly GameState gameState;
		private TurnForecast[] turnForecasts;

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
		}

		public void EndTurn(TurnState turnState)
		{
		}

		public void BuildForecast(TurnState turnState)
		{
			turnForecasts = new TurnForecast[Settings.NAVIGATION_PATH_DEPTH];
			for (var depth = 0; depth < Settings.NAVIGATION_PATH_DEPTH; depth++)
				turnForecasts[depth] = new TurnForecast();

			var prevPositions = new int[turnState.enemyShips.Count];
			for (var i = 0; i < turnState.enemyShips.Count; i++)
				prevPositions[i] = turnState.enemyShips[i].fposition;
			for (var depth = 0; depth < Settings.NAVIGATION_PATH_DEPTH; depth++)
			{
				var nextPositions = new int[prevPositions.Length];
				for (var i = 0; i < prevPositions.Length; i++)
				{
					var position = prevPositions[i];
					var movement = FastShipPosition.Move(position, ShipMoveCommand.Wait);
					nextPositions[i] = FastShipPosition.GetFinalPosition(movement);
				}
				turnForecasts[depth].enemyShipsPositions = nextPositions;
				prevPositions = nextPositions;
			}
			prevPositions = new int[turnState.myShips.Count];
			for (var i = 0; i < turnState.myShips.Count; i++)
				prevPositions[i] = turnState.myShips[i].fposition;
			for (int depth = 0; depth < Settings.NAVIGATION_PATH_DEPTH; depth++)
			{
				var nextPositions = new int[prevPositions.Length];
				for (var i = 0; i < prevPositions.Length; i++)
				{
					var position = prevPositions[i];
					var movement = FastShipPosition.Move(position, ShipMoveCommand.Wait);
					nextPositions[i] = FastShipPosition.GetFinalPosition(movement);
				}
				turnForecasts[depth].myShipsPositions = nextPositions;
				prevPositions = nextPositions;
			}
		}

		public void ApplyPath(Ship ship, List<ShipMoveCommand> path)
		{
			var index = ship.index;
			var movedShip = ship.fposition;
			for (var i = 0; i < path.Count; i++)
			{
				var moveCommand = path[i];
				movedShip = FastShipPosition.GetFinalPosition(FastShipPosition.Move(movedShip, moveCommand));
				GetTurnForecast(i).myShipsPositions[index] = movedShip;
			}
			for (var i = path.Count; i < Settings.NAVIGATION_PATH_DEPTH; i++)
			{
				movedShip = FastShipPosition.GetFinalPosition(FastShipPosition.Move(movedShip, ShipMoveCommand.Wait));
				GetTurnForecast(i).myShipsPositions[index] = movedShip;
			}
		}

		public class TurnForecast
		{
			public int[] enemyShipsPositions;
			public int[] myShipsPositions;
		}
	}
}