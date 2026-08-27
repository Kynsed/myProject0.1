using System;
using Microsoft.Xna.Framework;

namespace Monocle
{
    public class ParticleEmitter : Component
    {
        public ParticleSystem System;
        public ParticleType Type;
        public Entity Track;
        public float Interval;
        public Vector2 Position;
        public Vector2 Range;
        public int Amount;
        public float? Direction;

        private float timer;

        public ParticleEmitter(ParticleSystem system, ParticleType type, Vector2 position, Vector2 range, int amount, float interval)
            : base(true, false)
        {
            System = system;
            Type = type;
            Position = position;
            Range = range;
            Amount = amount;
            Interval = interval;
        }

        public ParticleEmitter(ParticleSystem system, ParticleType type, Vector2 position, Vector2 range, float direction, int amount, float interval)
            : this(system, type, position, range, amount, interval)
        {
            Direction = direction;
        }

        public ParticleEmitter(ParticleSystem system, ParticleType type, Entity track, Vector2 position, Vector2 range, float direction, int amount, float interval)
            : this(system, type, position, range, amount, interval)
        {
            Direction = direction;
            Track = track;
        }

        public void SimulateCycle()
        {
            Simulate(Type.LifeMax);
        }

        public void Simulate(float duration)
        {
            float steps = duration / Interval;
            for (int step = 0; step < steps; step++)
            {
                for (int i = 0; i < Amount; i++)
                {
                    Particle particle = new Particle();
                    Vector2 position = Entity.Position + Position + Calc.Random.Range(-Range, Range);

                    if (Direction != null)
                        particle = Type.Create(ref particle, position, Direction.Value);
                    else
                        particle = Type.Create(ref particle, position);
                    particle.Track = Track;

                    float simulateFor = duration - Interval * step;
                    if (particle.SimulateFor(simulateFor))
                        System.Add(particle);
                }
            }
        }

        public void Emit()
        {
            if (Direction != null)
                System.Emit(Type, Amount, Entity.Position + Position, Range, Direction.Value);
            else
                System.Emit(Type, Amount, Entity.Position + Position, Range);
        }

        public override void Update()
        {
            timer -= Engine.DeltaTime;
            if (timer <= 0)
            {
                timer = Interval;
                Emit();
            }
        }
    }
}
