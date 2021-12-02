namespace ME3TweaksCore.NativeMods.Interfaces
{
    public interface IUnknownInstalledASIMod : IInstalledASIMod
    {
        /// <summary>
        /// The name of the ASI file
        /// </summary>
        public string UnmappedFilename { get; set; }

        /// <summary>
        /// Description read from the metadata of the file (if any was supplied)
        /// </summary>
        public string DllDescription { get; set; }
    }
}
