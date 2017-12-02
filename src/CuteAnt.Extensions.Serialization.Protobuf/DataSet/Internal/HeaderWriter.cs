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

namespace CuteAnt.Extensions.Serialization.Protobuf.Internal
{
	using System;
	using System.Collections.Generic;
	using ProtoBuf;

	internal sealed class HeaderWriter
	{
		private readonly ProtoWriter m_writer;

		public HeaderWriter(ProtoWriter writer)
		{
			if (writer == null) { throw new ArgumentNullException("writer"); }

			m_writer = writer;
		}

		public void WriteHeader(IEnumerable<ProtoDataColumn> columns)
		{
			if (columns == null)
			{
				throw new ArgumentNullException("columns");
			}

			foreach (ProtoDataColumn column in columns)
			{
				// for each, write the name and data type
				ProtoWriter.WriteFieldHeader(2, WireType.StartGroup, m_writer);
				SubItemToken columnToken = ProtoWriter.StartSubItem(column, m_writer);
				ProtoWriter.WriteFieldHeader(1, WireType.String, m_writer);
				ProtoWriter.WriteString(column.ColumnName, m_writer);
				ProtoWriter.WriteFieldHeader(2, WireType.Variant, m_writer);
				ProtoWriter.WriteInt32((int)column.ProtoDataType, m_writer);
				ProtoWriter.EndSubItem(columnToken, m_writer);
			}
		}
	}
}