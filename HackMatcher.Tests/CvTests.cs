using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using HackMatcher.Solver;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HackMatcher.Tests
{
    [TestClass]
    public class CvTests
    {
        private Bitmap ReadGameSectionFromFile(string filename)
        {
            var image = Image.FromFile(filename);
            var bitmap = new Bitmap(360, 540, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                var srcRect = new Rectangle(312, 142, 360, 540);
                var destRect = new Rectangle(0, 0, 360, 540);
                g.DrawImage(image, destRect, srcRect, GraphicsUnit.Pixel);
            }
            bitmap.Save($"{filename}2.png");
            return bitmap;
        }

        [TestMethod]
        public void Read_WithLoadingScreen_IsEmpty()
        {
            var read = CV.ReadBitmap(ReadGameSectionFromFile("Images/capture-before-start.png"));
            Assert.AreEqual(0, read.GetItemCount());
        }

        [TestMethod]
        public void Read_AtStart_FindsPieces()
        {
            var read = CV.ReadBitmap(ReadGameSectionFromFile("Images/capture-at-start.png"));
            
            // nothing is held
            Assert.IsNull(read.held);

            // grid is a full 3x7, but the first row is snipped.
            Assert.AreEqual(2 * 7, read.GetItemCount());
            
            // one teal bomb
            var bombs = read.board.Cast<Piece>().Where(p => p != null && p.bomb).ToList();
            Assert.AreEqual(1, bombs.Count);
            Assert.AreEqual(PieceColor.TEAL, bombs.Single().color);
        }

        [TestMethod]
        public void Read_CurrentlyGrabbing_IsNull()
        {
            var read = CV.ReadBitmap(ReadGameSectionFromFile("Images/capture-grabbing.png"));

            // expect an empty field since there is a gap
            Assert.IsNull(read);
        }

        [TestMethod]
        public void Read_WhileGrabbed_HasHeld()
        {
            var read = CV.ReadBitmap(ReadGameSectionFromFile("Images/capture-grabbed.png"));
            
            Assert.IsNotNull(read.held);
            Assert.AreEqual(PieceColor.YELLOW, read.held.color);
            Assert.IsFalse(read.held.bomb);
        }
    }
}
