using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialAssistant
{
    class Point
    {
        // 构造函数
        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        // 坐标x
        public double X { get; set; }

        // 坐标y
        public double Y { get; set; }
    }
}
