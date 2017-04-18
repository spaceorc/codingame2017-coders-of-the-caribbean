using Game.State;

namespace Game.Mining
{
	public class Miner
	{
		private readonly GameState gameState;
		public readonly int shipId;
		public int cooldown;

		public Miner(int shipId, GameState gameState)
		{
			this.gameState = gameState;
			this.shipId = shipId;
		}

		public void PrepareToMine(TurnState turnState)
		{
			if (cooldown > 0)
				cooldown--;
		}

		public bool Mine(TurnState turnState)
		{
			if (!Settings.USE_MINING)
				return false;
			// todo dont mine own ships!
			if (cooldown > 0)
				return false;
			var ship = turnState.myShipsById[shipId];
			ship.Mine();
			cooldown = Constants.MINING_COOLDOWN + 1;
			return true;
		}
		
		public string Dump(string gameStateRef)
		{
			return $"new {nameof(Miner)}({shipId}, gameStateRef) {{ {nameof(cooldown)} = {cooldown} }}";
		}
	}
}