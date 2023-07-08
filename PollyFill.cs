namespace GrayBMP;
class PolyFill {
   public void AddLine (int x0, int y0, int x1, int y1) {
      var pt1 = (x0, y0); var pt2 = (x1, y1);
      if (pt1 == pt2) return;
      // Adjust segment ends along the sweep direction (at a lower y and then lower x).
      if (Compare (pt1, pt2) > 0) (pt1, pt2) = (pt2, pt1);
      Add (pt1); Add (pt2);

      void Add ((int x, int y) pt) {
         if (pt.x < xLeft) xLeft = pt.x;
         if (pt.x > xRight) xRight = pt.x;
         mPts.Add (pt);
      }
   }

   public void Fill (GrayBMP bmp, int color) => Fill (mPts.ToArray (), bmp, color);

   // Fill polygon using sweepline
   void Fill ((int x, int y)[] pts, GrayBMP bmp, int color) {
      // Build event queue by adding enter and exit events for every 'edge'.
      // We use an array here as we never add any new event during the scan.
      int[] events = Enumerable.Range (0, pts.Length).ToArray ();
      // Sort events list to event queue.
      Array.Sort (events, CompareEvent);

      // Scan range.
      int yStart = pts[events[0]].y, yEnd = -1;
      // The active-edge-list
      List<int> ael = new ();
      // The intersection points at a given 'y'.
      List<double> xi = new ();
      // Process events from event queue.
      for (int i = 1; i < events.Length; i++) {
         int id = events[i];
         // record the event point
         yEnd = pts[id].y;
         bool enter = id % 2 == 0; // Enter event?
         if (enter) {
            // Add an edge to the active edge list.
            ael.Add (id);
            HandleEvent ();
         } else {
            // Remove the edge from the active list.
            HandleEvent ();
            ael.Remove (id - 1);
         }
      }

      // Move through the scan range and fill region within polygons.
      void HandleEvent () {
         if (ael.Count < 2) return;
         for (int y = yStart; y < yEnd; y++) {
            xi.Clear (); // Clear intersection list.
            // yScan is y shifted by 0.5.
            double yS = y + 0.5;
            foreach (var edge in ael) {
               // Get the edge and the corresponding end points.
               var (x1, y1) = pts[edge]; var (x2, y2) = pts[edge + 1];
               // Test intersection and record the inersection point
               if (!Geo.Intersects (xLeft, yS, xRight, yS, x1, y1, x2, y2, out var x, out _)) continue;
               xi.Add (x);
            }
            xi.Sort ();
            for (int i = 0; i < xi.Count; i += 2)
               bmp.DrawHorizontalLine ((int)xi[i], (int)xi[i + 1], y, color);
         }
         yStart = yEnd;
      }

      int CompareEvent (int id1, int id2) => Compare (pts[id1], pts[id2]);
   }

   // Fill polygons by checking all intersections (brute-force).
   void FillSlow ((int x, int y)[] pts, GrayBMP bmp, int color) {
      List<double> xi = new (); // Intersection points.
      int yMin = pts.Min (pt => pt.y), yMax = pts.Max (pt => pt.y);

      for (int y = yMin; y <= yMax; y++) {
         double yS = y + 0.5;
         xi.Clear ();
         for (int i = 0; i < pts.Length; i += 2) {
            var pt1 = pts[i]; var pt2 = pts[i + 1];
            if (!Geo.Intersects (xLeft, yS, xRight, yS, pt1.x, pt1.y, pt2.x, pt2.y, out var x, out _)) continue;
            xi.Add (x);
         }
         xi.Sort ();
         for (int i = 0; i < xi.Count; i += 2)
            bmp.DrawHorizontalLine ((int)xi[i], (int)xi[i + 1], y, color);         
      }
   }

   // Max Scan width
   int xLeft = int.MaxValue, xRight = int.MinValue;
   readonly List<(int x, int y)> mPts = new ();

   // Compares event points along the sweep direction.
   static int Compare ((int x, int y) pt1, (int x, int y) pt2) {
      var res = pt1.y.CompareTo (pt2.y); if (res != 0) return res;
      return pt1.x.CompareTo (pt2.x);
   }
}

/// <summary>This class contains reusable utilities used in this application.</summary>
public static class Geo {
   public const double Epsilon = 1E-8;

   /// <summary>Checks if a real number is equal to zero within EPSILON.</summary>
   public static bool IsZero (this double f) => Math.Abs (f) < Epsilon;

   /// <summary>
   /// Computes intersection between two lines segements passing through 
   /// (x1, y1)-(x2, y2) and (x3, y3)-(x4, y4) respectively.
   /// </summary>
   /// <returns>True if the segments intersect, false otherwise.</returns>
   public static bool Intersects (
      double x1, double y1, double x2, double y2,
      double x3, double y3, double x4, double y4,
      out double x, out double y) {
      x = double.NaN; y = double.NaN;
      double Ax = x2 - x1, Ay = y2 - y1;
      double Bx = x3 - x4, By = y3 - y4;
      double Cx = x1 - x3, Cy = y1 - y3;
      var alpha = By * Cx - Bx * Cy;
      var denom = Ay * Bx - Ax * By;

      bool iIntersects = true;

      if (denom.IsZero ()) {
         iIntersects = false;
      } else {
         if (denom > 0) {
            // lie check on first segment
            if (alpha < 0 || alpha > denom) {
               iIntersects = false;
            }
         } else if (alpha > 0 || alpha < denom) { // lie check on first segment
            iIntersects = false;
         }

         if (iIntersects) {
            var beta = Ax * Cy - Ay * Cx;
            if (denom > 0) {
               // lie check on second segment
               if (beta < 0 || beta > denom) {
                  iIntersects = false;
               }
            } else if (beta > 0 || beta < denom) { // lie check on second segment
               iIntersects = false;
            }
         }
      }

      if (iIntersects) {
         alpha /= denom;
         x = x1 + alpha * Ax;
         y = y1 + alpha * Ay;
      }

      return iIntersects;
   }
}
