using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVM_Base.Model
{
    public class IdentifierService
    {
        private string failed = "failed";
        /// <summary>
        /// 失敗識別子
        /// </summary>
        public string Failed
        {
            get => failed;
        }

        private string canceled = "canceled";
        /// <summary>
        /// 失敗識別子
        /// </summary>
        public string Canceled
        {
            get => canceled;
        }
    }
}
