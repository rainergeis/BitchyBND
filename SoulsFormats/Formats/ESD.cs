using System;
using System.Collections.Generic;
using System.Linq;

namespace SoulsFormats;

/// <summary>
///     A state machine used for gameplay, menus, and dialog throughout the series.
/// </summary>
public class ESD : SoulsFile<ESD>
{

    /// <summary>
    ///     1 for DS1/DSR, 2 for DS2/SotFS/BB, 3 for DS3
    /// </summary>
    public int DarkSoulsCount;

    /// <summary>
    ///     If true, write in 64-bit format; if false, write in 32-bit format.
    /// </summary>
    public bool LongFormat;

    /// <summary>
    ///     Name and/or brief description of the file, or null if not present.
    /// </summary>
    public string Name;

    /// <summary>
    ///     State groups indexed by their ID, containing individual states indexed by their IDs.
    /// </summary>
    public Dictionary<long, Dictionary<long, State>> StateGroups;

    /// <summary>
    ///     Unknown; not bytecode, not floats, not text. Perhaps a hash of something, but if so it isn't checked.
    /// </summary>
    public int Unk70, Unk74, Unk78, Unk7C;

    /// <summary>
    ///     Creates a new ESD formatted for DS1 with no state groups.
    /// </summary>
    public ESD() : this(false, 1)
    {
    }

    /// <summary>
    ///     Creates a new ESD with the given format and no state groups.
    /// </summary>
    public ESD(bool longFormat, int darkSoulsCount)
    {
        LongFormat = longFormat;
        DarkSoulsCount = darkSoulsCount;
        Name = null;
        Unk70 = 0;
        Unk74 = 0;
        Unk78 = 0;
        Unk7C = 0;
        StateGroups = new Dictionary<long, Dictionary<long, State>>();
    }

    /// <summary>
    ///     Checks whether the data appears to be a file of this format.
    /// </summary>
    protected override bool Is(BinaryReaderEx br)
    {
        if (br.Length < 4)
            return false;

        var magic = br.GetASCII(0, 4);
        return magic == "fSSL" || magic == "fsSL";
    }

