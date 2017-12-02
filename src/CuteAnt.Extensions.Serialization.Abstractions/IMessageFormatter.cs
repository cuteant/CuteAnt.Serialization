﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.Buffers;
using Microsoft.Extensions.Logging;

namespace CuteAnt.Extensions.Serialization
{
  /// <summary>Interface that allows third-party serializers to perform serialization, even when
  /// the types being serialized are not known (generics) at initialization time.
  /// 
  /// Types that inherit this interface are discovered through dependency injection and 
  /// automatically incorporated in the Serialization Manager.
  /// </summary>
  public interface IMessageFormatter
  {
    /// <summary>Gets or sets the <see cref="IRequiredMemberSelector"/> used to determine required members.</summary>
    IRequiredMemberSelector RequiredMemberSelector { get; set; }

    /// <summary>Gets or sets the <see cref="ILogger"/>.</summary>
    ILogger Logger { get; set; }

    /// <summary>Determines whether this <see cref="IMessageFormatter"/> can deserialize an object of the specified type.</summary>
    /// <remarks>Derived classes must implement this method and indicate if a type can or cannot be deserialized.</remarks>
    /// <param name="type">The type of object that will be deserialized.</param>
    /// <returns><c>true</c> if this <see cref="IMessageFormatter"/> can deserialize an object of that type; otherwise <c>false</c>.</returns>
    bool CanReadType(Type type);

    /// <summary>Determines whether this <see cref="IMessageFormatter"/> can serialize an object of the specified type.</summary>
    /// <remarks>Derived classes must implement this method and indicate if a type can or cannot be serialized.</remarks>
    /// <param name="type">The type of object that will be serialized.</param>
    /// <returns><c>true</c> if this <see cref="IMessageFormatter"/> can serialize an object of that type; otherwise <c>false</c>.</returns>
    bool CanWriteType(Type type);

    /// <summary>Informs the serialization manager whether this serializer supports the type for serialization.</summary>
    /// <param name="type">The type of the item to be serialized</param>
    /// <returns>A value indicating whether the item can be serialized.</returns>
    bool IsSupportedType(Type type);

    /// <summary>Initializes the external serializer. Called once when the serialization manager creates 
    /// an instance of this type</summary>
    void Initialize(ILogger logger);

    /// <summary>Tries to create a copy of source.</summary>
    /// <param name="source">The item to create a copy of</param>
    /// <returns>The copy</returns>
    object DeepCopy(object source);

    /// <summary>Tries to serialize an item.</summary>
    /// <param name="item">The instance of the object being serialized</param>
    /// <param name="writer">The writer used for serialization</param>
    /// <param name="expectedType">The type that the deserializer will expect</param>
    void Serialize(object item, BufferManagerOutputStream writer, Type expectedType);

    /// <summary>Tries to deserialize an item.</summary>
    /// <param name="reader">The reader used for binary deserialization</param>
    /// <param name="expectedType">The type that should be deserialzied</param>
    /// <returns>The deserialized object</returns>
    object Deserialize(Type expectedType, BufferManagerStreamReader reader);

    #region -- Read --

    /// <summary>Returns an object of the given <paramref name="type"/> from the given <paramref name="readStream"/></summary>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="readStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="type">The type of the object to deserialize.</param>
    /// <param name="readStream">The <see cref="BufferManagerStreamReader"/> to read.</param>
    /// <returns>an object of the given type.</returns>
    /// <exception cref="NotSupportedException">Derived types need to support reading.</exception>
    /// <seealso cref="CanReadType(Type)"/>
    object ReadFromStream(Type type, BufferManagerStreamReader readStream);

    /// <summary>Returns an object of the given <paramref name="type"/> from the given <paramref name="readStream"/></summary>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="readStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="type">The type of the object to deserialize.</param>
    /// <param name="readStream">The <see cref="BufferManagerStreamReader"/> to read.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when reading.</param>
    /// <returns>An object of the given type.</returns>
    /// <exception cref="NotSupportedException">Derived types need to support reading.</exception>
    /// <seealso cref="CanReadType(Type)"/>
    object ReadFromStream(Type type, BufferManagerStreamReader readStream, Encoding effectiveEncoding);

    /// <summary>Returns a <see cref="Task"/> to deserialize an object of the given type from the given <paramref name="readStream"/></summary>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="readStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="readStream">The <see cref="BufferManagerStreamReader"/> to read.</param>
    /// <returns>An object of the given type.</returns>
    /// <exception cref="NotSupportedException">Derived types need to support reading.</exception>
    /// <seealso cref="CanReadType(Type)"/>
    T ReadFromStream<T>(BufferManagerStreamReader readStream);

