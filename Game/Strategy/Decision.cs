using Game.Geometry;

namespace Game.Strategy
{
	public class Decision
	{
		public readonly Coord coord;
		public readonly DecisionType type;

		private Decision(DecisionType type, Coord coord)
		{
			this.type = type;
			this.coord = coord;
		}

		public static Decision Unknown()
		{
			return new Decision(DecisionType.Unknown, default(Coord));
		}

		public static Decision Goto(Coord coord)
		{
			return new Decision(DecisionType.Goto, coord);
		}
	}
}