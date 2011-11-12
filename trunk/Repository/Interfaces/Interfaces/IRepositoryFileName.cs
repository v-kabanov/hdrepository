//-----------------------------------------------------------------------------
// <created>2/17/2010 4:54:56 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		Interface of an object representing data file name.
	/// </summary>
	/// <remarks>
	///		Certain attributes of the data file content are included in the file name to be able to locate data.
	/// </remarks>
	public interface IRepositoryFileName
	{
		/// <summary>
		///		Get timestamp of the first (chronologically) data item in the file (inclusive).
		/// </summary>
		DateTime FirstItemTimestamp
		{ get; set; }

		/// <summary>
		///		Get timestamp of the last (chronologically) data item in the file (inclusive).
		/// </summary>
		DateTime LastItemTimestamp
		{ get; set; }

		/// <summary>
		///		Get the end of the datetime range of data items in this file (exclusive).
		/// </summary>
		/// <remarks>
		///		This is <code>LastItemTimestamp</code> minus minimal distinguishable amound of time (e.g. ns).
		/// </remarks>
		DateTime End
		{ get; }

		/// <summary>
		///		Get or set code of the compressor used to save this file.
		/// </summary>
		/// <remarks>
		///		Compressors are identified by their codes and registered with repository via <see cref="IRepositoryManager.ObjectFactory.AddCompressor(ICoder, bool)"/>.
		/// </remarks>
		///	<seealso cref="IObjectFactory.GetCompressor(string)"/>
		string CompressorCode
		{ get; set; }

		/// <summary>
		///		Whether the file is encrypted.
		/// </summary>
		bool Encrypted
		{ get; }

		/// <summary>
		///		Get or set code of the encryptor used to save this file.
		/// </summary>
		/// <remarks>
		///		Encryptors are identified by their codes and registered with repository via <see cref="IRepositoryManager.ObjectFactory.AddEncryptor(ICoder, bool)"/>.
		/// </remarks>
		///	<seealso cref="IObjectFactory.GetEncryptor(string)"/>
		string EncryptorCode
		{ get; set; }

		/// <summary>
		///		Get file name with extension, without directory path.
		/// </summary>
		string FileName
		{ get; }

		/// <summary>
		///		Check whether file datetime range is covering the specified item timestamp.
		/// </summary>
		/// <param name="itemTimestamp">
		///		Data item timestamp
		/// </param>
		/// <returns>
		///		<see langword="true"/> if the <paramref name="itemTimestamp"/> is within the range of this file (which means that if there's an item
		///		with the specified timestamp in this data stream, it will live in this file;
		///		<see langword="false"/> otherwise
		/// </returns>
		/// <remarks>
		///		As data files in any given data stream (represented by repository folder) must not overlap file is covering if the timestamp is between
		///		<see cref="FirstItemTimestamp"/> and <see cref="LastItemTimestamp"/>.
		/// </remarks>
		bool IsCovering(DateTime itemTimestamp);
	}
}
