using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace bfs.Repository.Util
{
	/// <summary>
	///		Binary writer which can be configured not to close underlying stream (flush still done when disposing).
	/// </summary>
	public class BinaryWriterEx : BinaryWriter
	{
		/// <summary>
		///		Create and configure new instance.
		/// </summary>
		/// <param name="output">
		///		The output stream.
		/// </param>
		/// <param name="closeStream">
		///		Whether to close <paramref name="output"/> when this instance is disposed.
		/// </param>
		/// <remarks>
		///		IDisposable contract still has to be followed. Dispose first flushes the writer and then the base class' implementation
		///		closes the stream. We do not want to interfere with the first part, just need to opt out of closing the stream.
		/// </remarks>
		public BinaryWriterEx(Stream output, bool closeStreamWhenDisposing)
			: base(output)
		{
			CloseStreamOnDispose = closeStreamWhenDisposing;
			if (!closeStreamWhenDisposing)
			{
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		///		Whether the writer owns the underlying stream and will close it when disposed.
		/// </summary>
		public bool CloseStreamOnDispose
		{ get; private set; }

		protected override void Dispose(bool disposing)
		{
			if (CloseStreamOnDispose)
			{
				// this will close the stream
				base.Dispose(disposing);
			}
		}
	}
}
