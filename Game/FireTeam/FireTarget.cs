using Game.Geometry;

namespace Game.FireTeam
{
	public class FireTarget
	{
		public readonly int rum;
		public readonly int ftarget;
		public readonly int turns;
		public readonly FireTargetType TargetType;

		public FireTarget(int ftarget, int turns, int rum, FireTargetType targetType)
		{
			this.ftarget = ftarget;
			this.turns = turns;
			this.rum = rum;
			this.TargetType = targetType;
		}

		public override string ToString()
		{
			return $"{nameof(ftarget)}: {FastCoord.ToCoord(ftarget)}, {nameof(rum)}: {rum}, {nameof(turns)}: {turns}, {nameof(TargetType)}: {TargetType}";
		}
	}
}