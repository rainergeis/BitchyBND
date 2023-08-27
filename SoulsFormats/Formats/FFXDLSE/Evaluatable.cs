using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SoulsFormats;

public partial class FFXDLSE
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    #region XmlInclude

    [XmlInclude(typeof(EvaluatableConstant))]
    [XmlInclude(typeof(Evaluatable2))]
    [XmlInclude(typeof(Evaluatable3))]
    [XmlInclude(typeof(EvaluatableCurrentTick))]
    [XmlInclude(typeof(EvaluatableTotalTick))]
    [XmlInclude(typeof(EvaluatableAnd))]
    [XmlInclude(typeof(EvaluatableOr))]
    [XmlInclude(typeof(EvaluatableGE))]
    [XmlInclude(typeof(EvaluatableGT))]
    [XmlInclude(typeof(EvaluatableLE))]
    [XmlInclude(typeof(EvaluatableLT))]
    [XmlInclude(typeof(EvaluatableEQ))]
    [XmlInclude(typeof(EvaluatableNE))]
    [XmlInclude(typeof(EvaluatableNot))]
    [XmlInclude(typeof(EvaluatableChildExists))]
    [XmlInclude(typeof(EvaluatableParentExists))]
    [XmlInclude(typeof(EvaluatableDistanceFromCamera))]
    [XmlInclude(typeof(EvaluatableEmittersStopped))]

    #endregion

    public abstract class Evaluatable : FXSerializable
    {

        public Evaluatable()
        {
        }

        internal Evaluatable(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override string ClassName => "FXSerializableEvaluatable<dl_int32>";

        internal override int Version => 1;

        internal abstract int Opcode { get; }

        internal abstract int Type { get; }

        protected internal override void Deserialize(BinaryReaderEx br, List<string> classNames)
        {
            br.AssertInt32(Opcode);
            br.AssertInt32(Type);
        }

        protected internal override void Serialize(BinaryWriterEx bw, List<string> classNames)
        {
            bw.WriteInt32(Opcode);
            bw.WriteInt32(Type);
        }

        internal static Evaluatable Read(BinaryReaderEx br, List<string> classNames)
        {
            // Don't @ me.
            var opcode = br.GetInt32(br.Position + 0xA);
            switch (opcode)
            {
                case 1: return new EvaluatableConstant(br, classNames);
                case 2: return new Evaluatable2(br, classNames);
                case 3: return new Evaluatable3(br, classNames);
                case 4: return new EvaluatableCurrentTick(br, classNames);
                case 5: return new EvaluatableTotalTick(br, classNames);
                case 8: return new EvaluatableAnd(br, classNames);
                case 9: return new EvaluatableOr(br, classNames);
                case 10: return new EvaluatableGE(br, classNames);
                case 11: return new EvaluatableGT(br, classNames);
                case 12: return new EvaluatableLE(br, classNames);
                case 13: return new EvaluatableLT(br, classNames);
                case 14: return new EvaluatableEQ(br, classNames);
                case 15: return new EvaluatableNE(br, classNames);
                case 20: return new EvaluatableNot(br, classNames);
                case 21: return new EvaluatableChildExists(br, classNames);
                case 22: return new EvaluatableParentExists(br, classNames);
                case 23: return new EvaluatableDistanceFromCamera(br, classNames);
                case 24: return new EvaluatableEmittersStopped(br, classNames);

                default:
                    throw new NotImplementedException($"Unimplemented evaluatable opcode: {opcode}");
            }
        }
    }

    public abstract class EvaluatableUnary : Evaluatable
    {

        public EvaluatableUnary()
        {
        }

        internal EvaluatableUnary(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Type => 1;

        public Evaluatable Operand { get; set; }

        protected internal override void Deserialize(BinaryReaderEx br, List<string> classNames)
        {
            base.Deserialize(br, classNames);
            Operand = Read(br, classNames);
        }

        internal override void AddClassNames(List<string> classNames)
        {
            base.AddClassNames(classNames);
            Operand.AddClassNames(classNames);
        }

        protected internal override void Serialize(BinaryWriterEx bw, List<string> classNames)
        {
            base.Serialize(bw, classNames);
            Operand.Write(bw, classNames);
        }
    }

    public abstract class EvaluatableBinary : Evaluatable
    {

        public EvaluatableBinary()
        {
        }

        internal EvaluatableBinary(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Type => 1;

        public Evaluatable Left { get; set; }

        public Evaluatable Right { get; set; }

        protected internal override void Deserialize(BinaryReaderEx br, List<string> classNames)
        {
            base.Deserialize(br, classNames);
            Right = Read(br, classNames);
            Left = Read(br, classNames);
        }

        internal override void AddClassNames(List<string> classNames)
        {
            base.AddClassNames(classNames);
            Left.AddClassNames(classNames);
            Right.AddClassNames(classNames);
        }

        protected internal override void Serialize(BinaryWriterEx bw, List<string> classNames)
        {
            base.Serialize(bw, classNames);
            Right.Write(bw, classNames);
            Left.Write(bw, classNames);
        }
    }

    public class EvaluatableConstant : Evaluatable
    {

        public EvaluatableConstant()
        {
        }

        internal EvaluatableConstant(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 1;

        internal override int Type => 3;

        [XmlAttribute] public int Value { get; set; }

        protected internal override void Deserialize(BinaryReaderEx br, List<string> classNames)
        {
            base.Deserialize(br, classNames);
            Value = br.ReadInt32();
        }

        protected internal override void Serialize(BinaryWriterEx bw, List<string> classNames)
        {
            base.Serialize(bw, classNames);
            bw.WriteInt32(Value);
        }

        public override string ToString()
        {
            return $"{Value}";
        }
    }

    public class Evaluatable2 : Evaluatable
    {

        public Evaluatable2()
        {
        }

        internal Evaluatable2(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 2;

        internal override int Type => 3;

        [XmlAttribute] public int Unk00 { get; set; }

        [XmlAttribute] public int ArgIndex { get; set; }

        protected internal override void Deserialize(BinaryReaderEx br, List<string> classNames)
        {
            base.Deserialize(br, classNames);
            Unk00 = br.ReadInt32();
            ArgIndex = br.ReadInt32();
        }

        protected internal override void Serialize(BinaryWriterEx bw, List<string> classNames)
        {
            base.Serialize(bw, classNames);
            bw.WriteInt32(Unk00);
            bw.WriteInt32(ArgIndex);
        }

        public override string ToString()
        {
            return $"{{2: {Unk00}, {ArgIndex}}}";
        }
    }

    public class Evaluatable3 : Evaluatable
    {

        public Evaluatable3()
        {
        }

        internal Evaluatable3(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 3;

        internal override int Type => 3;

        [XmlAttribute] public int Unk00 { get; set; }

        [XmlAttribute] public int ArgIndex { get; set; }

        protected internal override void Deserialize(BinaryReaderEx br, List<string> classNames)
        {
            base.Deserialize(br, classNames);
            Unk00 = br.ReadInt32();
            ArgIndex = br.ReadInt32();
        }

        protected internal override void Serialize(BinaryWriterEx bw, List<string> classNames)
        {
            base.Serialize(bw, classNames);
            bw.WriteInt32(Unk00);
            bw.WriteInt32(ArgIndex);
        }

        public override string ToString()
        {
            return $"{{3: {Unk00}, {ArgIndex}}}";
        }
    }

    public class EvaluatableCurrentTick : Evaluatable
    {

        public EvaluatableCurrentTick()
        {
        }

        internal EvaluatableCurrentTick(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 4;

        internal override int Type => 3;

        public override string ToString()
        {
            return "CurrentTick";
        }
    }

    public class EvaluatableTotalTick : Evaluatable
    {

        public EvaluatableTotalTick()
        {
        }

        internal EvaluatableTotalTick(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 5;

        internal override int Type => 3;

        public override string ToString()
        {
            return "TotalTick";
        }
    }

    public class EvaluatableAnd : EvaluatableBinary
    {

        public EvaluatableAnd()
        {
        }

        internal EvaluatableAnd(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 8;

        public override string ToString()
        {
            return $"({Left}) && ({Right})";
        }
    }

    public class EvaluatableOr : EvaluatableBinary
    {

        public EvaluatableOr()
        {
        }

        internal EvaluatableOr(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 9;

        public override string ToString()
        {
            return $"({Left}) || ({Right})";
        }
    }

    public class EvaluatableGE : EvaluatableBinary
    {

        public EvaluatableGE()
        {
        }

        internal EvaluatableGE(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 10;

        public override string ToString()
        {
            return $"({Left}) >= ({Right})";
        }
    }

    public class EvaluatableGT : EvaluatableBinary
    {

        public EvaluatableGT()
        {
        }

        internal EvaluatableGT(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 11;

        public override string ToString()
        {
            return $"({Left}) > ({Right})";
        }
    }

    public class EvaluatableLE : EvaluatableBinary
    {

        public EvaluatableLE()
        {
        }

        internal EvaluatableLE(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 12;

        public override string ToString()
        {
            return $"({Left}) <= ({Right})";
        }
    }

    public class EvaluatableLT : EvaluatableBinary
    {

        public EvaluatableLT()
        {
        }

        internal EvaluatableLT(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 13;

        public override string ToString()
        {
            return $"({Left}) < ({Right})";
        }
    }

    public class EvaluatableEQ : EvaluatableBinary
    {

        public EvaluatableEQ()
        {
        }

        internal EvaluatableEQ(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 14;

        public override string ToString()
        {
            return $"({Left}) == ({Right})";
        }
    }

    public class EvaluatableNE : EvaluatableBinary
    {

        public EvaluatableNE()
        {
        }

        internal EvaluatableNE(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 15;

        public override string ToString()
        {
            return $"({Left}) != ({Right})";
        }
    }

    public class EvaluatableNot : EvaluatableUnary
    {

        public EvaluatableNot()
        {
        }

        internal EvaluatableNot(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 20;

        public override string ToString()
        {
            return $"!({Operand})";
        }
    }

    public class EvaluatableChildExists : Evaluatable
    {

        public EvaluatableChildExists()
        {
        }

        internal EvaluatableChildExists(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 21;

        internal override int Type => 3;

        public override string ToString()
        {
            return "ChildExists";
        }
    }

    public class EvaluatableParentExists : Evaluatable
    {

        public EvaluatableParentExists()
        {
        }

        internal EvaluatableParentExists(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 22;

        internal override int Type => 3;

        public override string ToString()
        {
            return "ParentExists";
        }
    }

    public class EvaluatableDistanceFromCamera : Evaluatable
    {

        public EvaluatableDistanceFromCamera()
        {
        }

        internal EvaluatableDistanceFromCamera(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 23;

        internal override int Type => 3;

        public override string ToString()
        {
            return "DistanceFromCamera";
        }
    }

    public class EvaluatableEmittersStopped : Evaluatable
    {

        public EvaluatableEmittersStopped()
        {
        }

        internal EvaluatableEmittersStopped(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override int Opcode => 24;

        internal override int Type => 3;

        public override string ToString()
        {
            return "EmittersStopped";
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}