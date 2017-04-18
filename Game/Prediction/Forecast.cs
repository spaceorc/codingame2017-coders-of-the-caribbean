using System.Collections.Generic;
using Game.Entities;
using Game.State;

namespace Game.Prediction
{
	public class Forecaster
	{
		public readonly GameState gameState;
		public List<List<Ship>> enemyShipsMoved;
		public List<List<Ship>> myShipsMoved;

		public Forecaster(GameState gameState)
		{
			this.gameState = gameState;
		}

		public void BuildForecast(TurnState turnState)
		{
			enemyShipsMoved = new List<List<Ship>>();
			var prevShips = turnState.enemyShips;
			for (int i = 0; i < Settings.NAVIGATION_PATH_DEPTH; i++)
			{
				var ships = new List<Ship>();
				enemyShipsMoved.Add(ships);
				foreach (var ship in prevShips)
					ships.Add(ship.Apply(ShipMoveCommand.Wait)[1]);
				prevShips = ships;
			}
			myShipsMoved = new List<List<Ship>>();
			prevShips = turnState.myShips;
			for (int i = 0; i < Settings.NAVIGATION_PATH_DEPTH; i++)
			{
				var ships = new List<Ship>();
				myShipsMoved.Add(ships);
				foreach (var ship in prevShips)
					ships.Add(ship.Apply(ShipMoveCommand.Wait)[1]);
				prevShips = ships;
			}
		}
	}
}