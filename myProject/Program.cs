using System;
using MonocleSmoke;

// Modos:
//   (sem arg)        -> SmokeGame grafico (teste original do Monocle)
//   --phys-test      -> validacao headless da fisica Actor/Solid (sem janela)
//   --player-smoke   -> roda o Player real headless por 180 frames (verifica crash)
//   --play           -> demo grafico jogavel: Player real sobre chao, hitboxes, teclado
string mode = args.Length > 0 ? args[0] : "";

if (mode == "--phys-test")
{
    Environment.Exit(PhysTest.Run());
}
if (mode == "--player-smoke")
{
    Environment.Exit(PlayerSmoke.Run());
}
if (mode == "--player-fuzz")
{
    Environment.Exit(PlayerFuzz.Run());
}
if (mode == "--parity")
{
    Environment.Exit(ParityTest.Run());
}
if (mode == "--play")
{
    using var play = new PlayGame();
    play.Run();
    return;
}

using var game = new SmokeGame();
game.Run();