    /// <summary>
    ///     Deserializes file data from a stream.
    /// </summary>
    protected override void Read(BinaryReaderEx br)
    {
        br.BigEndian = false;

        var magic = br.AssertASCII("fSSL", "fsSL");
        LongFormat = magic == "fsSL";

        br.AssertInt32(1);
        DarkSoulsCount = br.AssertInt32(1, 2, 3);
        br.AssertInt32(DarkSoulsCount);
        br.AssertInt32(0x54);
        var dataSize = br.ReadInt32();
        br.AssertInt32(6);
        br.AssertInt32(LongFormat ? 0x48 : 0x2C);
        br.AssertInt32(1);
        var stateGroupSize = br.AssertInt32(LongFormat ? 0x20 : 0x10);
        var stateGroupCount = br.ReadInt32();
        var stateSize = br.AssertInt32(LongFormat ? 0x48 : 0x24);
        var stateCount = br.ReadInt32();
        br.AssertInt32(LongFormat ? 0x38 : 0x1C);
        var conditionCount = br.ReadInt32();
        br.AssertInt32(LongFormat ? 0x18 : 0x10);
        var commandCallCount = br.ReadInt32();
        br.AssertInt32(LongFormat ? 0x10 : 0x8);
        var commandArgCount = br.ReadInt32();
        var conditionOffsetsOffset = br.ReadInt32();
        var conditionOffsetsCount = br.ReadInt32();
        var nameBlockOffset = br.ReadInt32();
        var nameLength = br.ReadInt32();
        var unkOffset1 = br.ReadInt32();
        br.AssertInt32(0);
        var unkOffset2 = br.ReadInt32();
        br.AssertInt32(0);

        var dataStart = br.Position;
        br.AssertInt32(1);
        Unk70 = br.ReadInt32();
        Unk74 = br.ReadInt32();
        Unk78 = br.ReadInt32();
        Unk7C = br.ReadInt32();
        if (LongFormat)
            br.AssertInt32(0);

        var stateGroupsOffset = ReadVarint(br, LongFormat);
        AssertVarint(br, LongFormat, stateGroupCount);
        var nameOffset = ReadVarint(br, LongFormat);
        AssertVarint(br, LongFormat, nameLength);
        long unkNull = DarkSoulsCount == 1 ? 0 : -1;
        AssertVarint(br, LongFormat, unkNull);
        AssertVarint(br, LongFormat, unkNull);

        if (nameLength > 0)
            Name = br.GetUTF16(dataStart + nameOffset);
        else
            Name = null;

        var stateGroupOffsets = new Dictionary<long, long[]>(stateGroupCount);
        for (var i = 0; i < stateGroupCount; i++)
        {
            var id = ReadVarint(br, LongFormat);
            var stateOffsets = ReadStateGroup(br, LongFormat, dataStart, stateSize);
            if (stateGroupOffsets.ContainsKey(id))
                throw new FormatException("Duplicate state group ID.");
            stateGroupOffsets[id] = stateOffsets;
        }

        var states = new Dictionary<long, State>(stateCount);
        for (var i = 0; i < stateCount; i++)
            states[br.Position - dataStart] = new State(br, LongFormat, dataStart);

        var conditions = new Dictionary<long, Condition>(conditionCount);
        for (var i = 0; i < conditionCount; i++)
            conditions[br.Position - dataStart] = new Condition(br, LongFormat, dataStart);

        foreach (State state in states.Values)
            state.GetConditions(conditions);

        StateGroups = new Dictionary<long, Dictionary<long, State>>(stateGroupCount);
        var groupedStateOffsets = new Dictionary<long, Dictionary<long, long>>();
        foreach (var stateGroupID in stateGroupOffsets.Keys)
        {
            var stateOffsets = stateGroupOffsets[stateGroupID];
            Dictionary<long, State> stateGroup =
                TakeStates(stateSize, stateOffsets, states, out Dictionary<long, long> stateIDs);
            StateGroups[stateGroupID] = stateGroup;
            groupedStateOffsets[stateGroupID] = stateIDs;

            foreach (State state in stateGroup.Values)
            foreach (Condition condition in state.Conditions)
                condition.GetStateAndConditions(stateIDs, conditions);
        }

        if (states.Count > 0)
            throw new FormatException("Orphaned states found.");
    }

