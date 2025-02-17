﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace SoulsFormats;

public partial class MSBB
{
    internal enum PartType : uint
    {
        MapPiece = 0,
        Object = 1,
        Enemy = 2,
        Player = 4,
        Collision = 5,
        Navmesh = 8,
        DummyObject = 9,
        DummyEnemy = 10,
        ConnectCollision = 11,
        Other = 0xFFFFFFFF
    }

    /// <summary>
    ///     All instances of concrete things in the map.
    /// </summary>
    public class PartsParam : Param<Part>, IMsbParam<IMsbPart>
    {

        /// <summary>
        ///     Creates an empty PartsParam.
        /// </summary>
        public PartsParam()
        {
            MapPieces = new List<Part.MapPiece>();
            Objects = new List<Part.Object>();
            Enemies = new List<Part.Enemy>();
            Players = new List<Part.Player>();
            Collisions = new List<Part.Collision>();
            Navmeshes = new List<Part.Navmesh>();
            DummyObjects = new List<Part.DummyObject>();
            DummyEnemies = new List<Part.DummyEnemy>();
            ConnectCollisions = new List<Part.ConnectCollision>();
            Others = new List<Part.Other>();
        }

        internal override int Version => 3;
        internal override string Name => "PARTS_PARAM_ST";

        /// <summary>
        ///     All of the fixed visual geometry of the map.
        /// </summary>
        public List<Part.MapPiece> MapPieces { get; set; }

        /// <summary>
        ///     Dynamic props and interactive things.
        /// </summary>
        public List<Part.Object> Objects { get; set; }

        /// <summary>
        ///     All non-player characters.
        /// </summary>
        public List<Part.Enemy> Enemies { get; set; }

        /// <summary>
        ///     These have something to do with player spawn points.
        /// </summary>
        public List<Part.Player> Players { get; set; }

        /// <summary>
        ///     Invisible physical geometry of the map.
        /// </summary>
        public List<Part.Collision> Collisions { get; set; }

        /// <summary>
        ///     AI navigation meshes.
        /// </summary>
        public List<Part.Navmesh> Navmeshes { get; set; }

        /// <summary>
        ///     Objects that don't appear normally; either unused, or used for cutscenes.
        /// </summary>
        public List<Part.DummyObject> DummyObjects { get; set; }

        /// <summary>
        ///     Enemies that don't appear normally; either unused, or used for cutscenes.
        /// </summary>
        public List<Part.DummyEnemy> DummyEnemies { get; set; }

        /// <summary>
        ///     Dummy parts that reference an actual collision and cause it to load another map.
        /// </summary>
        public List<Part.ConnectCollision> ConnectCollisions { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public List<Part.Other> Others { get; set; }

        IMsbPart IMsbParam<IMsbPart>.Add(IMsbPart item)
        {
            return Add((Part)item);
        }

        IReadOnlyList<IMsbPart> IMsbParam<IMsbPart>.GetEntries()
        {
            return GetEntries();
        }

        /// <summary>
        ///     Adds a part to the appropriate list for its type; returns the part.
        /// </summary>
        public Part Add(Part part)
        {
            switch (part)
            {
                case Part.MapPiece p:
                    MapPieces.Add(p);
                    break;
                case Part.Object p:
                    Objects.Add(p);
                    break;
                case Part.Enemy p:
                    Enemies.Add(p);
                    break;
                case Part.Player p:
                    Players.Add(p);
                    break;
                case Part.Collision p:
                    Collisions.Add(p);
                    break;
                case Part.Navmesh p:
                    Navmeshes.Add(p);
                    break;
                case Part.DummyObject p:
                    DummyObjects.Add(p);
                    break;
                case Part.DummyEnemy p:
                    DummyEnemies.Add(p);
                    break;
                case Part.ConnectCollision p:
                    ConnectCollisions.Add(p);
                    break;
                case Part.Other p:
                    Others.Add(p);
                    break;

                default:
                    throw new ArgumentException($"Unrecognized type {part.GetType()}.", nameof(part));
            }

            return part;
        }

        /// <summary>
        ///     Returns every Part in the order they'll be written.
        /// </summary>
        public override List<Part> GetEntries()
        {
            return SFUtil.ConcatAll<Part>(
                MapPieces, Objects, Enemies, Players, Collisions,
                Navmeshes, DummyObjects, DummyEnemies, ConnectCollisions, Others);
        }

        internal override Part ReadEntry(BinaryReaderEx br)
        {
            PartType type = br.GetEnum32<PartType>(br.Position + 0x14);
            switch (type)
            {
                case PartType.MapPiece:
                    return MapPieces.EchoAdd(new Part.MapPiece(br));

                case PartType.Object:
                    return Objects.EchoAdd(new Part.Object(br));

                case PartType.Enemy:
                    return Enemies.EchoAdd(new Part.Enemy(br));

                case PartType.Player:
                    return Players.EchoAdd(new Part.Player(br));

                case PartType.Collision:
                    return Collisions.EchoAdd(new Part.Collision(br));

                case PartType.Navmesh:
                    return Navmeshes.EchoAdd(new Part.Navmesh(br));

                case PartType.DummyObject:
                    return DummyObjects.EchoAdd(new Part.DummyObject(br));

                case PartType.DummyEnemy:
                    return DummyEnemies.EchoAdd(new Part.DummyEnemy(br));

                case PartType.ConnectCollision:
                    return ConnectCollisions.EchoAdd(new Part.ConnectCollision(br));

                case PartType.Other:
                    return Others.EchoAdd(new Part.Other(br));

                default:
                    throw new NotImplementedException($"Unimplemented part type: {type}");
            }
        }
    }

