using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.Strategy
{
	public class Strategy2vs2
	{
		public readonly Strateg strateg;

		public Strategy2vs2(Strateg strateg)
		{
			this.strateg = strateg;
		}

		public void MakeStrategicDecisions(TurnState turnState)
		{
			if (turnState.barrels.Any())
				CollectBarrels(turnState);
			else 
				RunOrSuicide(turnState);
		}

		private void CollectBarrels(TurnState turnState)
		{
			for (int i = 0; i < turnState.myShips.Count; i++)
			{
				if (turnState.enemyShips.Count > i)
					strateg.decisions[turnState.myShips[i].id] = Play1vs1(turnState, turnState.myShips[i], turnState.enemyShips[i]);
				else
					strateg.decisions[turnState.myShips[i].id] = CollectFreeBarrels(turnState, turnState.myShips[i]);
			}
		}

		public StrategicDecision CollectFreeBarrels(TurnState turnState, Ship ship)
		{
			StrategicDecision prevDecision;
			strateg.decisions.TryGetValue(ship.id, out prevDecision);
			var barrel = strateg.FindBestBarrelToCollect(turnState, ship);
			if (barrel != null)
				return strateg.Collect(barrel.barrel);
			return strateg.WalkFree(turnState, ship, prevDecision);
		}

		public StrategicDecision Play1vs1(TurnState turnState, Ship ship, Ship enemyShip)
		{
			StrategicDecision prevDecision;
			strateg.decisions.TryGetValue(ship.id, out prevDecision);
			var myBarrel = strateg.FindNearestBarrelToCollect(turnState, ship);
			var enemyBarrel1 = strateg.FindNearestBarrelToCollect(turnState, enemyShip);
			if (enemyBarrel1 == null)
			{
				if (myBarrel != null)
					return strateg.Collect(myBarrel.barrel);
				return strateg.WalkFree(turnState, ship, prevDecision);
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


		private void RunOrSuicide(TurnState turnState)
		{
			if (turnState.myShips.Max(s => s.rum) > turnState.enemyShips.Max(s => s.rum) || turnState.myShips.Min(s => s.rum) > 50)
			{
				foreach (var ship in turnState.myShips)
				{
					StrategicDecision prevDecision;
					strateg.decisions.TryGetValue(ship.id, out prevDecision);
					strateg.decisions[ship.id] = strateg.RunAway(turnState, ship, prevDecision);
				}
				return;
			}

			if (turnState.myShips.Count == 1)
			{
				var ship = turnState.myShips[0];
				StrategicDecision prevDecision;
				strateg.decisions.TryGetValue(ship.id, out prevDecision);
				strateg.decisions[ship.id] = strateg.RunAway(turnState, ship, prevDecision);
				return;
			}

			var ship1 = turnState.myShips[0];
			var ship2 = turnState.myShips[1];
			if (ship1.rum > ship2.rum)
			{
				var tmp = ship1;
				ship1 = ship2;
				ship2 = tmp;
			}
			if (FastCoord.Distance(ship1.fbow, ship2.fbow) <= 4)
			{
				StrategicDecision prevDecision;
				strateg.decisions.TryGetValue(ship1.id, out prevDecision);
				if (prevDecision?.role == StrategicRole.Fire || prevDecision?.role == StrategicRole.Explicit)
				{
					strateg.decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Explicit, explicitCommand = ShipMoveCommand.Slower };
					strateg.decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = prevDecision.fireToCoord };
				}
				else
				{
					var nextShip1Position = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship1.fposition, ShipMoveCommand.Wait));
					nextShip1Position = FastShipPosition.GetFinalPosition(FastShipPosition.Move(nextShip1Position, ShipMoveCommand.Slower));
					strateg.decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Fire, fireToCoord = FastShipPosition.Coord(nextShip1Position) };
					strateg.decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastShipPosition.Coord(nextShip1Position) };
				}
			}
			else
			{
				var x = (FastCoord.GetX(ship1.fcoord) + FastCoord.GetX(ship2.fcoord)) / 2;
				var y = (FastCoord.GetY(ship1.fcoord) + FastCoord.GetY(ship2.fcoord)) / 2;
				if (x < 5)
					x = 5;
				if (x > Constants.MAP_WIDTH - 6)
					x = Constants.MAP_WIDTH - 6;
				if (y < 5)
					y = 5;
				if (y > Constants.MAP_HEIGHT - 6)
					x = Constants.MAP_HEIGHT - 6;

				strateg.decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastCoord.Create(x, y) };
				strateg.decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastCoord.Create(x, y) };
			}
		}
	}
}