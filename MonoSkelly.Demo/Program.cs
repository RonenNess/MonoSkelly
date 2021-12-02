using System;

namespace MonoSkelly.Demo
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new MonoSkellyDemo())
                game.Run();
        }
    }
}
