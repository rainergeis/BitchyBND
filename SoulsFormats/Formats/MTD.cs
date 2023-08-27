using System;
using System.Collections.Generic;
using System.IO;

namespace SoulsFormats;

/// <summary>
///     A material definition format used in all souls games.
/// </summary>
public class MTD : SoulsFile<MTD>
{

    /// <summary>
    ///     The blending mode of the material, used in value g_BlendMode.
    /// </summary>
    public enum BlendMode
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Normal = 0,
        TexEdge = 1,
        Blend = 2,
        Water = 3,
        Add = 4,
        Sub = 5,
        Mul = 6,
        AddMul = 7,
        SubMul = 8,
        WaterWave = 9,
        LSNormal = 32,
        LSTexEdge = 33,
        LSBlend = 34,
        LSWater = 35,
        LSAdd = 36,
        LSSub = 37,
        LSMul = 38,
        LSAddMul = 39,
        LSSubMul = 40,
        LSWaterWave = 41,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    ///     The lighting type of a material, used in value g_LightingType.
    /// </summary>
    public enum LightingType
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        None = 0,
        HemDirDifSpcx3 = 1,
        HemEnvDifSpc = 3,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    ///     Value types of MTD params.
    /// </summary>
    // I believe the engine supports Bool2-4 and Int3-4 as well, but they're never used so I won't bother yet.
    public enum ParamType
    {
        /// <summary>
        ///     A one-byte boolean value.
        /// </summary>
        Bool,

        /// <summary>
        ///     A four-byte integer.
        /// </summary>
        Int,

        /// <summary>
        ///     An array of two four-byte integers.
        /// </summary>
        Int2,

        /// <summary>
        ///     A four-byte floating point number.
        /// </summary>
        Float,

        /// <summary>
        ///     An array of two four-byte floating point numbers.
        /// </summary>
        Float2,

        /// <summary>
        ///     An array of three four-byte floating point numbers.
        /// </summary>
        Float3,

        /// <summary>
        ///     An array of four four-byte floating point numbers.
        /// </summary>
        Float4
    }

    /// <summary>
    ///     Creates an MTD with default values.
    /// </summary>
    public MTD()
    {
        ShaderPath = "Unknown.spx";
        Description = "";
        Params = new List<Param>();
        Textures = new List<Texture>();
    }

    /// <summary>
    ///     A path to the shader source file, which also determines which compiled shader to use for this material.
    /// </summary>
    public string ShaderPath { get; set; }

    /// <summary>
    ///     A description of this material's purpose.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///     Values determining material properties.
    /// </summary>
    public List<Param> Params { get; set; }

    /// <summary>
    ///     Texture types required by the material shader.
    /// </summary>
    public List<Texture> Textures { get; set; }

    /// <summary>
    ///     Checks whether the data appears to be a file of this format.
    /// </summary>
    protected override bool Is(BinaryReaderEx br)
    {
        if (br.Length < 0x30)
            return false;

        var magic = br.GetASCII(0x2C, 4);
        return magic == "MTD ";
    }

    /// <summary>
    ///     Deserializes file data from a stream.
    /// </summary>
    protected override void Read(BinaryReaderEx br)
    {
        br.BigEndian = false;
        Block.Read(br, 0, 3, 0x01); // File
        {
            Block.Read(br, 1, 2, 0xB0); // Header
            {
                AssertMarkedString(br, 0x34, "MTD ");
                br.AssertInt32(1000);
            }
            AssertMarker(br, 0x01);

            Block.Read(br, 2, 4, 0xA3); // Data
            {
                ShaderPath = ReadMarkedString(br, 0xA3);
                Description = ReadMarkedString(br, 0x03);
                br.AssertInt32(1);

                Block.Read(br, 3, 4, 0xA3); // Lists
                {
                    br.AssertInt32(0);
                    AssertMarker(br, 0x03);

                    var paramCount = br.ReadInt32();
                    Params = new List<Param>(paramCount);
                    for (var i = 0; i < paramCount; i++)
                        Params.Add(new Param(br));
                    AssertMarker(br, 0x03);

                    var textureCount = br.ReadInt32();
                    Textures = new List<Texture>(textureCount);
                    for (var i = 0; i < textureCount; i++)
                        Textures.Add(new Texture(br));
                    AssertMarker(br, 0x04);
                    br.AssertInt32(0);
                }
                AssertMarker(br, 0x04);
                br.AssertInt32(0);
            }
            AssertMarker(br, 0x04);
            br.AssertInt32(0);
        }
    }