    /// <summary>
    ///     Serializes file data to a stream.
    /// </summary>
    protected override void Write(BinaryWriterEx bw)
    {
        bw.BigEndian = false;

        bw.WriteASCII(LongFormat ? "fsSL" : "fSSL");
        bw.WriteInt32(1);
        bw.WriteInt32(DarkSoulsCount);
        bw.WriteInt32(DarkSoulsCount);
        bw.WriteInt32(0x54);
        bw.ReserveInt32("DataSize");
        bw.WriteInt32(6);
        bw.WriteInt32(LongFormat ? 0x48 : 0x2C);
        bw.WriteInt32(1);
        bw.WriteInt32(LongFormat ? 0x20 : 0x10);
        bw.WriteInt32(StateGroups.Count);
        var stateSize = LongFormat ? 0x48 : 0x24;
        bw.WriteInt32(stateSize);
        bw.WriteInt32(StateGroups.Values.Sum(sg => sg.Count + (sg.Count == 1 ? 0 : 1)));
        bw.WriteInt32(LongFormat ? 0x38 : 0x1C);
        bw.ReserveInt32("ConditionCount");
        bw.WriteInt32(LongFormat ? 0x18 : 0x10);
        bw.ReserveInt32("CommandCallCount");
        bw.WriteInt32(LongFormat ? 0x10 : 0x8);
        bw.ReserveInt32("CommandArgCount");
        bw.ReserveInt32("ConditionOffsetsOffset");
        bw.ReserveInt32("ConditionOffsetsCount");
        bw.ReserveInt32("NameBlockOffset");
        bw.WriteInt32(Name == null ? 0 : Name.Length + 1);
        bw.ReserveInt32("UnkOffset1");
        bw.WriteInt32(0);
        bw.ReserveInt32("UnkOffset2");
        bw.WriteInt32(0);

        var dataStart = bw.Position;
        bw.WriteInt32(1);
        bw.WriteInt32(Unk70);
        bw.WriteInt32(Unk74);
        bw.WriteInt32(Unk78);
        bw.WriteInt32(Unk7C);
        if (LongFormat)
            bw.WriteInt32(0);

        ReserveVarint(bw, LongFormat, "StateGroupsOffset");
        WriteVarint(bw, LongFormat, StateGroups.Count);
        ReserveVarint(bw, LongFormat, "NameOffset");
        WriteVarint(bw, LongFormat, Name == null ? 0 : Name.Length + 1);
        long unkNull = DarkSoulsCount == 1 ? 0 : -1;
        WriteVarint(bw, LongFormat, unkNull);
        WriteVarint(bw, LongFormat, unkNull);

        // Collect and sort all the IDs so everything is definitely in the same order everywhere
        var stateGroupIDs = StateGroups.Keys.ToList();
        stateGroupIDs.Sort();
        var stateIDs = new Dictionary<long, List<long>>();
        foreach (var groupID in stateGroupIDs)
        {
            stateIDs[groupID] = StateGroups[groupID].Keys.ToList();
            stateIDs[groupID].Sort();
        }

        if (StateGroups.Count == 0)
        {
            FillVarint(bw, LongFormat, "StateGroupsOffset", -1);
        }
        else
        {
            FillVarint(bw, LongFormat, "StateGroupsOffset", bw.Position - dataStart);
            foreach (var groupID in stateGroupIDs)
            {
                WriteVarint(bw, LongFormat, groupID);
                ReserveVarint(bw, LongFormat, $"StateGroup{groupID}:StatesOffset1");
                WriteVarint(bw, LongFormat, StateGroups[groupID].Count);
                ReserveVarint(bw, LongFormat, $"StateGroup{groupID}:StatesOffset2");
            }
        }

        var stateOffsets = new Dictionary<long, Dictionary<long, long>>();
        var weirdStateOffsets = new List<long[]>();
        foreach (var groupID in stateGroupIDs)
        {
            stateOffsets[groupID] = new Dictionary<long, long>();
            FillVarint(bw, LongFormat, $"StateGroup{groupID}:StatesOffset1", bw.Position - dataStart);
            FillVarint(bw, LongFormat, $"StateGroup{groupID}:StatesOffset2", bw.Position - dataStart);
            var firstStateOffset = bw.Position;
            foreach (var stateID in stateIDs[groupID])
            {
                stateOffsets[groupID][stateID] = bw.Position - dataStart;
                StateGroups[groupID][stateID].WriteHeader(bw, LongFormat, groupID, stateID);
            }

            if (StateGroups[groupID].Count > 1)
            {
                weirdStateOffsets.Add(new[] { firstStateOffset, bw.Position });
                bw.Position += stateSize;
            }
        }

        // Make a list of every unique condition
        var conditions = new Dictionary<long, List<Condition>>();
        foreach (var groupID in stateGroupIDs)
        {
            conditions[groupID] = new List<Condition>();

            void addCondition(Condition cond)
            {
                if (!conditions[groupID].Any(c => ReferenceEquals(cond, c)))
                {
                    conditions[groupID].Add(cond);
                    foreach (Condition subCond in cond.Subconditions)
                        addCondition(subCond);
                }
            }

            foreach (State state in StateGroups[groupID].Values)
            foreach (Condition cond in state.Conditions)
                addCondition(cond);
        }

        bw.FillInt32("ConditionCount", conditions.Values.Sum(group => group.Count));

        // Yes, I do in fact want this to be keyed by reference
        var conditionOffsets = new Dictionary<Condition, long>();
        foreach (var groupID in stateGroupIDs)
            for (var i = 0; i < conditions[groupID].Count; i++)
            {
                Condition cond = conditions[groupID][i];
                conditionOffsets[cond] = bw.Position - dataStart;
                cond.WriteHeader(bw, LongFormat, groupID, i, stateOffsets[groupID]);
            }

        var commands = new List<CommandCall>();
        foreach (var groupID in stateGroupIDs)
        {
            foreach (var stateID in stateIDs[groupID])
                StateGroups[groupID][stateID].WriteCommandCalls(bw, LongFormat, groupID, stateID, dataStart, commands);
            for (var i = 0; i < conditions[groupID].Count; i++)
                conditions[groupID][i].WriteCommandCalls(bw, LongFormat, groupID, i, dataStart, commands);
        }

        bw.FillInt32("CommandCallCount", commands.Count);
        bw.FillInt32("CommandArgCount", commands.Sum(command => command.Arguments.Count));

        for (var i = 0; i < commands.Count; i++) commands[i].WriteArgs(bw, LongFormat, i, dataStart);

        bw.FillInt32("ConditionOffsetsOffset", (int)(bw.Position - dataStart));
        var conditionOffsetsCount = 0;
        foreach (var groupID in stateGroupIDs)
        {
            foreach (var stateID in stateIDs[groupID])
                conditionOffsetsCount += StateGroups[groupID][stateID]
                    .WriteConditionOffsets(bw, LongFormat, groupID, stateID, dataStart, conditionOffsets);
            for (var i = 0; i < conditions[groupID].Count; i++)
                conditionOffsetsCount += conditions[groupID][i]
                    .WriteConditionOffsets(bw, LongFormat, groupID, i, dataStart, conditionOffsets);
        }

        bw.FillInt32("ConditionOffsetsCount", conditionOffsetsCount);

        foreach (var groupID in stateGroupIDs)
            for (var i = 0; i < conditions[groupID].Count; i++)
                conditions[groupID][i].WriteEvaluator(bw, LongFormat, groupID, i, dataStart);
        for (var i = 0; i < commands.Count; i++) commands[i].WriteBytecode(bw, LongFormat, i, dataStart);

        bw.FillInt32("NameBlockOffset", (int)(bw.Position - dataStart));
        if (Name == null)
        {
            FillVarint(bw, LongFormat, "NameOffset", -1);
        }
        else
        {
            bw.Pad(2);
            FillVarint(bw, LongFormat, "NameOffset", bw.Position - dataStart);
            bw.WriteUTF16(Name, true);
        }

        bw.FillInt32("UnkOffset1", (int)(bw.Position - dataStart));
        bw.FillInt32("UnkOffset2", (int)(bw.Position - dataStart));
        bw.FillInt32("DataSize", (int)(bw.Position - dataStart));

        if (DarkSoulsCount == 1)
            bw.Pad(4);
        else if (DarkSoulsCount == 2)
            bw.Pad(0x10);

        foreach (var offsets in weirdStateOffsets)
        {
            bw.Position = offsets[0];
            var bytes = new byte[stateSize];
            bw.Stream.Read(bytes, 0, stateSize);
            bw.Position = offsets[1];
            bw.WriteBytes(bytes);
        }
    }

