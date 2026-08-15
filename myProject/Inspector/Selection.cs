using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject.Inspector
{
    // Estado de selecao do inspector. Isolado da UI e da reflexao de proposito:
    // quem seleciona (clique no mundo, lista, atalho) nao precisa conhecer o painel.
    public sealed class Selection
    {
        private object current;

        public event Action<object> Changed;

        public object Current => current;
        public bool HasSelection => current != null;

        /// Valores dos membros no momento da selecao — base do botao Reset e do marcador
        /// de "campo modificado".
        public readonly Dictionary<string, object> Baseline = new Dictionary<string, object>();

        public void Select(object target)
        {
            if (ReferenceEquals(current, target))
                return;
            current = target;
            CaptureBaseline();
            Changed?.Invoke(current);
        }

        public void Clear() => Select(null);

        public void CaptureBaseline()
        {
            Baseline.Clear();
            if (current == null)
                return;
            var type = Reflection.TypeCache.Get(current.GetType());
            foreach (var m in type.Members)
                Baseline[m.Name] = m.GetValue(current);
        }

        public bool IsModified(string memberName, object value)
        {
            if (!Baseline.TryGetValue(memberName, out var original))
                return false;
            if (original == null || value == null)
                return !ReferenceEquals(original, value);
            return !original.Equals(value);
        }

        public object GetBaseline(string memberName)
            => Baseline.TryGetValue(memberName, out var v) ? v : null;

        /// Entidade da cena sob um ponto do mundo. Usa o collider quando existe;
        /// senao, uma area de toque ao redor da posicao.
        public static Entity PickAt(Scene scene, Vector2 worldPoint)
        {
            if (scene == null)
                return null;
            Entity best = null;
            float bestDepth = float.MaxValue;
            foreach (Entity e in scene.Entities)
            {
                bool hit;
                if (e.Collider != null)
                    hit = e.CollidePoint(worldPoint);
                else
                    hit = Vector2.DistanceSquared(e.Position, worldPoint) <= 64f; // 8px
                if (hit && e.Depth <= bestDepth)
                {
                    bestDepth = e.Depth;
                    best = e;
                }
            }
            return best;
        }
    }
}
