﻿// PolyFillWin.cs - Demo window for testing the PolyFill class
// ---------------------------------------------------------------------------------------
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Reflection;
using System.IO;
using System.Windows.Threading;

namespace GrayBMP;

class PolyFillWin : Window {
   public PolyFillWin () {
      Width = 900; Height = 600;
      Left = 200; Top = 50; WindowStyle = WindowStyle.None;

      mBmp = new GrayBMP (Width * mScale, Height * mScale);
      Image image = new () {
         Stretch = Stretch.Fill,
         HorizontalAlignment = HorizontalAlignment.Left,
         VerticalAlignment = VerticalAlignment.Top,
         Source = mBmp.Bitmap
      };
      RenderOptions.SetBitmapScalingMode (image, BitmapScalingMode.HighQuality);
      RenderOptions.SetEdgeMode (image, EdgeMode.Unspecified);
      Content = image;

      mDwg = LoadDrawing ();
      mPolygons.AddRange (mDwg.Polys);
      mDwg = new (); mDwg.Add (mPolygons.RemoveLast ());
      DispatcherTimer timer = new () {
         Interval = TimeSpan.FromMilliseconds (20), IsEnabled = true,
      };
      timer.Tick += NextFrame;
   }
   readonly GrayBMP mBmp;
   readonly int mScale = 1;
   readonly List<Polygon> mPolygons = new ();

   void NextFrame (object s, EventArgs e) {
      using (new BlockTimer ("Leaf")) {
         mBmp.Begin ();
         if (mPolygons.Count > 0)
            mDwg.Add (mPolygons.RemoveLast ());
         DrawLeaf ();
         // DrawFatLines ();
         mBmp.End ();
      }
   }

   void DrawFatLines () {
      mBmp.Begin ();
      mBmp.Clear (192);
      int thick = 2;
      for (int x = 50; x < 850; x += 100, thick += 5) 
         mBmp.DrawThickLine (new Point2(50, 550), new (x, 50), thick, 0);
      mBmp.End ();
   }

   void DrawLeaf () {
      mBmp.Begin ();
      mBmp.Clear (192);

      Matrix2 xfm1 = Matrix2.Rotation (mRotate * Math.PI / 180);
      var bound = mDwg.GetBound (xfm1);
      var xfm2 = ComposeViewXfm (bound, mBmp.Width, mBmp.Height, 20);

      Matrix2 xfm = xfm1 * xfm2;
      mPF.Reset (); 
      var segs = mDwg.EnumLines (xfm).ToArray ();
      foreach (var (a, b) in segs) mPF.AddLine (a, b);
      mPF.Fill (mBmp, 255);

      foreach (var (a, b) in segs) mBmp.DrawLine (a, b, 0);

      var hull = mDwg.ConvexHull.Select (p => p * xfm); var last = hull.Last ();
      foreach (var pt in hull) {
         mBmp.DrawLine (last, pt, 0);
         last = pt;
      }

      mBmp.End ();
      mRotate++;
   }
   PolyFill mPF = new ();
   int mRotate = 45;

   Matrix2 ComposeViewXfm (Bound2 bound, int width, int height, int margin) {
      double xScale = ((width - margin * 2) / bound.Width),
             yScale = ((height - margin * 2) / bound.Height);
      double scale = Math.Min (xScale, yScale);
      return Matrix2.Translation (new Vector2 (-bound.Midpoint.X, -bound.Midpoint.Y))
           * Matrix2.Scaling (scale)
           * Matrix2.Translation (new Vector2 (width / 2, height / 2));
   }

   Drawing LoadDrawing () {
      Drawing dwg = new ();
      //using (var stm = File.OpenRead ("C:/Etc/leaf3-fill.txt"))
      using (var stm = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("GrayBMP.Data.leaf-poly.txt"))
      using (var sr = new StreamReader (stm)) {
         List<Point2> pts = new ();
         int cPolys = int.Parse (sr.ReadLine ());
         for (int i = 0; i < cPolys; i++) {
            pts.Clear ();
            int cNodes = int.Parse (sr.ReadLine ());
            for (int j = 0; j < cNodes; j++) {
               double[] w = sr.ReadLine ().Trim ().Split ().Select (double.Parse).Select (a => a * 3.5).ToArray ();
               pts.Add (new (w[0] + 20, w[1] + 20));
            }
            dwg.Add (new Polygon (pts));
         }
      }
      return dwg;
   }
   Drawing mDwg;
}

static class Extensions {
   // Returns and removes the last point.
   public static T RemoveLast<T> (this List<T> list) {
      T data = list[^1]; list.RemoveAt (list.Count - 1); return data;
   }
}