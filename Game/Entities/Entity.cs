using Game.Geometry;

namespace Game.Entities
{
	public abstract class Entity
	{
		public readonly Coord coord;
		public readonly int id;
		public readonly EntityType type;

		protected Entity(int id, EntityType type, int x, int y)
		{
			this.id = id;
			this.type = type;
			coord = new Coord(x, y);
		}

		public override string ToString()
		{
			return $"{type}[{id}] at ({coord})";
		}
	}
}