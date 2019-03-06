using BeatTracker.DFTPrototype.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeatTracker.UI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //var buffer = new DataBuffer<int>(4, 2);
            //buffer.NextFrame += (data) => Console.WriteLine(string.Join(", ", data));
            //for (int i = 0; i < 100; i++)
            //    buffer.Write(new[] { i }, 1);

            //Application.Run(new PreprocessingVisualization());

            Application.Run(new MainForm());
        }
    }
}
