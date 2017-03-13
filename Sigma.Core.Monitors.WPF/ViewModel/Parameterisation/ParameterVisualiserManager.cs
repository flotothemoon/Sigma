﻿using System;
using System.Collections.Generic;
using log4net;
using Sigma.Core.Monitors.WPF.Annotations;
using Sigma.Core.Monitors.WPF.Utils;
using Sigma.Core.Monitors.WPF.View.Parameterisation;
using Sigma.Core.Monitors.WPF.View.Parameterisation.Defaults;

namespace Sigma.Core.Monitors.WPF.ViewModel.Parameterisation
{
	/// <summary>
	/// The default implementation of <see cref="IParameterVisualiserManager"/> that works either by
	/// manually speecifying the class item mapping or automatically by marking them with the attribute <see cref="ParameterVisualiserAttribute"/>.
	/// </summary>
	public class ParameterVisualiserManager : IParameterVisualiserManager
	{
		/// <summary>
		/// The logger.
		/// </summary>
		private readonly ILog _log = LogManager.GetLogger(typeof(ParameterVisualiserManager));

		/// <summary>
		/// Maps a given parameter type (e.g. <c>bool</c>) to a
		/// <see cref="IParameterVisualiser"/> implementation.
		/// </summary>
		protected readonly Dictionary<Type, Type> TypeMapping;

		/// <summary>
		/// Maps a given parameter type (e.g. <c>bool</c>) to a 
		/// <see cref="IParameterVisualiserInfo"/> that contains 
		/// details like <see cref="VisualiserPriority"/>.
		/// </summary>
		protected readonly Dictionary<Type, IParameterVisualiserInfo> AttributeMapping;


		/// <summary>
		/// The default constructor.
		/// </summary>
		/// <param name="autoAssign">If <c>true</c> it will automatically add all classes marked with the attribute <see cref="ParameterVisualiserAttribute"/>.</param>
		public ParameterVisualiserManager(bool autoAssign = true)
		{
			TypeMapping = new Dictionary<Type, Type>();
			AttributeMapping = new Dictionary<Type, IParameterVisualiserInfo>();

			if (autoAssign)
			{
				// ReSharper disable once VirtualMemberCallInConstructor
				AssignMarkedClasses();
			}
		}

		/// <summary>
		/// Assign all classes that are marked with <see cref="ParameterVisualiserAttribute"/>. 
		/// </summary>
		protected virtual void AssignMarkedClasses()
		{
			// get all classes that have the custom attribute
			IEnumerable<Type> classes = AttributeUtils.GetTypesWithAttribute(typeof(ParameterVisualiserAttribute));

			foreach (Type @class in classes)
			{
				ParameterVisualiserAttribute[] visualisers = (ParameterVisualiserAttribute[])Attribute.GetCustomAttributes(@class, typeof(ParameterVisualiserAttribute));

				foreach (ParameterVisualiserAttribute visualiser in visualisers)
				{
					Add(@class, visualiser);
				}
			}
		}

		/// <summary>
		/// Add a new <see cref="IParameterVisualiser"/> to the mapping. This requires the class itself and some additional information.
		/// </summary>
		/// <param name="visualiserClass">The class that represents the type (e.g. <see cref="SigmaCheckBox"/>).
		/// For the default Sigma parameter handling, <see ref="visualiserClass"/> has to implement <see cref="IParameterVisualiser"/> and
		/// requires a public parameterless constructor.</param>
		/// <param name="parameterInfo">The info for the visualiser (type it represents ... ).</param>
		/// <returns><c>True</c> if the type could be added successfully. <c>False</c> otherwise (e.g. an <see cref="IParameterVisualiser"/> with a higher
		/// priority has already been added).</returns>
		public virtual bool Add(Type visualiserClass, IParameterVisualiserInfo parameterInfo)
		{
			if (!typeof(IParameterVisualiser).IsAssignableFrom(visualiserClass))
			{
				_log.Warn($"{visualiserClass.Name} does not implement the interface {nameof(IParameterVisualiser)} - be aware that this can cause weird errors when using Sigmas default parameter display.");
			}

			if (visualiserClass.GetConstructor(Type.EmptyTypes) == null)
			{
				_log.Warn($"{visualiserClass.Name} does not have a public parameterless constructor - be aware that this can cause weird errors when using Sigmas default parameter display.");
			}

			Type storedClass;
			IParameterVisualiserInfo storedAttribte;

			// if the mapping has already been added 
			if (TypeMapping.TryGetValue(parameterInfo.Type, out storedClass) && AttributeMapping.TryGetValue(parameterInfo.Type, out storedAttribte))
			{
				// if the new values have a lower priority, we return false
				if (parameterInfo.Priority <= storedAttribte.Priority)
				{
					_log.Warn($"{parameterInfo.Type} is currently visualised by {storedClass.Name}; {visualiserClass.Name} tried to be the visualiser but has a lower priority ({parameterInfo.Priority} <= {storedAttribte.Priority}).");

					return false;
				}

				_log.Info($"{parameterInfo.Type} was visualised by {storedClass.Name}; {visualiserClass.Name} has a higher priority and is therefore the new visualiser ({parameterInfo.Priority} > {storedAttribte.Priority}).");
			}

			TypeMapping[parameterInfo.Type] = visualiserClass;
			AttributeMapping[parameterInfo.Type] = parameterInfo;

			return true;
		}

		/// <inheritdoc />
		public virtual bool Remove(Type type)
		{
			return TypeMapping.Remove(type) && AttributeMapping.Remove(type);
		}

		/// <summary>
		/// Get the type which is used to visualise given type (e.g. <c>typeof(bool)</c>). 
		/// </summary>
		/// <param name="type">The object type which will be displayed.</param>
		/// <returns>The closest type for visualisation.</returns>
		/// <exception cref="ArgumentNullException">If the given type is null.</exception>
		public Type VisualiserType([NotNull] Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			Type orig = type;

			// it is null when calling typeof(object).BaseType
			// prevent that everything with an interface is an object
			while (type.BaseType != null)
			{
				if (TypeMapping.ContainsKey(type))
				{
					return type;
				}

				type = type.BaseType;
			}

			// check all interfaces
			type = orig;

			Type[] interfaces = type.GetInterfaces();
			foreach (Type iface in interfaces)
			{
				if (TypeMapping.ContainsKey(iface))
				{
					return iface;
				}
			}

			// if nothing fits, it has to be an object
			return typeof(object);
		}

		/// <summary>
		/// Get the type which is used to visualise given object (reference not type). 
		/// </summary>
		/// <param name="obj">The object which will be displayed.</param>
		/// <returns>The closest type for visualisation.</returns>
		/// <exception cref="ArgumentNullException">If the given object is null.</exception>
		public Type VisualiserTypeByReference([NotNull] object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException(nameof(obj));
			}

			return VisualiserType(obj.GetType());
		}
	}
}