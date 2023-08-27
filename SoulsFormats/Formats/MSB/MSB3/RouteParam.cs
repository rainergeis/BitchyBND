using System.Collections.Generic;
using System.IO;

namespace SoulsFormats;

public partial class MSB3
{
    /// <summary>
    ///     A section containing routes. Purpose unknown.
    /// </summary>
    private class RouteParam : Param<Route>
    {

        /// <summary>
        ///     Creates a new RouteParam with no routes.
        /// </summary>
        public RouteParam()
        {
            Routes = new List<Route>();
        }

        internal override int Version => 3;
        internal override string Type => "ROUTE_PARAM_ST";

        /// <summary>
        ///     The routes in this section.
        /// </summary>
        public List<Route> Routes { get; }

        /// <summary>
        ///     Returns every route in the order they will be written.
        /// </summary>
        public override List<Route> GetEntries()
        {
            return Routes;
        }

        internal override Route ReadEntry(BinaryReaderEx br)
        {
            return Routes.EchoAdd(new Route(br));
        }
    }

    /// <summary>
    ///     Unknown.
    /// </summary>
    public class Route : NamedEntry
    {

        /// <summary>
        ///     Creates a new Route with default values.
        /// </summary>
        public Route()
        {
            Name = "XX-XX";
        }

        internal Route(BinaryReaderEx br)
        {
            var start = br.Position;

            var nameOffset = br.ReadInt64();
            Unk08 = br.ReadInt32();
            Unk0C = br.ReadInt32();
            br.AssertInt32(4); // Type
            br.ReadInt32(); // ID
            br.AssertPattern(0x68, 0x00);

            if (nameOffset == 0)
                throw new InvalidDataException($"{nameof(nameOffset)} must not be 0.");

            br.Position = start + nameOffset;
            Name = br.ReadUTF16();
        }

        /// <summary>
        ///     The name of this route.
        /// </summary>
        public override string Name { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public int Unk08 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public int Unk0C { get; set; }

        /// <summary>
        ///     Creates a deep copy of the route.
        /// </summary>
        public Route DeepCopy()
        {
            return (Route)MemberwiseClone();
        }

        internal override void Write(BinaryWriterEx bw, int id)
        {
            var start = bw.Position;

            bw.ReserveInt64("NameOffset");
            bw.WriteInt32(Unk08);
            bw.WriteInt32(Unk0C);
            bw.WriteInt32(4);
            bw.WriteInt32(id);
            bw.WritePattern(0x68, 0x00);

            bw.FillInt64("NameOffset", bw.Position - start);
            bw.WriteUTF16(Name, true);
            bw.Pad(8);
        }

        /// <summary>
        ///     Returns the name and values of this route.
        /// </summary>
        public override string ToString()
        {
            return $"\"{Name}\" {Unk08} {Unk0C}";
        }
    }
}