using UnityEngine;
using System;

namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    /// <summary>
    /// A comparison. An unset side reads as the other side's default (false, 0 or ""); values of different kinds
    /// compare false (with one warning per comparison); strings order ordinally; bools only support == and !=.
    /// </summary>
    public sealed class CompareNode : ExpressionNode
    {
        public CompareNode(CompareOp op, ExpressionNode left, ExpressionNode right, int position, int length, string source)
            : base(position, length, source)
        {
            Op = op;
            Left = left;
            Right = right;
        }

        public CompareOp Op { get; }

        public ExpressionNode Left { get; }

        public ExpressionNode Right { get; }

        private bool warned;

        public override DialogValue Evaluate(IDialogContext context, DialogFunctions functions)
        {
            DialogValue left = Left.Evaluate(context, functions);
            DialogValue right = Right.Evaluate(context, functions);
            if (!warned && left.Kind != DialogValueKind.Unset && right.Kind != DialogValueKind.Unset && left.Kind != right.Kind)
            {
                warned = true;
                Debug.LogWarning($"[DialogSystem] '{Source}' compares {left.Kind} with {right.Kind}; it is false.");
            }
            return DialogValue.FromBool(Compare(Op, left, right));
        }

        public static bool Compare(CompareOp op, DialogValue left, DialogValue right)
        {
            if (left.Kind == DialogValueKind.Unset)
            {
                left = DialogValue.DefaultOf(right.Kind);
            }
            if (right.Kind == DialogValueKind.Unset)
            {
                right = DialogValue.DefaultOf(left.Kind);
            }
            if (left.Kind != right.Kind)
            {
                return false;
            }

            int order;
            switch (left.Kind)
            {
                case DialogValueKind.Unset:
                    order = 0;
                    break;
                case DialogValueKind.Bool:
                    if (op != CompareOp.Eq && op != CompareOp.Ne)
                    {
                        return false;
                    }
                    order = left.Bool == right.Bool ? 0 : 1;
                    break;
                case DialogValueKind.Number:
                    order = left.Number.CompareTo(right.Number);
                    break;
                default:
                    order = string.CompareOrdinal(left.String, right.String);
                    break;
            }

            return op switch
            {
                CompareOp.Eq => order == 0,
                CompareOp.Ne => order != 0,
                CompareOp.Lt => order < 0,
                CompareOp.Le => order <= 0,
                CompareOp.Gt => order > 0,
                CompareOp.Ge => order >= 0,
                _ => throw new ArgumentOutOfRangeException(nameof(op)),
            };
        }
    }
}
