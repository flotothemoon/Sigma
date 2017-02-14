/* 
MIT License

Copyright (c) 2016-2017 Florian C�sar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

// ReSharper disable VirtualMemberCallInConstructor

namespace Sigma.Core.Monitors.WPF.Panels
{
	public abstract class SigmaPanel : Card
	{
		/// <summary>
		///     The style for this panel (since it is
		///     a card the card style will be applied)
		/// </summary>
		private static Style _cardStyle;

		/// <summary>
		///     The Grid for the content (only to be
		///     future proof)
		/// </summary>
		protected readonly Grid ContentGrid;

		/// <summary>
		///     The panel that is contained by the card.
		/// </summary>
		public readonly DockPanel RootPanel;

		/// <summary>
		///     The content that will be inside the contentGrid.
		/// </summary>
		private UIElement _content;

		/// <summary>
		///     Create a SigmaPanel with a given title.
		///     If a title is not sufficient modify <see cref="Header" />.
		/// </summary>
		/// <param name="title">The given tile.</param>
		/// <param name="content">The content for the header. If <c>null</c> is passed,
		/// the title will be used.</param>
		protected SigmaPanel(string title, object content = null)
		{
			//Don't do this in static constructor, otherwise
			//we cannot guarantee that the application is already running
			if (_cardStyle == null)
			{
				_cardStyle = Application.Current.Resources[typeof(Card)] as Style;
			}

			Style = _cardStyle;

			Title = title;
			RootPanel = CreateDockPanel();

			Header = CreateHeader(content ?? title);
			AddHeader(RootPanel, Header);

			ContentGrid = CreateContentGrid();
			AddContentGrid(RootPanel, ContentGrid);

			base.Content = RootPanel;
		}

		/// <summary>
		///     The title of the Panel
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		///     The Grid Header. Use / modify this if
		///     you want to add icons etc. instead of
		///     a simple label.
		/// </summary>
		public Grid Header { get; set; }

		/// <summary>
		///     The content that is visible.
		/// </summary>
		public new object Content
		{
			get { return _content; }
			set
			{
				if (_content != null)
				{
					ContentGrid.Children.Remove(_content);
				}

				if (value == null)
				{
					_content = null;
				}
				else
				{
					_content = value as UIElement ?? new Label { Content = value.ToString() };
					ContentGrid.Children.Add(_content);
				}
			}
		}

		#region CardProperties

		/// <summary>
		///     Gets or sets the outer margin of an element.
		///     <returns>
		///         Provides margin values for the element. The default value is a <see cref="Thickness" />
		///         with all properties equal to 0 (zero).
		///     </returns>
		/// </summary>
		public new Thickness Margin
		{
			get { return ContentGrid.Margin; }
			set { ContentGrid.Margin = value; }
		}

		#endregion

		/// <summary>
		///     Add the content to the grid (may be used for callbacks
		///     etc.)
		/// </summary>
		/// <param name="panel">The root <see cref="UIElement" /> of the panel.</param>
		/// <param name="content">The content that will be added.</param>
		protected virtual void AddContentGrid(DockPanel panel, UIElement content)
		{
			panel.Children.Add(content);
		}

		/// <summary>
		///     Create the header grid and apply the correct theme to it.
		///     (This could also be done via a custom style)
		/// </summary>
		/// <param name="content">The content for the header. (Set the content of a label).</param>
		/// <returns>The newly created grid. </returns>
		protected virtual Grid CreateHeader(object content)
		{
			Grid header = new Grid();

			header.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
			header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

			Label headerContent = new Label { Content = content };
			header.Children.Add(headerContent);

			header.SetResourceReference(BackgroundProperty, "SigmaPanelHeaderBackground");
			headerContent.SetResourceReference(ForegroundProperty, "SigmaPanelHeaderForeground");

			return header;
		}

		/// <summary>
		///     Add the header the specified panel.
		/// </summary>
		/// <param name="panel">The root <see cref="UIElement" /> of the panel.</param>
		/// <param name="header">The header to be added.</param>
		protected virtual void AddHeader(DockPanel panel, UIElement header)
		{
			panel.Children.Add(header);
			DockPanel.SetDock(header, Dock.Top);
		}

		/// <summary>
		///     Create the grid where the content is placed in.
		/// </summary>
		/// <returns>The newly created grid. </returns>
		protected virtual Grid CreateContentGrid()
		{
			Grid grid = new Grid();

			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			return grid;
		}

		/// <summary>
		///     Create the default panel in which every other element is contained.
		/// </summary>
		/// <returns>The newly create <see cref="DockPanel" />.</returns>
		protected virtual DockPanel CreateDockPanel()
		{
			return new DockPanel { LastChildFill = true, Margin = new Thickness(-1, 0, 0, 0) };
		}
	}
}