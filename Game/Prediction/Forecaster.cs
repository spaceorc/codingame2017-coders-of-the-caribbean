using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.Prediction
{
	public class Forecaster
	{
		public readonly GameState gameState;
		public List<int[]> enemyShipsNextPositions;
		public List<int[]> myShipsNextPositions;

		public Forecaster(GameState gameState)
		{
			this.gameState = gameState;
		}

		public void BuildForecast(TurnState turnState)
		{
			enemyShipsNextPositions = new List<int[]>();
			var prevPositions = turnState.enemyShips.Select(s => s.fposition).ToArray();
			for (int depth = 0; depth < Settings.NAVIGATION_PATH_DEPTH; depth++)
			{
				var nextPositions = new int[prevPositions.Length];
				enemyShipsNextPositions.Add(nextPositions);
				for (var i = 0; i < prevPositions.Length; i++)
				{
					var position = prevPositions[i];
					var movement = FastShipPosition.Move(position, ShipMoveCommand.Wait);
					nextPositions[i] = FastShipPosition.GetFinalPosition(movement);
				}
				prevPositions = nextPositions;
			}
			myShipsNextPositions = new List<int[]>();
			prevPositions = turnState.myShips.Select(s => s.fposition).ToArray();
			for (int depth = 0; depth < Settings.NAVIGATION_PATH_DEPTH; depth++)
			{
				var nextPositions = new int[prevPositions.Length];
				myShipsNextPositions.Add(nextPositions);
				for (var i = 0; i < prevPositions.Length; i++)
				{
					var position = prevPositions[i];
					var movement = FastShipPosition.Move(position, ShipMoveCommand.Wait);
					nextPositions[i] = FastShipPosition.GetFinalPosition(movement);
				}
				prevPositions = nextPositions;
			}
		}

		public void ApplyPath(TurnState turnState, Ship ship, List<ShipMoveCommand> path)
		{
			var index = turnState.myShips.FindIndex(s => s.id == ship.id);
			var movedShip = ship.fposition;
			for (var i = 0; i < path.Count; i++)
			{
				var moveCommand = path[i];
				movedShip = FastShipPosition.GetFinalPosition(FastShipPosition.Move(movedShip, moveCommand));
				myShipsNextPositions[i][index] = movedShip;
			}
			for (var i = path.Count; i < Settings.NAVIGATION_PATH_DEPTH; i++)
			{
				movedShip = FastShipPosition.GetFinalPosition(FastShipPosition.Move(movedShip, ShipMoveCommand.Wait));
				myShipsNextPositions[i][index] = movedShip;
			}
		}
	}
}