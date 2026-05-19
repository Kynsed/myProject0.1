using Microsoft.Xna.Framework;
using Monocle;

System.Console.WriteLine("=== Smoke test Ease===");

System.Console.WriteLine("Ease.Linear(0.5)     = " + Ease.Linear(0.5f));      // 0.5
System.Console.WriteLine("Ease.QuadIn(0.5)     = " + Ease.QuadIn(0.5f));      // 0.25
System.Console.WriteLine("Ease.QuadOut(0.5)    = " + Ease.QuadOut(0.5f));     // 0.75
System.Console.WriteLine("Ease.CubeIn(0.5)     = " + Ease.CubeIn(0.5f));      // 0.125
System.Console.WriteLine("Ease.SineInOut(0.5)  = " + Ease.SineInOut(0.5f));   // 0.5
System.Console.WriteLine("Ease.BounceOut(1.0)  = " + Ease.BounceOut(1.0f));   // ~1.0
System.Console.WriteLine("Ease.UpDown(0.5)     = " + Ease.UpDown(0.5f));      // 1.0
System.Console.WriteLine("Ease.UpDown(0.25)    = " + Ease.UpDown(0.25f));     // 0.5
var inv = Ease.Invert(Ease.QuadIn);
System.Console.WriteLine("Invert(QuadIn)(0.5)  = " + inv(0.5f));              // 0.75

System.Console.WriteLine("=== OK ===");
