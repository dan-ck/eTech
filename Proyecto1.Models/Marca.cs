﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto1.Models //Changed namespace - Sábado 14
{
    public class Marca
    {
        [Key]
        public int id { get; set; }
        [Required]
        [MaxLength(50)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }
        [Required]
        public bool Estado { get; set; }
    }
}
