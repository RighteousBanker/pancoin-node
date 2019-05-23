using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Panode.CLI
{
    public interface IProgram
    {
        object Run(IServiceProvider serviceProvider, string[] args);
    }
}
