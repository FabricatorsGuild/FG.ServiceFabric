using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG.CQRS.Exceptions
{
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    [Serializable]
    public class LoadEventStreamException : Exception
    {
        public LoadEventStreamException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public LoadEventStreamException(string message) : base(message)
        {
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected LoadEventStreamException
        (
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