    /// <summary>
    ///     Common information for all concrete entities.
    /// </summary>
    public abstract class Part : Entry, IMsbPart
    {
        private int ModelIndex;

        private protected Part(string name)
        {
            Description = "";
            Name = name;
            SibPath = "";
            Scale = Vector3.One;
            DrawGroups = new uint[8]
            {
                0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF
            };
            DispGroups = new uint[8]
            {
                0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF
            };
            BackreadGroups = new uint[8]
            {
                0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF
            };
            EntityID = -1;
        }

        private protected Part(BinaryReaderEx br)
        {
            var start = br.Position;
            var descOffset = br.ReadInt64();
            var nameOffset = br.ReadInt64();
            InstanceID = br.ReadInt32();
            br.AssertUInt32((uint)Type);
            br.ReadInt32(); // ID
            ModelIndex = br.ReadInt32();
            var sibOffset = br.ReadInt64();
            Position = br.ReadVector3();
            Rotation = br.ReadVector3();
            Scale = br.ReadVector3();
            DrawGroups = br.ReadUInt32s(8);
            DispGroups = br.ReadUInt32s(8);
            BackreadGroups = br.ReadUInt32s(8);
            br.AssertInt32(0);
            var entityDataOffset = br.ReadInt64();
            var typeDataOffset = br.ReadInt64();
            var gparamOffset = br.ReadInt64();
            var sceneGparamOffset = br.ReadInt64();

            if (descOffset == 0)
                throw new InvalidDataException($"{nameof(descOffset)} must not be 0 in type {GetType()}.");
            if (nameOffset == 0)
                throw new InvalidDataException($"{nameof(nameOffset)} must not be 0 in type {GetType()}.");
            if (sibOffset == 0)
                throw new InvalidDataException($"{nameof(sibOffset)} must not be 0 in type {GetType()}.");
            if (entityDataOffset == 0)
                throw new InvalidDataException($"{nameof(entityDataOffset)} must not be 0 in type {GetType()}.");
            if (HasTypeData ^ (typeDataOffset != 0))
                throw new InvalidDataException(
                    $"Unexpected {nameof(typeDataOffset)} 0x{typeDataOffset:X} in type {GetType()}.");
            if (HasGparamConfig ^ (gparamOffset != 0))
                throw new InvalidDataException(
                    $"Unexpected {nameof(gparamOffset)} 0x{gparamOffset:X} in type {GetType()}.");
            if (HasSceneGparamConfig ^ (sceneGparamOffset != 0))
                throw new InvalidDataException(
                    $"Unexpected {nameof(sceneGparamOffset)} 0x{sceneGparamOffset:X} in type {GetType()}.");

            br.Position = start + descOffset;
            Description = br.ReadUTF16();

            br.Position = start + nameOffset;
            Name = br.ReadUTF16();

            br.Position = start + sibOffset;
            SibPath = br.ReadUTF16();

            br.Position = start + entityDataOffset;
            ReadEntityData(br);

            if (HasTypeData)
            {
                br.Position = start + typeDataOffset;
                ReadTypeData(br);
            }

            if (HasGparamConfig)
            {
                br.Position = start + gparamOffset;
                ReadGparamConfig(br);
            }

            if (HasSceneGparamConfig)
            {
                br.Position = start + sceneGparamOffset;
                ReadSceneGparamConfig(br);
            }
        }

        private protected abstract PartType Type { get; }
        private protected abstract bool HasTypeData { get; }
        private protected abstract bool HasGparamConfig { get; }
        private protected abstract bool HasSceneGparamConfig { get; }

        /// <summary>
        ///     A description of the part, usually left blank.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Unknown; appears to count up with each instance of a model that was added.
        /// </summary>
        public int InstanceID { get; set; }

