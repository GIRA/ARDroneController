using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ARDrone.Control.Utils.Mio
{
  public  class Analyzer
    {
        /*PROBLEMA ACA PUEDE PINCHAR POR MANEJO D MEMORIA
      STACK IMBALANCE*/

      [DllImport("ImageAnalyzer", CallingConvention = CallingConvention.Cdecl)]
      public static extern int findAllRectangles(ref ushort image, int width, int height, ref int results);
      [DllImport("ImageAnalyzer", CallingConvention = CallingConvention.Cdecl)]
      public static extern void trackMainRectangle(ref ushort image, int width, int height, ref int results);

        [DllImport("ImageAnalyzer", CallingConvention= CallingConvention.Cdecl)]
        public static extern void setHue(double a, double b);
        [DllImport("ImageAnalyzer", CallingConvention = CallingConvention.Cdecl)]
        public static extern void setSaturation(double a, double b);
        [DllImport("ImageAnalyzer", CallingConvention = CallingConvention.Cdecl)]
        public static extern void setBrightness(double a, double b);
 
        [DllImport("ImageAnalyzer", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RGBtoHSV(double r, double g,double b, ref double h, ref double s, ref double v);     
    }

}