    private long[] ReadStateGroup(BinaryReaderEx br, bool longFormat, long dataStart, long stateSize)
    {
        var statesOffset = ReadVarint(br, longFormat);
        var stateCount = ReadVarint(br, longFormat);
        AssertVarint(br, longFormat, statesOffset);

        var stateOffsets = new long[stateCount];
        for (var i = 0; i < stateCount; i++)
            stateOffsets[i] = statesOffset + i * stateSize;

        // Every state group with more than one state has a dummy state after the end
        // that's identical to the first state, for some reason
        if (stateCount > 1)
        {
            var state0Bytes = br.GetBytes(dataStart + statesOffset, (int)stateSize);
            br.StepIn(dataStart + statesOffset + stateSize * stateCount);
            {
                for (var i = 0; i < stateSize; i++)
                    br.AssertByte(state0Bytes[i]);
            }
            br.StepOut();
        }

        return stateOffsets;
    }

    private Dictionary<long, State> TakeStates(long stateSize, long[] stateOffsets, Dictionary<long, State> states,
        out Dictionary<long, long> stateIDs)
    {
        stateIDs = new Dictionary<long, long>(stateOffsets.Length + 1);

        if (stateOffsets.Length > 1)
        {
            var weirdStateOffset = stateOffsets[0] + stateSize * stateOffsets.Length;
            if (!states.Remove(weirdStateOffset))
                throw new FormatException("Weird state not found.");
        }

        var stateGroup = new Dictionary<long, State>(stateOffsets.Length);
        foreach (var offset in stateOffsets)
        {
            State state = states[offset];
            if (stateGroup.ContainsKey(state.ID))
                throw new FormatException("Duplicate state ID.");
            stateGroup[state.ID] = state;
            states.Remove(offset);
            stateIDs[offset] = state.ID;
        }

        stateOffsets = null;

        return stateGroup;
    }

