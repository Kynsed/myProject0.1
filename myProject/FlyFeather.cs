using Monocle;

namespace myProject
{
    // NOTE: bloco de movimento especial (fly feather / voo). Stub agora, portar fiel depois.
    [Tracked(false)]
    public class FlyFeather : Entity
    {
        public static ParticleType P_Boost = new ParticleType();
        public static ParticleType P_Flying = new ParticleType();
    }
}
