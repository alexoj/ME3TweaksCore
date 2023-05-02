using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

namespace ME3TweaksCore.Helpers.MEM
{
    /// <summary>
    /// Information package describing the result of a MassEffectModderNoGui session
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class MEMSessionResult
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
        /// If this session is the 'install' session, in which the game will be modified.
        /// </summary>
        public bool IsInstallSession { get; set; }
        
        /// <summary>
        /// The last file that MEM was operating on. This is useful for debugging crashes and errors in MEM.
        /// </summary>
        public string CurrentFile { get; set; }

        /// <summary>
        /// Adds an error message
        /// </summary>
        /// <param name="msg">The error message</param>
        public void AddError(string msg)
        {
            Errors.Add(msg);
        }

        /// <summary>
        /// Gets a list of the errors
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<string> GetErrors()
        {
            return Errors;
        }

        /// <summary>
        /// If there are any errors in the result
        /// </summary>
        /// <returns></returns>
        public bool HasAnyErrors()
        {
            return Errors.Any();
        }

        /// <summary>
        /// Adds an item to the list of errors at the front
        /// </summary>
        /// <param name="error">The error message</param>
        public void AddFirstError(string error)
        {
            Errors.Insert(0, error);
        }
    }
}
