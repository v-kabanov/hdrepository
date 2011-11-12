using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Storage;
using bfs.Repository.Interfaces;

namespace bfs.Repository.Util
{
	internal static class CheckHelper
	{
		public static void CheckRepositoryNotDisposed(IRepositoryManager repo)
		{
			Check.RequireArgumentNotNull(repo, "repo");
			if (repo.IsDisposed)
			{
				throw new ObjectDisposedException(StorageResources.RepositoryIsDisposed, (Exception)null);
			}
		}

		public static void CheckRepositoryNotDisposed(IRepositoryDataAccessor accessor)
		{
			CheckRepositoryNotDisposed(accessor.Repository);
		}

		/// <summary>
		///		Check that code is not null or empty and does not contain characters which cannot be part of file name.
		/// </summary>
		/// <param name="keyCode">
		///		The unique identifier of the coder within repository, <see cref="ICoder.KeyCode"/>.
		/// </param>
		public static void CheckRealCoderCode(string keyCode)
		{
			Check.DoAssertLambda(!string.IsNullOrEmpty(keyCode), () => new ArgumentException(StorageResources.CoderCodeEmpty));
			Check.DoAssertLambda(keyCode.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0
				, () => new ArgumentException(StorageResources.IllegalCharactersInCoderЦоде));
		}

		public static void CheckExistingCompressor(string keyCode, IObjectFactory factory)
		{
			CheckRealCoderCode(keyCode: keyCode);
			try
			{
				factory.GetCompressor(keyCode);
			}
			catch (KeyNotFoundException e)
			{
				throw new ArgumentException(string.Format(StorageResources.UnknownCompressor_Name, keyCode), e);
			}
		}

		public static void CheckExistingEncryptor(string keyCode, IObjectFactory factory)
		{
			CheckRealCoderCode(keyCode: keyCode);
			try
			{
				factory.GetEncryptor(keyCode);
			}
			catch (KeyNotFoundException e)
			{
				throw new ArgumentException(string.Format(StorageResources.UnknownEncryptor_Name, keyCode), e);
			}
		}
	}
}
