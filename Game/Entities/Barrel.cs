namespace Game.Entities
{
	public class Barrel : Entity
	{
		public readonly int rum;

		public Barrel(int id, int x, int y, int rum) : base(id, EntityType.Barrel, x, y)
		{
			this.rum = rum;
		}

		public string Dump()
		{
			return $"new Barrel({id}, {coord.x}, {coord.y}, {rum})";
		}
	}
}