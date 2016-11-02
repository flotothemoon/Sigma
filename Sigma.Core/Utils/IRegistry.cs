﻿/* 
MIT License

Copyright (c) 2016 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Sigma.Core.Utils
{
	/// <summary>
	/// A collection of keys and values (similar to a dictionary) where types and keys are registered for easier inspection. Registries can be chained and represent a hierarchy, which can then be referred to using dot notation.
	/// </summary>
	public interface IRegistry : IDictionary<string, object>
	{
		/// <summary>
		/// Set a value with a given identifier. 
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="value">The value to set.</param>
		/// <param name="valueType">Optionally set the type subsequent values associated with this identifier should have, unless explicitly overwritten.</param>
		void Set(string identifier, object value, Type valueType = null);

		/// <summary>
		/// Get the type-checked value associated with a given identifier.
		/// </summary>
		/// <typeparam name="T">The type the value should be cast to.</typeparam>
		/// <param name="identifier">The identifier.</param>
		/// <returns>The value of type T (if any) or null.</returns>
		T Get<T>(string identifier);

		/// <summary>
		/// Get the value associated with a given identifier.  
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returnsstringThe value (if any) or null.</returns>
		object Get(string identifier);

		/// <summary>
		/// Get all values of type T matching the identifier and optionally check if they match a certain type. 
		/// </summary>
		/// <typeparam name="T">The type.</typeparam>
		/// <param name="matchIdentifier">The identifier to match.</param>
		/// <param name="matchType">The type to match.</param>
		/// <returns></returns>
		T[] GetAllValues<T>(string matchIdentifier, Type matchType = null);

		/// <summary>
		/// Removes the identifier and the associated type-checked value.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns>The value previously associated with the identifier.</returns>
		T Remove<T>(string identifier);

		/// <summary>
		/// Returns an iterator over all keys.
		/// </summary>
		/// <returns>An iterator over all keys.</returns>
		IEnumerator GetKeyIterator();

		/// <summary>
		/// Returns an iterator over all values.
		/// </summary>
		/// <returns>An iterator over all values.</returns>
		IEnumerator GetValueIterator();
	}
}
