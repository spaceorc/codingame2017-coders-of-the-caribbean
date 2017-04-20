using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.Cannons
{
	public class Cannoneer
	{
		public readonly GameState gameState;
		public readonly int shipId;
		public bool fire;

		public Cannoneer(int shipId, GameState gameState)
		{
			this.gameState = gameState;
			this.shipId = shipId;
		}

		public void PrepareToFire(TurnState turnState)
		{
			fire = !fire;
		}

		public bool Fire(TurnState turnState)
		{
			if (!fire)
				return false;
			var ship = turnState.myShipsById[shipId];
			var fireTarget = SelectFireTarget(turnState, ship);
			if (fireTarget == null)
				return false;
			ship.Fire(fireTarget.ftarget);
			return true;
		}


		public string Dump(string gameStateRef)
		{
			return $"new {nameof(Cannoneer)}({shipId}, {gameStateRef}) {{ {nameof(fire)} = {fire.ToString().ToLower()} }}";
		}

		private FireTarget SelectFireTarget(TurnState turnState, Ship ship)
		{
			FireTarget bestFireTarget = null;
			foreach (var enemyShip in turnState.enemyShips)
			{
				var fireTargets = GetFireTargets(ship, enemyShip);
				foreach (var fireTarget in fireTargets)
				{
					if (bestFireTarget == null ||
					    fireTarget.TargetType < bestFireTarget.TargetType ||
					    fireTarget.TargetType == bestFireTarget.TargetType && fireTarget.turns < bestFireTarget.turns)
						bestFireTarget = fireTarget;
				}
			}
			return bestFireTarget;
		}

		private List<FireTarget> GetFireTargets(Ship ship, Ship enemyShip)
		{
			var result = new List<FireTarget>();
			var enemyIndex = enemyShip.index;
			var cannonCoord = FastShipPosition.Bow(ship.fposition);
			for (var turn = 0; turn < 5; turn++)
			{
				var forecast = gameState.forecaster.turnForecasts[0];
				var enemyPosition = forecast.enemyShipsPositions[enemyIndex];

				var coords = new[] { FastShipPosition.Coord(enemyPosition), FastShipPosition.Bow(enemyPosition), FastShipPosition.Stern(enemyPosition) };
				var targetTypes = new[] { FireTargetType.ShipCenter, FireTargetType.ShipBow, FireTargetType.ShipStern };
				for (var i = 0; i < coords.Length; i++)
				{
					var target = coords[i];
					if (FastCoord.IsInsideMap(target))
					{
						if (forecast.myShipsPositions.Any(m => FastShipPosition.Collides(m, target)))
							continue;
						var distanceTo = FastCoord.Distance(cannonCoord, target);
						if (distanceTo <= 10)
						{
							var travelTime = (int)(1 + Math.Round(distanceTo / 3.0));
							if (travelTime == turn)
								result.Add(new FireTarget(target, turn, i == 0 ? Constants.HIGH_DAMAGE : Constants.LOW_DAMAGE, targetTypes[i]));
						}
					}
				}
			}
			return result;
		}
	}
}