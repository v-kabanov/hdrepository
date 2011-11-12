//-----------------------------------------------------------------------------
// <created>2/2/2010 1:59:23 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace bfs.Repository.Interfaces
{
	internal interface IRepositoryFileAccessor
	{
		/// <summary>
		///		Write whole file compressing and encrypting if necessary
		/// </summary>
		/// <param name="rawData">
		///		Serialized raw data
		/// </param>
		void Write(Stream rawData, Stream fileStream);

		/// <summary>
		///		Read whole file, decode it and return raw data stream
		/// </summary>
		/// <returns>
		///		Raw data, unencrypted and uncompressed, ready 
		///		for desirialization
		/// </returns>
		Stream Read();
	}
}