    /// <summary>
    ///     Serializes file data to a stream.
    /// </summary>
    protected override void Write(BinaryWriterEx bw)
    {
        bw.BigEndian = false;
        var fileBlock = Block.Write(bw, 0, 3, 0x01);
        {
            var headerBlock = Block.Write(bw, 1, 2, 0xB0);
            {
                WriteMarkedString(bw, 0x34, "MTD ");
                bw.WriteInt32(1000);
            }
            headerBlock.Finish(bw);
            WriteMarker(bw, 0x01);

            var dataBlock = Block.Write(bw, 2, 4, 0xA3);
            {
                WriteMarkedString(bw, 0xA3, ShaderPath);
                WriteMarkedString(bw, 0x03, Description);
                bw.WriteInt32(1);

                var listsBlock = Block.Write(bw, 3, 4, 0xA3);
                {
                    bw.WriteInt32(0);
                    WriteMarker(bw, 0x03);

                    bw.WriteInt32(Params.Count);
                    foreach (Param internalEntry in Params)
                        internalEntry.Write(bw);
                    WriteMarker(bw, 0x03);

                    bw.WriteInt32(Textures.Count);
                    foreach (Texture externalEntry in Textures)
                        externalEntry.Write(bw);
                    WriteMarker(bw, 0x04);
                    bw.WriteInt32(0);
                }
                listsBlock.Finish(bw);
                WriteMarker(bw, 0x04);
                bw.WriteInt32(0);
            }
            dataBlock.Finish(bw);
            WriteMarker(bw, 0x04);
            bw.WriteInt32(0);
        }
        fileBlock.Finish(bw);
    }

    /// <summary>
    ///     A value defining the material's properties.
    /// </summary>
    public class Param
    {

        /// <summary>
        ///     Creates a new Param with the specified values.
        /// </summary>
        public Param(string name, ParamType type, object value = null)
        {
            Name = name;
            Type = type;
            Value = value;
            if (Value == null)
                switch (type)
                {
                    case ParamType.Bool:
                        Value = false;
                        break;
                    case ParamType.Float:
                        Value = 0f;
                        break;
                    case ParamType.Float2:
                        Value = new float[2];
                        break;
                    case ParamType.Float3:
                        Value = new float[3];
                        break;
                    case ParamType.Float4:
                        Value = new float[4];
                        break;
                    case ParamType.Int:
                        Value = 0;
                        break;
                    case ParamType.Int2:
                        Value = new int[2];
                        break;
                }
        }

        internal Param(BinaryReaderEx br)
        {
            Block.Read(br, 4, 4, 0xA3); // Param
            {
                Name = ReadMarkedString(br, 0xA3);
                var type = ReadMarkedString(br, 0x04);
                Type = (ParamType)Enum.Parse(typeof(ParamType), type, true);
                br.AssertInt32(1);

                Block.Read(br, null, 1, null); // Value
                {
                    br.ReadInt32(); // Value count

                    if (Type == ParamType.Int)
                        Value = br.ReadInt32();
                    else if (Type == ParamType.Int2)
                        Value = br.ReadInt32s(2);
                    else if (Type == ParamType.Bool)
                        Value = br.ReadBoolean();
                    else if (Type == ParamType.Float)
                        Value = br.ReadSingle();
                    else if (Type == ParamType.Float2)
                        Value = br.ReadSingles(2);
                    else if (Type == ParamType.Float3)
                        Value = br.ReadSingles(3);
                    else if (Type == ParamType.Float4)
                        Value = br.ReadSingles(4);
                }
                AssertMarker(br, 0x04);
                br.AssertInt32(0);
            }
        }

        /// <summary>
        ///     The name of the param.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The type of this value.
        /// </summary>
        public ParamType Type { get; }

        /// <summary>
        ///     The value itself.
        /// </summary>
        public object Value { get; set; }

