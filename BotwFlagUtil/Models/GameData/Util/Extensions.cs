using System.Linq;
using BotwFlagUtil.GameData.Util;
using BymlLibrary;
using BymlLibrary.Nodes.Containers;

namespace BotwFlagUtil.Models.GameData.Util
{
    internal static class Extensions
    {
        internal static Byml ToByml(this int[] ints)
        {
            return new(new BymlArray(ints.Select(i => new Byml(i))));
        }
        internal static Byml ToByml(this float[] floats)
        {
            return new(new BymlArray(floats.Select(f => new Byml(f))));
        }
        internal static Byml ToByml(this string[] strings)
        {
            return new(new BymlArray(strings.Select(s => new Byml(s))));
        }
        internal static Byml ToByml(this Vec2[] vecs)
        {
            return new(new BymlArray(vecs.Select(v => v.ToByml())));
        }
        internal static Byml ToByml(this Vec3[] vecs)
        {
            return new(new BymlArray(vecs.Select(v => v.ToByml())));
        }
        internal static Byml ToByml(this Vec4[] vecs)
        {
            return new(new BymlArray(vecs.Select(v => v.ToByml())));
        }
    }
}
