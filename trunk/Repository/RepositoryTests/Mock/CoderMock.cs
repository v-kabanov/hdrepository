using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace RepositoryTests.Mock
{
	internal class MockCoderStream : Stream
	{
		private string _signature;
		private Stream _wrappedStream;
		private bool _firstIO = true;

		public MockCoderStream(string signature, Stream wrappedStream)
		{
			_signature = signature;
			_wrappedStream = wrappedStream;
		}

		public override bool CanRead
		{
			get { return _wrappedStream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _wrappedStream.CanSeek; }
		}

		public override bool CanWrite
		{
			get { return _wrappedStream.CanWrite; }
		}

		public override void Flush()
		{
			_wrappedStream.Flush();
		}

		public override long Length
		{
			get { return _wrappedStream.Length; }
		}

		public override long Position
		{
			get { return _wrappedStream.Position; }
			set { _wrappedStream.Position = value; }
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int posIncrement = 0;
			if (_firstIO)
			{
				BinaryReader rdr = new BinaryReader(_wrappedStream, Encoding.ASCII);
				string prefix = rdr.ReadString();
				if (!string.Equals(_signature, prefix))
				{
					throw new ApplicationException("Decoding failed - signature mismatch");
				}
				_firstIO = false;
			}
			return _wrappedStream.Read(buffer, offset + posIncrement, count - posIncrement);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _wrappedStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_wrappedStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			int posIncrement = 0;
			if (_firstIO)
			{
				BinaryWriter wr = new BinaryWriter(_wrappedStream, Encoding.ASCII);
				wr.Write(_signature);
				_firstIO = false;
			}
			_wrappedStream.Write(buffer, offset + posIncrement, count - posIncrement);
		}
	}


	public class CoderMock : ICoder
	{
		public CoderMock(string keyCode)
		{
			KeyCode = keyCode;
		}

		public string KeyCode
		{ get; private set; }

		public System.IO.Stream WrapInEncodingStream(System.IO.Stream output)
		{
			return new MockCoderStream(KeyCode, output);
		}

		public System.IO.Stream WrapInDecodingStream(System.IO.Stream encodedDataStream)
		{
			return new MockCoderStream(KeyCode, encodedDataStream);
		}
	}
}
