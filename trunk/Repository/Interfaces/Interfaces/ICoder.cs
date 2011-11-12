//-----------------------------------------------------------------------------
// <created>2/2/2010 11:25:33 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		Data encoding/decoding abstraction.
	/// </summary>
	public interface ICoder
	{
		/// <summary>
		///		The unique identifier of the coder within repository.
		/// </summary>
		/// <remarks>
		///		Compressors and encryptors codes are embedded into data file name to enable the use of appropriate encoders when reading.
		///		The codes must be lower-case.
		/// </remarks>
		/// <seealso cref="IObjectFactory.DefaultCompressor"/>
		/// <seealso cref="IObjectFactory.GetCompressor(System.String)"/>
		/// <seealso cref="IObjectFactory.GetEncryptor(System.String)"/>
		string KeyCode
		{ get; }

		/// <summary>
		///		Create stream accepting raw data, encoding it and outputting encoded data into the specified output stream.
		/// </summary>
		/// <param name="output">
		///		Stream to receive encoded data.
		/// </param>
		/// <returns>
		///		Stream ready to accept and encode raw data. The stream may not close the <paramref name="output"/> when disposed.
		/// </returns>
		/// <remarks>
		///		Remember to follow IDisposable contract and close/dispose the created stream after writing all data into it. Flushing it is not enough.
		/// </remarks>
		Stream WrapInEncodingStream(Stream output);

		/// <summary>
		///		Create stream reading and decoding data from another stream containing encoded data.
		/// </summary>
		/// <param name="encodedDataStream">
		///		Stream containing encoded source data, correctly positioned.
		/// </param>
		/// <returns>
		///		Remember to follow IDisposable contract and close/dispose the created stream after reading all data from it.
		/// </returns>
		Stream WrapInDecodingStream(Stream encodedDataStream);
	}
}