    private static long ReadVarint(BinaryReaderEx br, bool longFormat)
    {
        if (longFormat)
            return br.ReadInt64();
        return br.ReadInt32();
    }

    private static long[] ReadVarints(BinaryReaderEx br, bool longFormat, long count)
    {
        if (longFormat)
            return br.ReadInt64s((int)count);
        return Array.ConvertAll(br.ReadInt32s((int)count), i => (long)i);
    }

    private static long AssertVarint(BinaryReaderEx br, bool longFormat, params long[] values)
    {
        if (longFormat)
            return br.AssertInt64(values);
        return br.AssertInt32(Array.ConvertAll(values, l => (int)l));
    }

    private static void WriteVarint(BinaryWriterEx bw, bool longFormat, long value)
    {
        if (longFormat)
            bw.WriteInt64(value);
        else
            bw.WriteInt32((int)value);
    }

    private static void ReserveVarint(BinaryWriterEx bw, bool longFormat, string name)
    {
        if (longFormat)
            bw.ReserveInt64(name);
        else
            bw.ReserveInt32(name);
    }

    private static void FillVarint(BinaryWriterEx bw, bool longFormat, string name, long value)
    {
        if (longFormat)
            bw.FillInt64(name, value);
        else
            bw.FillInt32(name, (int)value);
    }

    /// <summary>
    ///     A node in the state graph.
    /// </summary>
    public class State
    {
        private long[] conditionOffsets;

        /// <summary>
        ///     Possible transitions to other states.
        /// </summary>
        public List<Condition> Conditions;

        /// <summary>
        ///     Commands to be executed when the state is entered.
        /// </summary>
        public List<CommandCall> EntryCommands;

        /// <summary>
        ///     Commands to be executed when the state is exited.
        /// </summary>
        public List<CommandCall> ExitCommands;

        internal long ID;

        /// <summary>
        ///     Unknown. Speculation: commands to be executed constantly while in the state.
        /// </summary>
        public List<CommandCall> WhileCommands;

        /// <summary>
        ///     Creates a new State with no conditions or commands.
        /// </summary>
        public State()
        {
            Conditions = new List<Condition>();
            EntryCommands = new List<CommandCall>();
            ExitCommands = new List<CommandCall>();
            WhileCommands = new List<CommandCall>();
        }

        internal State(BinaryReaderEx br, bool longFormat, long dataStart)
        {
            ID = ReadVarint(br, longFormat);
            var conditionOffsetsOffset = ReadVarint(br, longFormat);
            var conditionOffsetCount = ReadVarint(br, longFormat);
            var entryCommandsOffset = ReadVarint(br, longFormat);
            var entryCommandCount = ReadVarint(br, longFormat);
            var exitCommandsOffset = ReadVarint(br, longFormat);
            var exitCommandCount = ReadVarint(br, longFormat);
            var whileCommandsOffset = ReadVarint(br, longFormat);
            var whileCommandCount = ReadVarint(br, longFormat);

            br.StepIn(0);
            {
                br.Position = dataStart + conditionOffsetsOffset;
                conditionOffsets = ReadVarints(br, longFormat, conditionOffsetCount);

                br.Position = dataStart + entryCommandsOffset;
                EntryCommands = new List<CommandCall>((int)entryCommandCount);
                for (var i = 0; i < entryCommandCount; i++)
                    EntryCommands.Add(new CommandCall(br, longFormat, dataStart));

                br.Position = dataStart + exitCommandsOffset;
                ExitCommands = new List<CommandCall>((int)exitCommandCount);
                for (var i = 0; i < exitCommandCount; i++)
                    ExitCommands.Add(new CommandCall(br, longFormat, dataStart));

                br.Position = dataStart + whileCommandsOffset;
                WhileCommands = new List<CommandCall>((int)whileCommandCount);
                for (var i = 0; i < whileCommandCount; i++)
                    WhileCommands.Add(new CommandCall(br, longFormat, dataStart));
            }
            br.StepOut();
        }