        /// <summary>
        ///     A path to a .sib file, presumed to be some kind of editor placeholder.
        /// </summary>
        public string SibPath { get; set; }

        /// <summary>
        ///     Controls when the part is visible.
        /// </summary>
        public uint[] DrawGroups { get; private set; }

        /// <summary>
        ///     Controls when the part is visible.
        /// </summary>
        public uint[] DispGroups { get; private set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public uint[] BackreadGroups { get; private set; }

        /// <summary>
        ///     Identifies the part in external files.
        /// </summary>
        public int EntityID { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte UnkE04 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte UnkE05 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte UnkE06 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte UnkE07 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte LanternID { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte LodParamID { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte UnkE0E { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte UnkE0F { get; set; }

        /// <summary>
        ///     The name of the part.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The model of the Part, corresponding to an entry in the ModelParam.
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        ///     Location of the part.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        ///     Rotation of the part, in degrees.
        /// </summary>
        public Vector3 Rotation { get; set; }

        /// <summary>
        ///     Scale of the part, only meaningful for map pieces and objects.
        /// </summary>
        public Vector3 Scale { get; set; }

        IMsbPart IMsbPart.DeepCopy()
        {
            return DeepCopy();
        }

        /// <summary>
        ///     Creates a deep copy of the part.
        /// </summary>
        public Part DeepCopy()
        {
            var part = (Part)MemberwiseClone();
            part.DrawGroups = (uint[])DrawGroups.Clone();
            part.DispGroups = (uint[])DispGroups.Clone();
            part.BackreadGroups = (uint[])BackreadGroups.Clone();
            DeepCopyTo(part);
            return part;
        }

        private protected virtual void DeepCopyTo(Part part)
        {
        }

        private void ReadEntityData(BinaryReaderEx br)
        {
            EntityID = br.ReadInt32();
            UnkE04 = br.ReadByte();
            UnkE05 = br.ReadByte();
            UnkE06 = br.ReadByte();
            UnkE07 = br.ReadByte();
            br.AssertInt32(0);
            LanternID = br.ReadByte();
            LodParamID = br.ReadByte();
            UnkE0E = br.ReadByte();
            UnkE0F = br.ReadByte();
        }

        private protected virtual void ReadTypeData(BinaryReaderEx br)
        {
            throw new NotImplementedException($"Type {GetType()} missing valid {nameof(ReadTypeData)}.");
        }

        private protected virtual void ReadGparamConfig(BinaryReaderEx br)
        {
            throw new NotImplementedException($"Type {GetType()} missing valid {nameof(ReadGparamConfig)}.");
        }

        private protected virtual void ReadSceneGparamConfig(BinaryReaderEx br)
        {
            throw new NotImplementedException($"Type {GetType()} missing valid {nameof(ReadSceneGparamConfig)}.");
        }

        internal override void Write(BinaryWriterEx bw, int id)
        {
            var start = bw.Position;
            bw.ReserveInt64("DescOffset");
            bw.ReserveInt64("NameOffset");
            bw.WriteInt32(InstanceID);
            bw.WriteUInt32((uint)Type);
            bw.WriteInt32(Type == PartType.Other ? 0 : id);
            bw.WriteInt32(ModelIndex);
            bw.ReserveInt64("SibOffset");
            bw.WriteVector3(Position);
            bw.WriteVector3(Rotation);
            bw.WriteVector3(Scale);
            bw.WriteUInt32s(DrawGroups);
            bw.WriteUInt32s(DispGroups);
            bw.WriteUInt32s(BackreadGroups);
            bw.WriteInt32(0);
            bw.ReserveInt64("EntityDataOffset");
            bw.ReserveInt64("TypeDataOffset");
            bw.ReserveInt64("GparamOffset");
            bw.ReserveInt64("SceneGparamOffset");

            var stringsStart = bw.Position;
            bw.FillInt64("DescOffset", bw.Position - start);
            bw.WriteUTF16(Description, true);

            bw.FillInt64("NameOffset", bw.Position - start);
            bw.WriteUTF16(MSB.ReambiguateName(Name), true);

            bw.FillInt64("SibOffset", bw.Position - start);
            bw.WriteUTF16(SibPath, true);
            if (bw.Position - stringsStart <= 0x38)
                bw.WritePattern(0x3C - (int)(bw.Position - stringsStart), 0x00);
            else
                bw.Pad(8);

            bw.FillInt64("EntityDataOffset", bw.Position - start);
            WriteEntityData(bw);
            if (Type != PartType.MapPiece)
                bw.Pad(8);

            if (HasTypeData)
            {
                bw.FillInt64("TypeDataOffset", bw.Position - start);
                WriteTypeData(bw);
            }
            else
            {
                bw.FillInt64("TypeDataOffset", 0);
            }

            bw.Pad(8);

            if (HasGparamConfig)
            {
                bw.FillInt64("GparamOffset", bw.Position - start);
                WriteGparamConfig(bw);
            }
            else
            {
                bw.FillInt64("GparamOffset", 0);
            }

            if (HasSceneGparamConfig)
            {
                bw.FillInt64("SceneGparamOffset", bw.Position - start);
                WriteSceneGparamConfig(bw);
            }
            else
            {
                bw.FillInt64("SceneGparamOffset", 0);
            }
        }

        private void WriteEntityData(BinaryWriterEx bw)
        {
            bw.WriteInt32(EntityID);
            bw.WriteByte(UnkE04);
            bw.WriteByte(UnkE05);
            bw.WriteByte(UnkE06);
            bw.WriteByte(UnkE07);
            bw.WriteInt32(0);
            bw.WriteByte(LanternID);
            bw.WriteByte(LodParamID);
            bw.WriteByte(UnkE0E);
            bw.WriteByte(UnkE0F);
        }

        private protected virtual void WriteTypeData(BinaryWriterEx bw)
        {
            throw new NotImplementedException($"Type {GetType()} missing valid {nameof(WriteTypeData)}.");
        }

        private protected virtual void WriteGparamConfig(BinaryWriterEx bw)
        {
            throw new NotImplementedException($"Type {GetType()} missing valid {nameof(WriteGparamConfig)}.");
        }

        private protected virtual void WriteSceneGparamConfig(BinaryWriterEx bw)
        {
            throw new NotImplementedException($"Type {GetType()} missing valid {nameof(WriteSceneGparamConfig)}.");
        }

        internal virtual void GetNames(MSBB msb, Entries entries)
        {
            ModelName = MSB.FindName(entries.Models, ModelIndex);
        }

        internal virtual void GetIndices(MSBB msb, Entries entries)
        {
            ModelIndex = MSB.FindIndex(entries.Models, ModelName);
        }

        /// <summary>
        ///     Returns a string representation of the part.
        /// </summary>
        public override string ToString()
        {
            if (Description == "")
                return $"{Type} {Name}";
            return $"{Type} {Name} - {Description}";
        }

        /// <summary>
        ///     Gparam value IDs for various part types.
        /// </summary>
        public class GparamConfig
        {

            /// <summary>
            ///     Creates a GparamConfig with default values.
            /// </summary>
            public GparamConfig()
            {
            }

            internal GparamConfig(BinaryReaderEx br)
            {
                LightSetID = br.ReadInt32();
                FogParamID = br.ReadInt32();
                LightScatteringID = br.ReadInt32();
                EnvMapID = br.ReadInt32();
                br.AssertPattern(0x10, 0x00);
            }

            /// <summary>
            ///     ID of the value set from LightSet ParamEditor to use.
            /// </summary>
            public int LightSetID { get; set; }

            /// <summary>
            ///     ID of the value set from FogParamEditor to use.
            /// </summary>
            public int FogParamID { get; set; }

            /// <summary>
            ///     ID of the value set from LightScattering : ParamEditor to use.
            /// </summary>
            public int LightScatteringID { get; set; }

            /// <summary>
            ///     ID of the value set from Env Map:Editor to use.
            /// </summary>
            public int EnvMapID { get; set; }

            /// <summary>
            ///     Creates a deep copy of the gparam config.
            /// </summary>
            public GparamConfig DeepCopy()
            {
                return (GparamConfig)MemberwiseClone();
            }

            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteInt32(LightSetID);
                bw.WriteInt32(FogParamID);
                bw.WriteInt32(LightScatteringID);
                bw.WriteInt32(EnvMapID);
                bw.WritePattern(0x10, 0x00);
            }

            /// <summary>
            ///     Returns the four gparam values as a string.
            /// </summary>
            public override string ToString()
            {
                return $"{LightSetID}, {FogParamID}, {LightScatteringID}, {EnvMapID}";
            }
        }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public class SceneGparamConfig
        {

            /// <summary>
            ///     Creates a SceneGparamConfig with default values.
            /// </summary>
            public SceneGparamConfig()
            {
                EventIDs = new sbyte[4];
            }

            internal SceneGparamConfig(BinaryReaderEx br)
            {
                Unk00 = br.ReadInt32();
                Unk04 = br.ReadInt32();
                Unk08 = br.ReadInt32();
                Unk0C = br.ReadInt32();
                Unk10 = br.ReadInt32();
                Unk14 = br.ReadInt32();
                br.AssertPattern(0x24, 0x00);
                EventIDs = br.ReadSBytes(4);
                Unk40 = br.ReadSingle();
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
            }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public int Unk00 { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public int Unk04 { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public int Unk08 { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public int Unk0C { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public int Unk10 { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public int Unk14 { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public sbyte[] EventIDs { get; private set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public float Unk40 { get; set; }

            /// <summary>
            ///     Creates a deep copy of the scene gparam config.
            /// </summary>
            public SceneGparamConfig DeepCopy()
            {
                var config = (SceneGparamConfig)MemberwiseClone();
                config.EventIDs = (sbyte[])EventIDs.Clone();
                return config;
            }

            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteInt32(Unk00);
                bw.WriteInt32(Unk04);
                bw.WriteInt32(Unk08);
                bw.WriteInt32(Unk0C);
                bw.WriteInt32(Unk10);
                bw.WriteInt32(Unk14);
                bw.WritePattern(0x24, 0x00);
                bw.WriteSBytes(EventIDs);
                bw.WriteSingle(Unk40);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
            }
        }

        /// <summary>
        ///     A visible but not physical model making up the map.
        /// </summary>
        public class MapPiece : Part
        {

            /// <summary>
            ///     Creates a MapPiece with default values.
            /// </summary>
            public MapPiece() : base("mXXXXXX_XXXX")
            {
                Gparam = new GparamConfig();
            }

            internal MapPiece(BinaryReaderEx br) : base(br)
            {
            }

            private protected override PartType Type => PartType.MapPiece;
            private protected override bool HasTypeData => true;
            private protected override bool HasGparamConfig => true;
            private protected override bool HasSceneGparamConfig => false;

            /// <summary>
            ///     Gparam IDs for this map piece.
            /// </summary>
            public GparamConfig Gparam { get; set; }

            private protected override void DeepCopyTo(Part part)
            {
                var piece = (MapPiece)part;
                piece.Gparam = Gparam.DeepCopy();
            }

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
                br.AssertInt32(0);
                br.AssertInt32(0);
            }

            private protected override void ReadGparamConfig(BinaryReaderEx br)
            {
                Gparam = new GparamConfig(br);
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(0);
            }

            private protected override void WriteGparamConfig(BinaryWriterEx bw)
            {
                Gparam.Write(bw);
            }
        }

        /// <summary>
        ///     Common base data for objects and dummy objects.
        /// </summary>
        public abstract class ObjectBase : Part
        {
            private int CollisionIndex;

            private protected ObjectBase() : base("oXXXXXX_XXXX")
            {
                Gparam = new GparamConfig();
                AnimIDs = new short[4] { -1, -1, -1, -1 };
                ModelSfxParamRelativeIDs = new short[4];
            }

            private protected ObjectBase(BinaryReaderEx br) : base(br)
            {
            }

            private protected override bool HasTypeData => true;
            private protected override bool HasGparamConfig => true;
            private protected override bool HasSceneGparamConfig => false;

            /// <summary>
            ///     Gparam IDs for this object.
            /// </summary>
            public GparamConfig Gparam { get; set; }

            /// <summary>
            ///     Collision that controls loading of the object.
            /// </summary>
            public string CollisionName { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public sbyte BreakTerm { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public sbyte NetSyncType { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public bool CollisionFilter { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public bool SetMainObjStructureBooleans { get; set; }

            /// <summary>
            ///     Automatically playing animations; only the first is actually used, according to Pav.
            /// </summary>
            public short[] AnimIDs { get; private set; }

            /// <summary>
            ///     Value added to the base ModelSfxParam ID; only the first is actually used, according to Pav.
            /// </summary>
            public short[] ModelSfxParamRelativeIDs { get; private set; }

            private protected override void DeepCopyTo(Part part)
            {
                var obj = (ObjectBase)part;
                obj.Gparam = Gparam.DeepCopy();
                obj.AnimIDs = (short[])AnimIDs.Clone();
                obj.ModelSfxParamRelativeIDs = (short[])ModelSfxParamRelativeIDs.Clone();
            }

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
                br.AssertInt32(0);
                br.AssertInt32(0);
                CollisionIndex = br.ReadInt32();
                BreakTerm = br.ReadSByte();
                NetSyncType = br.ReadSByte();
                CollisionFilter = br.ReadBoolean();
                SetMainObjStructureBooleans = br.ReadBoolean();
                AnimIDs = br.ReadInt16s(4);
                ModelSfxParamRelativeIDs = br.ReadInt16s(4);
            }

            private protected override void ReadGparamConfig(BinaryReaderEx br)
            {
                Gparam = new GparamConfig(br);
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(CollisionIndex);
                bw.WriteSByte(BreakTerm);
                bw.WriteSByte(NetSyncType);
                bw.WriteBoolean(CollisionFilter);
                bw.WriteBoolean(SetMainObjStructureBooleans);
                bw.WriteInt16s(AnimIDs);
                bw.WriteInt16s(ModelSfxParamRelativeIDs);
            }

            private protected override void WriteGparamConfig(BinaryWriterEx bw)
            {
                Gparam.Write(bw);
            }

            internal override void GetNames(MSBB msb, Entries entries)
            {
                base.GetNames(msb, entries);
                CollisionName = MSB.FindName(entries.Parts, CollisionIndex);
            }

            internal override void GetIndices(MSBB msb, Entries entries)
            {
                base.GetIndices(msb, entries);
                CollisionIndex = MSB.FindIndex(entries.Parts, CollisionName);
            }
        }

        /// <summary>
        ///     A dynamic or interactible part of the map.
        /// </summary>
        public class Object : ObjectBase
        {

            /// <summary>
            ///     Creates an Object with default values.
            /// </summary>
            public Object()
            {
            }

            internal Object(BinaryReaderEx br) : base(br)
            {
            }

            private protected override PartType Type => PartType.Object;
        }

        /// <summary>
        ///     Common base data for enemies and dummy enemies.
        /// </summary>
        public abstract class EnemyBase : Part
        {
            private int CollisionIndex;
            private short[] MovePointIndices;

            private protected EnemyBase() : base("cXXXX_XXXX")
            {
                Gparam = new GparamConfig();
                ThinkParamID = -1;
                NPCParamID = -1;
                TalkID = -1;
                CharaInitID = -1;
                MovePointNames = new string[8];
            }

            private protected EnemyBase(BinaryReaderEx br) : base(br)
            {
            }

            private protected override bool HasTypeData => true;
            private protected override bool HasGparamConfig => true;
            private protected override bool HasSceneGparamConfig => false;

            /// <summary>
            ///     Gparam IDs for this enemy.
            /// </summary>
            public GparamConfig Gparam { get; set; }

            /// <summary>
            ///     ID in NPCThinkParam determining AI properties.
            /// </summary>
            public int ThinkParamID { get; set; }

            /// <summary>
            ///     ID in NPCParam determining character properties.
            /// </summary>
            public int NPCParamID { get; set; }

            /// <summary>
            ///     ID of a talk ESD used by the character.
            /// </summary>
            public int TalkID { get; set; }

            /// <summary>
            ///     ID in CharaInitParam determining equipment and stats for humans.
            /// </summary>
            public int CharaInitID { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public int UnkT18 { get; set; }

            /// <summary>
            ///     Collision that controls loading of the enemy.
            /// </summary>
            public string CollisionName { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public short UnkT20 { get; set; }

            /// <summary>
            ///     Regions for the enemy to patrol.
            /// </summary>
            public string[] MovePointNames { get; private set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public int InitAnimID { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public int DamageAnimID { get; set; }

            private protected override void DeepCopyTo(Part part)
            {
                var enemy = (EnemyBase)part;
                enemy.Gparam = Gparam.DeepCopy();
                enemy.MovePointNames = (string[])MovePointNames.Clone();
            }

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
                br.AssertInt32(0);
                br.AssertInt32(0);
                ThinkParamID = br.ReadInt32();
                NPCParamID = br.ReadInt32();
                TalkID = br.ReadInt32();
                CharaInitID = br.ReadInt32();
                UnkT18 = br.ReadInt32();
                CollisionIndex = br.ReadInt32();
                UnkT20 = br.ReadInt16();
                br.AssertInt16(0);
                br.AssertInt32(0);
                MovePointIndices = br.ReadInt16s(8);
                InitAnimID = br.ReadInt32();
                DamageAnimID = br.ReadInt32();
            }

            private protected override void ReadGparamConfig(BinaryReaderEx br)
            {
                Gparam = new GparamConfig(br);
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(ThinkParamID);
                bw.WriteInt32(NPCParamID);
                bw.WriteInt32(TalkID);
                bw.WriteInt32(CharaInitID);
                bw.WriteInt32(UnkT18);
                bw.WriteInt32(CollisionIndex);
                bw.WriteInt16(UnkT20);
                bw.WriteInt16(0);
                bw.WriteInt32(0);
                bw.WriteInt16s(MovePointIndices);
                bw.WriteInt32(InitAnimID);
                bw.WriteInt32(DamageAnimID);
            }

            private protected override void WriteGparamConfig(BinaryWriterEx bw)
            {
                Gparam.Write(bw);
            }

            internal override void GetNames(MSBB msb, Entries entries)
            {
                base.GetNames(msb, entries);
                CollisionName = MSB.FindName(entries.Parts, CollisionIndex);

                MovePointNames = new string[MovePointIndices.Length];
                for (var i = 0; i < MovePointIndices.Length; i++)
                    MovePointNames[i] = MSB.FindName(entries.Regions, MovePointIndices[i]);
            }

            internal override void GetIndices(MSBB msb, Entries entries)
            {
                base.GetIndices(msb, entries);
                CollisionIndex = MSB.FindIndex(entries.Parts, CollisionName);

                MovePointIndices = new short[MovePointNames.Length];
                for (var i = 0; i < MovePointNames.Length; i++)
                    MovePointIndices[i] = (short)MSB.FindIndex(entries.Regions, MovePointNames[i]);
            }
        }

        /// <summary>
        ///     Any living entity besides the player character.
        /// </summary>
        public class Enemy : EnemyBase
        {

            /// <summary>
            ///     Creates an Enemy with default values.
            /// </summary>
            public Enemy()
            {
            }

            internal Enemy(BinaryReaderEx br) : base(br)
            {
            }

            private protected override PartType Type => PartType.Enemy;
        }

        /// <summary>
        ///     Unknown exactly what these do.
        /// </summary>
        public class Player : Part
        {

            /// <summary>
            ///     Creates a Player with default values.
            /// </summary>
            public Player() : base("c0000_XXXX")
            {
            }

            internal Player(BinaryReaderEx br) : base(br)
            {
            }

            private protected override PartType Type => PartType.Player;
            private protected override bool HasTypeData => true;
            private protected override bool HasGparamConfig => false;
            private protected override bool HasSceneGparamConfig => false;

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
            }
        }

        /// <summary>
        ///     Invisible but physical geometry.
        /// </summary>
        public class Collision : Part
        {

            /// <summary>
            ///     Creates a Collision with default values.
            /// </summary>
            public Collision() : base("hXXXXXX_XXXX")
            {
                Gparam = new GparamConfig();
                SceneGparam = new SceneGparamConfig();
                MapNameID = -1;
                DisableBonfireEntityID = -1;
                LockCamParamID1 = -1;
                LockCamParamID2 = -1;
            }

            internal Collision(BinaryReaderEx br) : base(br)
            {
            }

            private protected override PartType Type => PartType.Collision;
            private protected override bool HasTypeData => true;
            private protected override bool HasGparamConfig => true;
            private protected override bool HasSceneGparamConfig => true;

            /// <summary>
            ///     Gparam IDs for this collision.
            /// </summary>
            public GparamConfig Gparam { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public SceneGparamConfig SceneGparam { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public byte HitFilterID { get; set; }

            /// <summary>
            ///     Causes sounds to be modulated when standing on the collision.
            /// </summary>
            public byte SoundSpaceType { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public short EnvLightMapSpotIndex { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public float ReflectPlaneHeight { get; set; }

            /// <summary>
            ///     Controls displays of the map name on screen or the loading menu.
            /// </summary>
            public short MapNameID { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public bool DisableStart { get; set; }

            /// <summary>
            ///     Unknown.
            /// </summary>
            public byte UnkT0B { get; set; }

            /// <summary>
            ///     If set, disables a bonfire when any enemy is on the collision.
            /// </summary>
            public int DisableBonfireEntityID { get; set; }

            /// <summary>
            ///     An ID used for multiplayer eligibility.
            /// </summary>
            public int PlayRegionID { get; set; }

            /// <summary>
            ///     ID in LockCamParam determining camera properties.
            /// </summary>
            public short LockCamParamID1 { get; set; }

            /// <summary>
            ///     ID in LockCamParam determining camera properties.
            /// </summary>
            public short LockCamParamID2 { get; set; }

            private protected override void DeepCopyTo(Part part)
            {
                var collision = (Collision)part;
                collision.Gparam = Gparam.DeepCopy();
                collision.SceneGparam = SceneGparam.DeepCopy();
            }

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
                HitFilterID = br.ReadByte();
                SoundSpaceType = br.ReadByte();
                EnvLightMapSpotIndex = br.ReadInt16();
                ReflectPlaneHeight = br.ReadSingle();
                MapNameID = br.ReadInt16();
                DisableStart = br.ReadBoolean();
                UnkT0B = br.ReadByte();
                DisableBonfireEntityID = br.ReadInt32();
                PlayRegionID = br.ReadInt32();
                LockCamParamID1 = br.ReadInt16();
                LockCamParamID2 = br.ReadInt16();
            }

            private protected override void ReadGparamConfig(BinaryReaderEx br)
            {
                Gparam = new GparamConfig(br);
            }

            private protected override void ReadSceneGparamConfig(BinaryReaderEx br)
            {
                SceneGparam = new SceneGparamConfig(br);
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
                bw.WriteByte(HitFilterID);
                bw.WriteByte(SoundSpaceType);
                bw.WriteInt16(EnvLightMapSpotIndex);
                bw.WriteSingle(ReflectPlaneHeight);
                bw.WriteInt16(MapNameID);
                bw.WriteBoolean(DisableStart);
                bw.WriteByte(UnkT0B);
                bw.WriteInt32(DisableBonfireEntityID);
                bw.WriteInt32(PlayRegionID);
                bw.WriteInt16(LockCamParamID1);
                bw.WriteInt16(LockCamParamID2);
            }

            private protected override void WriteGparamConfig(BinaryWriterEx bw)
            {
                Gparam.Write(bw);
            }

            private protected override void WriteSceneGparamConfig(BinaryWriterEx bw)
            {
                SceneGparam.Write(bw);
            }
        }

        /// <summary>
        ///     An AI navigation mesh.
        /// </summary>
        public class Navmesh : Part
        {

            /// <summary>
            ///     Creates a Navmesh with default values.
            /// </summary>
            public Navmesh() : base("nXXXXBX")
            {
            }

            internal Navmesh(BinaryReaderEx br) : base(br)
            {
            }

            private protected override PartType Type => PartType.Navmesh;
            private protected override bool HasTypeData => true;
            private protected override bool HasGparamConfig => false;
            private protected override bool HasSceneGparamConfig => false;

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
                br.AssertInt32(0);
                br.AssertInt32(0);
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(0);
            }
        }

        /// <summary>
        ///     A normally invisible object, either unused or for a cutscene.
        /// </summary>
        public class DummyObject : ObjectBase
        {

            /// <summary>
            ///     Creates a DummyObject with default values.
            /// </summary>
            public DummyObject()
            {
            }

            internal DummyObject(BinaryReaderEx br) : base(br)
            {
            }

            private protected override PartType Type => PartType.DummyObject;
        }

        /// <summary>
        ///     A normally invisible enemy, either unused or for a cutscene.
        /// </summary>
        public class DummyEnemy : EnemyBase
        {

            /// <summary>
            ///     Creates a DummyEnemy with default values.
            /// </summary>
            public DummyEnemy()
            {
            }

            internal DummyEnemy(BinaryReaderEx br) : base(br)
            {
            }

            private protected override PartType Type => PartType.DummyEnemy;
        }

        /// <summary>
        ///     Attaches to an actual Collision and causes another map to be loaded when standing on it.
        /// </summary>
        public class ConnectCollision : Part
        {
            private int CollisionIndex;

            /// <summary>
            ///     Creates a ConnectCollision with default values.
            /// </summary>
            public ConnectCollision() : base("hXXXXBX_XXXX")
            {
                MapID = new byte[4] { 10, 2, 0, 0 };
            }

            internal ConnectCollision(BinaryReaderEx br) : base(br)
            {
            }

            private protected override PartType Type => PartType.ConnectCollision;
            private protected override bool HasTypeData => true;
            private protected override bool HasGparamConfig => false;
            private protected override bool HasSceneGparamConfig => false;

            /// <summary>
            ///     The collision which will load another map.
            /// </summary>
            public string CollisionName { get; set; }

            /// <summary>
            ///     Four bytes specifying the map ID to load.
            /// </summary>
            public byte[] MapID { get; private set; }

            private protected override void DeepCopyTo(Part part)
            {
                var connect = (ConnectCollision)part;
                connect.MapID = (byte[])MapID.Clone();
            }

            private protected override void ReadTypeData(BinaryReaderEx br)
            {
                CollisionIndex = br.ReadInt32();
                MapID = br.ReadBytes(4);
                br.AssertInt32(0);
                br.AssertInt32(0);
            }

            private protected override void WriteTypeData(BinaryWriterEx bw)
            {
                bw.WriteInt32(CollisionIndex);
                bw.WriteBytes(MapID);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
            }

            internal override void GetNames(MSBB msb, Entries entries)
            {
                base.GetNames(msb, entries);
                CollisionName = MSB.FindName(msb.Parts.Collisions, CollisionIndex);
            }

            internal override void GetIndices(MSBB msb, Entries entries)
            {
                base.GetIndices(msb, entries);
                CollisionIndex = MSB.FindIndex(msb.Parts.Collisions, CollisionName);
            }
        }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public class Other : Part
        {

            /// <summary>
            ///     Creates an Other with default values.
            /// </summary>
            // TODO verify this
            public Other() : base("hXXXXBX_XXXX")
            {
            }

            internal Other(BinaryReaderEx br) : base(br)
            {
            }

            private protected override PartType Type => PartType.Other;
            private protected override bool HasTypeData => false;
            private protected override bool HasGparamConfig => false;
            private protected override bool HasSceneGparamConfig => false;
        }
    }
}