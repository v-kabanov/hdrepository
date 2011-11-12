/*
 * User: vasily
 * Date: 28/02/2011
 * Time: 9:16 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	/// <summary>
	/// 	Repository-wide settings.
	/// </summary>
	public class RepositorySettings : IRepositorySettings
	{
		public RepositorySettings(IRepository repository)
		{
			Repository = repository;
			StorageTransactionSettings = 0;
		}
		
		public StorageTransactionSettings StorageTransactionSettings
		{ get; set; }
		
		public IRepository Repository
		{ get; private set; }
			
	}
}
