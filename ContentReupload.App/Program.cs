using ContentReupload.App.Compilations;
using System;
using System.Threading;

namespace ContentReupload.App
{
    class Program
    {
        static void Main(string[] args)
        {
            ICompilation[] compilationManagers = new ICompilation[] {
                new GeneralTwitchCompilation(),
                new CSGOCompilation(),
                new Dota2Compilation(),
                //new GTARPCompilation() // not enough clips
            };

            foreach (ICompilation c in compilationManagers)
            {
                Thread thread = new Thread(() => c.ManageUploadsAsync().Wait());
                thread.Start();
            }
        }
    }
}
