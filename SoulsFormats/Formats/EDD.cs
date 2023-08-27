using System;
using System.Collections.Generic;

namespace SoulsFormats;

/// <summary>
///     A description format for ESDs, published only for DS2. It is not read by the game.
/// </summary>
public class EDD : SoulsFile<EDD>
{

    /// <summary>
    ///     Creates a new EDD with no data.
    /// </summary>
    public EDD()
    {
        LongFormat = false;
        FunctionSpecs = new List<FunctionSpec>();
        CommandSpecs = new List<CommandSpec>();
        Machines = new List<MachineDesc>();
        UnkB0 = new int[4];
    }

    /// <summary>
    ///     Whether the EDD is in 64-bit or 32-bit format.
    /// </summary>
    public bool LongFormat { get; set; }

    /// <summary>
    ///     Descriptions of built-in functions which can be used in the ESD file.
    /// </summary>
    public List<FunctionSpec> FunctionSpecs { get; set; }

    /// <summary>
    ///     Descriptions of built-in commands which can be used in the ESD file.
    /// </summary>
    public List<CommandSpec> CommandSpecs { get; set; }

    /// <summary>
    ///     Descriptions of machines and states defined in the ESD file.
    /// </summary>
    public List<MachineDesc> Machines { get; set; }

    /// <summary>
    ///     Unknown.
    /// </summary>
    public int Unk80 { get; set; }

    /// <summary>
    ///     Unknown.
    /// </summary>
    public int[] UnkB0 { get; private set; }

    /// <summary>
    ///     Deserializes file data from a stream.
    /// </summary>
    protected override void Read(BinaryReaderEx br)
    {
        br.BigEndian = false;

        var magic = br.AssertASCII("fSSL", "fsSL");
        LongFormat = magic == "fsSL";
        br.VarintLong = LongFormat;

        br.AssertInt32(1);
        br.AssertInt32(1);
        br.AssertInt32(1);
        br.AssertInt32(0x7C);
        var dataSize = br.ReadInt32();
        br.AssertInt32(11);

        br.AssertInt32(LongFormat ? 0x58 : 0x34);
        br.AssertInt32(1);
        br.AssertInt32(LongFormat ? 0x10 : 8);
        var stringCount = br.ReadInt32();
        br.AssertInt32(4);
        br.AssertInt32(0);
        br.AssertInt32(8);
        var functionSpecCount = br.ReadInt32();
        var conditionSize = br.AssertInt32(LongFormat ? 0x10 : 8);
        var conditionCount = br.ReadInt32();
        br.AssertInt32(LongFormat ? 0x10 : 8);
        br.AssertInt32(0);
        br.AssertInt32(LongFormat ? 0x18 : 0x10);
        var commandSpecCount = br.ReadInt32();
        var commandSize = br.AssertInt32(4);
        var commandCount = br.ReadInt32();
        var passCommandSize = br.AssertInt32(LongFormat ? 0x10 : 8);
        var passCommandCount = br.ReadInt32();
        var stateSize = br.AssertInt32(LongFormat ? 0x78 : 0x3C);
        var stateCount = br.ReadInt32();
        br.AssertInt32(LongFormat ? 0x48 : 0x30);
        var machineCount = br.ReadInt32();

        var stringsOffset = br.ReadInt32();
        br.AssertInt32(0);
        br.AssertInt32(stringsOffset);
        Unk80 = br.ReadInt32();
        br.AssertInt32(dataSize);
        br.AssertInt32(0);
        br.AssertInt32(dataSize);
        br.AssertInt32(0);

        var dataStart = br.Position;
        br.AssertVarint(0);
        var commandSpecOffset = br.ReadVarint();
        br.AssertVarint(commandSpecCount);
        var functionSpecOffset = br.ReadVarint();
        br.AssertVarint(functionSpecCount);
        var machineOffset = br.ReadVarint();
        br.AssertInt32(machineCount);
        UnkB0 = br.ReadInt32s(4);
        if (LongFormat) br.AssertInt32(0);
        br.AssertVarint(LongFormat ? 0x58 : 0x34);
        br.AssertVarint(stringCount);

        var strings = new List<string>();
        for (var i = 0; i < stringCount; i++)
        {
            var stringOffset = br.ReadVarint();
            // Char count not needed as all strings are null-terminated
            br.ReadVarint();
            var str = br.GetUTF16(dataStart + stringOffset);
            strings.Add(str);
        }

        FunctionSpecs = new List<FunctionSpec>();
        for (var i = 0; i < functionSpecCount; i++) FunctionSpecs.Add(new FunctionSpec(br, strings));

        var conditions = new Dictionary<long, ConditionDesc>();
        for (var i = 0; i < conditionCount; i++)
        {
            var offset = br.Position - dataStart;
            conditions[offset] = new ConditionDesc(br);
        }

        CommandSpecs = new List<CommandSpec>();
        for (var i = 0; i < commandSpecCount; i++) CommandSpecs.Add(new CommandSpec(br, strings));

        var commands = new Dictionary<long, CommandDesc>();
        for (var i = 0; i < commandCount; i++)
        {
            var offset = br.Position - dataStart;
            commands[offset] = new CommandDesc(br, strings);
        }

        if (LongFormat)
        {
            // Data-start-aligned padding.
            var offset = br.Position - dataStart;
            if (offset % 8 > 0) br.Skip(8 - (int)(offset % 8));
        }

        var passCommands = new Dictionary<long, PassCommandDesc>();
        for (var i = 0; i < passCommandCount; i++)
        {
            var offset = br.Position - dataStart;
            passCommands[offset] = new PassCommandDesc(br, commands, commandSize);
        }

        var states = new Dictionary<long, StateDesc>();
        for (var i = 0; i < stateCount; i++)
        {
            var offset = br.Position - dataStart;
            states[offset] = new StateDesc(br, strings, dataStart, conditions, conditionSize, commands, commandSize,
                passCommands, passCommandSize);
        }

        Machines = new List<MachineDesc>();
        for (var i = 0; i < machineCount; i++) Machines.Add(new MachineDesc(br, strings, states, stateSize));

        if (conditions.Count > 0 || commands.Count > 0 || passCommands.Count > 0 || states.Count > 0)
            throw new FormatException("Orphaned ESD descriptions found");
    }

