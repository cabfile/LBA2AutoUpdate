using System;

namespace LBA2AutoUpdate {
	internal struct Version : IEquatable<Version> {
		public int Major;
		public int Minor;
		public int Patch;

		public Version(int major, int minor = 0, int patch = 0) {
			Major = major;
			Minor = minor;
			Patch = patch;
		}
		public Version(byte[] rawVersion) {
			Major = Convert.ToInt32(((char)rawVersion[0]).ToString(), 16);
			Minor = Convert.ToInt32(((char)rawVersion[1]).ToString(), 16);
			Patch = Convert.ToInt32(((char)rawVersion[2]).ToString(), 16);
		}
		public Version(string version) {
			string[] s = version.Split('.');
			Major = Convert.ToInt32(s[0]);
			if(s.Length > 1) Minor = Convert.ToInt32(s[1]); else Minor = 0;
			if(s.Length > 2) Patch = Convert.ToInt32(s[2]); else Patch = 0;
		}
		public override bool Equals(object obj) {
			return obj is Version other && Major == other.Major && Minor == other.Minor && Patch == other.Patch;
		}

		public bool Equals(Version other) {
			return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
		}

		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = hash * 31 + Major.GetHashCode();
				hash = hash * 31 + Minor.GetHashCode();
				hash = hash * 31 + Patch.GetHashCode();
				return hash;
			}
		}
		public override string ToString() {
			return Major + "." + Minor + "." + Patch;
		}
		public static bool operator ==(Version left, Version right) {
			return left.Major == right.Major && left.Minor == right.Minor && left.Patch == right.Patch;
		}
		public static bool operator !=(Version left, Version right) {
			return left.Major != right.Major || left.Minor != right.Minor || left.Patch != right.Patch;
		}
		public static bool operator >(Version left, Version right) {
			if(left.Major != right.Major) return left.Major > right.Major;
			if(left.Minor != right.Minor) return left.Minor > right.Minor;
			return left.Patch > right.Patch;
		}

		public static bool operator <(Version left, Version right) {
			if(left.Major != right.Major) return left.Major < right.Major;
			if(left.Minor != right.Minor) return left.Minor < right.Minor;
			return left.Patch < right.Patch;
		}
	}
}
