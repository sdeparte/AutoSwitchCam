using System;
using System.Windows;

namespace AutoSwitchCam.Helper
{
    class PointHelper
    {
        public static Point[] CoordonatesToPointArray(double X1, double Y1, double X2, double Y2, double X3, double Y3, double X4, double Y4)
        {
            Point[] points = new Point[4];

            points[0] = new Point(X1, Y1);
            points[1] = new Point(X2, Y2);
            points[2] = new Point(X3, Y3);
            points[3] = new Point(X4, Y4);

            return points;
        }

        public static bool PointInPolygon(Point[] Points, double? X, double? Y)
        {
            if (X == null || Y == null)
            {
                return false;
            }

            // Get the angle between the point and the
            // first and last vertices.
            int max_point = Points.Length - 1;
            double total_angle = GetAngle(Points[max_point].X, Points[max_point].Y, X.Value, Y.Value, Points[0].X, Points[0].Y);

            // Add the angles from the point
            // to each other pair of vertices.
            for (int i = 0; i < max_point; i++)
            {
                total_angle += GetAngle(Points[i].X, Points[i].Y, X.Value, Y.Value, Points[i + 1].X, Points[i + 1].Y);
            }

            // The total angle should be 2 * PI or -2 * PI if
            // the point is in the polygon and close to zero
            // if the point is outside the polygon.
            // The following statement was changed. See the comments.
            //return (Math.Abs(total_angle) > 0.000001);
            return (Math.Abs(total_angle) > 1);
        }

        private static double GetAngle(double Ax, double Ay, double Bx, double By, double Cx, double Cy)
        {
            // Get the dot product.
            double dot_product = DotProduct(Ax, Ay, Bx, By, Cx, Cy);

            // Get the cross product.
            double cross_product = CrossProductLength(Ax, Ay, Bx, By, Cx, Cy);

            // Calculate the angle.
            return (float)Math.Atan2(cross_product, dot_product);
        }
        private static double CrossProductLength(double Ax, double Ay, double Bx, double By, double Cx, double Cy)
        {
            // Get the vectors' coordinates.
            double BAx = Ax - Bx;
            double BAy = Ay - By;
            double BCx = Cx - Bx;
            double BCy = Cy - By;

            // Calculate the Z coordinate of the cross product.
            return (BAx * BCy - BAy * BCx);
        }

        private static double DotProduct(double Ax, double Ay, double Bx, double By, double Cx, double Cy)
        {
            // Get the vectors' coordinates.
            double BAx = Ax - Bx;
            double BAy = Ay - By;
            double BCx = Cx - Bx;
            double BCy = Cy - By;

            // Calculate the dot product.
            return (BAx * BCx + BAy * BCy);
        }
    }
}
