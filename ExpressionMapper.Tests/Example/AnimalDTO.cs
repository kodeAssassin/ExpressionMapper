using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionMapper.Tests.Example
{
    /// <summary>
    /// Example Classes representing a DTO with Properties/Fields with matching names and matched and 
    /// mis-matched types
    /// </summary>
    public class AnimalDTO: BaseDTO
    {
        public string Name { get; set; }
        public Nullable<int> Age { get; set; }
        public int Weight { get; set; }
        public bool IsTame { get; set; }
        public String Id { get; set; }
        public String Price { get; set; }
        public bool IsFlat { get; set; }
        public String Imported { get; set; }
        public Nullable<bool> SpecialDiet { get; set; }
        public Nullable<int> Endangered { get; set; }
        public Nullable<Guid> Code { get; set; }
        public string Species { get; set; }
        public string Sound { get; set; }

        public String Color;
        public bool IsPredator;

        public DateTime Updated { get; set; }
        public List<String> HandlerIds { get; set; }
    }
}
