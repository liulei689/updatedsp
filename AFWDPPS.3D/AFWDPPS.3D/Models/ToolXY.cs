using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp3D.Models
{
    public  class ToolInfo
    {
        public double PointX { get; set; }
        public double PointY { get; set; }
        public double PointZ { get; set; }

        public double OffSetX { get; set; }
        public double OffSetY { get; set; }
        public double OffSetZ { get; set; }

        public string ToolType {  get; set; }

        public string ToolName { set; get; }


    }
}
