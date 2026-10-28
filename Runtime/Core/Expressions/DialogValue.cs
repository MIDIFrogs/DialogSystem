using System.Globalization;

namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    /// <summary>A value an expression reads, compares or assigns: bool, number (double), string or unset.</summary>
    public readonly struct DialogValue
    {
        private DialogValue(DialogValueKind kind, bool boolean, double number, string text)
        {
            Kind = kind;
            Bool = boolean;
            Number = number;
            String = text;
        }

        public static DialogValue Unset => default;

        public DialogValueKind Kind { get; }

        public bool Bool { get; }

        public double Number { get; }

        public string String { get; }

        /// <summary>true for true, a non-zero number or a non-empty string.</summary>
        public bool IsTruthy => Kind switch
        {
            DialogValueKind.Bool => Bool,
            DialogValueKind.Number => Number != 0d,
            DialogValueKind.String => !string.IsNullOrEmpty(String),
            _ => false,
        };

        public static DialogValue FromBool(bool value) => new(DialogValueKind.Bool, value, 0d, null);

        public static DialogValue FromNumber(double value) => new(DialogValueKind.Number, false, value, null);

        public static DialogValue FromString(string value) => new(DialogValueKind.String, false, 0d, value ?? string.Empty);

        /// <summary>Converts a context value; unsupported types read as unset.</summary>
        public static DialogValue From(object value) => value switch
        {
            null => Unset,
            bool b => FromBool(b),
            string s => FromString(s),
            double d => FromNumber(d),
            float f => FromNumber(f),
            int i => FromNumber(i),
            long l => FromNumber(l),
            short s16 => FromNumber(s16),
            byte b8 => FromNumber(b8),
            decimal m => FromNumber((double)m),
            _ => Unset,
        };

        /// <summary>The default of <paramref name="kind"/>: false, 0 or "" (what an unset variable reads as).</summary>
        public static DialogValue DefaultOf(DialogValueKind kind) => kind switch
        {
            DialogValueKind.Bool => FromBool(false),
            DialogValueKind.Number => FromNumber(0d),
            DialogValueKind.String => FromString(string.Empty),
            _ => Unset,
        };

        public object ToObject() => Kind switch
        {
            DialogValueKind.Bool => Bool,
            DialogValueKind.Number => Number,
            DialogValueKind.String => String,
            _ => null,
        };

        public override string ToString() => Kind switch
        {
            DialogValueKind.Bool => Bool ? "true" : "false",
            DialogValueKind.Number => Number.ToString(CultureInfo.InvariantCulture),
            DialogValueKind.String => "\"" + String + "\"",
            _ => "unset",
        };
    }
}
