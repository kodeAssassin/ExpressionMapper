using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionMapper.Tests.Example
{
    /// <summary>
    /// Example classes representing a Model with Properties/Fields with matching names and matched and 
    /// mis-matched types
    /// </summary>
    public class Animal : BaseEntity
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public int Weight { get; set; }
        public Nullable<bool> IsTame { get; set; }
        public Guid Id { get; set; }
        public decimal Price { get; set; }
        public String IsFlat { get; set; }
        public bool Imported { get; set; }
        public bool SpecialDiet { get; set; }
        public Nullable<bool> Endangered { get; set; }
        public String Code { get; set; }
        public string Sound { get; set; }
        public string Species { get; set; }

        public String Color;
        public Nullable<bool> IsPredator;

        public List<int> HandlerIds { get; set; }
    }
}