    /// <summary>Returns a <see cref="Task"/> to deserialize an object of the given type from the given <paramref name="readStream"/></summary>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="readStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="readStream">The <see cref="BufferManagerStreamReader"/> to read.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when reading.</param>
    /// <returns>An object of the given type.</returns>
    /// <exception cref="NotSupportedException">Derived types need to support reading.</exception>
    /// <seealso cref="CanReadType(Type)"/>
    T ReadFromStream<T>(BufferManagerStreamReader readStream, Encoding effectiveEncoding);

    /// <summary>Returns a <see cref="Task"/> to deserialize an object of the given <paramref name="type"/> from the given <paramref name="readStream"/></summary>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="readStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="type">The type of the object to deserialize.</param>
    /// <param name="readStream">The <see cref="BufferManagerStreamReader"/> to read.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when reading.</param>
    /// <returns>A <see cref="Task"/> whose result will be an object of the given type.</returns>
    /// <exception cref="NotSupportedException">Derived types need to support reading.</exception>
    /// <seealso cref="CanReadType(Type)"/>
    Task<object> ReadFromStreamAsync(Type type, BufferManagerStreamReader readStream, Encoding effectiveEncoding);

    /// <summary>Returns a <see cref="Task"/> to deserialize an object of the given <paramref name="type"/> from the given <paramref name="readStream"/></summary>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="readStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="type">The type of the object to deserialize.</param>
    /// <param name="readStream">The <see cref="BufferManagerStreamReader"/> to read.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when reading.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> whose result will be an object of the given type.</returns>
    /// <seealso cref="CanReadType(Type)"/>
    Task<object> ReadFromStreamAsync(Type type, BufferManagerStreamReader readStream, Encoding effectiveEncoding, CancellationToken cancellationToken);

    /// <summary>Returns a <see cref="Task"/> to deserialize an object of the given type from the given <paramref name="readStream"/></summary>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="readStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="readStream">The <see cref="BufferManagerStreamReader"/> to read.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when reading.</param>
    /// <returns>A <see cref="Task"/> whose result will be an object of the given type.</returns>
    /// <exception cref="NotSupportedException">Derived types need to support reading.</exception>
    /// <seealso cref="CanReadType(Type)"/>
    Task<T> ReadFromStreamAsync<T>(BufferManagerStreamReader readStream, Encoding effectiveEncoding);

    /// <summary>Returns a <see cref="Task"/> to deserialize an object of the given type from the given <paramref name="readStream"/></summary>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="readStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="readStream">The <see cref="BufferManagerStreamReader"/> to read.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when reading.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> whose result will be an object of the given type.</returns>
    /// <seealso cref="CanReadType(Type)"/>
    Task<T> ReadFromStreamAsync<T>(BufferManagerStreamReader readStream, Encoding effectiveEncoding, CancellationToken cancellationToken);

    #endregion

    #region -- Write --

    /// <summary>Tries to serializes the given <paramref name="value"/> of the given <paramref name="type"/>
    /// to the given <paramref name="writeStream"/>.</summary>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="writeStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="type">The type of the object to write.</param>
    /// <param name="value">The object value to write.  It may be <c>null</c>.</param>
    /// <param name="writeStream">The <see cref="BufferManagerOutputStream"/> to which to write.</param>
    /// <exception cref="NotSupportedException">Derived types need to support writing.</exception>
    /// <seealso cref="CanWriteType(Type)"/>
    void WriteToStream(Type type, Object value, BufferManagerOutputStream writeStream);

    /// <summary>Tries to serializes the given <paramref name="value"/> of the given <paramref name="type"/>
    /// to the given <paramref name="writeStream"/>.</summary>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="writeStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="type">The type of the object to write.</param>
    /// <param name="value">The object value to write.  It may be <c>null</c>.</param>
    /// <param name="writeStream">The <see cref="BufferManagerOutputStream"/> to which to write.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when writing.</param>
    /// <exception cref="NotSupportedException">Derived types need to support writing.</exception>
    /// <seealso cref="CanWriteType(Type)"/>
    void WriteToStream(Type type, Object value, BufferManagerOutputStream writeStream, Encoding effectiveEncoding);

