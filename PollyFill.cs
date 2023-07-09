using System.Diagnostics.CodeAnalysis;

namespace GrayBMP;
class PolyFill {
   public void AddLine (int x0, int y0, int x1, int y1) {
      Point pt1 = new (x0, y0), pt2 = new (x1, y1);
      if (pt1.Y == pt2.Y) return;
      mLines.Add (new Line (pt1, pt2));
   }

   public void Fill (GrayBMP bmp, int color) => FillFast (bmp, color);

   // Fill polygon using sweepline
   void FillFast (GrayBMP bmp, int color) {
      // Build event queue by adding enter and exit events for every 'edge'.
      // We use an array here as we never add any new event during the scan.     
      List<Event> events = new (2 * mLines.Count);
      for (int i = 0; i < mLines.Count; i++) {
         var L = mLines[i];
         events.Add (new Event (L.A.Y, i, true));
         events.Add (new Event (L.B.Y, i, false));
      }
      // Sort events list to event queue.
      events.Sort ();

      // Scan range.
      int yStart = events[0].Y;
      // The active-edge-list
      List<int> ael = new (64);
      // Process events from event queue.
      foreach (var e in events) {
         if (e.Enter) {
            // Add an edge to the active edge list.
            ael.Add (e.Line);
         } else {
            // Handle the event and update ael.
            if (yStart != e.Y) {
               // Move through the scan range and fill region within polygons.
               Fill (bmp, color, yStart, e.Y, ael);
               yStart = e.Y;
            }
            // Remove the edge from the active list.
            ael.Remove (e.Line);
         }
      }
   }

   // Fill polygons by checking all intersections (brute-force).
   void FillSlow (GrayBMP bmp, int color) {
      int yMin = mLines.Min (pt => pt.A.Y), yMax = mLines.Max (pt => pt.B.Y);
      Fill (bmp, color, yMin, yMax, Enumerable.Range (0, mLines.Count).ToList ());
   }

   void Fill (GrayBMP bmp, int color, int yStart, int yEnd, List<int> lines) {
      for (int y = yStart; y < yEnd; y++) {
         mX.Clear (); // Clear intersection list.
         double yS = y + 0.5; // yScan is y shifted by 0.5.
         foreach (var id in lines) {
            // Test intersection and record the inersection point
            if (!mLines[id].GetXLieAtY (yS, out var x)) continue;
            mX.Add (x);
         }
         mX.Sort ();
         for (int i = 0; i < mX.Count; i += 2)
            bmp.DrawHorizontalLine ((int)mX[i], (int)mX[i + 1], y, color);
      }
   }
   // The intersection points at a given 'y'.
   readonly List<double> mX = new (64);

   // Max Scan width
   readonly List<Line> mLines = new ();

   // An integer point on Polygon, tuned for the sweepline operations
   readonly struct Point : IComparable<Point> {
      public Point (int x, int y) => (X, Y) = (x, y);

      public readonly int X;
      public readonly int Y;

      #region Interface methods --------------------------------------
      public readonly int CompareTo (Point other) {
         var res = Y.CompareTo (other.Y); if (res != 0) return res;
         return X.CompareTo (other.X);
      }
      #endregion

      #region Inequality Operators -----------------------------------
      public static bool operator > (in Point a, in Point b) => a.CompareTo (b) > 0;
      public static bool operator < (in Point a, in Point b) => a.CompareTo (b) < 0;
      #endregion
   }

   // A line segment of the Polygon.
   readonly struct Line {
      public Line (in Point start, in Point end) {
         // Adjust segment ends along the sweep direction (at a lower y and then lower x).
         (A, B) = start > end ? (end, start) : (start, end);
         mDxDy = B.X - A.X; mDxDy /= (B.Y - A.Y);
      }

      // Start point
      public readonly Point A;
      // End point.
      public readonly Point B;

      // Returns the x coordinate of the intersection point between this line
      // and another line parallel to the X-Axis at a given y.
      public readonly bool GetXLieAtY (double y, out double x) {
         x = double.NaN;
         if (y < A.Y || y > B.Y) return false;
         x = A.X; if (A.X != B.X) x += (y - A.Y) * mDxDy;
         return true;
      }
      // The cached inverse slope of this line.
      readonly double mDxDy;
   }

   // Event points of the sweepline. We only need 'enter' and 'exit' events.
   readonly struct Event : IComparable<Event> {
      public Event (int y, int line, bool enter) => (Y, Line, Enter) = (y, line, enter);
      public readonly bool Enter;         // Is this an enter event? Otherwise it is an exit event.
      public readonly int Y;              // The event point.
      public readonly int Line;           // Line this event belongs to.

      public readonly int CompareTo (Event other) => Y.CompareTo (other.Y);
   }
}
