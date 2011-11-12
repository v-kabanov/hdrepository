/*
 * User: vasily
 * Date: 26/02/2011
 * Time: 7:50 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage.FileSystem
{
	/// <summary>
	/// 	Storage transaction scope which does nothing.
	/// </summary>
	public class DummyStorageTransactionScope : IStorageTransactionScope
	{
		public DummyStorageTransactionScope(IRepository repository)
		{
			PreviousTransaction = UnderlyingTransaction = repository.ObjectFactory.FileSystemProvider.AmbientTransaction;
		}
		
		public void Complete()
		{
		}
		
		public void Dispose()
		{
		}
		
		public bool ToBeDisposed
		{ get { return false; } }
		
		public bool HasChangedContext
		{ get { return false; } }
		
		public bool IsTransactionOwner
		{ get { return false; } }

		public bfs.Repository.Interfaces.Infrastructure.IFileSystemTransaction UnderlyingTransaction
		{ get; private set; }
		
		public bfs.Repository.Interfaces.Infrastructure.IFileSystemTransaction DetachTransaction()
		{ return null; }

		public Interfaces.Infrastructure.IFileSystemTransaction PreviousTransaction
		{ get; private set; }
	}
}
