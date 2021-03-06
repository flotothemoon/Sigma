﻿/* 
MIT License

Copyright (c) 2016-2017 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

namespace Sigma.Core.Utils
{
	/// <summary>
	/// A deep copyable object. "Deep" means all child references are recursively copied down to actual copyable primitives. 
	/// Note: If some members cannot be deep copied, they may be shallow copied or left out, assuming the object is still in a valid state for the required task (e.g. seek content).
	/// </summary>
	public interface IDeepCopyable
	{
		/// <summary>
		/// Deep copy this object.
		/// </summary>
		/// <returns>A deep copy of this object.</returns>
		object DeepCopy();
	}
}