        internal void GetConditions(Dictionary<long, Condition> conditions)
        {
            Conditions = new List<Condition>(conditionOffsets.Length);
            foreach (var offset in conditionOffsets)
                Conditions.Add(conditions[offset]);
            conditionOffsets = null;
        }

        internal void WriteHeader(BinaryWriterEx bw, bool longFormat, long groupID, long stateID)
        {
            WriteVarint(bw, longFormat, stateID);
            ReserveVarint(bw, longFormat, $"State{groupID}-{stateID}:ConditionsOffset");
            WriteVarint(bw, longFormat, Conditions.Count);
            ReserveVarint(bw, longFormat, $"State{groupID}-{stateID}:EntryCommandsOffset");
            WriteVarint(bw, longFormat, EntryCommands.Count);
            ReserveVarint(bw, longFormat, $"State{groupID}-{stateID}:ExitCommandsOffset");
            WriteVarint(bw, longFormat, ExitCommands.Count);
            ReserveVarint(bw, longFormat, $"State{groupID}-{stateID}:WhileCommandsOffset");
            WriteVarint(bw, longFormat, WhileCommands.Count);
        }

        internal void WriteCommandCalls(BinaryWriterEx bw, bool longFormat, long groupID, long stateID, long dataStart,
            List<CommandCall> commands)
        {
            if (EntryCommands.Count == 0)
            {
                FillVarint(bw, longFormat, $"State{groupID}-{stateID}:EntryCommandsOffset", -1);
            }
            else
            {
                FillVarint(bw, longFormat, $"State{groupID}-{stateID}:EntryCommandsOffset", bw.Position - dataStart);
                foreach (CommandCall command in EntryCommands)
                {
                    command.WriteHeader(bw, longFormat, commands.Count);
                    commands.Add(command);
                }
            }

            if (ExitCommands.Count == 0)
            {
                FillVarint(bw, longFormat, $"State{groupID}-{stateID}:ExitCommandsOffset", -1);
            }
            else
            {
                FillVarint(bw, longFormat, $"State{groupID}-{stateID}:ExitCommandsOffset", bw.Position - dataStart);
                foreach (CommandCall command in ExitCommands)
                {
                    command.WriteHeader(bw, longFormat, commands.Count);
                    commands.Add(command);
                }
            }

            if (WhileCommands.Count == 0)
            {
                FillVarint(bw, longFormat, $"State{groupID}-{stateID}:WhileCommandsOffset", -1);
            }
            else
            {
                FillVarint(bw, longFormat, $"State{groupID}-{stateID}:WhileCommandsOffset", bw.Position - dataStart);
                foreach (CommandCall command in WhileCommands)
                {
                    command.WriteHeader(bw, longFormat, commands.Count);
                    commands.Add(command);
                }
            }
        }

        internal int WriteConditionOffsets(BinaryWriterEx bw, bool longFormat, long groupID, long stateID,
            long dataStart, Dictionary<Condition, long> conditionOffsets)
        {
            FillVarint(bw, longFormat, $"State{groupID}-{stateID}:ConditionsOffset", bw.Position - dataStart);
            foreach (Condition cond in Conditions)
                WriteVarint(bw, longFormat, conditionOffsets[cond]);
            return Conditions.Count;
        }
    }

    /// <summary>
    ///     Represents a transition between states when certain conditions are met.
    /// </summary>
    public class Condition
    {
        private long[] conditionOffsets;

        /// <summary>
        ///     Bytecode which determines whether the condition passes.
        /// </summary>
        public byte[] Evaluator;

        /// <summary>
        ///     Commands to be executed if the condition passes.
        /// </summary>
        public List<CommandCall> PassCommands;

        private long stateOffset;

