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
               if (!GetXLieAtY (x1, y1, x2, y2, yS, out var x)) continue; 
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
            if (!GetXLieAtY (pt1.x, pt1.y, pt2.x, pt2.y, yS, out var x)) continue;
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

   static bool GetXLieAtY (int x1, int y1, int x2, int y2, double y, out double x) {
      x = double.NaN;
      if (y1 == y2) return false; // Not expecting any other line parallel to x-axis at 0.5 offest
      else if ((y1 < y2) && (y < y1 || y > y2)) return false;
      else if ((y1 > y2) && (y < y2 || y > y1)) return false;
      x = x1;
      if (x1 != x2) {
         double dx = x2 - x1;
         x += (y - y1) * (dx / (y2 - y1));
      }
      return true;
   }
}
