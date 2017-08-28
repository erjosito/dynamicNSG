using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace dynamicNSG.Models
{
    public class IP
    {
        [Key]
        [DisplayName("IP address")]
        [RegularExpression("[0-9]{1,3}%2E;{1,3}%2E;[0-9]{1,3}%2E;[0-9]{1,3}")]
        public string Id { get; set; }
        [DisplayName("NIC name")]
        public string NicId { get; set; }

    }
}