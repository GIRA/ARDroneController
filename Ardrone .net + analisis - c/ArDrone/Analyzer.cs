using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ARDrone
{
    class Analyzer
    {
        #region Color
        
        public static Rectangle FindColor(Bitmap anImage, Color aColor)
        {
            int top, bottom, left, right;
            top = bottom = left = right=0;
            foreach (Point p in GetSamplePoints(anImage,aColor))
            {
                if (top == 0 || top > p.Y) top = p.Y;
                if (bottom == 0 || bottom < p.Y) bottom = p.Y;
                if (left == 0 || left > p.X) left = p.X;
                if (right == 0 || right < p.X) right = p.X;
            }
            int width = right - left;
            int height = bottom - top;
            return new Rectangle(left, top, width, height);   
        
        }
        private static double getDistance(Point A, Point B)
        {
            return (Math.Sqrt(Math.Pow(A.X - B.X, 2) + Math.Pow(A.Y - B.Y, 2)));
        }

        static int _skip = 4;
        private static List<Point> GetSamplePoints(Bitmap _image, Color aColor)
        {
            List<Point> puntos = new List<Point>();
            double promx, promy;
            promx = promy = 0;
            for (int x = 0; x < _image.Width / _skip; x++)
            {
                for (int y = 0; y < _image.Height / _skip; y++)
                {
                    Color px = _image.GetPixel(x * _skip, y * _skip);
                    bool valid = false;
                    Point Current = new Point(x * _skip, y * _skip);
                    if (ColorMatches(aColor, px,20))
                    {
                        valid = true;
                        foreach (Point p in puntos)
                        {
                            if (getDistance(p, Current) < 5)
                            {
                                valid = false;
                                break;
                            }

                        }
                    }

                    if (valid)
                    {
                        promx += Current.X;
                        promy += Current.Y;

                        puntos.Add(Current);

                    }

                }


            }
            List<Point> _p2 = new List<Point>();

            if (puntos.Count > 0)
            {
                Point prom = new Point((int)(promx / puntos.Count), (int)(promy / puntos.Count));
                foreach (Point p in puntos)
                {
                    if (getDistance(prom, p) < 40)
                    {
                        _p2.Add(p);
                    }

                }
            }
            return _p2;
        }
        public static bool ColorMatches(Color A, Color B,double tolerance)
        {
            if (Math.Abs(A.R - B.R) < tolerance)
            {
                if (Math.Abs(A.G - B.G) < tolerance)
                {
                    if (Math.Abs(A.B - B.B) < tolerance)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        

         
    }

}
