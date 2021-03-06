﻿/* 
MIT License

Copyright (c) 2016-2017 Florian Cäsar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using System;
using System.Collections;
using System.Collections.Generic;
using Sigma.Core.Utils;

namespace Sigma.Core.Data
{
	/// <summary>
	/// A default implementation of the databuffer interface, compatible with the default Sigma.DiffSharp implementation.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class DataBuffer<T> : IDataBuffer<T>
	{
		[NonSerialized]
		private readonly IDataBuffer<T> _underlyingBuffer;
		[NonSerialized]
		private readonly IDataBuffer<T> _underlyingRootBuffer;

		protected internal readonly T[] _data;

		/// <inheritdoc />
		public long Length { get; }

		/// <inheritdoc />
		public long Offset { get; }

		/// <inheritdoc />
		public long RelativeOffset { get; }

		/// <inheritdoc />
		public IDataType Type { get; }

		/// <inheritdoc />
		public T[] Data
		{
			get
			{
				OnReadWriteAccess();

				return _data;
			}
		}

		/// <summary>
		/// Create a data buffer of a certain type with a certain underlying buffer.
		/// </summary>
		/// <param name="underlyingBuffer">The underlying buffer.</param>
		/// <param name="offset">The offset relative to the underlying buffer.</param>
		/// <param name="length">The length this buffer should have.</param>
		public DataBuffer(IDataBuffer<T> underlyingBuffer, long offset, long length)
		{
			if (underlyingBuffer == null)
			{
				throw new ArgumentNullException(nameof(underlyingBuffer));
			}

			CheckBufferBounds(offset, length, offset + length, underlyingBuffer.Length);

			Length = length;
			RelativeOffset = offset;
			Offset = offset + underlyingBuffer.Offset;

			_data = underlyingBuffer.Data;
			Type = underlyingBuffer.Type;
			_underlyingBuffer = underlyingBuffer;
			_underlyingRootBuffer = underlyingBuffer.GetUnderlyingRootBuffer() == null ? underlyingBuffer : underlyingBuffer.GetUnderlyingRootBuffer();
		}

		/// <summary>
		/// Create a data buffer of a certain large chunked array.
		/// </summary>
		/// <param name="data">The large chunked array data.</param>
		/// <param name="underlyingType">The underlying data type (inferred if not given explicitly).</param>
		public DataBuffer(T[] data, IDataType underlyingType = null) : this(data, 0L, data?.Length ?? 0L, underlyingType)
		{
		}

		/// <summary>
		/// Create a data buffer of a certain array within certain bounds.
		/// </summary>
		/// <param name="data">The data array.</param>
		/// <param name="offset">The offset (relative to the given data array).</param>
		/// <param name="length">The length this buffer should have.</param>
		/// <param name="underlyingType">The underlying type (inferred if not explicitly given).</param>
		public DataBuffer(T[] data, long offset, long length, IDataType underlyingType = null)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			CheckBufferBounds(offset, length, offset + length, data.Length);

			_data = data;
			Length = length;
			RelativeOffset = offset;
			Offset = offset;

			Type = InferDataType(underlyingType);
		}

		/// <summary>
		/// Create a data buffer of a certain length.
		/// </summary>
		/// <param name="length">The length this buffer should have.</param>
		/// <param name="underlyingType">The underlying type (inferred if not explicitly given).</param>
		public DataBuffer(long length, IDataType underlyingType = null)
		{
			if (length < 1)
			{
				throw new ArgumentException($"Length must be >= 1 but was {length}.");
			}

			Length = length;
			_data = new T[length];

			Type = InferDataType(underlyingType);
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="other">The buffer to copy.</param>
		public DataBuffer(DataBuffer<T> other)
		{
			_underlyingBuffer = other._underlyingBuffer;
			_underlyingRootBuffer = other._underlyingRootBuffer;
			Type = other.Type;
			_data = other.Data;
			Offset = other.Offset;
			RelativeOffset = other.RelativeOffset;
			Length = other.Length;
		}

		private void CheckBufferBounds(long offset, long length, long requestedEndPosition, long underlyingLength)
		{
			if (offset < 0)
			{
				throw new ArgumentException($"Offset must be > 0 but was {offset}.");
			}

			if (length < 0)
			{
				throw new ArgumentException($"Length must be >= 0 but was {length}.");
			}

			if (requestedEndPosition > underlyingLength)
			{
				throw new ArgumentException($"Buffer length (+offset) cannot exceed length of its underlying buffer, but underlying buffer length was {underlyingLength} and requested length + offset {requestedEndPosition}.");
			}
		}

		private IDataType InferDataType(IDataType givenType)
		{
			if (givenType != null)
			{
				return givenType;
			}

			try
			{
				return DataTypes.GetMatchingType(typeof(T));
			}
			catch (ArgumentException e)
			{
				throw new ArgumentException($"Could not infer type interface for underlying system type {typeof(T)} (system type not registered) and no data type interface was explicitly given.", e);
			}
		}

		/// <inheritdoc />
		public virtual IDataBuffer<T> ShallowCopy()
		{
			OnReadAccess();

			return new DataBuffer<T>(this);
		}

		/// <inheritdoc />
		public virtual object DeepCopy()
		{
			OnReadAccess();

			// not sure if entire data or just subsection should be copied
			return new DataBuffer<T>((T[]) Data.Clone(), Offset, Length, Type);
		}

		/// <inheritdoc />
		public T GetValue(long index)
		{
			OnReadAccess();

			return Data[Offset + index];
		}

		/// <inheritdoc />
		public TOther GetValueAs<TOther>(long index)
		{
			OnReadAccess();

			return (TOther) Convert.ChangeType(Data.GetValue(Offset + index), typeof(TOther));
		}

		/// <inheritdoc />
		public virtual IDataBuffer<T> GetValues(long startIndex, long length)
		{
			OnReadAccess();

			return new DataBuffer<T>(this, startIndex, length);
		}

		/// <inheritdoc />
		public virtual IDataBuffer<TOther> GetValuesAs<TOther>(long startIndex, long length) where TOther : struct
		{
			OnReadAccess();

			return new DataBuffer<TOther>(GetValuesArrayAs<TOther>(startIndex, length), 0L, length);
		}

		/// <inheritdoc />
		public T[] GetValuesArray(long startIndex, long length)
		{
			OnReadAccess();;

			T[] valuesArray = new T[length];

			System.Array.Copy(Data, Offset + startIndex, valuesArray, 0, length);

			return valuesArray;
		}

		/// <inheritdoc />
		public TOther[] GetValuesArrayAs<TOther>(long startIndex, long length)
		{
			OnReadAccess();

			TOther[] otherData = new TOther[length];

			long absoluteStart = Offset + startIndex;
			Type otherType = typeof(TOther);

			for (long i = 0; i < length; i++)
			{
				otherData[i] = (TOther) Convert.ChangeType(Data[i + absoluteStart], otherType);
			}

			return otherData;
		}

		/// <inheritdoc />
		public void SetValue(T value, long index)
		{
			OnWriteAccess();

			Data.SetValue(value, index + Offset);
		}

		/// <inheritdoc />
		public void SetValues(IDataBuffer<T> buffer, long sourceStartIndex, long destStartIndex, long length)
		{
			OnWriteAccess();;

			System.Array.Copy(buffer.Data, sourceStartIndex, Data, Offset + destStartIndex, length);
		}

		/// <inheritdoc />
		public void SetValues(T[] values, long sourceStartIndex, long destStartIndex, long length)
		{
			OnWriteAccess();

			System.Array.Copy(values, sourceStartIndex, Data, Offset + destStartIndex, length);
		}

		/// <inheritdoc />
		public IDataBuffer<T> GetUnderlyingBuffer()
		{
			return _underlyingBuffer;
		}

		/// <inheritdoc />
		public IDataBuffer<T> GetUnderlyingRootBuffer()
		{
			return _underlyingRootBuffer;
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (long i = 0; i < Length; i++)
			{
				OnReadAccess();

				yield return Data[i + Offset];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			for (long i = 0; i < Length; i++)
			{
				OnReadAccess();

				yield return Data[i + Offset];
			}
		}

		/// <summary>
		/// Called before any operation that reads from the local data.
		/// </summary>
		protected virtual void OnReadAccess()
		{
		}

		/// <summary>
		/// Called before any operation that writes to the local data.
		/// </summary>
		protected virtual void OnWriteAccess()
		{
		}

		/// <summary>
		/// Called before any operation that reads from and writes to the local data.
		/// </summary>
		protected virtual void OnReadWriteAccess()
		{
		}

		/// <inheritdoc />
		public override string ToString()
		{
			OnReadAccess();

			return $"databuffer {Type}x{Length}: " + "[" + string.Join(",", Data.SubArray((int) Offset, (int) Length)) + "]";
		}
	}
}
