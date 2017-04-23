using Game.Entities;

namespace Game.Strategy
{
	public class CollectableBarrel
	{
		public Barrel barrel;
		public int dist;

		public override string ToString()
		{
			return $"{barrel}, {nameof(dist)}: {dist}";
		}
	}
}