using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces
{
	public interface IRepositoryDataAccessor : IDisposable
	{
		/// <summary>
		///		Check whether the accessor is accessing data in the <paramref name="folder"/> or any of its descendants.
		///		Descandants include <paramref name="folder" /> itself.
		/// </summary>
		/// <param name="folder">
		///		<see cref="IRepositoryFolder"/> instance representing the folder or the whole subtree 
		///		(the folder and all its descendants), depending on <paramref name="subtree"/>
		/// </param>
		/// <param name="subtree">
		///		<code>bool</code> indicating whether to check access to any of the descendants of <paramref name="folder"/> (<see langword="true"/>)
		///		or just <paramref name="folder"/> itself.
		/// </param>
		/// <returns>
		///		<see langword="true"/> if the accessor is accessing data in the specified folder or folders
		///		<see langword="false"/> otherwise
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="folder"/> is <see langword="null"/>
		/// </exception>
		/// <exception cref="DifferentRepositoriesException">
		///		<paramref name="folder"/> is not attached to the same <see cref="IRepositoryManager"/> instance.
		///		(<see cref="Repository"/>)
		/// </exception>
		bool IsAccessing(IRepositoryFolder folder, bool subtree);

		/// <summary>
		///		Get target repository
		/// </summary>
		IRepositoryManager Repository
		{ get; }

		/// <summary>
		///		Close data accessor and free resources.
		/// </summary>
		/// <remarks>
		///		After being closed an accessor becomes unable to access data. It is equivalent to Dispose().
		/// </remarks>
		void Close();
	}
}