        /// <summary>
        ///     If present and this condition passes, evaluation will continue to these conditions.
        /// </summary>
        public List<Condition> Subconditions;

        /// <summary>
        ///     The ID of the state to enter if the condition passes, or null if subconditions are present.
        /// </summary>
        public long? TargetState;

        /// <summary>
        ///     Creates a new Condition with no target state, commands, or subconditions, and an empty evaluator.
        /// </summary>
        public Condition()
        {
            TargetState = null;
            PassCommands = new List<CommandCall>();
            Subconditions = new List<Condition>();
            Evaluator = new byte[0];
        }

        /// <summary>
        ///     Creates a new Condition with the given target state and evaluator, and no commands or subconditions.
        /// </summary>
        public Condition(long targetState, byte[] evaluator)
        {
            TargetState = targetState;
            PassCommands = new List<CommandCall>();
            Subconditions = new List<Condition>();
            Evaluator = evaluator;
        }

        internal Condition(BinaryReaderEx br, bool longFormat, long dataStart)
        {
            stateOffset = ReadVarint(br, longFormat);
            var passCommandsOffset = ReadVarint(br, longFormat);
            var passCommandCount = ReadVarint(br, longFormat);
            var conditionOffsetsOffset = ReadVarint(br, longFormat);
            var conditionOffsetCount = ReadVarint(br, longFormat);
            var evaluatorOffset = ReadVarint(br, longFormat);
            var evaluatorLength = ReadVarint(br, longFormat);

            br.StepIn(0);
            {
                br.Position = dataStart + passCommandsOffset;
                PassCommands = new List<CommandCall>((int)passCommandCount);
                for (var i = 0; i < passCommandCount; i++)
                    PassCommands.Add(new CommandCall(br, longFormat, dataStart));

                br.Position = dataStart + conditionOffsetsOffset;
                conditionOffsets = ReadVarints(br, longFormat, conditionOffsetCount);

                Evaluator = br.GetBytes(dataStart + evaluatorOffset, (int)evaluatorLength);
            }
            br.StepOut();
        }

        internal void GetStateAndConditions(Dictionary<long, long> stateOffsets, Dictionary<long, Condition> conditions)
        {
            // Already processed
            if (stateOffset == -2)
                return;

            if (stateOffset == -1)
                TargetState = null;
            else if (stateOffsets.ContainsKey(stateOffset))
                TargetState = stateOffsets[stateOffset];
            else
                throw new FormatException("Condition target state not found.");
            stateOffset = -2;

            Subconditions = new List<Condition>(conditionOffsets.Length);
            foreach (var offset in conditionOffsets)
                Subconditions.Add(conditions[offset]);
            conditionOffsets = null;

            foreach (Condition condition in Subconditions)
                condition.GetStateAndConditions(stateOffsets, conditions);
        }

        internal void WriteHeader(BinaryWriterEx bw, bool longFormat, long groupID, int index,
            Dictionary<long, long> stateOffsets)
        {
            if (TargetState.HasValue)
                WriteVarint(bw, longFormat, stateOffsets[TargetState.Value]);
            else
                WriteVarint(bw, longFormat, -1);

            ReserveVarint(bw, longFormat, $"Condition{groupID}-{index}:PassCommandsOffset");
            WriteVarint(bw, longFormat, PassCommands.Count);
            ReserveVarint(bw, longFormat, $"Condition{groupID}-{index}:ConditionsOffset");
            WriteVarint(bw, longFormat, Subconditions.Count);
            ReserveVarint(bw, longFormat, $"Condition{groupID}-{index}:EvaluatorOffset");
            WriteVarint(bw, longFormat, Evaluator.Length);
        }

        internal void WriteCommandCalls(BinaryWriterEx bw, bool longFormat, long groupID, int index, long dataStart,
            List<CommandCall> commands)
        {
            if (PassCommands.Count == 0)
            {
                FillVarint(bw, longFormat, $"Condition{groupID}-{index}:PassCommandsOffset", -1);
            }
            else
            {
                FillVarint(bw, longFormat, $"Condition{groupID}-{index}:PassCommandsOffset", bw.Position - dataStart);
                foreach (CommandCall command in PassCommands)
                {
                    command.WriteHeader(bw, longFormat, commands.Count);
                    commands.Add(command);
                }
            }
        }

