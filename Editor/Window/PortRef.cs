namespace MIDIFrogs.DialogSystem.Editor.Window
{
    internal enum PortKind
    {
        Input,
        Start,
        Next,
        Answer,
    }

    /// <summary>What a port stands for in the asset.</summary>
    internal readonly struct PortRef
    {
        private PortRef(PortKind kind, string lineId, string answerId)
        {
            Kind = kind;
            LineId = lineId;
            AnswerId = answerId;
        }

        public PortKind Kind { get; }

        public string LineId { get; }

        public string AnswerId { get; }

        public static PortRef ForStart() => new(PortKind.Start, null, null);

        public static PortRef ForInput(string lineId) => new(PortKind.Input, lineId, null);

        public static PortRef ForNext(string lineId) => new(PortKind.Next, lineId, null);

        public static PortRef ForAnswer(string lineId, string answerId) => new(PortKind.Answer, lineId, answerId);
    }
}
