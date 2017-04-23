using System.Linq;
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
			{
				strateg.MakeStandardStrategicDecisions(turnState);
				return;
			}
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