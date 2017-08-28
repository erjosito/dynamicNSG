using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace dynamicNSG.Models
{
    public class Group
    {
        public string GroupId { get; set; }

        [DisplayName("Group Name")]
        public string GroupName { get; set; }
    }
}