//-----------------------------------------------------------------------------
// <copyright file="RepositoryFile.cs" company="BFS">
//      Copyright © 2010 Vasily Kabanov
//      All rights reserved.
// </copyright>
// <created>2/2/2010 1:45:55 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using bfs.Repository.Interfaces;
using System.Text.RegularExpressions;

namespace bfs.Repository.Storage
{

	/// <summary>
	///		Encapsulates file name interpretation.
	/// </summary>
	/// <remarks>
	///		 File name format: {start}.{end}.{coder}[.{encryptor}]
	///		 where
	///			- {start} and {end} are DateTime.Ticks as hexadecimal strings or first and last data item timestamp;
	///			- {coder} is the string identifier of a compressor, such as "gzip"
	///			- {encryptor} is the string identifier of an encryptor, optionall if not present, file is not encrypted
	/// </remarks>
	[DebuggerDisplay("{FileName}")]
	public class RepositoryFileName : IRepositoryFileName
	{
		#region const declarations --------------------------------------------

		private const int _nameExpressionGroupIndexFirstTimestamp = 1;
		private const int _nameExpressionGroupIndexLastTimestamp = 2;
		private const int _nameExpressionGroupIndexExtension = 3;
		private const int _nameExpressionGroupIndexEncryption = 4;

		#endregion const declarations -----------------------------------------

		#region fields --------------------------------------------------------

		//
		private static readonly Regex _nameExpression = new Regex(
			@"^([0-9A-F]+)\.([0-9A-F]+)\.(\w+)(\.\w+)?$"
			, RegexOptions.IgnoreCase
			| RegexOptions.CultureInvariant);

		#endregion fields -----------------------------------------------------

		#region constructors --------------------------------------------------

		public RepositoryFileName()
		{
		}


		#endregion constructors -----------------------------------------------

		#region protected and internal methods --------------------------------

		/// <summary>
		///		
		/// </summary>
		/// <param name="fileName">
		///		File name without path
		/// </param>
		/// <returns>
		/// </returns>
		internal static RepositoryFileName GetFileDescriptorImpl(string fileName)
		{
			RepositoryFileName retval = null;
			Match match = _nameExpression.Match(fileName);
			if (match.Success)
			{
				retval = new RepositoryFileName();
				retval.FirstItemTimestamp = new DateTime(
					long.Parse(
						match.Groups[_nameExpressionGroupIndexFirstTimestamp].Value,
						System.Globalization.NumberStyles.HexNumber));
				retval.LastItemTimestamp = new DateTime(
					long.Parse(
						match.Groups[_nameExpressionGroupIndexLastTimestamp].Value,
						System.Globalization.NumberStyles.HexNumber));
				retval.EncryptorCode = match.Groups[_nameExpressionGroupIndexEncryption].Value;
				retval.CompressorCode = match.Groups[_nameExpressionGroupIndexExtension].Value;
			}
			return retval;
		}

		#endregion protected and internal methods -----------------------------

		#region IRepositoryFileName Members

		/// <summary>
		///		Get UTC timestamp of the first item in the file
		/// </summary>
		public DateTime FirstItemTimestamp
		{
			get;
			set;
		}

		/// <summary>
		///		Get UTC timestamp of the last item in the file
		/// </summary>
		public DateTime LastItemTimestamp
		{
			get;
			set;
		}

		public string CompressorCode
		{
			get;
			set;
		}

		public bool Encrypted
		{ get { return !string.IsNullOrEmpty(EncryptorCode); } }

		/// <summary>
		///		Get or set code identifying the encryptor used to encrypt contents of this file.
		/// </summary>
		public string EncryptorCode
		{ get; set; }

		/// <summary>
		///		Get file name with extension, without directory path
		/// </summary>
		public string FileName
		{
			get
			{
				StringBuilder bld = new StringBuilder(60);
				bld.AppendFormat("{0:X}.{1:X}.{2}", FirstItemTimestamp.Ticks, LastItemTimestamp.Ticks, CompressorCode);
				if (this.Encrypted)
				{
					bld.Append(".").Append(EncryptorCode);
				}
				return bld.ToString();
			}
		}

		/// <summary>
		///		Get end of the datetime range of data items in this file (exclusive)
		/// </summary>
		public DateTime End
		{
			get { return LastItemTimestamp.AddTicks(1); }
		}

		/// <summary>
		///		Check whether file datetime range is covering the specified item timestamp. As data files in any given data stream (represented
		///		by repository folder) must not overlap file is covering if the timestamp is between <see cref="FirstItemTimestamp"/> and
		///		<see cref="LastItemTimestamp"/>.
		/// </summary>
		/// <param name="itemTimestamp">
		///		Data item timestamp
		/// </param>
		/// <returns>
		///		<see langword="true"/> if the <paramref name="itemTimestamp"/> is within the range of this file (which means that if there's an item
		///		with the specified timestamp in this data stream, it will live in this file;
		///		<see langword="false"/> otherwise
		/// </returns>
		public bool IsCovering(DateTime itemTimestamp)
		{
			return itemTimestamp >= FirstItemTimestamp && itemTimestamp <= LastItemTimestamp;
		}

		#endregion
	}



}