    private static List<T> GetUniqueOffsetList<T>(long offset, long count, Dictionary<long, T> offsets, int objSize)
    {
        var objs = new List<T>();
        for (var i = 0; i < count; i++)
        {
            if (!offsets.ContainsKey(offset))
                throw new FormatException($"Nonexistent or reused {typeof(T)} at index {i}/{count}, offset {offset}");
            objs.Add(offsets[offset]);
            offsets.Remove(offset);
            offset += objSize;
        }

        return objs;
    }

    /// <summary>
    ///     A description of a built-in function in this type of ESD.
    /// </summary>
    public class FunctionSpec
    {

        /// <summary>
        ///     Creates a function spec with the given function ID and description, or defaults if not provided.
        /// </summary>
        public FunctionSpec(int id = 0, string name = null)
        {
            ID = id;
            Name = name;
        }

        internal FunctionSpec(BinaryReaderEx br, List<string> strings)
        {
            ID = br.ReadInt32();
            var nameIndex = br.ReadInt16();
            Unk06 = br.ReadByte();
            Unk07 = br.ReadByte();

            Name = strings[nameIndex];
        }

        /// <summary>
        ///     ID used in ESD to call the function.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        ///     Description of the function.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte Unk06 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte Unk07 { get; set; }
    }

    /// <summary>
    ///     A data structure associated with conditions. It has no data in DS2.
    /// </summary>
    public class ConditionDesc
    {
        /// <summary>
        ///     Creates a condition with no data.
        /// </summary>
        public ConditionDesc()
        {
        }

        internal ConditionDesc(BinaryReaderEx br)
        {
            br.AssertVarint(-1);
            br.AssertVarint(0);
        }
    }

    /// <summary>
    ///     A description of a built-in command in this type of ESD.
    /// </summary>
    public class CommandSpec
    {

        /// <summary>
        ///     Creates a command spec with the given command ID and description, or defaults if not provided.
        /// </summary>
        public CommandSpec(long id = 0, string name = null)
        {
            ID = id;
            Name = name;
        }

        internal CommandSpec(BinaryReaderEx br, List<string> strings)
        {
            ID = br.ReadVarint();
            br.AssertVarint(-1);
            br.AssertInt32(0);
            var nameIndex = br.ReadInt16();
            Unk0E = br.ReadInt16();

            Name = strings[nameIndex];
        }

        /// <summary>
        ///     ID used in ESD to call the command.
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        ///     Description of the command.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public short Unk0E { get; set; }
    }

    /// <summary>
    ///     A description of a command used in a state of the ESD.
    /// </summary>
    public class CommandDesc
    {

        /// <summary>
        ///     Creates a command description with the given name, or default if not provided.
        /// </summary>
        public CommandDesc(string name = null)
        {
            Name = name;
        }

        internal CommandDesc(BinaryReaderEx br, List<string> strings)
        {
            var nameIndex = br.ReadInt16();
            br.AssertByte(1);
            br.AssertByte(0xFF);

            Name = strings[nameIndex];
        }

        /// <summary>
        ///     Description text. This often matches the command specification text, but is sometimes overridden.
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    ///     A description of commands in the pass command block of a condition. This appears to
    ///     ignore the pass block if only contains the 'return' command, bank 7 id -1, so this
    ///     annotation is uncommon in DS2.
    /// </summary>
    public class PassCommandDesc
    {

        /// <summary>
        ///     Creates a new empty pass command description.
        /// </summary>
        public PassCommandDesc()
        {
            PassCommands = new List<CommandDesc>();
        }

        internal PassCommandDesc(BinaryReaderEx br, Dictionary<long, CommandDesc> commands, int commandSize)
        {
            var commandOffset = br.ReadInt32();
            var commandCount = br.ReadInt32();
            PassCommands = GetUniqueOffsetList(commandOffset, commandCount, commands, commandSize);
        }

