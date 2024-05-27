using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GomBuild.Model
{
    public class GridViewFileInfo
    {
        public GridViewFileInfo() { }
        public string NAME { get; set; }

        public string EXTEND { get; set; }
        public int? Type { get; set; }
        public string Status { get; set; }
        public GridViewFileInfo(string NAME, string EXTEND, int type, string Status)
        {
            this.NAME = NAME;
            this.EXTEND = EXTEND;
            Type = type;
            this.Status = Status;
        }
    }
}
