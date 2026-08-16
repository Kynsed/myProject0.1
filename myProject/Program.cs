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
if (mode == "--audio-test")
{
    Environment.Exit(AudioTest.Run());
}
if (mode == "--sprite-test")
{
    Environment.Exit(SpriteTest.Run());
}
if (mode == "--input-test")
{
    Environment.Exit(InputTest.Run());
}
if (mode == "--camera-test")
{
    Environment.Exit(CameraTest.Run());
}
if (mode == "--poda-test")
{
    Environment.Exit(PodaTest.Run());
}
if (mode == "--combat-test")
{
    Environment.Exit(CombatTest.Run());
}
if (mode == "--inspector-test")
{
    Environment.Exit(InspectorTest.Run());
}
if (mode == "--play")
{
    using var play = new PlayGame();
    play.Run();
    return;
}
if (mode == "--inspector-shot")
{
    using var shot = new PlayGame();
    shot.ScreenshotPath = args.Length > 1 ? args[1] : "inspector.png";
    shot.Run();
    return;
}

using var game = new SmokeGame();
game.Run();
