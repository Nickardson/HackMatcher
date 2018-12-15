// TODO
//      - Search is slow.
//          - Improve bottlenecks.
//          - Parallel While

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HackMatcher.Solver;

namespace HackMatcher {
    class Program {
        public static IntPtr selfHandle;

        public static void Main(string[] args) {
            // Run EXAPUNKS at 1366*768 resolution and disable HACK*MATCH CRT effect in the settings.
            // Launch HACK*MATCH, wait for the menu to show, then launch the solver.

            SetUpWindowHandles();

            //CaptureSequence(100, TimeSpan.FromMilliseconds(1000));
            //return;

            IBoardSolver solver = new QuinnBoardSolver();

            while (true) {
                State state = null;
                Color heldColor = Color.White;
                while (state == null) {
                    //var image = Image.FromFile("last.png");
                    var image = ScreenCapture.CaptureWindowV2(Util32.handle);
                    Bitmap bitmap = new Bitmap(360, 540, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(bitmap)) {
                        Rectangle srcRect = new Rectangle(312, 142, 360, 540);
                        Rectangle destRect = new Rectangle(0, 0, 360, 540);
                        g.DrawImage(image, destRect, srcRect, GraphicsUnit.Pixel);
                    }
                    bitmap.Save("last.png");
                    state = CV.ReadBitmap(bitmap);
                    heldColor = new Bitmap(image).GetPixel(320, 610);
                }
                Console.WriteLine("Holding: " + state.held);
                var moves = solver.FindMoves(state, out bool hasMatch).ToList();
                if (!moves.Any()) {
                    continue;
                }
                
                foreach (Move move in moves) {
                    Console.WriteLine(move);
                }
                Util32.ExecuteMoves(new Queue<Move>(moves));
                if (hasMatch) {
                    Console.WriteLine("Found match, sleeping 500ms...");
                    Thread.Sleep(500);
                }
            }
        }

        private static void SetUpWindowHandles()
        {
            Process[] processes = Process.GetProcessesByName("EXAPUNKS");
            if (processes.Length == 0)
            {
                Console.WriteLine("Couldn't find an open instance of EXAPUNKS. Press any key to quit.");
                Console.ReadKey();
                Environment.Exit(1);
            }
            Process process = processes.OrderBy(e => e.StartTime).First();
            selfHandle = Process.GetCurrentProcess().MainWindowHandle;
            IntPtr hWnd = process.MainWindowHandle;
            if (hWnd != IntPtr.Zero)
            {
                Util32.handle = hWnd;
            }
            Util32.ForegroundWindow();
        }

        /// <summary>
        /// For debugging, captures a series of images
        /// </summary>
        static void CaptureSequence(int count, TimeSpan delay)
        {
            for (var i = 0; i < count; i++)
            {
                Console.WriteLine($"Capture {i} / {count}");
                var image = ScreenCapture.CaptureWindowV2(Util32.handle);
                image.Save($"capture-{i:D5}.png");
                Thread.Sleep(delay);
            }
        }
    }
}
