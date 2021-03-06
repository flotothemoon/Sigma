﻿/* 
MIT License

Copyright (c) 2016-2017 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Repository.Hierarchy;
using Sigma.Core.Utils;

namespace Sigma.Core.Monitors
{
	/// <summary>
	/// An HTTP monitor that visualises a sigma environment on a website.
	/// </summary>
	public class HttpMonitor : IMonitor
	{
		private readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The sigma environment associated with this monitor.
		/// </summary>
		public SigmaEnvironment Sigma { get; set; }
		/// <inheritdoc />
		public IRegistry Registry { get; set; }

		private HttpListener _listener;
		private readonly string[] _uriPrefixes;
		private MemoryAppender _memoryAppender;

		/// <summary>
		/// Create an HTTP monitor with a certain list of URI prefixes under which it will be available.
		/// 
		/// In non-priveleged execution mode, the http monitor is not allowed to run on a port smaller than 1023 or on any other IP except localhost.
		/// In order to run on port 80 and available in the network execute the following command in windows cmd: netsh http add urlacl url="http://+:80" user=everyone
		/// </summary>
		/// <param name="uriPrefixes">The URI prefixes.
		/// </param>
		public HttpMonitor(params string[] uriPrefixes)
		{
			if (uriPrefixes == null) throw new ArgumentNullException(nameof(uriPrefixes));
			if (uriPrefixes.Length == 0) throw new ArgumentException($"There must be > 0 prefixes but none were given.");

			_uriPrefixes = uriPrefixes;
		}

		/// <summary>
		/// This function will be called before the start of the monitor.
		/// </summary>
		public void Initialise()
		{
			_listener = new HttpListener();

			foreach (string uriPrefix in _uriPrefixes)
			{
				_listener.Prefixes.Add(uriPrefix);
			}

			_memoryAppender = new MemoryAppender();
			_memoryAppender.AddFilter(new LevelRangeFilter { LevelMin = Level.Info, LevelMax = Level.Fatal });

			((Hierarchy)LogManager.GetRepository()).Root.AddAppender(_memoryAppender);
		}

		/// <summary>
		/// Start this <see cref="IMonitor"/>.
		/// If the <see cref="IMonitor"/> runs in a new <see cref="Thread"/>, this function should block until the monitor is completely up and running. 
		/// </summary>
		public void Start()
		{
			Thread listeningThread = new Thread(() =>
			{
				_listener.Start();

				_logger.Info($"Started HTTP monitor, listening on URI prefixes {string.Join(", ", _uriPrefixes)}...");

				try
				{
					string responseStaticHeader = $"<head>" +
											 $" <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
											 $" <title>Sigma:{Sigma.Name}</title>" +
											 $" <style>" +
											 @"   * {font-family: 'Lucida Console';}" +
											 @"   body {background-color: 36474F; color: #EEEEEE;}" +
											 @"   a {color: #CCCCCC;}" +
											 @"   .meta {margin-top: 1rem; margin-bottom: 1rem;}" +
											 @"   .meta > table {width: 65%; margin: auto; font-size: 16px; }" +
											 @"   .console {background-color: 232D33; width: 65%; height: 80%; margin: auto; overflow-y: scroll; overflow-x: hidden; }" +
											 @"   .console {border: 1px solid grey;}" +
											 @"   .console > table {border-spacing: 0.5rem 0rem;}" +
											 @"   .console > table > tbody > tr {line-height: 95%; font-size: 13px; padding-bottom: 0.4rem; padding-top: 0.4rem; margin-top: 0rem; margin-bottom: 0rem; cursor: default;}" +
											 @"   .console > table > tbody > tr:hover {background-color: #3B4C56;}" +
											 @"   ::selection {background-color: 1B2428;}" +
											 @"   ::-webkit-scrollbar { height: 12px; width: 16px; background: #1B2428; } " +
											 @"   ::-webkit-scrollbar-thumb { background: #3B4C56; } " +
											 @"   ::-webkit-scrollbar-corner { background: #000; }" +
											 @"   .footer {color: #CCCCCC; font-size: 13px; text-align: center; position: absolute; right: 0; bottom: 0; left: 0; padding: 1rem}" +
											 @"   @media (max-width: 50rem) {" +
											 @"    .console {width: 100%; overflow-x: scroll}" +
											 @"    .meta > table {width: 90%;}" +
											 @"    .footer {display: none;}" +
											 @"   }" +
											 @"   " +
											 @"   " +
											 $" </style>" +
											 $"</head>";

					string responseStaticConsoleBegin = $"<div class=\"console\"><table>";
					string responseStaticConsoleEnd = $"</table></div>";

					while (_listener.IsListening)
					{
						ThreadPool.QueueUserWorkItem(c =>
						{
							HttpListenerContext context = c as HttpListenerContext;

							if (context == null)
							{
								_logger.Warn($"Received invalid context data, unable to handle client request...");

								return;
							}

							try
							{
								string responseDynamicMetaTrainers = string.Join("<br>", Sigma.RunningOperatorsByTrainer.Keys.Select(
									t => $"<b>{t.ToString()}</b>" +
										$"<br> using dataset <b>\"{t.TrainingDataIterator.UnderlyingDataset.Name}\"</b>" +
										$"<br> optimising with <b>{t.Optimiser.GetType().Name}</b>" +
										$"<br> powered by <b>{t.Operator.GetType().Name}</b>"));

								string responseDynamicMeta = $"<div class=\"meta\">" +
															 $"  <table>" +
															 $"     <caption><b>Sigma Remote HTTP Monitor</b></caption>" +
															 $"     <tr><td>Environment:</td><td>{Sigma.Name}</td></tr>" +
															 $"     <tr><td>Active Trainers ({Sigma.RunningOperatorsByTrainer.Count}):</td><td>{responseDynamicMetaTrainers}</td></tr>" +
															 $"  </table>" +
															 $"</div>";

								const string responseDynamicConsoleItem = "<tr><td>{0:T}</td><td>{1}</td><td>[{2}]</td><td>{3}</td><td>{4}</td></tr>";
								string responseDynamicFooter = $"<div class=\"footer\">" +
															   $"<a href=\"javascript:window.location.href=window.location.href\">refresh</a><br>" +
															   $"generated {DateTime.Now:G}" +
															   $"</div>";

								StringBuilder builder = new StringBuilder();
								builder.Append("<html>");
								builder.Append(responseStaticHeader);
								builder.Append(responseDynamicMeta);
								builder.Append(responseStaticConsoleBegin);

								foreach (LoggingEvent loggingEvent in _memoryAppender.GetEvents())
								{
									string loggerName = loggingEvent.LoggerName.Substring(loggingEvent.LoggerName.LastIndexOf(".", StringComparison.Ordinal) + 1);

									builder.Append(string.Format(responseDynamicConsoleItem, loggingEvent.TimeStamp, loggingEvent.Level.Name,
										loggingEvent.ThreadName, loggerName, loggingEvent.MessageObject.ToString()));
								}

								builder.Append(responseStaticConsoleEnd);
								builder.Append(responseDynamicFooter);
								builder.Append("</html>");

								byte[] buffer = Encoding.UTF8.GetBytes(builder.ToString());
								context.Response.ContentLength64 = buffer.Length;
								context.Response.OutputStream.Write(buffer, 0, buffer.Length);
							}
							catch (Exception e)
							{
								_logger.Error($"Error occured while processing HTTP monitor client request: " + e, e);
							}
							finally
							{
								context.Response.OutputStream.Close();
							}
						}, _listener.GetContext());
					}
				}
				catch (Exception e)
				{
					_logger.Error($"Error occured while processing HTTP monitor: " + e, e);
				}
			});

			listeningThread.Start();
		}

		/// <summary>
		/// Signal this <see cref="IMonitor"/> to stop. This method should call dispose.
		/// This could for example stop the Sigma learning process (if mandatory), or clear up registry entries, resources ...
		/// </summary>
		public void SignalStop()
		{
			_listener.Stop();
			_listener.Close();

			_logger.Info($"Stopped HTTP monitor.");
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
		}
	}
}