        /// <summary>
        ///     Descriptions for the commands in the pass block.
        /// </summary>
        public List<CommandDesc> PassCommands { get; set; }
    }

    /// <summary>
    ///     A description of a state defined in the ESD.
    /// </summary>
    public class StateDesc
    {

        /// <summary>
        ///     Creates a new state description with the given id and name, or defaults if not provided.
        /// </summary>
        public StateDesc(long id = 0, string name = null)
        {
            ID = id;
            Name = name;
            EntryCommands = new List<CommandDesc>();
            ExitCommands = new List<CommandDesc>();
            WhileCommands = new List<CommandDesc>();
            PassCommands = new List<PassCommandDesc>();
            Conditions = new List<ConditionDesc>();
        }

        internal StateDesc(
            BinaryReaderEx br, List<string> strings, long dataStart,
            Dictionary<long, ConditionDesc> conditions, int conditionSize,
            Dictionary<long, CommandDesc> commands, int commandSize,
            Dictionary<long, PassCommandDesc> passCommands, int passCommandSize)
        {
            ID = br.ReadVarint();
            var nameIndexOffset = br.ReadVarint();
            br.AssertVarint(1);
            var entryCommandOffset = br.ReadVarint();
            var entryCommandCount = br.ReadVarint();
            var exitCommandOffset = br.ReadVarint();
            var exitCommandCount = br.ReadVarint();
            var whileCommandOffset = br.ReadVarint();
            var whileCommandCount = br.ReadVarint();
            var passCommandOffset = br.ReadVarint();
            var passCommandCount = br.ReadVarint();
            var conditionOffset = br.ReadVarint();
            var conditionCount = br.ReadVarint();
            br.AssertVarint(-1);
            br.AssertVarint(0);

            var nameIndex = br.GetInt16(dataStart + nameIndexOffset);
            Name = strings[nameIndex];
            EntryCommands = GetUniqueOffsetList(entryCommandOffset, entryCommandCount, commands, commandSize);
            ExitCommands = GetUniqueOffsetList(exitCommandOffset, exitCommandCount, commands, commandSize);
            WhileCommands = GetUniqueOffsetList(whileCommandOffset, whileCommandCount, commands, commandSize);
            PassCommands = GetUniqueOffsetList(passCommandOffset, passCommandCount, passCommands, passCommandSize);
            Conditions = GetUniqueOffsetList(conditionOffset, conditionCount, conditions, conditionSize);
        }

        /// <summary>
        ///     ID of the state.
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        ///     Description text.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Descriptions for commands in the entry block.
        /// </summary>
        public List<CommandDesc> EntryCommands { get; set; }

        /// <summary>
        ///     Descriptions for commands in the exit block.
        /// </summary>
        public List<CommandDesc> ExitCommands { get; set; }

        /// <summary>
        ///     Descriptions for commands in the while block.
        /// </summary>
        public List<CommandDesc> WhileCommands { get; set; }

        /// <summary>
        ///     Descriptions for commands in conditions' pass blocks when nontrivial.
        /// </summary>
        public List<PassCommandDesc> PassCommands { get; set; }

        /// <summary>
        ///     Descriptions for conditions. Doesn't contain anything interesting.
        /// </summary>
        public List<ConditionDesc> Conditions { get; set; }
    }

    /// <summary>
    ///     A description of a machine defined in the ESD.
    /// </summary>
    public class MachineDesc
    {

        /// <summary>
        ///     Creates a new machine description with the given id and name, or defaults if not provided.
        /// </summary>
        public MachineDesc(int id = 0, string name = null)
        {
            ID = id;
            Name = name;
            ParamNames = new string[8];
            States = new List<StateDesc>();
        }

        internal MachineDesc(BinaryReaderEx br, List<string> strings, Dictionary<long, StateDesc> states, int stateSize)
        {
            ID = br.ReadInt32();
            var nameIndex = br.ReadInt16();
            Unk06 = br.ReadInt16();
            var paramIndices = br.ReadInt16s(8);
            br.AssertVarint(-1);
            br.AssertVarint(0);
            br.AssertVarint(-1);
            br.AssertVarint(0);
            var stateOffset = br.ReadVarint();
            var stateCount = br.ReadVarint();
            States = GetUniqueOffsetList(stateOffset, stateCount, states, stateSize);

            Name = strings[nameIndex];
            ParamNames = new string[8];
            for (var i = 0; i < 8; i++)
                if (paramIndices[i] >= 0)
                    ParamNames[i] = strings[paramIndices[i]];
        }

        /// <summary>
        ///     ID of the machine.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        ///     Text description.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public short Unk06 { get; set; }

        /// <summary>
        ///     Text description of params to the machine, when it is callable by other machines.
        /// </summary>
        public string[] ParamNames { get; }

        /// <summary>
        ///     Descriptions of the machine's states.
        /// </summary>
        public List<StateDesc> States { get; set; }
    }
}