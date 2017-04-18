using System;
using System.Collections.Generic;
using Game.Cannons;
using Game.Entities;

namespace Game.State
{
	public class GameState
	{
		public readonly Dictionary<int, CannonMaster> cannonMasters = new Dictionary<int, CannonMaster>();

		public CannonMaster GetCannonMaster(Ship ship)
		{
			CannonMaster cannonMaster;
			if (!cannonMasters.TryGetValue(ship.id, out cannonMaster))
				cannonMasters.Add(ship.id, cannonMaster = new CannonMaster(ship.id, this));
			return cannonMaster;
		}

		public void Dump()
		{
			Console.Error.WriteLine("var gameState = new GameState();");
			foreach (var cannonMaster in cannonMasters)
				Console.Error.WriteLine($"gameState.cannonMasters[{cannonMaster.Key}] = {cannonMaster.Value.Dump("gameState")}");
			// todo strategies
			// todo miners
		}
	}
}