using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Exceptions
{
	internal static class DifferentRepositoriesExceptionHelper
	{
		public static void Check(Interfaces.IRepositoryManager r1, Interfaces.IRepositoryManager r2)
		{
			Util.Check.DoAssertLambda(object.ReferenceEquals(r1, r2), () => new ArgumentException(Storage.StorageResources.DifferentRepositoriesException));
		}
	}
}
