using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionMapper.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this String candidate) 
        {
            return String.IsNullOrEmpty(candidate);
        }
    }
}
