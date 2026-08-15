using System;
using Microsoft.Xna.Framework;

namespace myProject
{
    // Jogo proprio: portoes de movimento p/ o level design do metroidvania.
    //
    // A liberdade de movimento do Celeste (dash em 8 direcoes, escalar qualquer parede)
    // dissolve o gate de progressao de um metroidvania. Em vez de arrancar o codigo do
    // port, cada liberdade fica atras de um portao aqui: desligado por padrao, ligado
    // depois pela progressao (upgrade de personagem). O port continua inteiro embaixo.
    //
    // Os harnesses de paridade (--parity) ligam TUDO via EnableAll(): eles auditam a
    // fidelidade do port, nao o design do jogo.
    public static class Abilities
    {
        public static bool DashDiagonal;  // dash em diagonal (junto: hyper dash, que exige diagonal p/ baixo)
        public static bool DashVertical;  // dash reto p/ cima/baixo (junto: super wall jump, que exige dash p/ cima)
        public static bool WallClimb;     // agarrar e escalar parede (junto: climb jump e ledge hop)

        // estado do jogo hoje: so dash horizontal, sem escalar parede
        public static void ResetToDefaults()
        {
            DashDiagonal = false;
            DashVertical = false;
            WallClimb = false;
        }

        // movimento do Celeste inteiro (usado pelos testes de paridade)
        public static void EnableAll()
        {
            DashDiagonal = true;
            DashVertical = true;
            WallClimb = true;
        }

        // Converte a direcao pedida no dash p/ o que o personagem sabe fazer hoje.
        // 'dir' ja passou pelo CorrectDashPrecision; 'facing' decide o lado quando o
        // input era puramente vertical (dash p/ cima vira dash p/ frente).
        public static Vector2 ConstrainDash(Vector2 dir, Facings facing)
        {
            if (dir == Vector2.Zero)
                return dir;

            bool diagonal = dir.X != 0f && dir.Y != 0f;
            if (diagonal ? DashDiagonal : (dir.X != 0f || DashVertical))
                return dir;

            int sign = (dir.X != 0f) ? Math.Sign(dir.X) : (int)facing;
            return new Vector2((float)sign, 0f);
        }
    }
}
