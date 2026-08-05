using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace Monocle
{
    public class Tracker
    {
        public static Dictionary<Type, List<Type>> TrackedEntityTypes { get; private set; }
        public static Dictionary<Type, List<Type>> TrackedComponentTypes { get; private set; }
        public static HashSet<Type> StoredEntityTypes { get; private set; }
        public static HashSet<Type> StoredComponentTypes { get; private set; }

        public static void Initialize()
        {
            TrackedEntityTypes = new Dictionary<Type, List<Type>>();
            TrackedComponentTypes = new Dictionary<Type, List<Type>>();
            StoredEntityTypes = new HashSet<Type>();
            StoredComponentTypes = new HashSet<Type>();

            foreach (Type type in Assembly.GetEntryAssembly().GetTypes())
            {
                object[] attrs = type.GetCustomAttributes(typeof(Tracked), false);
                if (attrs.Length == 0)
                    continue;

                bool inherited = (attrs[0] as Tracked).Inherited;

                if (typeof(Entity).IsAssignableFrom(type))
                {
                    if (!type.IsAbstract)
                    {
                        if (!TrackedEntityTypes.ContainsKey(type))
                            TrackedEntityTypes.Add(type, new List<Type>());
                        TrackedEntityTypes[type].Add(type);
                    }
                    StoredEntityTypes.Add(type);

                    if (inherited)
                        foreach (Type subtype in GetSubclasses(type))
                            if (!subtype.IsAbstract)
                            {
                                if (!TrackedEntityTypes.ContainsKey(subtype))
                                    TrackedEntityTypes.Add(subtype, new List<Type>());
                                TrackedEntityTypes[subtype].Add(type);
                            }
                }
                else if (typeof(Component).IsAssignableFrom(type))
                {
                    if (!type.IsAbstract)
                    {
                        if (!TrackedComponentTypes.ContainsKey(type))
                            TrackedComponentTypes.Add(type, new List<Type>());
                        TrackedComponentTypes[type].Add(type);
                    }
                    StoredComponentTypes.Add(type);

                    if (inherited)
                        foreach (Type subtype in GetSubclasses(type))
                            if (!subtype.IsAbstract)
                            {
                                if (!TrackedComponentTypes.ContainsKey(subtype))
                                    TrackedComponentTypes.Add(subtype, new List<Type>());
                                TrackedComponentTypes[subtype].Add(type);
                            }
                }
                else
                    throw new Exception("Type '" + type.Name + "' cannot be Tracked because it does not derive from Entity or Component");
            }
        }

        private static List<Type> GetSubclasses(Type type)
        {
            List<Type> matches = new List<Type>();

            foreach (Type check in Assembly.GetEntryAssembly().GetTypes())
                if (type != check && type.IsAssignableFrom(check))
                    matches.Add(check);

            return matches;
        }

        public Dictionary<Type, List<Entity>> Entities { get; private set; }
        public Dictionary<Type, List<Component>> Components { get; private set; }

        public Tracker()
        {
            Entities = new Dictionary<Type, List<Entity>>(TrackedEntityTypes.Count);
            foreach (Type type in StoredEntityTypes)
                Entities.Add(type, new List<Entity>());

            Components = new Dictionary<Type, List<Component>>(TrackedComponentTypes.Count);
            foreach (Type type in StoredComponentTypes)
                Components.Add(type, new List<Component>());
        }

        #region Entities

        public bool IsEntityTracked<T>() where T : Entity
        {
            return Entities.ContainsKey(typeof(T));
        }

        public bool IsComponentTracked<T>() where T : Component
        {
            return Components.ContainsKey(typeof(T));
        }

        public T GetEntity<T>() where T : Entity
        {
            var list = Entities[typeof(T)];
            if (list.Count == 0)
                return default;
            else
                return list[0] as T;
        }

        public T GetNearestEntity<T>(Vector2 nearestTo) where T : Entity
        {
            var list = GetEntities<T>();

            T nearest = default;
            float nearestDistSq = 0;

            foreach (T entity in list)
            {
                float distSq = Vector2.DistanceSquared(nearestTo, entity.Position);
                if (nearest == null || distSq < nearestDistSq)
                {
                    nearest = entity;
                    nearestDistSq = distSq;
                }
            }

            return nearest;
        }

        public List<Entity> GetEntities<T>() where T : Entity
        {
            return Entities[typeof(T)];
        }

        public List<Entity> GetEntitiesCopy<T>() where T : Entity
        {
            return new List<Entity>(GetEntities<T>());
        }

        public IEnumerator<T> EnumerateEntities<T>() where T : Entity
        {
            foreach (Entity entity in Entities[typeof(T)])
                yield return entity as T;
        }

        public int CountEntities<T>() where T : Entity
        {
            return Entities[typeof(T)].Count;
        }

        #endregion

        #region Components

        public T GetComponent<T>() where T : Component
        {
            var list = Components[typeof(T)];
            if (list.Count == 0)
                return default;
            else
                return list[0] as T;
        }

        public T GetNearestComponent<T>(Vector2 nearestTo) where T : Component
        {
            var list = GetComponents<T>();

            T nearest = default;
            float nearestDistSq = 0;

            foreach (T component in list)
            {
                float distSq = Vector2.DistanceSquared(nearestTo, component.Entity.Position);
                if (nearest == null || distSq < nearestDistSq)
                {
                    nearest = component;
                    nearestDistSq = distSq;
                }
            }

            return nearest;
        }

        public List<Component> GetComponents<T>() where T : Component
        {
            return Components[typeof(T)];
        }

        public List<Component> GetComponentsCopy<T>() where T : Component
        {
            return new List<Component>(GetComponents<T>());
        }

        public IEnumerator<T> EnumerateComponents<T>() where T : Component
        {
            foreach (Component component in Components[typeof(T)])
                yield return component as T;
        }

        public int CountComponents<T>() where T : Component
        {
            return Components[typeof(T)].Count;
        }

        #endregion

        #region Add / Remove

        internal void EntityAdded(Entity entity)
        {
            Type type = entity.GetType();
            List<Type> trackAs;
            if (TrackedEntityTypes.TryGetValue(type, out trackAs))
                foreach (Type track in trackAs)
                    Entities[track].Add(entity);
        }

        internal void EntityRemoved(Entity entity)
        {
            Type type = entity.GetType();
            List<Type> trackAs;
            if (TrackedEntityTypes.TryGetValue(type, out trackAs))
                foreach (Type track in trackAs)
                    Entities[track].Remove(entity);
        }

        internal void ComponentAdded(Component component)
        {
            Type type = component.GetType();
            List<Type> trackAs;
            if (TrackedComponentTypes.TryGetValue(type, out trackAs))
                foreach (Type track in trackAs)
                    Components[track].Add(component);
        }

        internal void ComponentRemoved(Component component)
        {
            Type type = component.GetType();
            List<Type> trackAs;
            if (TrackedComponentTypes.TryGetValue(type, out trackAs))
                foreach (Type track in trackAs)
                    Components[track].Remove(component);
        }

        #endregion

        public void LogEntities()
        {
            foreach (var kv in Entities)
            {
                string output = kv.Key.Name + " : " + kv.Value.Count;
                Engine.Commands.Log(output);
            }
        }

        public void LogComponents()
        {
            foreach (var kv in Components)
            {
                string output = kv.Key.Name + " : " + kv.Value.Count;
                Engine.Commands.Log(output);
            }
        }
    }
}
