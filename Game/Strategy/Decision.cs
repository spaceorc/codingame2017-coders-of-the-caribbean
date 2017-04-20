using Game.Geometry;

namespace Game.Strategy
{
	public class Decision
	{
		public readonly int fcoord;
		public readonly DecisionType type;

		private Decision(DecisionType type, int fcoord)
		{
			this.type = type;
			this.fcoord = fcoord;
		}

		public static Decision Unknown()
		{
			return new Decision(DecisionType.Unknown, -1);
		}

		public static Decision Goto(int fastCoord)
		{
			return new Decision(DecisionType.Goto, fastCoord);
		}

		public override string ToString()
		{
			return type == DecisionType.Unknown ? $"{type}" : $"{type}[{FastCoord.ToCoord(fcoord)}]";
		}
	}
}