using System;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FG.Common.Utils
{
	public class MiniId : IEquatable<string>, IEquatable<MiniId>
	{
		private static char[] _values = new char[]
		{
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
			'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
			'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
		};

		private static char[] Build(byte[] bytes)
		{
			var values = new char[8];
			for (int i = 0; i < 8; i++)
			{
				values[i] = _values[((int) bytes[i] % 45)];
			}
			return values;
		}

		public MiniId()
		{
			var guid = Guid.NewGuid().ToByteArray();
			Id = new string(Build(guid));
		}

		public string Id { get; private set; }

		public bool Equals(MiniId other)
		{
			if (other == null) return Id == null;
			return Id.Equals(other.Id, StringComparison.InvariantCultureIgnoreCase);
		}

		public bool Equals(string other)
		{
			if (other == null) return false;
			return ((other.Length == 6) && other.Equals(Id, StringComparison.InvariantCultureIgnoreCase));
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((MiniId) obj);
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public static implicit operator string(MiniId id)
		{
			return id.Id;
		}

		public override string ToString()
		{
			return Id;
		}
	}
}