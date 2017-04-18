using Game.Geometry;

namespace Game.Cannons
{
	public class FireTarget
	{
		public readonly int rum;
		public readonly Coord target;
		public readonly int turns;
		public readonly FireTargetType TargetType;

		public FireTarget(Coord target, int turns, int rum, FireTargetType targetType)
		{
			this.target = target;
			this.turns = turns;
			this.rum = rum;
			this.TargetType = targetType;
		}

		public override string ToString()
		{
			return
				$"{nameof(target)}: {target}, {nameof(rum)}: {rum}, {nameof(turns)}: {turns}, {nameof(TargetType)}: {TargetType}";
		}
	}
}