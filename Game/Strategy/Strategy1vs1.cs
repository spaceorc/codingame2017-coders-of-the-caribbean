using System.Collections.Generic;
using Game.Entities;
using Game.State;

namespace Game.Strategy
{
	public class Strategy1vs1
	{
		public readonly Strateg strateg;

		public Strategy1vs1(Strateg strateg)
		{
			this.strateg = strateg;
		}

		public void MakeStrategicDecisions(TurnState turnState)
		{
			var ship = turnState.myShips[0];
			var enemyShip = turnState.enemyShips[0];
			StrategicDecision decision;
			strateg.decisions.TryGetValue(ship.id, out decision);
			var newDecision = MakeStrategicDecision(turnState, decision, ship, enemyShip);
			strateg.decisions[ship.id] = newDecision;
		}

		public StrategicDecision MakeStrategicDecision(TurnState turnState, StrategicDecision prevDecision, Ship ship, Ship enemyShip)
		{
			var myBarrel = strateg.FindNearestBarrelToCollect(turnState, ship);
			var enemyBarrel1 = strateg.FindNearestBarrelToCollect(turnState, enemyShip);
			if (enemyBarrel1 == null)
			{
				if (myBarrel != null)
					return strateg.Collect(myBarrel.barrel);
				return strateg.RunAway(turnState, ship, prevDecision);
			}

			var enemyBarrel2 = strateg.FindNearestBarrelToCollect(turnState, enemyBarrel1.barrel.fcoord, new HashSet<int> { enemyBarrel1.barrel.id });
			if (enemyBarrel2 == null)
			{
				if (myBarrel != null)
				{
					if (myBarrel.barrel != enemyBarrel1.barrel)
						return strateg.Collect(myBarrel.barrel).FireTo(strateg.SelectEnemyBarrelToFire(turnState, ship, enemyBarrel1)?.fcoord);
					if (myBarrel.dist < enemyBarrel1.dist)
						return strateg.Collect(myBarrel.barrel);
				}
				return strateg.WalkFree(turnState, ship, prevDecision).FireTo(strateg.SelectEnemyBarrelToFire(turnState, ship, enemyBarrel1)?.fcoord);
			}

			var enemyBarrel3 = strateg.FindNearestBarrelToCollect(turnState, enemyBarrel2.barrel.fcoord, new HashSet<int> { enemyBarrel1.barrel.id, enemyBarrel2.barrel.id });
			if (enemyBarrel3 == null)
				return strateg.Collect(enemyBarrel2.barrel).FireTo(strateg.SelectEnemyBarrelToFire(turnState, ship, enemyBarrel1)?.fcoord);

			return strateg.Collect(enemyBarrel3.barrel).FireTo(strateg.SelectEnemyBarrelToFire(turnState, ship, enemyBarrel1, enemyBarrel2)?.fcoord);
		}
	}
}