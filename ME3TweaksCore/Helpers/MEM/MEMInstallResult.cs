using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME3TweaksCore.Helpers.MEM
{
    /// <summary>
    /// Information package describing the result of a MassEffectModderNoGui installation
    /// </summary>
    public class MEMInstallResult
    {
        /// <summary>
        /// The exit code of the application, if any
        /// </summary>
        public int? ExitCode { get; set; }

        private List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// The process ID that was assigned to the application
        /// </summary>
        public int ProcessID { get; set; }

        /// <summary>
        /// Adds an error message
        /// </summary>
        /// <param name="msg">The error message</param>
        public void AddError(string msg)
        {
            Errors.Add(msg);
        }

        public IReadOnlyList<string> GetErrors()
        {
            return Errors;
        }
    }
}
