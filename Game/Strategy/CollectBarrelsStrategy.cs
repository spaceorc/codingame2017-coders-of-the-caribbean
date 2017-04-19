using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.Strategy
{
	public class CollectBarrelsStrategy : IStrategy
	{
		public readonly GameState gameState;
		public readonly int shipId;
		public int? currentTargetId;
		public Coord? currentTarget;

		public CollectBarrelsStrategy(int shipId, GameState gameState)
		{
			this.shipId = shipId;
			this.gameState = gameState;
		}

		public Decision Decide(TurnState turnState)
		{
			var ship = turnState.myShipsById[shipId];
			if (!turnState.barrels.Any())
				return Decision.Unknown();

			var used = new HashSet<int>();
			foreach (var myShip in turnState.myShips)
			{
				IStrategy otherStrategy;
				if (myShip.id != ship.id && gameState.admiral.strategies.TryGetValue(myShip.id, out otherStrategy))
				{
					var otherBarrelId = (otherStrategy as CollectBarrelsStrategy)?.currentTargetId;
					if (otherBarrelId.HasValue)
						used.Add(otherBarrelId.Value);
				}
			}

			if (currentTargetId == null || !turnState.barrelsById.ContainsKey(currentTargetId.Value))
			{
				var bestDist = int.MaxValue;
				Barrel bestBarrel = null;
				foreach (var barrel in turnState.barrels)
					if (!used.Contains(barrel.id))
					{
						var dist = ship.DistanceTo(barrel.coord);
						if (dist < bestDist)
						{
							bestBarrel = barrel;
							bestDist = dist;
						}
					}
				currentTargetId = bestBarrel?.id;
				currentTarget = bestBarrel?.coord;
				Console.Error.WriteLine($"New target for {ship.id}: {bestBarrel}");
			}

			return currentTargetId == null ? Decision.Unknown() : Decision.Goto(currentTarget.Value);
		}

		public string Dump(string gameStateRef)
		{
			return $"new {nameof(CollectBarrelsStrategy)}({shipId}, {gameStateRef}) " +
			       $"{{" +
			       $" {nameof(currentTargetId)} = {(currentTargetId.HasValue ? currentTargetId.ToString() : "null")}," +
			       $" {nameof(currentTarget)} = {(currentTarget.HasValue ? $"new {nameof(Coord)}({currentTarget.Value.x}, {currentTarget.Value.y})" : "null")}" +
			       $" }}";
		}
	}
}