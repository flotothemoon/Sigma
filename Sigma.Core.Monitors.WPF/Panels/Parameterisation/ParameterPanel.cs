﻿/* 
MIT License

Copyright (c) 2016-2017 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using System;
using System.Windows;
using Sigma.Core.Monitors.Synchronisation;
using Sigma.Core.Monitors.WPF.View.Parameterisation;
using Sigma.Core.Monitors.WPF.View.Windows;
using Sigma.Core.Monitors.WPF.ViewModel.Parameterisation;
using Sigma.Core.Utils;

namespace Sigma.Core.Monitors.WPF.Panels.Parameterisation
{
	public class ParameterPanel : GenericPanel<ParameterView>
	{
		/// <summary>
		///     Create a SigmaPanel with a given title.
		///     If a title is not sufficient modify <see cref="SigmaPanel.Header" />.
		/// </summary>
		/// <param name="title">The given tile.</param>
		/// <param name="headerContent">The content for the header. If <c>null</c> is passed,
		/// the title will be used.</param>
		public ParameterPanel(string title, IParameterVisualiserManager visualiserManager, ISynchronisationHandler synchronisationHandler, object headerContent = null) : base(title, headerContent)
		{
			Content = new ParameterView(visualiserManager, synchronisationHandler);
			Content.Margin = new Thickness(10, 10, 10, 0);
		}
		public ParameterPanel(string title, SigmaEnvironment environment, SigmaWindow window, object headerContent = null) : this(title, window.ParameterVisualiser, environment.SynchronisationHandler, headerContent) { }

		public void Add(string name, Type type, IRegistry registry, string key)
		{
			Content.Add(name, type, registry, key);
		}

		public void Add(string name, object visualiserAndDisplayer, IRegistry registry, string key)
		{
			Content.Add(name, visualiserAndDisplayer, registry, key);
		}
	}
}