        internal void Write(BinaryWriterEx bw)
        {
            var paramBlock = Block.Write(bw, 4, 4, 0xA3);
            {
                WriteMarkedString(bw, 0xA3, Name);
                WriteMarkedString(bw, 0x04, Type.ToString().ToLower());
                bw.WriteInt32(1);

                var valueBlockType = -1;
                byte valueBlockMarker = 0xFF;
                if (Type == ParamType.Bool)
                {
                    valueBlockType = 0x1000;
                    valueBlockMarker = 0xC0;
                }
                else if (Type == ParamType.Int || Type == ParamType.Int2)
                {
                    valueBlockType = 0x1001;
                    valueBlockMarker = 0xC5;
                }
                else if (Type == ParamType.Float || Type == ParamType.Float2 || Type == ParamType.Float3 ||
                         Type == ParamType.Float4)
                {
                    valueBlockType = 0x1002;
                    valueBlockMarker = 0xCA;
                }

                var valueBlock = Block.Write(bw, valueBlockType, 1, valueBlockMarker);
                {
                    var valueCount = -1;
                    if (Type == ParamType.Bool || Type == ParamType.Int || Type == ParamType.Float)
                        valueCount = 1;
                    else if (Type == ParamType.Int2 || Type == ParamType.Float2)
                        valueCount = 2;
                    else if (Type == ParamType.Float3)
                        valueCount = 3;
                    else if (Type == ParamType.Float4)
                        valueCount = 4;
                    bw.WriteInt32(valueCount);

                    if (Type == ParamType.Int)
                        bw.WriteInt32((int)Value);
                    else if (Type == ParamType.Int2)
                        bw.WriteInt32s((int[])Value);
                    else if (Type == ParamType.Bool)
                        bw.WriteBoolean((bool)Value);
                    else if (Type == ParamType.Float)
                        bw.WriteSingle((float)Value);
                    else if (Type == ParamType.Float2)
                        bw.WriteSingles((float[])Value);
                    else if (Type == ParamType.Float3)
                        bw.WriteSingles((float[])Value);
                    else if (Type == ParamType.Float4)
                        bw.WriteSingles((float[])Value);
                }
                valueBlock.Finish(bw);
                WriteMarker(bw, 0x04);
                bw.WriteInt32(0);
            }
            paramBlock.Finish(bw);
        }

        /// <summary>
        ///     Returns the name and value of the param.
        /// </summary>
        public override string ToString()
        {
            if (Type == ParamType.Float2 || Type == ParamType.Float3 || Type == ParamType.Float4)
                return $"{Name} = {{{string.Join(", ", (float[])Value)}}}";
            if (Type == ParamType.Int2)
                return $"{Name} = {{{string.Join(", ", (int[])Value)}}}";
            return $"{Name} = {Value}";
        }
    }

    /// <summary>
    ///     Texture types used by the material, filled in in each FLVER.
    /// </summary>
    public class Texture
    {

        /// <summary>
        ///     Creates a Texture with default values.
        /// </summary>
        public Texture()
        {
            Type = "g_DiffuseTexture";
            Path = "";
            UnkFloats = new List<float>();
        }

        internal Texture(BinaryReaderEx br)
        {
            var textureBlock = Block.Read(br, 0x2000, null, 0xA3);
            {
                if (textureBlock.Version == 3)
                    Extended = false;
                else if (textureBlock.Version == 5)
                    Extended = true;
                else
                    throw new InvalidDataException(
                        $"Texture block version is expected to be 3 or 5, but it was {textureBlock.Version}.");

                Type = ReadMarkedString(br, 0x35);
                UVNumber = br.ReadInt32();
                AssertMarker(br, 0x35);
                ShaderDataIndex = br.ReadInt32();

                if (Extended)
                {
                    br.AssertInt32(0xA3);
                    Path = ReadMarkedString(br, 0xBA);
                    var floatCount = br.ReadInt32();
                    UnkFloats = new List<float>(br.ReadSingles(floatCount));
                }
                else
                {
                    Path = "";
                    UnkFloats = new List<float>();
                }
            }
        }

