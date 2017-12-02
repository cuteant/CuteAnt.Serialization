﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using Newtonsoft.Json;
#if NET40
using System.Reflection;
#endif

namespace CuteAnt.Extensions.Serialization
{
  /// <summary><see cref="MessageFormatter"/> class to handle Json.</summary>
  public class JsonMessageSerializer : BaseJsonMessageSerializer
  {
    #region @@ Fields @@

    /// <summary>The default singlegton instance</summary>
    public static readonly JsonMessageSerializer DefaultInstance = new JsonMessageSerializer();

    #endregion

    #region @@ Constructors @@

    /// <summary>Initializes a new instance of the <see cref="JsonMessageSerializer"/> class.</summary>
    public JsonMessageSerializer()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="JsonMessageSerializer"/> class.</summary>
    /// <param name="formatter">The <see cref="JsonMessageSerializer"/> instance to copy settings from.</param>
    protected JsonMessageSerializer(JsonMessageSerializer formatter)
      : base(formatter)
    {
      Contract.Assert(formatter != null);

      JsonFormatting = formatter.JsonFormatting;
    }

    #endregion

    #region -- CreateJsonReader --

    /// <inheritdoc />
    public override JsonReader CreateJsonReader(Type type, Stream readStream, Encoding effectiveEncoding, IArrayPool<char> charPool)
    {
      if (readStream == null) { throw new ArgumentNullException(nameof(readStream)); }
      if (effectiveEncoding == null) { throw new ArgumentNullException(nameof(effectiveEncoding)); }

      return new JsonTextReader(new StreamReaderX(readStream, effectiveEncoding)) { ArrayPool = charPool };
    }

    #endregion

    #region -- CreateJsonWriter --

    /// <inheritdoc />
    public override JsonWriter CreateJsonWriter(Type type, Stream writeStream, Encoding effectiveEncoding, IArrayPool<char> charPool)
    {
      if (type == null) { throw new ArgumentNullException(nameof(type)); }
      if (writeStream == null) { throw new ArgumentNullException(nameof(writeStream)); }
      if (effectiveEncoding == null) { throw new ArgumentNullException(nameof(effectiveEncoding)); }

      var jsonWriter = new JsonTextWriter(new StreamWriterX(writeStream, effectiveEncoding)) { ArrayPool = charPool };
      if (Formatting.Indented == JsonFormatting.GetValueOrDefault())
      {
        jsonWriter.Formatting = Formatting.Indented;
      }

      return jsonWriter;
    }

    #endregion
  }
}
