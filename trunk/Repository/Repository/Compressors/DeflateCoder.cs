//-----------------------------------------------------------------------------
// <copyright file="DeflateCoder.cs" company="BFS">
//      Copyright © 2010 Vasily Kabanov
//      All rights reserved.
// </copyright>
// <created>2/2/2010 11:27:10 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using bfs.Repository.Util;

namespace bfs.Repository.Compressors
{
	/// <summary>
	///		Encoding and decoding with standard <see cref="DeflateStream"/>.
	/// </summary>
	/// <remarks>
	///		It is fast but compression ratio is mediocre.
	/// </remarks>
	public class DeflateCoder : Interfaces.ICoder
	{
		#region const declarations --------------------------------------------
	
		/// <summary>
		///		The coder's native identifier.
		/// </summary>
		public const string NativeKey = "gzip";

		#endregion const declarations -----------------------------------------

		/// <summary>
		///		Create new instance and associate it with its native code (<see cref="DeflateCoder.NativeKey"/>).
		/// </summary>
		public DeflateCoder()
			: this(NativeKey)
		{}

		/// <summary>
		///		Create new instance and associate it with the specified key.
		/// </summary>
		/// <param name="keyCode">
		///		The code with which to associate the coder. Must be lower case and must not contain illegal file name characters.
		/// </param>
		/// <exception cref="ArgumentException">
		///		<paramref name="keyCode"/> contains characters illegal for file names.
		/// </exception>
		public DeflateCoder(string keyCode)
		{
			Util.Check.DoRequireArgumentNotNull(keyCode, "keyCode");

			CheckHelper.CheckRealCoderCode(keyCode: keyCode);
			
			KeyCode = keyCode;
		}


		#region ICoder Members ------------------------------------------------

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
		public string KeyCode
		{ get; private set; }

		/// <summary>
		///		Create stream accepting raw data, encoding it and outputting encoded data into the specified output stream.
		/// </summary>
		/// <param name="output">
		///		Stream to receive encoded data.
		/// </param>
		/// <returns>
		///		Stream ready to accept and encode raw data.
		/// </returns>
		/// <remarks>
		///		Remember to follow IDisposable contract and close/dispose the created stream after writing all data into it. Flushing it is not enough.
		/// </remarks>
		public Stream WrapInEncodingStream(Stream output)
		{
			return new DeflateStream(output, CompressionMode.Compress);
		}

		/// <summary>
		///		Create stream reading and decoding data from another stream containing encoded data.
		/// </summary>
		/// <param name="encodedDataStream">
		///		Stream containing encoded source data, correctly positioned.
		/// </param>
		/// <returns>
		///		Remember to follow IDisposable contract and close/dispose the created stream after reading all data from it.
		/// </returns>
		public Stream WrapInDecodingStream(Stream encodedDataStream)
		{
			return new DeflateStream(encodedDataStream, CompressionMode.Decompress);
		}

		#endregion ICoder Members ---------------------------------------------

	}
}
