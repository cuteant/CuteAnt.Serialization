﻿// Copyright 2012 Richard Dingwall - http://richarddingwall.name
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace CuteAnt.Extensions.Serialization.Protobuf
{
	using System.Data;
	using System.IO;

	/// <summary>Serializes an <see cref="System.Data.IDataReader"/> to a binary stream.</summary>
	public interface IProtoDataWriter
	{
		/// <summary>Serialize an <see cref="System.Data.IDataReader"/> to a binary stream using protocol-buffers.</summary>
		/// <param name="stream">The <see cref="System.IO.Stream"/> to write to.</param>
		/// <param name="reader">The <see cref="System.Data.IDataReader"/>who's contents to serialize.</param>
		void Serialize(Stream stream, IDataReader reader);

		/// <summary>Serialize an <see cref="System.Data.IDataReader"/> to a binary stream using protocol-buffers.</summary>
		/// <param name="stream">The <see cref="System.IO.Stream"/> to write to.</param>
		/// <param name="reader">The <see cref="System.Data.IDataReader"/> who's contents to serialize.</param>
		/// <param name="options"><see cref="ProtoDataWriterOptions"/> specifying any custom serialization options.</param>
		void Serialize(Stream stream, IDataReader reader, ProtoDataWriterOptions options);
	}
}