using System.Collections.Generic;

namespace SoulsFormats;

public partial class FFXDLSE
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class State : FXSerializable
    {

        public State()
        {
            Actions = new List<Action>();
            Triggers = new List<Trigger>();
        }

        internal State(BinaryReaderEx br, List<string> classNames) : base(br, classNames)
        {
        }

        internal override string ClassName => "FXSerializableState";

        internal override int Version => 1;

        public List<Action> Actions { get; set; }

        public List<Trigger> Triggers { get; set; }

        protected internal override void Deserialize(BinaryReaderEx br, List<string> classNames)
        {
            var actionCount = br.ReadInt32();
            var triggerCount = br.ReadInt32();
            Actions = new List<Action>(actionCount);
            for (var i = 0; i < actionCount; i++)
                Actions.Add(new Action(br, classNames));
            Triggers = new List<Trigger>(triggerCount);
            for (var i = 0; i < triggerCount; i++)
                Triggers.Add(new Trigger(br, classNames));
        }

        internal override void AddClassNames(List<string> classNames)
        {
            base.AddClassNames(classNames);
            foreach (Action action in Actions)
                action.AddClassNames(classNames);
            foreach (Trigger trigger in Triggers)
                trigger.AddClassNames(classNames);
        }

        protected internal override void Serialize(BinaryWriterEx bw, List<string> classNames)
        {
            bw.WriteInt32(Actions.Count);
            bw.WriteInt32(Triggers.Count);
            foreach (Action action in Actions)
                action.Write(bw, classNames);
            foreach (Trigger trigger in Triggers)
                trigger.Write(bw, classNames);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}