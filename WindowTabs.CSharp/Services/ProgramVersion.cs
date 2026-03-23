using System;
using System.Linq;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ProgramVersion : IComparable<ProgramVersion>
    {
        private readonly int[] parts;

        public ProgramVersion(string version)
        {
            parts = (version ?? string.Empty)
                .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(static part => int.TryParse(part, out var value) ? value : 0)
                .ToArray();
        }

        public int CompareTo(ProgramVersion other)
        {
            if (other == null)
            {
                return 1;
            }

            var length = Math.Max(parts.Length, other.parts.Length);
            for (var index = 0; index < length; index++)
            {
                var left = index < parts.Length ? parts[index] : 0;
                var right = index < other.parts.Length ? other.parts[index] : 0;
                if (left == right)
                {
                    continue;
                }

                return left.CompareTo(right);
            }

            return 0;
        }

        public bool IsNewerThan(ProgramVersion other)
        {
            return CompareTo(other) > 0;
        }
    }
}
