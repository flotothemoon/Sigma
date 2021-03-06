﻿/* 
MIT License

Copyright (c) 2016-2017 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using log4net;
using Sigma.Core.Data.Extractors;
using Sigma.Core.Data.Sources;
using System;
using System.Collections.Generic;
using System.IO;

namespace Sigma.Core.Data.Readers
{
	/// <summary>
	/// A byte record reader, which reads sources byte-wise.
	/// </summary>
	[Serializable]
	public class ByteRecordReader : IRecordReader
	{
		[NonSerialized]
		private readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public IDataSource Source { get; }

		private readonly int _headerBytes;
		private readonly int _recordSizeBytes;
		private bool _prepared;
		private bool _processedHeaderBytes;

		/// <summary>
		/// Create a byte record reader with a certain header size and per record size.
		/// </summary>
		/// <param name="source">The source which should read.</param>
		/// <param name="headerLengthBytes">The header length in bytes (which will be skipped in this implementation).</param>
		/// <param name="recordSizeBytes">The per record size in bytes.</param>
		public ByteRecordReader(IDataSource source, int headerLengthBytes, int recordSizeBytes)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (headerLengthBytes < 0)
			{
				throw new ArgumentException($"Header bytes must be >= 0, but header bytes was {headerLengthBytes}.");
			}

			if (recordSizeBytes <= 0)
			{
				throw new ArgumentException($"Record size bytes must be > 0, but record size bytes was {recordSizeBytes}.");
			}

			Source = source;
			_headerBytes = headerLengthBytes;
			_recordSizeBytes = recordSizeBytes;
		}

		/// <inheritdoc />
		public void Prepare()
		{
			Source.Prepare();

			_prepared = true;
		}

		public object Read(int numberOfRecords)
		{
			if (!_prepared)
			{
				throw new InvalidOperationException("Cannot read from source before preparing this reader (missing Prepare() call?).");
			}

			Stream stream = Source.Retrieve();

			if (!_processedHeaderBytes)
			{
				byte[] header = new byte[_headerBytes];
				int read = stream.Read(header, 0, _headerBytes);

				ProcessHeader(header, _headerBytes);

				_processedHeaderBytes = true;

				if (read != _headerBytes)
				{
					_logger.Warn($"Could not read the requested number of header bytes ({_headerBytes} bytes), could only read {read} bytes.");

					return null;
				}
			}

			List<byte[]> records = new List<byte[]>();

			for (int numberOfRecordsRead = 0;  numberOfRecordsRead < numberOfRecords; numberOfRecordsRead++)
			{
				byte[] buffer = new byte[_recordSizeBytes];

				int readBytes = stream.Read(buffer, 0, _recordSizeBytes);

				if (readBytes != _recordSizeBytes)
				{
					break;
				}

				records.Add(buffer);
			}

			if (records.Count == 0)
			{
				_logger.Debug($"No more records could be read (requested: {numberOfRecords} records), end of stream most likely reached.");

				return null;
			}

			return records.ToArray();
		}

		protected virtual void ProcessHeader(byte[] header, int headerBytes)
		{
		}

		public ArrayRecordExtractor<byte> Extractor(params object[] parameters)
		{
			return (ArrayRecordExtractor<byte>) Extractor(ArrayRecordExtractor<byte>.ParseExtractorParameters(parameters));
		}

		public IRecordExtractor Extractor(Dictionary<string, long[][]> indexMappings)
		{
			return Extractor(new ArrayRecordExtractor<byte>(indexMappings));
		}

		public IRecordExtractor Extractor(IRecordExtractor extractor)
		{
			extractor.Reader = this;

			return extractor;
		}

		public void Dispose()
		{
		}
	}
}
