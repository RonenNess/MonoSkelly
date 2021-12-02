using System;

namespace MonoSkelly.Editor
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new SkellyEditor())
                game.Run();
        }
    }
}
