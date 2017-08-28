using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace dynamicNSG.Models
{
    public class NIC
    {
        [DisplayName("NIC name")]
        public string NicId { get; set; }
        [DisplayName("VM name")]
        public string VmId { get; set; }

    }
}