        /// <summary>
        ///     The type of texture (g_Diffuse, g_Specular, etc).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        ///     Whether the texture has extended information for Sekiro.
        /// </summary>
        public bool Extended { get; set; }

        /// <summary>
        ///     Indicates the order of UVs in FLVER vertex data.
        /// </summary>
        public int UVNumber { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public int ShaderDataIndex { get; set; }

        /// <summary>
        ///     A fixed texture path for this material, only used in Sekiro.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///     Floats for an unknown purpose, only used in Sekiro.
        /// </summary>
        public List<float> UnkFloats { get; set; }

        internal void Write(BinaryWriterEx bw)
        {
            var textureBlock = Block.Write(bw, 0x2000, Extended ? 5 : 3, 0xA3);
            {
                WriteMarkedString(bw, 0x35, Type);
                bw.WriteInt32(UVNumber);
                WriteMarker(bw, 0x35);
                bw.WriteInt32(ShaderDataIndex);

                if (Extended)
                {
                    bw.WriteInt32(0xA3);
                    WriteMarkedString(bw, 0xBA, Path);
                    bw.WriteInt32(UnkFloats.Count);
                    bw.WriteSingles(UnkFloats);
                }
            }
            textureBlock.Finish(bw);
        }

        /// <summary>
        ///     Returns the type of the texture.
        /// </summary>
        public override string ToString()
        {
            return Type;
        }
    }

    #region Read/Write utilities

    private static byte ReadMarker(BinaryReaderEx br)
    {
        var marker = br.ReadByte();
        br.Pad(4);
        return marker;
    }

    private static byte AssertMarker(BinaryReaderEx br, byte marker)
    {
        br.AssertByte(marker);
        br.Pad(4);
        return marker;
    }

    private static void WriteMarker(BinaryWriterEx bw, byte marker)
    {
        bw.WriteByte(marker);
        bw.Pad(4);
    }

    private static string ReadMarkedString(BinaryReaderEx br, byte marker)
    {
        var length = br.ReadInt32();
        var str = br.ReadShiftJIS(length);
        AssertMarker(br, marker);
        return str;
    }

    private static string AssertMarkedString(BinaryReaderEx br, byte marker, string assert)
    {
        var str = ReadMarkedString(br, marker);
        if (str != assert)
            throw new InvalidDataException(
                $"Read marked string: {str} | Expected: {assert} | Ending position: 0x{br.Position:X}");
        return str;
    }

    private static void WriteMarkedString(BinaryWriterEx bw, byte marker, string str)
    {
        var bytes = SFEncoding.ShiftJIS.GetBytes(str);
        bw.WriteInt32(bytes.Length);
        bw.WriteBytes(bytes);
        WriteMarker(bw, marker);
    }

    private class Block
    {
        public uint Length;
        public byte Marker;
        public readonly long Start;
        public int Type;
        public readonly int Version;

        private Block(long start, uint length, int type, int version, byte marker)
        {
            Start = start;
            Length = length;
            Type = type;
            Version = version;
            Marker = marker;
        }

        public static Block Read(BinaryReaderEx br, int? assertType, int? assertVersion, byte? assertMarker)
        {
            br.AssertInt32(0);
            var length = br.ReadUInt32();
            var start = br.Position;
            var type = assertType.HasValue ? br.AssertInt32(assertType.Value) : br.ReadInt32();
            var version = assertVersion.HasValue ? br.AssertInt32(assertVersion.Value) : br.ReadInt32();
            var marker = assertMarker.HasValue ? AssertMarker(br, assertMarker.Value) : ReadMarker(br);
            return new Block(start, length, type, version, marker);
        }

        public static Block Write(BinaryWriterEx bw, int type, int version, byte marker)
        {
            bw.WriteInt32(0);
            var start = bw.Position + 4;
            bw.ReserveUInt32($"Block{start:X}");
            bw.WriteInt32(type);
            bw.WriteInt32(version);
            WriteMarker(bw, marker);
            return new Block(start, 0, type, version, marker);
        }

        public void Finish(BinaryWriterEx bw)
        {
            Length = (uint)(bw.Position - Start);
            bw.FillUInt32($"Block{Start:X}", Length);
        }
    }

    #endregion

}