    /// <summary>Tries to serializes the given <paramref name="value"/> of the given type
    /// to the given <paramref name="writeStream"/>.</summary>
    /// <typeparam name="T">The type of the object to write.</typeparam>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="writeStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="value">The object value to write.  It may be <c>null</c>.</param>
    /// <param name="writeStream">The <see cref="BufferManagerOutputStream"/> to which to write.</param>
    /// <exception cref="NotSupportedException">Derived types need to support writing.</exception>
    /// <seealso cref="CanWriteType(Type)"/>
    void WriteToStream<T>(T value, BufferManagerOutputStream writeStream);

    /// <summary>Tries to serializes the given <paramref name="value"/> of the given type
    /// to the given <paramref name="writeStream"/>.</summary>
    /// <typeparam name="T">The type of the object to write.</typeparam>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="writeStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="value">The object value to write.  It may be <c>null</c>.</param>
    /// <param name="writeStream">The <see cref="BufferManagerOutputStream"/> to which to write.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when writing.</param>
    /// <exception cref="NotSupportedException">Derived types need to support writing.</exception>
    /// <seealso cref="CanWriteType(Type)"/>
    void WriteToStream<T>(T value, BufferManagerOutputStream writeStream, Encoding effectiveEncoding);

    /// <summary>Returns a <see cref="Task"/> that serializes the given <paramref name="value"/> of the given <paramref name="type"/>
    /// to the given <paramref name="writeStream"/>.</summary>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="writeStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="type">The type of the object to write.</param>
    /// <param name="value">The object value to write.  It may be <c>null</c>.</param>
    /// <param name="writeStream">The <see cref="BufferManagerOutputStream"/> to which to write.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when writing.</param>
    /// <returns>A <see cref="Task"/> that will perform the write.</returns>
    /// <exception cref="NotSupportedException">Derived types need to support writing.</exception>
    /// <seealso cref="CanWriteType(Type)"/>
    Task WriteToStreamAsync(Type type, Object value, BufferManagerOutputStream writeStream, Encoding effectiveEncoding);

    /// <summary>Returns a <see cref="Task"/> that serializes the given <paramref name="value"/> of the given <paramref name="type"/>
    /// to the given <paramref name="writeStream"/>.</summary>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="writeStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="type">The type of the object to write.</param>
    /// <param name="value">The object value to write.  It may be <c>null</c>.</param>
    /// <param name="writeStream">The <see cref="BufferManagerOutputStream"/> to which to write.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when writing.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> that will perform the write.</returns>
    /// <seealso cref="CanWriteType(Type)"/>
    Task WriteToStreamAsync(Type type, Object value, BufferManagerOutputStream writeStream, Encoding effectiveEncoding, CancellationToken cancellationToken);

    /// <summary>Returns a <see cref="Task"/> that serializes the given <paramref name="value"/> of the given type
    /// to the given <paramref name="writeStream"/>.</summary>
    /// <typeparam name="T">The type of the object to write.</typeparam>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="writeStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="value">The object value to write.  It may be <c>null</c>.</param>
    /// <param name="writeStream">The <see cref="BufferManagerOutputStream"/> to which to write.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when writing.</param>
    /// <returns>A <see cref="Task"/> that will perform the write.</returns>
    /// <exception cref="NotSupportedException">Derived types need to support writing.</exception>
    /// <seealso cref="CanWriteType(Type)"/>
    Task WriteToStreamAsync<T>(T value, BufferManagerOutputStream writeStream, Encoding effectiveEncoding);

    /// <summary>Returns a <see cref="Task"/> that serializes the given <paramref name="value"/> of the given type
    /// to the given <paramref name="writeStream"/>.</summary>
    /// <typeparam name="T">The type of the object to write.</typeparam>
    /// <remarks>
    /// <para>This implementation throws a <see cref="NotSupportedException"/>. Derived types should override this method if the formatter
    /// supports reading.</para>
    /// <para>An implementation of this method should NOT close <paramref name="writeStream"/> upon completion.</para>
    /// </remarks>
    /// <param name="value">The object value to write.  It may be <c>null</c>.</param>
    /// <param name="writeStream">The <see cref="BufferManagerOutputStream"/> to which to write.</param>
    /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when writing.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> that will perform the write.</returns>
    /// <seealso cref="CanWriteType(Type)"/>
    Task WriteToStreamAsync<T>(T value, BufferManagerOutputStream writeStream, Encoding effectiveEncoding, CancellationToken cancellationToken);

    #endregion
  }
}