        internal int WriteConditionOffsets(BinaryWriterEx bw, bool longFormat, long groupID, int index, long dataStart,
            Dictionary<Condition, long> conditionOffsets)
        {
            if (Subconditions.Count == 0)
            {
                FillVarint(bw, longFormat, $"Condition{groupID}-{index}:ConditionsOffset", -1);
            }
            else
            {
                FillVarint(bw, longFormat, $"Condition{groupID}-{index}:ConditionsOffset", bw.Position - dataStart);
                foreach (Condition cond in Subconditions)
                    WriteVarint(bw, longFormat, conditionOffsets[cond]);
            }

            return Subconditions.Count;
        }

        internal void WriteEvaluator(BinaryWriterEx bw, bool longFormat, long groupID, int index, long dataStart)
        {
            FillVarint(bw, longFormat, $"Condition{groupID}-{index}:EvaluatorOffset", bw.Position - dataStart);
            bw.WriteBytes(Evaluator);
        }
    }

    /// <summary>
    ///     A function to be called when certain conditions are met.
    /// </summary>
    public class CommandCall
    {

        /// <summary>
        ///     Bytecode expressions to evaluate and pass as arguments to the command.
        /// </summary>
        public List<byte[]> Arguments;

        /// <summary>
        ///     Unknown. Speculation: some kind of command bank a la emevd. Should be 1, 5, 6, or 7.
        /// </summary>
        public int CommandBank;

        /// <summary>
        ///     ID of the command to be executed.
        /// </summary>
        public int CommandID;

        /// <summary>
        ///     Creates a new CommandCall with bank 1, ID 0, and no arguments.
        /// </summary>
        public CommandCall()
        {
            CommandBank = 1;
            CommandID = 0;
            Arguments = new List<byte[]>();
        }

        /// <summary>
        ///     Creates a new CommandCall with the given bank, ID, and arguments.
        /// </summary>
        public CommandCall(int commandBank, int commandID, params byte[][] arguments)
        {
            CommandBank = commandBank;
            CommandID = commandID;
            Arguments = arguments.ToList();
        }

        internal CommandCall(BinaryReaderEx br, bool longFormat, long dataStart)
        {
            CommandBank = br.AssertInt32(1, 5, 6, 7);
            CommandID = br.ReadInt32();
            var argsOffset = ReadVarint(br, longFormat);
            var argsCount = ReadVarint(br, longFormat);

            br.StepIn(dataStart + argsOffset);
            {
                Arguments = new List<byte[]>((int)argsCount);
                for (var i = 0; i < argsCount; i++)
                {
                    var argOffset = ReadVarint(br, longFormat);
                    var argSize = ReadVarint(br, longFormat);
                    Arguments.Add(br.GetBytes(dataStart + argOffset, (int)argSize));
                }
            }
            br.StepOut();
        }

        internal void WriteHeader(BinaryWriterEx bw, bool longFormat, int index)
        {
            bw.WriteInt32(CommandBank);
            bw.WriteInt32(CommandID);
            ReserveVarint(bw, longFormat, $"Command{index}:ArgsOffset");
            WriteVarint(bw, longFormat, Arguments.Count);
        }

        internal void WriteArgs(BinaryWriterEx bw, bool longFormat, int index, long dataStart)
        {
            FillVarint(bw, longFormat, $"Command{index}:ArgsOffset", bw.Position - dataStart);
            for (var i = 0; i < Arguments.Count; i++)
            {
                ReserveVarint(bw, longFormat, $"Command{index}-{i}:BytecodeOffset");
                WriteVarint(bw, longFormat, Arguments[i].Length);
            }
        }

        internal void WriteBytecode(BinaryWriterEx bw, bool longFormat, int index, long dataStart)
        {
            for (var i = 0; i < Arguments.Count; i++)
            {
                FillVarint(bw, longFormat, $"Command{index}-{i}:BytecodeOffset", bw.Position - dataStart);
                bw.WriteBytes(Arguments[i]);
            }
        }
    }
}