using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgHomeBot.Common.Contract;

public interface ILogFileProvider
{
    IReadOnlyList<string> GetLogFileList();

    Stream? GetLogFileContent(string filename, CancellationToken cancellationToken);
}
