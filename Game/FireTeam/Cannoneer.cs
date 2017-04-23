using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.FireTeam
{
	public class Cannoneer : ITeamMember
	{
		public readonly GameState gameState;
		public readonly int shipId;
		public bool cooldown;
		public bool canFire;
		public FireTarget fireTarget;

		public Cannoneer(int shipId, GameState gameState)
		{
			this.gameState = gameState;
			this.shipId = shipId;
		}

		public void StartTurn(TurnState turnState)
		{
			canFire = !cooldown;
			cooldown = false;
			fireTarget = null;
		}

		public void EndTurn(TurnState turnState)
		{
		}

		public void PrepareToFire(TurnState turnState, int? preferredFireTargetCoord)
		{
			if (!canFire)
				return;
			var ship = turnState.FindMyShip(shipId);
			fireTarget = TryGetPreferredFireTarget(ship, preferredFireTargetCoord) ?? SelectFireTarget(turnState, ship);
		}

		private static FireTarget TryGetPreferredFireTarget(Ship ship, int? preferredFireTargetCoord)
		{
			if (!preferredFireTargetCoord.HasValue)
				return null;

			var distanceTo = FastCoord.Distance(ship.fbow, preferredFireTargetCoord.Value);
			if (distanceTo > 10)
				return null;

			var travelTime = (int)(1 + Math.Round(distanceTo / 3.0));
			return new FireTarget(preferredFireTargetCoord.Value, travelTime, 0, FireTargetType.Explicit);
		}

		public bool Fire(TurnState turnState)
		{
			if (!canFire || fireTarget == null)
				return false;
			var ship = turnState.FindMyShip(shipId);
			ship.Fire(fireTarget.ftarget);
			cooldown = true;
			return true;
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
						fireTarget.turns < bestFireTarget.turns ||
						fireTarget.turns == bestFireTarget.turns && fireTarget.TargetType < bestFireTarget.TargetType)
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
			for (var turn = 0; turn < Settings.CANNONS_TRAVEL_TIME_LIMIT + 1; turn++)
			{
				var forecast = gameState.forecaster.GetTurnForecast(turn);
				var enemyPosition = forecast.enemyShipsFinalPositions[enemyIndex];

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

		public string Dump(string gameStateRef)
		{
			return $"new {nameof(Cannoneer)}({shipId}, {gameStateRef}) {{ {nameof(cooldown)} = {cooldown.ToString().ToLower()} }}";
		}
	}
}