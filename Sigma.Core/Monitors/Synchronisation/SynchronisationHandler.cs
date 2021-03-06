﻿/* 
MIT License

Copyright (c) 2016-2017 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Sigma.Core.Training.Operators;
using Sigma.Core.Utils;

namespace Sigma.Core.Monitors.Synchronisation
{
	/// <summary>
	/// The default synchronisation handler for Sigma. It is responsible for syncing values between monitors 
	/// and the environment itself.
	/// </summary>
	public class SynchronisationHandler : ISynchronisationHandler, ISynchronisationSource
	{
		/// <summary>
		/// The environment this handler is associated with.
		/// </summary>
		public SigmaEnvironment Sigma { get; }

		private readonly ILog _logger = LogManager.GetLogger(typeof(SigmaEnvironment));

		/// <summary>
		/// Map every registry to a resolver for that registry. 
		/// </summary>
		protected Dictionary<IRegistry, IRegistryResolver> RegistryResolvers { get; }

		/// <summary>
		/// The assigned synchronisation sources.
		/// </summary>
		protected List<ISynchronisationSource> Sources;

		/// <summary>
		/// Default constructor for <see cref="ISynchronisationHandler"/>. 
		/// </summary>
		/// <param name="sigma">The <see cref="SigmaEnvironment"/> this <see cref="ISynchronisationHandler"/> is
		/// associated with. May not be <c>null</c>.</param>
		public SynchronisationHandler(SigmaEnvironment sigma)
		{
			if (sigma == null) throw new ArgumentNullException(nameof(sigma));

			RegistryResolvers = new Dictionary<IRegistry, IRegistryResolver>();
			Sources = new List<ISynchronisationSource>();

			Sigma = sigma;
		}

		/// <inheritdoc />
		public void AddSynchronisationSource(ISynchronisationSource source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			if (source.Keys != null)
			{
				foreach (string key in source.Keys)
				{
					foreach (ISynchronisationSource savedSource in Sources)
					{
						if (savedSource.Keys != null)
						{
							if (savedSource.Keys.Contains(key))
							{
								_logger.Warn($"The key {key} is added to a synchronisation handler by {source.GetType()} but is already provided by {savedSource.GetType()}. It is uncertain which key will be taken (possible performance decrease).");
							}
						}
					}
				}
			}

			Sources.Add(source);
		}

		/// <inheritdoc />
		public bool RemoveSynchronisationSource(ISynchronisationSource source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			return Sources.Remove(source);
		}


		/// <summary>
		/// Check if a source is contained.
		/// </summary>
		/// <param name="source">The source that will be checked.</param>
		/// <returns><c>True</c>, if the handler contains the source - <c>false</c> otherwise</returns>
		public bool ContainsSynchronisationSoruce(ISynchronisationSource source)
		{
			return Sources.Contains(source);
		}

		/// <inheritdoc />
		public virtual void SynchroniseSet<T>(IRegistry registry, string key, T val, Action<T> onSuccess = null, Action<Exception> onError = null)
		{
			// check if the registry is from an operator
			foreach (IOperator op in Sigma.RunningOperatorsByTrainer.Values)
			{
				if (ReferenceEquals(op.Registry, registry))
				{
					//TODO: test if callback is called
					//TODO: on error check sources for other to set the value
					op.InvokeCommand(new SetValueCommand<T>(key, val, () => onSuccess?.Invoke(val)));

					return;
				}
			}

			IRegistryResolver resolver = RegistryResolvers.TryGetValue(registry, () => new RegistryResolver(registry));

			// check if at least one value has been set
			if (resolver.ResolveSet(key, val, true, typeof(T)).Length > 0)
			{
				onSuccess?.Invoke(val);
			}
			else
			{
				onError?.Invoke(new KeyNotFoundException($"{key} was not found in {registry} and could not be created."));
			}
		}

		/// <inheritdoc />
		public virtual T SynchroniseGet<T>(IRegistry registry, string key)
		{
			if (registry != null)
			{
				IRegistryResolver resolver = RegistryResolvers.TryGetValue(registry, () => new RegistryResolver(registry));
				//return resolver.ResolveGetSingle<>()
				string[] emptyArrayThrowaway;

				T[] result = resolver.ResolveGet<T>(key, out emptyArrayThrowaway);

				if (result.Length != 0)
				{
					return result[0];
				}
			}

			foreach (ISynchronisationSource source in Sources)
			{
				T res;
				if (source.TryGet(key, out res))
				{
					return res;
				}
			}

			return default(T);
		}

		/// <summary>
		///	Update a value with a given action if it has changed (<see cref="object.Equals(object)"/>).
		/// </summary>
		/// <typeparam name="T">The type of the value that will be gathered.</typeparam>
		/// <param name="registry">The registry in which the entry will be set.</param>
		/// <param name="key">The fully resolved identifier for the parameter that will be received.</param>
		/// <param name="currentVal">The current value of the object.</param>
		/// <param name="update">The method that will be called if the parameter has to be updated.</param>
		public void SynchroniseUpdate<T>(IRegistry registry, string key, T currentVal, Action<T> update)
		{
			if (update == null) throw new ArgumentNullException(nameof(update));

			T newObj = SynchroniseGet<T>(registry, key);
			if (newObj != null && currentVal == null || newObj != null && !newObj.Equals(currentVal))
			{
				update(newObj);
			}
		}

		public string[] Keys { get; }

		/// <inheritdoc />
		bool ISynchronisationSource.TryGet<T>(string key, out T val)
		{
			throw new NotImplementedException("Get currently not implemented as no registry is passed");
		}


		/// <inheritdoc />
		bool ISynchronisationSource.Contains(string key)
		{
			throw new NotImplementedException("Get currently not implemented as no registry is passed");
		}

		/// <inheritdoc />
		bool ISynchronisationSource.TrySet<T>(string key, T val)
		{
			throw new NotImplementedException("Set currently not implemented as there is no error");
		}

	}
}