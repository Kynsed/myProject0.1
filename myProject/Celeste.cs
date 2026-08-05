using Monocle;

namespace myProject
{
    // NOTE: classe-jogo podada. So o hitstop (Freeze), que afeta o timing do movimento.
    public class Celeste
    {
        public static void Freeze(float time)
        {
            if (Engine.FreezeTimer < time)
                Engine.FreezeTimer = time;
        }
    }
}
