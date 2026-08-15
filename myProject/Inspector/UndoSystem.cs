using System;
using System.Collections.Generic;
using myProject.Inspector.Reflection;

namespace myProject.Inspector
{
    public interface IUndoCommand
    {
        string Description { get; }
        void Apply();
        void Revert();
        /// Funde com o comando anterior (ex.: arrastar um slider gera 1 entrada, nao 60).
        bool TryMerge(IUndoCommand next);
    }

    // Alteracao de um membro de um objeto. Guarda o valor antigo e o novo.
    public sealed class SetMemberCommand : IUndoCommand
    {
        private readonly object target;
        private readonly InspectedMember member;
        private readonly object oldValue;
        private object newValue;

        public SetMemberCommand(object target, InspectedMember member, object oldValue, object newValue)
        {
            this.target = target;
            this.member = member;
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        public string Description => member.Label;

        public void Apply() => member.TrySetValue(target, newValue);
        public void Revert() => member.TrySetValue(target, oldValue);

        public bool TryMerge(IUndoCommand next)
        {
            if (next is SetMemberCommand other
                && ReferenceEquals(other.target, target)
                && ReferenceEquals(other.member, member))
            {
                newValue = other.newValue; // mantem o oldValue original
                return true;
            }
            return false;
        }
    }

    // Pilha de undo/redo com fusao por janela de tempo (arrastes viram uma entrada so).
    public sealed class UndoSystem
    {
        public const int Capacity = 128;
        private const double MergeWindowSeconds = 0.6;

        private readonly List<IUndoCommand> undo = new List<IUndoCommand>();
        private readonly List<IUndoCommand> redo = new List<IUndoCommand>();
        private DateTime lastPush = DateTime.MinValue;

        public int UndoCount => undo.Count;
        public int RedoCount => redo.Count;
        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;
        public string NextUndoLabel => CanUndo ? undo[undo.Count - 1].Description : null;

        /// Registra um comando ja aplicado ao objeto.
        public void Record(IUndoCommand command)
        {
            redo.Clear();
            var now = DateTime.UtcNow;
            bool withinWindow = (now - lastPush).TotalSeconds <= MergeWindowSeconds;
            lastPush = now;

            if (withinWindow && undo.Count > 0 && undo[undo.Count - 1].TryMerge(command))
                return;

            undo.Add(command);
            if (undo.Count > Capacity)
                undo.RemoveAt(0);
        }

        /// Forca o proximo Record a criar uma entrada nova (chamado ao soltar o mouse).
        public void BreakMerge() => lastPush = DateTime.MinValue;

        public bool Undo()
        {
            if (!CanUndo)
                return false;
            var cmd = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            cmd.Revert();
            redo.Add(cmd);
            BreakMerge();
            return true;
        }

        public bool Redo()
        {
            if (!CanRedo)
                return false;
            var cmd = redo[redo.Count - 1];
            redo.RemoveAt(redo.Count - 1);
            cmd.Apply();
            undo.Add(cmd);
            BreakMerge();
            return true;
        }

        public void Clear()
        {
            undo.Clear();
            redo.Clear();
            BreakMerge();
        }
    }
}
