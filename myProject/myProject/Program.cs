using System;
using MonocleSmoke;
using myProject;   // PlayGame/PlayScene

// Modos:
//   (sem arg)        -> SmokeGame grafico (teste original do Monocle)
//   --phys-test      -> validacao headless da fisica Actor/Solid (sem janela)
//   --player-smoke   -> roda o Player real headless por 180 frames (verifica crash)
//   --play           -> demo grafico jogavel: Player real sobre chao, hitboxes, teclado
//   --gfx-test       -> asserts do atlas de PNGs (abre janela por um instante)
//   --sprite-test    -> contrato de animacoes: todo id do Player.cs resolve no banco
//   --starfly-test   -> estado StarFly com render (flash branco + fim da anim de intro)
//   --tile-test      -> contrato do autotile: qual celula do tileset cada mascara escolhe
//   --shot <png> [n] -> roda o --play por n frames e salva o backbuffer em PNG
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
if (mode == "--gfx-test")
{
    Environment.Exit(GfxTest.Run());
}
if (mode == "--sprite-test")
{
    Environment.Exit(SpriteTest.Run());
}
if (mode == "--starfly-test")
{
    Environment.Exit(StarFlyTest.Run());
}
if (mode == "--tile-test")
{
    Environment.Exit(TileTest.Run());
}
if (mode == "--shot")
{
    Environment.Exit(ShotGame.Run(args));
}
if (mode == "--play")
{
    using var play = new PlayGame();
    play.Run();
    return;
}

using var game = new SmokeGame();
game.Run();
