using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces.Infrastructure
{
	public interface IRepository : IRepositoryManager
	{
		/// <summary>
		///		Get root repository folder.
		/// </summary>
		/// <remarks>
		///		The root folder cannot be deleted or renamed (its name is empty string), it has no parent.
		/// </remarks>
		new IFolder RootFolder
		{ get; }

		/// <summary>
		///		Get or set object factory implementation.
		/// </summary>
		/// <remarks>
		///		Object factory works as a dependency resolver. It allows you to plug your implementation of one or more repository components.
		/// </remarks>
		IObjectFactory ObjectFactory
		{ get; set; }

		/// <summary>
		/// 	Register new reader instance
		/// </summary>
		/// <param name="reader">
		/// 	New reader instance
		/// </param>
		void RegisterReader(IRepositoryDataAccessor reader);

		/// <summary>
		/// 	Register new writer instance
		/// </summary>
		/// <param name="writer">
		/// 	New writer instance
		/// </param>
		void RegisterWriter(IRepositoryDataAccessor writer);

		/// <summary>
		/// 	De-register reader which is being disposed.
		/// </summary>
		/// <param name="reader">
		/// 	The reader being disposed (must not work afterwards).
		/// </param>
		/// <returns>
		///		Whether the reader has been unregistered (false if it was not registered).
		/// </returns>
		bool UnRegisterReader(IRepositoryDataAccessor reader);

		/// <summary>
		/// 	De-register writer which is being disposed.
		/// </summary>
		/// <param name="writer">
		/// 	The writer being disposed (must not work afterwards).
		/// </param>
		/// <returns>
		///		Whether the writer has been unregistered (false if it was not registered).
		/// </returns>
		bool UnRegisterWriter(IRepositoryDataAccessor writer);
	}
}
