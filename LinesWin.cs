// LinesWin.cs - Demo window for testing the DrawLine and related functions
// ---------------------------------------------------------------------------------------
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Reflection;
using System.IO;

namespace GrayBMP;

class LinesWin : Window {
   public LinesWin () {
      Width = 900; Height = 600;
      Left = 200; Top = 50;
      WindowStyle = WindowStyle.None;
      mBmp = new GrayBMP (Width, Height);

      Image image = new () {
         Stretch = Stretch.None,
         HorizontalAlignment = HorizontalAlignment.Left,
         VerticalAlignment = VerticalAlignment.Top,
         Source = mBmp.Bitmap
      };
      RenderOptions.SetBitmapScalingMode (image, BitmapScalingMode.NearestNeighbor);
      RenderOptions.SetEdgeMode (image, EdgeMode.Aliased);
      Content = image;
      mDX = mBmp.Width; mDY = mBmp.Height;

      using var stm = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("GrayBMP.res.leaf-fill.txt");
      StreamReader reader = new (stm);
      for (var line = reader.ReadLine (); line != null; line = reader.ReadLine ()) {
         if (string.IsNullOrEmpty (line.Trim ())) continue;
         var pt = line.Split ().Select (int.Parse).ToArray ();
         mPollyFill.AddLine (pt[0], pt[1], pt[2], pt[3]);
      }
      //NextFrame (null, EventArgs.Empty);
      //return;

      // Start a timer to repaint a new frame every 33 milliseconds
      DispatcherTimer timer = new () {
         Interval = TimeSpan.FromMilliseconds (100), IsEnabled = true,
      };
      timer.Tick += NextFrame;
   }
   PolyFill mPollyFill = new ();

   readonly GrayBMP mBmp;
   readonly int mDX, mDY;

   void NextFrame (object sender, EventArgs e) {
      using (new BlockTimer ("Lines")) {
         mBmp.Begin ();
         mBmp.Clear (0);
         for (int i = 0; i < 10; i++) {
            mPollyFill.Fill (mBmp, R.Next (155, 255));
         }
         mBmp.End ();
      }
   }
   Random R = new ();
}

class BlockTimer : IDisposable {
   public BlockTimer (string message) {
      mStart = DateTime.Now;
      mMessage = message;
   }
   readonly DateTime mStart;
   readonly string mMessage;

   public void Dispose () {
      int elapsed = (int)((DateTime.Now - mStart).TotalMilliseconds + 0.5);
      Console.WriteLine ($"{mMessage}: {elapsed}ms");
   }
}