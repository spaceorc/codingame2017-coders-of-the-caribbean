using System;
using Game.Geometry;

namespace Game.Strategy
{
	public class StrategicDecision : IEquatable<StrategicDecision>
	{
		public StrategicRole role;
		public int? targetCoord;
		public int? targetBarrelId;
		public int? fireToCoord;

		public StrategicDecision FireTo(int? coord)
		{
			var result = (StrategicDecision)MemberwiseClone();
			result.fireToCoord = coord;
			return result;
		}

		public bool Equals(StrategicDecision other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return role == other.role && targetCoord == other.targetCoord && targetBarrelId == other.targetBarrelId && fireToCoord == other.fireToCoord;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != this.GetType())
				return false;
			return Equals((StrategicDecision)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (int)role;
				hashCode = (hashCode * 397) ^ targetCoord.GetHashCode();
				hashCode = (hashCode * 397) ^ targetBarrelId.GetHashCode();
				hashCode = (hashCode * 397) ^ fireToCoord.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator==(StrategicDecision left, StrategicDecision right)
		{
			return Equals(left, right);
		}

		public static bool operator!=(StrategicDecision left, StrategicDecision right)
		{
			return !Equals(left, right);
		}

		public override string ToString()
		{
			var targetCoordString = targetCoord.HasValue ? $", {nameof(targetCoord)}: {FastCoord.ToCoord(targetCoord.Value)}" : "";
			var targetBarrelIdString = targetBarrelId.HasValue ? $", {nameof(targetBarrelId)}: {targetBarrelId.Value}" : "";
			var fireToCoordString = fireToCoord.HasValue ? $", {nameof(fireToCoord)}: {FastCoord.ToCoord(fireToCoord.Value)}" : "";
			return $"{role}{targetCoordString}{targetBarrelIdString}{fireToCoordString}";
		}

		public string Dump()
		{
			return $"new {nameof(StrategicDecision)} {{ {nameof(role)} = {nameof(StrategicRole)}.{role}," +
					$" {nameof(targetBarrelId)} = {(targetBarrelId.HasValue ? targetBarrelId.ToString() : "null")}, " +
					$" {nameof(fireToCoord)} = {(fireToCoord.HasValue ? fireToCoord.ToString() : "null")}, " +
					$" {nameof(targetCoord)} = {(targetCoord.HasValue ? targetCoord.ToString() : "null")} }}";
		}
	}
}