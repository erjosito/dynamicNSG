using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace dynamicNSG.Models
{
    public class Rules
    {
        [Key]
        public int Id { get; set; }

        [RegularExpression("name|os|tag:.+", ErrorMessage = "Can be one of these: name, os, tag:xx")]
        public string Operand1 { get; set; }

        [RegularExpression("contains|startswith|endswith|equals", ErrorMessage = "Can be one of these: contains, startswith, endswith, equals")]
        public string Operator { get; set; }

        public string Operand2 { get; set; }

        public string Description { get; set; }

        public string GroupId { get; set; }
    }
}