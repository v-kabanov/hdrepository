using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using bfs.Repository.Util;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
    /// <summary>
    ///		The class to hold results of a data file seek.
    /// </summary>
    public class SeekFileResult
    {
        public SeekFileResult(DateTime seekTime, IDataFolder foundFolder, IRepositoryFileName foundFile)
        {
            this.SeekDateTime = seekTime;
			this.RepositoryFile = new RepositoryFile(containingFolder: foundFolder, fileName: foundFile);
        }

		public SeekFileResult(DateTime seekTime, IRepositoryFile file)
		{
			this.SeekDateTime = seekTime;
			this.RepositoryFile = file;
		}

        /// <summary>
		///		Check is this seek is valid for the specified seek time.
		/// </summary>
		/// <remarks>
		///		Seek results are valid for for all seeks with the target datetime between original seek datetime
		///		and last (according to the <see cref="Direction"/>) items timestamp in the found data file
        ///		if any. If no data file was found the upper limit does not apply.
		/// </remarks>
		public bool IsValidFor(DateTime seekDateTime)
        {
			if (Direction == EnumerationDirection.Forwards)
			{
				return this.SeekDateTime <= seekDateTime
					&& (!this.IsSuccessful || this.DataFileName.LastItemTimestamp >= seekDateTime);
			}
			else
			{
				return this.SeekDateTime >= seekDateTime
					&& (!this.IsSuccessful || this.DataFileName.FirstItemTimestamp <= seekDateTime);
			}
        }

		public EnumerationDirection Direction
		{ get; private set; }

		public IRepositoryFile RepositoryFile
		{ get; private set; }

        /// <summary>
        ///     Get target sought timestamp
        /// </summary>
        public DateTime SeekDateTime
        { get; private set; }

        /// <summary>
        ///     Get found data folder - first data folder which contains data items dated
        ///     at or after <see cref="SeekDateTime"/>
        /// </summary>
        public IDataFolder DataFolder
        {
			get { return IsSuccessful ? this.RepositoryFile.ContainingFolder : null; }
		}

        /// <summary>
        ///     Get found data file - data folder which contains data items dated
        ///     at or after <see cref="SeekDateTime"/>
        /// </summary>
        public IRepositoryFileName DataFileName
		{ get { return IsSuccessful ? this.RepositoryFile.Name : null; } }

        /// <summary>
        ///     Check whether file seek was successful. If not, <see cref="DataFileName"/>
        ///     will return <see langword="null"/>
        /// </summary>
        public bool IsSuccessful
        {
            get
            {
                return null != this.RepositoryFile;
            }
        }
    }
}
