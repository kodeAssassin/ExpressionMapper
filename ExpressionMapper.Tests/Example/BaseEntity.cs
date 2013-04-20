using System;

namespace ExpressionMapper.Tests.Example
{
    public class BaseEntity
    {
        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public Guid TrackId { get; set; }
    }
}
