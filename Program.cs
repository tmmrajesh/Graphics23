using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace A25;

class MyWindow : Window {
   public MyWindow () {
      Width = 800; Height = 600;
      Left = 50; Top = 50;
      WindowStyle = WindowStyle.None;
      Image image = new Image () {
         Stretch = Stretch.None,
         HorizontalAlignment = HorizontalAlignment.Left,
         VerticalAlignment = VerticalAlignment.Top,
      };
      RenderOptions.SetBitmapScalingMode (image, BitmapScalingMode.NearestNeighbor);
      RenderOptions.SetEdgeMode (image, EdgeMode.Aliased);

      mBmp = new WriteableBitmap ((int)Width, (int)Height,
         96, 96, PixelFormats.Gray8, null);
      mStride = mBmp.BackBufferStride;
      image.Source = mBmp;
      Content = image;
      //DrawMandelbrot (-0.5, 0, 1);
      DrawRect (50, 60, 300, 200);
      DrawRect (500, 500, 450, 150);
      DrawRect (200, 350, 50, 550);
   }

   void DrawRect (int xs, int ys, int xe, int ye, bool diag = true) {
      DrawLine (xs, ys, xs, ye);
      DrawLine (xs, ye, xe, ye);
      DrawLine (xe, ye, xe, ys);
      DrawLine (xe, ys, xs, ys);
      if (diag) {
         DrawLine (xs, ys, xe, ye);
         DrawLine (xs, ye, xe, ys);
      }
   }

   void DrawLine (int x0, int y0, int x1, int y1) {
      var (dx, dy) = (x1 - x0, y1 - y0);
      // Prepare the co-ordinates for the normalized drawing where
      // we always draw along the increasing x 
      bool swapXY = Math.Abs (dy) > Math.Abs (dx);
      if (swapXY) (x0, y0, x1, y1, dx, dy) = (y0, x0, y1, x1, dy, dx);
      if (dx < 0) (x0, y0, x1, y1, dx, dy) = (x1, y1, x0, y0, -dx, -dy);
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;

         //// Floating point arithmatic.
         //var slope = (double)(y1 - y0) / (x1 - x0);
         //for (int x = x0, y = y0; x < x1; x++) {
         //   Set (x, y);
         //   y = y0 + (int)(slope * (x - x0));
         //}

         // Integer arithmatic
         int yi = 1;
         if (dy < 0) (yi, dy) = (-1, -dy);
         var d = (2 * dy) - dx;

         for (int x = x0, y = y0; x <= x1; x++) {
            Draw (x, y);
            if (d > 0) {
               y += yi;
               d -= 2 * dx;
            }
            d += 2 * dy;
         }
      } finally {
         mBmp.AddDirtyRect (new Int32Rect (x0, y0, dx + 1, dy + 1));
         mBmp.Unlock ();
      }

      void Draw (int x, int y) { 
         if (swapXY) SetPixel (y, x, 255);
         else SetPixel (x, y, 255); 
      }
   }

   void DrawMandelbrot (double xc, double yc, double zoom) {
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         int dx = mBmp.PixelWidth, dy = mBmp.PixelHeight;
         double step = 2.0 / dy / zoom;
         double x1 = xc - step * dx / 2, y1 = yc + step * dy / 2;
         for (int x = 0; x < dx; x++) {
            for (int y = 0; y < dy; y++) {
               Complex c = new Complex (x1 + x * step, y1 - y * step);
               SetPixel (x, y, Escape (c));
            }
         }
         mBmp.AddDirtyRect (new Int32Rect (0, 0, dx, dy));
      } finally {
         mBmp.Unlock ();
      }
   }

   byte Escape (Complex c) {
      Complex z = Complex.Zero;
      for (int i = 1; i < 32; i++) {
         if (z.NormSq > 4) return (byte)(i * 8);
         z = z * z + c;
      }
      return 0;
   }

   void OnMouseMove (object sender, MouseEventArgs e) {
      if (e.LeftButton == MouseButtonState.Pressed) {
         try {
            mBmp.Lock ();
            mBase = mBmp.BackBuffer;
            var pt = e.GetPosition (this);
            int x = (int)pt.X, y = (int)pt.Y;
            SetPixel (x, y, 255);
            mBmp.AddDirtyRect (new Int32Rect (x, y, 1, 1));
         } finally {
            mBmp.Unlock ();
         }
      }
   }

   void DrawGraySquare () {
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         for (int x = 0; x <= 255; x++) {
            for (int y = 0; y <= 255; y++) {
               SetPixel (x, y, (byte)x);
            }
         }
         mBmp.AddDirtyRect (new Int32Rect (0, 0, 256, 256));
      } finally {
         mBmp.Unlock ();
      }
   }

   void SetPixel (int x, int y, byte gray) {
      unsafe {
         var ptr = (byte*)(mBase + y * mStride + x);
         *ptr = gray;
      }
   }

   WriteableBitmap mBmp;
   int mStride;
   nint mBase;
}

internal class Program {
   [STAThread]
   static void Main (string[] args) {
      Window w = new MyWindow ();
      w.Show ();
      Application app = new Application ();
      app.Run ();
   }
}
