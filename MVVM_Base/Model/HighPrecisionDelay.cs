using MVVM_Base.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVM_Base.Model
{
    /// <summary>
    /// 指定msec分待つ
    /// </summary>
    public class HighPrecisionDelay
    {
        public async Task<OperationResult> WaitAsync(int milliseconds, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(milliseconds, token);
                return OperationResult.Success();
            }
            catch(OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }
    }
}
