using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.Exceptions
{
    public class ForbiddenException: Exception
    {
        public ForbiddenException(string message): base(message) { }
    }
}
