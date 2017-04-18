using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.State;

namespace Game.Cannons
{
	public class CannonMaster
	{
		private readonly GameState gameState;
		public readonly int shipId;
		public bool fired;

		public CannonMaster(Ship ship, GameState gameState)
		{
			this.gameState = gameState;
			shipId = ship.id;
		}

		public bool Fire(TurnState turnState)
		{
			if (fired)
			{
				fired = false;
				return false;
			}
			var ship = turnState.myShipsById[shipId];
			var fireTarget = SelectFireTarget(turnState, ship);
			if (fireTarget == null)
				return false;
			fired = true;
			ship.Fire(fireTarget.target);
			return true;
		}

		public void DontFire(TurnState turnState)
		{
			fired = false;
		}

		private static FireTarget SelectFireTarget(TurnState turnState, Ship ship)
		{
			FireTarget bestFireTarget = null;
			foreach (var enemyShip in turnState.enemyShips)
			{
				var fireTargets = GetFireTargets(turnState, ship, enemyShip);
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

		private static IEnumerable<FireTarget> GetFireTargets(TurnState turnState, Ship ship, Ship enemyShip)
		{
			var currentMyShips = turnState.myShips;
			for (var turns = 0; turns < 5; turns++)
			{
				enemyShip = enemyShip.Apply(ShipMoveCommand.Wait)[1];
				var coords = new[] { enemyShip.coord, enemyShip.bow, enemyShip.stern };
				var targetTypes = new[] { FireTargetType.ShipCenter, FireTargetType.ShipBow, FireTargetType.ShipStern };
				for (var i = 0; i < coords.Length; i++)
				{
					var target = coords[i];
					if (target.IsInsideMap())
					{
						if (currentMyShips.Any(m => m.DistanceTo(target) == 0))
							continue;
						var distanceTo = ship.bow.DistanceTo(target);
						if (distanceTo <= 10)
						{
							var travelTime = (int)(1 + Math.Round(distanceTo / 3.0));
							if (travelTime == turns)
								yield return new FireTarget(target, turns, i == 0 ? Constants.HIGH_DAMAGE : Constants.LOW_DAMAGE, targetTypes[i]);
						}
					}
				}
				currentMyShips = currentMyShips.Select(c => c.Apply(ShipMoveCommand.Wait)[1]).ToList();
			}
		}
	}
}