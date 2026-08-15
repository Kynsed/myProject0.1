using System;
using System.Collections.Generic;

namespace myProject.Inspector.Drawers
{
    // Ponto de extensao do inspector: registre um IValueDrawer p/ ensinar a UI a editar
    // um tipo novo. A busca por tipo e cacheada, entao CanDraw so roda uma vez por tipo.
    public sealed class DrawerRegistry
    {
        private readonly List<IValueDrawer> drawers = new List<IValueDrawer>();
        private readonly Dictionary<Type, IValueDrawer> resolved = new Dictionary<Type, IValueDrawer>();

        /// Drawers customizados entram na frente e tem prioridade sobre os embutidos.
        public void Register(IValueDrawer drawer)
        {
            drawers.Insert(0, drawer);
            resolved.Clear();
        }

        public void RegisterFallback(IValueDrawer drawer)
        {
            drawers.Add(drawer);
            resolved.Clear();
        }

        public IValueDrawer Find(Type type)
        {
            if (type == null)
                return null;
            if (resolved.TryGetValue(type, out var cached))
                return cached;
            IValueDrawer found = null;
            for (int i = 0; i < drawers.Count; i++)
            {
                if (drawers[i].CanDraw(type))
                {
                    found = drawers[i];
                    break;
                }
            }
            resolved[type] = found;
            return found;
        }

        public static DrawerRegistry CreateDefault(Action<object> onSelectReference)
        {
            var reg = new DrawerRegistry();
            reg.RegisterFallback(new BoolDrawer());
            reg.RegisterFallback(new NumericDrawer());
            reg.RegisterFallback(new StringDrawer());
            reg.RegisterFallback(new EnumDrawer());
            reg.RegisterFallback(new Vector2Drawer());
            reg.RegisterFallback(new ColorDrawer());
            reg.RegisterFallback(new RectangleDrawer());
            reg.RegisterFallback(new EntityRefDrawer { OnSelect = onSelectReference });
            return reg;
        }
    }
}
