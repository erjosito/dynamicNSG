using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace dynamicNSG.Models
{
    public class Tag
    {
        public int Id { get; set; }

        public string VmId { get; set; }

        public string TagName { get; set; }

        public string TagValue { get; set; }
    }
}